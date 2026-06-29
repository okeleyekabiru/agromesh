import 'package:flutter/foundation.dart';
import 'package:uuid/uuid.dart';
import '../models/models.dart';
import '../services/database_service.dart';
import '../services/sync_service.dart';

class AppStore extends ChangeNotifier {
  static const _farmId = 'farm_001';
  static const _uuid = Uuid();

  // ── STATE ──
  List<Section> sections = [];
  List<Block>   blocks   = [];
  List<Worker>  workers  = [];
  List<ActivityLog> activities = [];

  bool isOnline = true;
  bool isSyncing = false;
  int  pendingSyncCount = 0;
  String lastSynced = '--:-- --';

  bool isLoading = false;
  String? error;

  // Log activity form state
  Section? selectedSection;
  Block?   selectedBlock;

  // ── INIT ──
  Future<void> init() async {
    isLoading = true;
    notifyListeners();

    try {
      await _seedIfEmpty();
      await _loadAll();
      await _checkSync();

      // Listen for connectivity changes
      SyncService.connectivityStream.listen((online) async {
        isOnline = online;
        notifyListeners();
        if (online && pendingSyncCount > 0) {
          await syncNow();
        }
      });
    } catch (e) {
      error = e.toString();
    }

    isLoading = false;
    notifyListeners();
  }

  Future<void> _loadAll() async {
    sections   = await DatabaseService.getSections(_farmId);
    workers    = await DatabaseService.getWorkers(_farmId);
    activities = await DatabaseService.getActivities(_farmId);
    pendingSyncCount = await DatabaseService.getPendingSyncCount(_farmId);

    // Load blocks for all sections
    blocks = [];
    for (final s in sections) {
      final b = await DatabaseService.getBlocks(s.id);
      blocks.addAll(b);
    }
  }

  Future<void> _checkSync() async {
    isOnline = await SyncService.isOnline();
    pendingSyncCount = await DatabaseService.getPendingSyncCount(_farmId);
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
      farmId: _farmId,
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

    final result = await SyncService.syncPending();

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
    final h = dt.hour.toString().padLeft(2, '0');
    final m = dt.minute.toString().padLeft(2, '0');
    final ampm = dt.hour < 12 ? 'AM' : 'PM';
    final hour = dt.hour > 12 ? dt.hour - 12 : dt.hour;
    return '$hour:$m $ampm';
  }

  // ── SEED DATA (development only) ──
  Future<void> _seedIfEmpty() async {
    final existing = await DatabaseService.getSections(_farmId);
    if (existing.isNotEmpty) return;

    // Seed sections
    final seedSections = [
      const Section(id:'s1', farmId:_farmId, name:'Section A', crop:CropType.oilPalm, hectares:18, status:SectionStatus.active, lastActivityDate:'Today', blockCount:4),
      const Section(id:'s2', farmId:_farmId, name:'Section B', crop:CropType.oilPalm, hectares:16, status:SectionStatus.active, lastActivityDate:'Today', blockCount:4),
      const Section(id:'s3', farmId:_farmId, name:'Section C', crop:CropType.maize,   hectares:14, status:SectionStatus.harvestSoon, lastActivityDate:'Yesterday', blockCount:3),
      const Section(id:'s4', farmId:_farmId, name:'Section D', crop:CropType.mixed,   hectares:20, status:SectionStatus.idle, lastActivityDate:'3 days ago', blockCount:5),
      const Section(id:'s5', farmId:_farmId, name:'Section E', crop:CropType.oilPalm, hectares:16, status:SectionStatus.active, lastActivityDate:'Today', blockCount:4),
      const Section(id:'s6', farmId:_farmId, name:'Section F', crop:CropType.maize,   hectares:16, status:SectionStatus.idle, lastActivityDate:'5 days ago', blockCount:4),
    ];

    for (final s in seedSections) {
      await DatabaseService.upsertSection(s);
    }

    // Seed blocks for each section
    for (final s in seedSections) {
      for (int i = 0; i < s.blockCount; i++) {
        final block = Block(
          id: '${s.id}_b$i',
          sectionId: s.id,
          name: 'Block ${String.fromCharCode(65 + i)}',
          hectares: (s.hectares / s.blockCount).roundToDouble(),
        );
        await DatabaseService.upsertSection(s); // sections already inserted
        // Insert block directly
        final db = await DatabaseService.database;
        await db.insert('blocks', {
          'id': block.id, 'section_id': block.sectionId,
          'name': block.name, 'hectares': block.hectares,
        }, conflictAlgorithm: ConflictAlgorithm.ignore);
      }
    }

    // Seed workers
    final seedWorkers = [
      const Worker(id:'w1', farmId:_farmId, name:'Ibrahim Musa',  initials:'IM', role:'Field Supervisor',     type:WorkerType.employee,   active:true, dailyRate:3500),
      const Worker(id:'w2', farmId:_farmId, name:'Tunde Adeyemi', initials:'TA', role:'Assistant Supervisor', type:WorkerType.employee,   active:true, dailyRate:2800),
      const Worker(id:'w3', farmId:_farmId, name:'Emeka Johnson', initials:'EJ', role:'Farm Worker',          type:WorkerType.employee,   active:true, dailyRate:2200),
      const Worker(id:'w4', farmId:_farmId, name:'Fatima Bello',  initials:'FB', role:'Farm Worker',          type:WorkerType.employee,   active:true, dailyRate:2200),
      const Worker(id:'w5', farmId:_farmId, name:'Chukwu Obi',    initials:'CO', role:'Contractor',           type:WorkerType.contractor, active:true, dailyRate:4000),
    ];

    for (final w in seedWorkers) {
      await DatabaseService.upsertWorker(w);
    }
  }
}

// Needed for sqflite ConflictAlgorithm in seed
import 'package:sqflite/sqflite.dart';
