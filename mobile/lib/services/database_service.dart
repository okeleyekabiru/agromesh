import 'package:sqflite/sqflite.dart';
import 'package:path/path.dart';
import '../models/models.dart';

class DatabaseService {
  static Database? _db;
  static const _version = 1;
  static const _name = 'agromesh.db';

  static Future<Database> get database async {
    _db ??= await _initDb();
    return _db!;
  }

  static Future<Database> _initDb() async {
    final dbPath = await getDatabasesPath();
    final path = join(dbPath, _name);

    return openDatabase(
      path,
      version: _version,
      onCreate: _onCreate,
      onUpgrade: _onUpgrade,
    );
  }

  static Future<void> _onCreate(Database db, int version) async {
    await db.execute('''
      CREATE TABLE sections (
        id TEXT PRIMARY KEY,
        farm_id TEXT NOT NULL,
        name TEXT NOT NULL,
        crop INTEGER NOT NULL,
        hectares REAL NOT NULL,
        status INTEGER NOT NULL DEFAULT 0,
        last_activity_date TEXT,
        block_count INTEGER NOT NULL DEFAULT 0,
        created_at TEXT NOT NULL DEFAULT (datetime('now'))
      )
    ''');

    await db.execute('''
      CREATE TABLE blocks (
        id TEXT PRIMARY KEY,
        section_id TEXT NOT NULL,
        name TEXT NOT NULL,
        hectares REAL NOT NULL,
        tree_count INTEGER,
        FOREIGN KEY (section_id) REFERENCES sections(id)
      )
    ''');

    await db.execute('''
      CREATE TABLE workers (
        id TEXT PRIMARY KEY,
        farm_id TEXT NOT NULL,
        name TEXT NOT NULL,
        initials TEXT NOT NULL,
        role TEXT NOT NULL,
        type INTEGER NOT NULL DEFAULT 0,
        active INTEGER NOT NULL DEFAULT 1,
        daily_rate REAL NOT NULL DEFAULT 0
      )
    ''');

    await db.execute('''
      CREATE TABLE activity_logs (
        id TEXT PRIMARY KEY,
        farm_id TEXT NOT NULL,
        section_id TEXT NOT NULL,
        section_name TEXT NOT NULL,
        block_id TEXT NOT NULL,
        block_name TEXT NOT NULL,
        activity_type INTEGER NOT NULL,
        date TEXT NOT NULL,
        notes TEXT DEFAULT '',
        supervisor_id TEXT NOT NULL,
        supervisor_name TEXT NOT NULL,
        labour_cost REAL NOT NULL DEFAULT 0,
        input_cost REAL NOT NULL DEFAULT 0,
        sync_status INTEGER NOT NULL DEFAULT 1,
        created_at TEXT NOT NULL,
        synced_at TEXT
      )
    ''');

    await db.execute('''
      CREATE TABLE activity_workers (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        activity_id TEXT NOT NULL,
        worker_id TEXT NOT NULL,
        worker_name TEXT NOT NULL,
        worker_type INTEGER NOT NULL,
        hours_worked REAL NOT NULL DEFAULT 8,
        FOREIGN KEY (activity_id) REFERENCES activity_logs(id)
      )
    ''');

    await db.execute('''
      CREATE TABLE activity_inputs (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        activity_id TEXT NOT NULL,
        item TEXT NOT NULL,
        quantity REAL NOT NULL,
        unit TEXT NOT NULL,
        FOREIGN KEY (activity_id) REFERENCES activity_logs(id)
      )
    ''');

    // Indexes for common queries
    await db.execute('CREATE INDEX idx_activities_farm ON activity_logs(farm_id)');
    await db.execute('CREATE INDEX idx_activities_section ON activity_logs(section_id)');
    await db.execute('CREATE INDEX idx_activities_sync ON activity_logs(sync_status)');
    await db.execute('CREATE INDEX idx_activities_date ON activity_logs(date)');
  }

  static Future<void> _onUpgrade(Database db, int oldVersion, int newVersion) async {
    // Future migrations go here
  }

  // ── SECTIONS ──

  static Future<List<Section>> getSections(String farmId) async {
    final db = await database;
    final rows = await db.query('sections', where: 'farm_id = ?', whereArgs: [farmId]);
    return rows.map(Section.fromMap).toList();
  }

  static Future<void> upsertSection(Section section) async {
    final db = await database;
    await db.insert('sections', section.toMap(), conflictAlgorithm: ConflictAlgorithm.replace);
  }

  static Future<void> updateSectionStatus(String id, SectionStatus status, String lastActivity) async {
    final db = await database;
    await db.update(
      'sections',
      {'status': status.index, 'last_activity_date': lastActivity},
      where: 'id = ?', whereArgs: [id],
    );
  }

  // ── BLOCKS ──

