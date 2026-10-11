import 'dart:async';
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:uuid/uuid.dart';
import '../models/models.dart';
import '../services/api_client.dart';
import '../services/database_service.dart';
import '../services/sync_service.dart';

class AppStore extends ChangeNotifier {
  static const _uuid = Uuid();

  // ── SESSION-SCOPED STATE (set by AppGate once a farm is chosen) ──
  String farmId = '';
  String farmName = '';
  String farmLocation = '';
  bool _connectivitySubscribed = false;

  // ── STATE ──
  List<Block> blocks = [];
  List<Worker>  workers  = [];
  List<ActivityLog> activities = [];

  // Stock — cached from the server so inputs can be chosen (and checked) offline.
  List<StockItem> stockItems = [];
  List<OutstandingStock> outstanding = [];

  // Weather forecast for the farm — cached so the last one still shows offline.
  WeatherInfo? weather;

  // Next weeding / spraying per block (by block id), worked out on the server from the weather.
  Map<String, BlockWork> blockWork = {};

  bool isOnline = true;
  bool isSyncing = false;
  int  pendingSyncCount = 0;
  String lastSynced = '--:-- --';
  String? blocksError;

  bool isLoading = false;
  String? error;

  // Log activity form state
  Block? selectedBlock;

  // ── INIT ── (called once per active-farm selection, from AppGate)
  Future<void> init(String farmId, {String? farmName, String? farmLocation}) async {
    this.farmId = farmId;
    if (farmName != null) this.farmName = farmName;
    if (farmLocation != null) this.farmLocation = farmLocation;

    isLoading = true;
    notifyListeners();

    try {
      await refreshWorkers();
      await refreshFarmDetails();
      await refreshBlocks();
      await refreshStock();
      await refreshActivities();
      await refreshWeather();
      await _checkSync();

      if (!_connectivitySubscribed) {
        _connectivitySubscribed = true;
        SyncService.connectivityStream.listen((online) async {
          isOnline = online;
          notifyListeners();
          if (online && pendingSyncCount > 0) {
            await syncNow();
          }
        });
      }
    } catch (e) {
      error = e.toString();
    }

    isLoading = false;
    notifyListeners();
  }

  Future<void> _loadAll() async {
    blocks   = await DatabaseService.getBlocks(farmId);
    workers    = await DatabaseService.getWorkers(farmId);
    activities = await DatabaseService.getActivities(farmId);
    stockItems = (await DatabaseService.readCacheList('stock:$farmId')).map(StockItem.fromApiJson).toList();
    outstanding = (await DatabaseService.readCacheList('outstanding:$farmId')).map(OutstandingStock.fromApiJson).toList();
    if (blockWork.isEmpty) {
      blockWork = {
        for (final j in await DatabaseService.readCacheList('blockwork:$farmId'))
          j['blockId'] as String: BlockWork.fromJson(j),
      };
    }

    final pendingActivities = await DatabaseService.getPendingSyncCount(farmId);
    final pendingBoundaries = await DatabaseService.getPendingBoundaryCount(farmId);
    pendingSyncCount = pendingActivities + pendingBoundaries;
  }

  Future<void> _checkSync() async {
    isOnline = await SyncService.isOnline();
    await _loadAll();
  }

  // ── FARM DETAILS ──
  // Login only returns a thin {id, name, setupStatus} farm summary. Backfill
  // the rest (location, hectares, etc.) from the real farms list when online;
  // silently keep whatever we already have (from the login summary or a
  // previous fetch) when offline.
  Future<void> refreshFarmDetails() async {
    try {
      final json = await ApiClient.get('/api/farms');
      final list = (json as List).cast<Map<String, dynamic>>();
      final match = list.where((f) => f['id'] == farmId).toList();
      if (match.isNotEmpty) {
        final farm = Farm.fromJson(match.first);
        farmName = farm.name;
        farmLocation = farm.location;
      }
    } catch (_) {
      // Offline or server error — non-fatal, keep existing farmName/location.
    }
  }

  // ── BLOCKS ──

  /// Pulls the farm's blocks from the real backend and caches them locally
  /// so the Blocks tab works offline too. On failure, just falls back to
  /// whatever is already cached (offline-first read).
  /// Says why a load failed, so "can't refresh" is something the user (or we) can act on.
  String _describeLoadError(Object e, String what) {
    if (e is ApiException) {
      if (e.statusCode == 401) return 'Your session has expired. Sign out and sign in again to refresh $what.';
      return 'The server refused the request for $what: ${e.message}';
    }
    if (e is TimeoutException || e is SocketException || e.runtimeType.toString().contains('ClientException')) {
      return 'Cannot reach the server at ${ApiClient.baseUrl}. Check that the API is running and, on a phone or emulator, that this address is reachable from it.';
    }
    return 'Could not read the $what from the server ($e).';
  }

