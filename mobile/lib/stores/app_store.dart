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
  List<Section> sections = [];
  List<Block>   blocks   = [];
  List<Worker>  workers  = [];
  List<ActivityLog> activities = [];

  bool isOnline = true;
  bool isSyncing = false;
  int  pendingSyncCount = 0;
  String lastSynced = '--:-- --';
  String? sectionsError;

  bool isLoading = false;
  String? error;

  // Log activity form state
  Section? selectedSection;
  Block?   selectedBlock;

  // ── INIT ── (called once per active-farm selection, from AppGate)
  Future<void> init(String farmId, {String? farmName, String? farmLocation}) async {
    this.farmId = farmId;
    if (farmName != null) this.farmName = farmName;
    if (farmLocation != null) this.farmLocation = farmLocation;

    isLoading = true;
    notifyListeners();

    try {
      await _seedWorkersIfEmpty();
      await refreshFarmDetails();
      await refreshSections();
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
    sections   = await DatabaseService.getSections(farmId);
    workers    = await DatabaseService.getWorkers(farmId);
    activities = await DatabaseService.getActivities(farmId);

    final pendingActivities = await DatabaseService.getPendingSyncCount(farmId);
    final pendingBoundaries = await DatabaseService.getPendingBoundaryCount(farmId);
    pendingSyncCount = pendingActivities + pendingBoundaries;

    // Load blocks for all sections
    blocks = [];
    for (final s in sections) {
      final b = await DatabaseService.getBlocks(s.id);
      blocks.addAll(b);
    }
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

  // ── SECTIONS ──

  /// Pulls the farm's sections from the real backend and caches them locally
  /// so the Sections tab works offline too. On failure, just falls back to
  /// whatever is already cached (offline-first read).
  Future<void> refreshSections() async {
    sectionsError = null;
    try {
      final json = await ApiClient.get('/api/farms/$farmId/sections');
      final list = (json as List).cast<Map<String, dynamic>>();

      for (final j in list) {
        final section = Section.fromApiJson(j);
        await DatabaseService.upsertSection(section);

        final blocksJson = (j['blocks'] as List?) ?? const [];
        if (blocksJson.isNotEmpty) {
          for (final b in blocksJson) {
            await DatabaseService.upsertBlockFromApi(b as Map<String, dynamic>);
          }
        } else {
          await DatabaseService.ensureDefaultBlock(section.id, section.hectares);
        }
      }
    } catch (_) {
      sectionsError = isOnline
          ? 'Could not refresh sections from the server.'
          : 'Offline — showing saved sections.';
    }

    await _loadAll();
    notifyListeners();
  }

  /// Adds a section by name only (the lightweight mobile "quick add" flow —
  /// full farm/company/section setup lives on the web app). Requires
  /// connectivity: unlike activity logs and boundary points, this is not
  /// queued for later sync.
  Future<void> addSection(String name) async {
    if (!isOnline) {
      throw Exception('You need an internet connection to add a section.');
    }
    await ApiClient.post('/api/farms/$farmId/sections', {
      'name': name,
      'crops': [
        {'cropType': CropType.mixed.index, 'percentage': 100},
      ],
      'areaValue': 0,
    });
    await refreshSections();
  }

  // ── BOUNDARY CAPTURE ──

  /// Writes a completed GPS corner-capture session to the local outbox. This
  /// must succeed fully offline — sync happens best-effort afterwards (or on
  /// the next reconnect) via [SyncService].
  Future<void> finishBoundaryCapture(String sectionId, List<GeoPoint> points) async {
    final submission = BoundarySubmission(
      id: _uuid.v4(),
      sectionId: sectionId,
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
    required String sectionId,
    required String sectionName,
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
      sectionId: sectionId,
      sectionName: sectionName,
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

    if (result.synced > 0) {
      lastSynced = _formatTime(DateTime.now());
      await _loadAll();
    }

    isSyncing = false;
    notifyListeners();
  }

  // ── HELPERS ──
  List<Worker> get supervisors => workers.where((w) => w.role.toLowerCase().contains('supervisor')).toList();

  List<Block> blocksForSection(String sectionId) => blocks.where((b) => b.sectionId == sectionId).toList();

  Section? sectionById(String id) {
    try { return sections.firstWhere((s) => s.id == id); }
    catch (_) { return null; }
  }

  String _formatTime(DateTime dt) {
    final m = dt.minute.toString().padLeft(2, '0');
    final ampm = dt.hour < 12 ? 'AM' : 'PM';
    final hour = dt.hour > 12 ? dt.hour - 12 : dt.hour;
    return '$hour:$m $ampm';
  }

  // ── SEED DATA (development only — workers have no backend integration yet) ──
  Future<void> _seedWorkersIfEmpty() async {
    final existing = await DatabaseService.getWorkers(farmId);
    if (existing.isNotEmpty) return;

    final seedWorkers = [
      Worker(id: 'w1', farmId: farmId, name: 'Ibrahim Musa',  initials: 'IM', role: 'Field Supervisor',     type: WorkerType.employee,   active: true, dailyRate: 3500),
      Worker(id: 'w2', farmId: farmId, name: 'Tunde Adeyemi', initials: 'TA', role: 'Assistant Supervisor', type: WorkerType.employee,   active: true, dailyRate: 2800),
      Worker(id: 'w3', farmId: farmId, name: 'Emeka Johnson', initials: 'EJ', role: 'Farm Worker',          type: WorkerType.employee,   active: true, dailyRate: 2200),
      Worker(id: 'w4', farmId: farmId, name: 'Fatima Bello',  initials: 'FB', role: 'Farm Worker',          type: WorkerType.employee,   active: true, dailyRate: 2200),
      Worker(id: 'w5', farmId: farmId, name: 'Chukwu Obi',    initials: 'CO', role: 'Contractor',           type: WorkerType.contractor, active: true, dailyRate: 4000),
    ];

    for (final w in seedWorkers) {
      await DatabaseService.upsertWorker(w);
    }
  }
}