  static Future<List<Block>> getBlocks(String sectionId) async {
    final db = await database;
    final rows = await db.query('blocks', where: 'section_id = ?', whereArgs: [sectionId]);
    return rows.map((m) => Block(
      id: m['id'] as String,
      sectionId: m['section_id'] as String,
      name: m['name'] as String,
      hectares: (m['hectares'] as num).toDouble(),
      treeCount: m['tree_count'] as int?,
    )).toList();
  }

  // ── WORKERS ──

  static Future<List<Worker>> getWorkers(String farmId) async {
    final db = await database;
    final rows = await db.query('workers',
      where: 'farm_id = ? AND active = 1', whereArgs: [farmId]);
    return rows.map(Worker.fromMap).toList();
  }

  static Future<void> upsertWorker(Worker worker) async {
    final db = await database;
    await db.insert('workers', worker.toMap(), conflictAlgorithm: ConflictAlgorithm.replace);
  }

  // ── ACTIVITY LOGS ──

  static Future<void> insertActivity(ActivityLog log) async {
    final db = await database;

    await db.transaction((txn) async {
      // Insert main log
      await txn.insert('activity_logs', log.toMap(), conflictAlgorithm: ConflictAlgorithm.replace);

      // Insert worker logs
      for (final w in log.workers) {
        await txn.insert('activity_workers', {
          'activity_id': log.id,
          ...w.toMap(),
        });
      }

      // Insert inputs
      for (final inp in log.inputs) {
        await txn.insert('activity_inputs', {
          'activity_id': log.id,
          ...inp.toMap(),
        });
      }

      // Update section last activity
      await txn.update(
        'sections',
        {'last_activity_date': 'Today', 'status': SectionStatus.active.index},
        where: 'id = ?', whereArgs: [log.sectionId],
      );
    });
  }

  static Future<List<ActivityLog>> getActivities(String farmId, {int limit = 50}) async {
    final db = await database;
    final rows = await db.query(
      'activity_logs',
      where: 'farm_id = ?', whereArgs: [farmId],
      orderBy: 'created_at DESC', limit: limit,
    );

    final activities = <ActivityLog>[];
    for (final row in rows) {
      final id = row['id'] as String;

      final workerRows = await db.query('activity_workers', where: 'activity_id = ?', whereArgs: [id]);
      final inputRows  = await db.query('activity_inputs',  where: 'activity_id = ?', whereArgs: [id]);

      activities.add(ActivityLog(
        id: id,
        farmId: row['farm_id'] as String,
        sectionId: row['section_id'] as String,
        sectionName: row['section_name'] as String,
        blockId: row['block_id'] as String,
        blockName: row['block_name'] as String,
        activityType: ActivityType.values[row['activity_type'] as int],
        date: DateTime.parse(row['date'] as String),
        workers: workerRows.map(WorkerLog.fromMap).toList(),
        inputs: inputRows.map(InputUsed.fromMap).toList(),
        notes: row['notes'] as String? ?? '',
        supervisorId: row['supervisor_id'] as String,
        supervisorName: row['supervisor_name'] as String,
        labourCost: (row['labour_cost'] as num).toDouble(),
        inputCost: (row['input_cost'] as num).toDouble(),
        syncStatus: SyncStatus.values[row['sync_status'] as int],
        createdAt: DateTime.parse(row['created_at'] as String),
        syncedAt: row['synced_at'] != null ? DateTime.parse(row['synced_at'] as String) : null,
      ));
    }
    return activities;
  }

  static Future<List<ActivityLog>> getPendingSync(String farmId) async {
    final db = await database;
    final rows = await db.query(
      'activity_logs',
      where: 'farm_id = ? AND sync_status = ?',
      whereArgs: [farmId, SyncStatus.pending.index],
    );
    // Same hydration as above (simplified — reuse getActivities with filter in prod)
    return rows.map((row) => ActivityLog(
      id: row['id'] as String,
      farmId: row['farm_id'] as String,
      sectionId: row['section_id'] as String,
      sectionName: row['section_name'] as String,
      blockId: row['block_id'] as String,
      blockName: row['block_name'] as String,
      activityType: ActivityType.values[row['activity_type'] as int],
      date: DateTime.parse(row['date'] as String),
      workers: const [],
      inputs: const [],
      notes: row['notes'] as String? ?? '',
      supervisorId: row['supervisor_id'] as String,
      supervisorName: row['supervisor_name'] as String,
      syncStatus: SyncStatus.pending,
      createdAt: DateTime.parse(row['created_at'] as String),
    )).toList();
  }

  static Future<void> markSynced(String activityId) async {
    final db = await database;
    await db.update(
      'activity_logs',
      {'sync_status': SyncStatus.synced.index, 'synced_at': DateTime.now().toIso8601String()},
      where: 'id = ?', whereArgs: [activityId],
    );
  }

  static Future<int> getPendingSyncCount(String farmId) async {
    final db = await database;
    final result = await db.rawQuery(
      'SELECT COUNT(*) as count FROM activity_logs WHERE farm_id = ? AND sync_status = ?',
      [farmId, SyncStatus.pending.index],
    );
    return result.first['count'] as int;
  }
}