  Future<void> refreshBlocks() async {
    blocksError = null;
    try {
      final json = await ApiClient.get('/api/farms/$farmId/blocks');
      final list = (json as List).cast<Map<String, dynamic>>();

      final work = <String, BlockWork>{};
      for (final j in list) {
        final block = Block.fromApiJson(j);
        await DatabaseService.upsertBlock(block);
        work[block.id] = BlockWork.fromApiJson(j);
      }
      blockWork = work;
      await DatabaseService.saveCache('blockwork:$farmId',
          work.entries.map((e) => {'blockId': e.key, ...e.value.toJson()}).toList());
    } catch (e) {
      debugPrint('refreshBlocks failed: $e');
      blocksError = isOnline ? _describeLoadError(e, 'blocks') : 'Offline — showing saved blocks.';
    }

    await _loadAll();
    notifyListeners();
  }

  /// Adds a block by name only (the lightweight mobile "quick add" flow —
  /// full farm/company/block setup lives on the web app). Requires
  /// connectivity: unlike activity logs and boundary points, this is not
  /// queued for later sync.
  Future<void> addBlock(
    String name, {
    required List<BlockCrop> crops,
    double areaValue = 0,
    int? plantingYear,
  }) async {
    if (!isOnline) {
      throw Exception('You need an internet connection to add a block.');
    }
    await ApiClient.post('/api/farms/$farmId/blocks', {
      'name': name,
      'crops': [
        for (final c in crops) {'cropType': c.cropType.index, 'percentage': c.percentage},
      ],
      'areaValue': areaValue,
      if (plantingYear != null) 'plantingYear': plantingYear,
    });
    await refreshBlocks();
  }

  // ── BOUNDARY CAPTURE ──

  /// Writes a completed GPS corner-capture session to the local outbox. This
  /// must succeed fully offline — sync happens best-effort afterwards (or on
  /// the next reconnect) via [SyncService].
  Future<void> finishBoundaryCapture(String blockId, List<GeoPoint> points) async {
    final submission = BoundarySubmission(
      id: _uuid.v4(),
      blockId: blockId,
      farmId: farmId,
      points: points,
      createdAt: DateTime.now(),
    );
    await DatabaseService.insertBoundarySubmission(submission);
    await _loadAll();
    notifyListeners();

    if (isOnline) {
      await syncNow();
    }
  }

  // ── LOG ACTIVITY ──
  Future<void> logActivity({
    required String blockId,
    required String blockName,
    required ActivityType activityType,
    required List<WorkerLog> workers,
    required List<InputUsed> inputs,
    required String supervisorId,
    required String supervisorName,
    String notes = '',
    DateTime? date,
  }) async {
    final activity = ActivityLog(
      id: _uuid.v4(),
      farmId: farmId,
      blockId: blockId,
      blockName: blockName,
      activityType: activityType,
      date: date ?? DateTime.now(),
      workers: workers,
      inputs: inputs,
      notes: notes,
      supervisorId: supervisorId,
      supervisorName: supervisorName,
      syncStatus: SyncStatus.pending,
      createdAt: DateTime.now(),
    );

    // Save offline-first — always to SQLite first
    await DatabaseService.insertActivity(activity);
    await _applyLocalUsage(blockId, inputs);

    // Reload
    await _loadAll();
    notifyListeners();

    // Try to sync immediately if online
    if (isOnline) {
      await syncNow();
    }
  }

  // ── SYNC ──
  Future<void> syncNow() async {
    if (isSyncing) return;
    isSyncing = true;
    notifyListeners();

    final result = await SyncService.syncPending(farmId);
    await refreshWeather();
    await refreshBlocks(); // picks up recalculated (and AI-reviewed) weeding / spraying dates

    if (result.synced > 0) {
      lastSynced = _formatTime(DateTime.now());
      await refreshStock(); // the server is the source of truth for stock once usage is applied
      await refreshActivities();
      // A weeding or spraying changes when the next one is due: pull the recalculated dates.
      await refreshBlocks();
      await _loadAll();
    }

    isSyncing = false;
    notifyListeners();
  }

  // ── HELPERS ──
  List<Worker> get supervisors => workers.where((w) => w.role.toLowerCase().contains('supervisor')).toList();

  Block? blockById(String id) {
    try { return blocks.firstWhere((s) => s.id == id); }
    catch (_) { return null; }
  }

  String _formatTime(DateTime dt) {
    final m = dt.minute.toString().padLeft(2, '0');
    final ampm = dt.hour < 12 ? 'AM' : 'PM';
    final hour = dt.hour > 12 ? dt.hour - 12 : dt.hour;
    return '$hour:$m $ampm';
  }

  // ── WEATHER (from the server) ──

  /// Pulls the farm's forecast. Offline (or while the server can't resolve the farm's location) the
  /// last cached forecast is shown instead.
  Future<void> refreshWeather() async {
    try {
      final json = await ApiClient.get('/api/farms/$farmId/weather');
      final info = WeatherInfo.fromApiJson(json as Map<String, dynamic>);
      await DatabaseService.saveCache('weather:$farmId', [info.toJson()]);
      weather = info;
    } catch (_) {
      final cached = await DatabaseService.readCacheList('weather:$farmId');
      if (weather == null && cached.isNotEmpty) weather = WeatherInfo.fromApiJson(cached.first);
    }
    notifyListeners();
  }

  // ── ACTIVITIES (from the server) ──

  /// Pulls the farm's recent activities so work logged on the web (or another phone) shows here
  /// too. Offline, the cached list stays. Local pending activities are never touched.
  Future<void> refreshActivities() async {
    try {
      final json = await ApiClient.get('/api/farms/$farmId/activities?page=1&pageSize=50');
      final items = ((json as Map<String, dynamic>)['items'] as List).cast<Map<String, dynamic>>();
      final blockNames = {for (final b in await DatabaseService.getBlocks(farmId)) b.id: b.name};
      final workerNames = {for (final w in await DatabaseService.getWorkers(farmId)) w.id: w.name};
      final logs = items.map((j) => ActivityLog.fromApiJson(
            j,
            blockName: blockNames[j['blockId']] ?? 'Unknown block',
            supervisorName: workerNames[j['supervisorId']] ?? '',
          )).toList();
      await DatabaseService.replaceSyncedActivities(farmId, logs);
      await _loadAll();
      notifyListeners();
    } catch (_) {
      // Offline or server error — keep whatever is cached.
    }
  }

  // ── WORKERS (from the server) ──

  /// Pulls the farm's real workers and caches them. Activities reference workers by id, so the
  /// old locally-seeded demo workers could never sync. Offline, the last cached list is used.
  Future<void> refreshWorkers() async {
    try {
      final json = await ApiClient.get('/api/farms/$farmId/workers');
      final list = (json as List).cast<Map<String, dynamic>>();
      await DatabaseService.replaceWorkers(farmId, list.map(Worker.fromApiJson).toList());
    } catch (_) {
      // Offline or server error — keep whatever is cached.
    }
  }

  // ── STOCK ──

  /// Pulls the stock list and the "out on the farm" list and caches both for offline use.
  Future<void> refreshStock() async {
    try {
      final inv = await ApiClient.get('/api/farms/$farmId/inventory');
      final items = (inv as List).cast<Map<String, dynamic>>();
      await DatabaseService.saveCache('stock:$farmId', items.map((j) => StockItem.fromApiJson(j).toJson()).toList());
    } catch (_) {/* offline — keep the cached list */}
    try {
      final out = await ApiClient.get('/api/farms/$farmId/inventory/outstanding');
      final picks = (out as List).cast<Map<String, dynamic>>();
      await DatabaseService.saveCache('outstanding:$farmId', picks.map((j) => OutstandingStock.fromApiJson(j).toJson()).toList());
    } catch (_) {/* offline — keep the cached list */}
    stockItems = (await DatabaseService.readCacheList('stock:$farmId')).map(StockItem.fromApiJson).toList();
    outstanding = (await DatabaseService.readCacheList('outstanding:$farmId')).map(OutstandingStock.fromApiJson).toList();
    notifyListeners();
  }

  /// Stock already picked out for [blockId] and still unused.
  double pickedFor(String itemId, String blockId) => outstanding
      .where((o) => o.inventoryItemId == itemId && o.blockId == blockId)
      .fold(0.0, (sum, o) => sum + o.quantityOutstanding);

  /// What an activity on [blockId] can still use of an item: picked-out stock plus the store.
  double availableFor(String itemId, String blockId) {
    final matches = stockItems.where((s) => s.id == itemId);
    if (matches.isEmpty) return 0;
    return pickedFor(itemId, blockId) + matches.first.currentStock;
  }

  /// Mirrors the server's rule on the cached lists so offline logging stays consistent:
  /// picked-out stock is used first, then the store.
  Future<void> _applyLocalUsage(String blockId, List<InputUsed> inputs) async {
    for (final input in inputs) {
      var remaining = input.quantity;
      outstanding = outstanding.map((o) {
        if (remaining <= 0 || o.inventoryItemId != input.inventoryItemId || o.blockId != blockId) return o;
        final take = remaining < o.quantityOutstanding ? remaining : o.quantityOutstanding;
        remaining -= take;
        return o.copyWith(quantityOutstanding: o.quantityOutstanding - take);
      }).where((o) => o.quantityOutstanding > 0).toList();
      if (remaining > 0) {
        stockItems = stockItems.map((s) {
          if (s.id != input.inventoryItemId) return s;
          final left = s.currentStock - remaining;
          return s.copyWith(currentStock: left < 0 ? 0 : left);
        }).toList();
      }
    }
    await DatabaseService.saveCache('stock:$farmId', stockItems.map((s) => s.toJson()).toList());
    await DatabaseService.saveCache('outstanding:$farmId', outstanding.map((o) => o.toJson()).toList());
  }

  /// Hands leftover stock back to the store. Needs a connection (like adding a block): the server
  /// is the one that knows what is really still out. Throws [ApiException] with the reason.
  Future<void> returnStock({
    required String inventoryItemId,
    required String blockId,
    required double quantity,
    String? notes,
  }) async {
    if (!isOnline) {
      throw Exception('You need an internet connection to return stock.');
    }
    await ApiClient.post('/api/farms/$farmId/inventory/$inventoryItemId/return', {
      'quantity': quantity,
      'blockId': blockId,
      'notes': (notes == null || notes.trim().isEmpty) ? null : notes.trim(),
    });
    await refreshStock();
  }
}
