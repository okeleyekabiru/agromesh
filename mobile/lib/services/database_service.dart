import 'dart:convert';
import 'package:sqflite/sqflite.dart';
import 'package:path/path.dart';
import '../models/models.dart';

class DatabaseService {
  static Database? _db;

  // v1 -> v2: added farm/session-aware section columns (crops_json,
  // boundary_geojson, centroid_lat/lng) and the boundary_submissions outbox
  // table for offline GPS corner capture.
  // v2 -> v3: Farm → Section → Block collapsed into Farm → Block. The old
  // sub-section `blocks` table is dropped, `sections` becomes `blocks`, and
  // every section_id/section_name column becomes block_id/block_name.
  // v3 -> v4: activity_inputs.inventory_item_id (inputs are real stock items now) and a small
  // kv_cache table for offline copies of the stock list and "out on the farm" list.
  static const _version = 5;
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

  static Future<void> _createBlocksTable(Database db) => db.execute('''
    CREATE TABLE blocks (
      id TEXT PRIMARY KEY,
      farm_id TEXT NOT NULL,
      name TEXT NOT NULL,
      crop INTEGER NOT NULL,
      hectares REAL NOT NULL,
      status INTEGER NOT NULL DEFAULT 0,
      last_activity_date TEXT,
      tree_count INTEGER,
      crops_json TEXT,
      boundary_geojson TEXT,
      centroid_lat REAL,
      centroid_lng REAL,
      created_at TEXT NOT NULL DEFAULT (datetime('now'))
    )
  ''');

  static Future<void> _createActivityLogsTable(Database db, [String table = 'activity_logs']) => db.execute('''
    CREATE TABLE $table (
      id TEXT PRIMARY KEY,
      farm_id TEXT NOT NULL,
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
      synced_at TEXT,
      area_points_json TEXT
    )
  ''');

  // Offline outbox for GPS corner-capture sessions. `id` is the client
  // operation id (uuid.v4, minted once) — mirrors activity_logs' pattern of
  // local-write-first + best-effort POST-on-reconnect.
  static Future<void> _createBoundarySubmissionsTable(Database db, [String table = 'boundary_submissions']) => db.execute('''
    CREATE TABLE $table (
      id TEXT PRIMARY KEY,
      block_id TEXT NOT NULL,
      farm_id TEXT NOT NULL,
      points_json TEXT NOT NULL,
      sync_status INTEGER NOT NULL DEFAULT 1,
      created_at TEXT NOT NULL,
      synced_at TEXT
    )
  ''');

  static Future<void> _createKvCacheTable(Database db) => db.execute('''
    CREATE TABLE IF NOT EXISTS kv_cache (
      key TEXT PRIMARY KEY,
      json TEXT NOT NULL
    )
  ''');

  static Future<void> _createActivityIndexes(Database db) async {
    await db.execute('CREATE INDEX IF NOT EXISTS idx_activities_farm ON activity_logs(farm_id)');
    await db.execute('CREATE INDEX IF NOT EXISTS idx_activities_block ON activity_logs(block_id)');
    await db.execute('CREATE INDEX IF NOT EXISTS idx_activities_sync ON activity_logs(sync_status)');
    await db.execute('CREATE INDEX IF NOT EXISTS idx_activities_date ON activity_logs(date)');
  }

  static Future<void> _createBoundaryIndexes(Database db) async {
    await db.execute('CREATE INDEX IF NOT EXISTS idx_boundary_farm ON boundary_submissions(farm_id)');
    await db.execute('CREATE INDEX IF NOT EXISTS idx_boundary_block ON boundary_submissions(block_id)');
    await db.execute('CREATE INDEX IF NOT EXISTS idx_boundary_sync ON boundary_submissions(sync_status)');
  }

  static Future<void> _onCreate(Database db, int version) async {
    await _createBlocksTable(db);

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

    await _createActivityLogsTable(db);

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
        inventory_item_id TEXT,
        item TEXT NOT NULL,
        quantity REAL NOT NULL,
        unit TEXT NOT NULL,
        FOREIGN KEY (activity_id) REFERENCES activity_logs(id)
      )
    ''');

    await _createKvCacheTable(db);
    await _createBoundarySubmissionsTable(db);

    await _createActivityIndexes(db);
    await _createBoundaryIndexes(db);
  }

  // Upgrade steps run inside sqflite's upgrade transaction, so a failure part-way
  // rolls the whole step back. Each step uses the table/column names as they
  // were at that version — don't rename identifiers inside older steps.
  static Future<void> _onUpgrade(Database db, int oldVersion, int newVersion) async {
    if (oldVersion < 2) {
      await db.execute('ALTER TABLE sections ADD COLUMN crops_json TEXT');
      await db.execute('ALTER TABLE sections ADD COLUMN boundary_geojson TEXT');
      await db.execute('ALTER TABLE sections ADD COLUMN centroid_lat REAL');
      await db.execute('ALTER TABLE sections ADD COLUMN centroid_lng REAL');

      await db.execute('''
        CREATE TABLE IF NOT EXISTS boundary_submissions (
          id TEXT PRIMARY KEY,
          section_id TEXT NOT NULL,
          farm_id TEXT NOT NULL,
          points_json TEXT NOT NULL,
          sync_status INTEGER NOT NULL DEFAULT 1,
          created_at TEXT NOT NULL,
          synced_at TEXT
        )
      ''');
      await db.execute('CREATE INDEX IF NOT EXISTS idx_boundary_farm ON boundary_submissions(farm_id)');
      await db.execute('CREATE INDEX IF NOT EXISTS idx_boundary_section ON boundary_submissions(section_id)');
      await db.execute('CREATE INDEX IF NOT EXISTS idx_boundary_sync ON boundary_submissions(sync_status)');
    }

    if (oldVersion < 3) {
      // Old sub-section blocks (incl. the synthesized "Whole Section" placeholders) go away.
      await db.execute('DROP TABLE IF EXISTS blocks');

      // sections → blocks. Rebuilt rather than renamed so block_count can be
      // dropped and tree_count added without relying on ALTER TABLE DROP COLUMN.
      await _createBlocksTable(db);
      await db.execute('''
        INSERT INTO blocks (id, farm_id, name, crop, hectares, status, last_activity_date,
                            crops_json, boundary_geojson, centroid_lat, centroid_lng, created_at)
        SELECT id, farm_id, name, crop, hectares, status, last_activity_date,
               crops_json, boundary_geojson, centroid_lat, centroid_lng, created_at
        FROM sections
      ''');
      await db.execute('DROP TABLE sections');

      // activity_logs: the section becomes the block; the old block columns are dropped.
      // Pending (unsynced) rows are carried over so nothing in the outbox is lost.
      await _createActivityLogsTable(db, 'activity_logs_v3');
      await db.execute('''
        INSERT INTO activity_logs_v3 (id, farm_id, block_id, block_name, activity_type, date, notes,
                                      supervisor_id, supervisor_name, labour_cost, input_cost,
                                      sync_status, created_at, synced_at)
        SELECT id, farm_id, section_id, section_name, activity_type, date, notes,
               supervisor_id, supervisor_name, labour_cost, input_cost,
               sync_status, created_at, synced_at
        FROM activity_logs
      ''');
      await db.execute('DROP TABLE activity_logs');
      await db.execute('ALTER TABLE activity_logs_v3 RENAME TO activity_logs');
      await _createActivityIndexes(db);

      // boundary_submissions: section_id → block_id.
      await _createBoundarySubmissionsTable(db, 'boundary_submissions_v3');
      await db.execute('''
        INSERT INTO boundary_submissions_v3 (id, block_id, farm_id, points_json, sync_status, created_at, synced_at)
        SELECT id, section_id, farm_id, points_json, sync_status, created_at, synced_at
        FROM boundary_submissions
      ''');
      await db.execute('DROP TABLE boundary_submissions');
      await db.execute('ALTER TABLE boundary_submissions_v3 RENAME TO boundary_submissions');
      await _createBoundaryIndexes(db);
    }

    if (oldVersion < 4) {
      await db.execute('ALTER TABLE activity_inputs ADD COLUMN inventory_item_id TEXT');
      await _createKvCacheTable(db);
    }

    if (oldVersion < 5) {
      // The area that was worked (8+ GPS points). A database upgraded from before v3 already has the column,
      // because its activity_logs table was rebuilt from the current definition above.
      final cols = await db.rawQuery('PRAGMA table_info(activity_logs)');
      if (!cols.any((c) => c['name'] == 'area_points_json')) {
        await db.execute('ALTER TABLE activity_logs ADD COLUMN area_points_json TEXT');
      }
    }
  }

  // ── BLOCKS ──

  static Future<List<Block>> getBlocks(String farmId) async {
    final db = await database;
    final rows = await db.query('blocks', where: 'farm_id = ?', whereArgs: [farmId]);
    return rows.map(Block.fromMap).toList();
  }

  static Future<void> upsertBlock(Block block) async {
    final db = await database;
    await db.insert('blocks', block.toMap(), conflictAlgorithm: ConflictAlgorithm.replace);
  }

  static Future<void> updateBlockStatus(String id, BlockStatus status, String lastActivity) async {
    final db = await database;
    await db.update(
      'blocks',
      {'status': status.index, 'last_activity_date': lastActivity},
      where: 'id = ?', whereArgs: [id],
    );
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

      // Update block last activity
      await txn.update(
        'blocks',
        {'last_activity_date': 'Today', 'status': BlockStatus.active.index},
        where: 'id = ?', whereArgs: [log.blockId],
      );
    });
  }

  /// Replaces this farm's already-synced activities with the server's list. Pending and failed
  /// rows are kept: they are still waiting to reach the server (a synced local copy is replaced
  /// because the server issues its own id when it saves the activity).
  static Future<void> replaceSyncedActivities(String farmId, List<ActivityLog> logs) async {
    final db = await database;
    await db.transaction((txn) async {
      final old = await txn.query('activity_logs',
          columns: ['id'], where: 'farm_id = ? AND sync_status = ?', whereArgs: [farmId, SyncStatus.synced.index]);
      for (final r in old) {
        final id = r['id'] as String;
        await txn.delete('activity_workers', where: 'activity_id = ?', whereArgs: [id]);
        await txn.delete('activity_inputs', where: 'activity_id = ?', whereArgs: [id]);
        await txn.delete('activity_logs', where: 'id = ?', whereArgs: [id]);
      }
      for (final log in logs) {
        await txn.insert('activity_logs', log.toMap(), conflictAlgorithm: ConflictAlgorithm.replace);
        for (final w in log.workers) {
          await txn.insert('activity_workers', {'activity_id': log.id, ...w.toMap()});
        }
        for (final inp in log.inputs) {
          await txn.insert('activity_inputs', {'activity_id': log.id, ...inp.toMap()});
        }
      }
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

  static Future<int> getPendingSyncCount(String farmId) async {
    final db = await database;
    final result = await db.rawQuery(
      'SELECT COUNT(*) as count FROM activity_logs WHERE farm_id = ? AND sync_status = ?',
      [farmId, SyncStatus.pending.index],
    );
    return result.first['count'] as int;
  }

  // ── BOUNDARY SUBMISSIONS ──

  static Future<void> insertBoundarySubmission(BoundarySubmission submission) async {
    final db = await database;
    await db.insert('boundary_submissions', submission.toMap(), conflictAlgorithm: ConflictAlgorithm.replace);
  }

  static Future<int> getPendingBoundaryCount(String farmId) async {
    final db = await database;
    final result = await db.rawQuery(
      'SELECT COUNT(*) as count FROM boundary_submissions WHERE farm_id = ? AND sync_status = ?',
      [farmId, SyncStatus.pending.index],
    );
    return result.first['count'] as int;
  }

  // ── GENERIC SYNC HELPERS (shared by SyncService for activity_logs & boundary_submissions) ──

  /// Raw rows (not hydrated into models) with `sync_status = pending` for the
  /// given farm-scoped outbox table. Deliberately generic — see SyncService.
  static Future<List<Map<String, dynamic>>> getPendingRows(String table, {required String farmId}) async {
    final db = await database;
    return db.query(table, where: 'farm_id = ? AND sync_status = ?', whereArgs: [farmId, SyncStatus.pending.index]);
  }

  /// The server rejected this row for good (e.g. not enough stock) — stop retrying it.
  static Future<void> markRowFailed(String table, String id, {String? reason}) async {
    final db = await database;
    await db.update(table, {'sync_status': SyncStatus.failed.index}, where: 'id = ?', whereArgs: [id]);
    if (reason != null) await saveCache('sync_error:$id', [{'reason': reason}]);
  }

  /// Activities the server refused, with the reason it gave (empty when the reason was not kept).
  static Future<List<Map<String, dynamic>>> getFailedActivities(String farmId) async {
    final db = await database;
    final rows = await db.query('activity_logs',
        where: 'farm_id = ? AND sync_status = ?', whereArgs: [farmId, SyncStatus.failed.index], orderBy: 'created_at DESC');
    final out = <Map<String, dynamic>>[];
    for (final r in rows) {
      final cached = await readCacheList('sync_error:${r['id']}');
      out.add({
        'id': r['id'],
        'blockName': r['block_name'],
        'activityType': r['activity_type'],
        'reason': cached.isEmpty ? '' : '${cached.first['reason']}',
      });
    }
    return out;
  }

  static Future<void> deleteActivity(String id) async {
    final db = await database;
    await db.transaction((txn) async {
      await txn.delete('activity_workers', where: 'activity_id = ?', whereArgs: [id]);
      await txn.delete('activity_inputs', where: 'activity_id = ?', whereArgs: [id]);
      await txn.delete('activity_logs', where: 'id = ?', whereArgs: [id]);
      await txn.delete('kv_cache', where: 'key = ?', whereArgs: ['sync_error:$id']);
    });
  }

  /// Puts the refused activities back in the queue to be sent again.
  static Future<void> requeueFailedActivities(String farmId) async {
    final db = await database;
    await db.update('activity_logs', {'sync_status': SyncStatus.pending.index},
        where: 'farm_id = ? AND sync_status = ?', whereArgs: [farmId, SyncStatus.failed.index]);
  }

  // ── WORKERS (from the server; replaces the old local seed data) ──

  /// Replaces this farm's cached workers with the server's list.
  static Future<void> replaceWorkers(String farmId, List<Worker> workers) async {
    final db = await database;
    await db.transaction((txn) async {
      await txn.delete('workers', where: 'farm_id = ?', whereArgs: [farmId]);
      for (final w in workers) {
        await txn.insert('workers', w.toMap(), conflictAlgorithm: ConflictAlgorithm.replace);
      }
    });
  }

  // ── OFFLINE CACHE (stock list, out-on-the-farm list) ──

  static Future<void> saveCache(String key, Object json) async {
    final db = await database;
    await db.insert('kv_cache', {'key': key, 'json': jsonEncode(json)}, conflictAlgorithm: ConflictAlgorithm.replace);
  }

  static Future<List<Map<String, dynamic>>> readCacheList(String key) async {
    final db = await database;
    final rows = await db.query('kv_cache', where: 'key = ?', whereArgs: [key]);
    if (rows.isEmpty) return [];
    try {
      return (jsonDecode(rows.first['json'] as String) as List).cast<Map<String, dynamic>>();
    } catch (_) {
      return [];
    }
  }

  // ── SYNC PAYLOAD ──

  /// Builds the `LogActivityRequest` body for a pending `activity_logs` row. The local row is
  /// shaped for SQLite (snake_case, split tables); the API wants camelCase with workers and
  /// inputs inlined, and enums as ordinals.
  static Future<Map<String, dynamic>> buildActivityPayload(Map<String, dynamic> row) async {
    final db = await database;
    final id = row['id'];
    final workers = await db.query('activity_workers', where: 'activity_id = ?', whereArgs: [id]);
    final inputs = await db.query('activity_inputs', where: 'activity_id = ?', whereArgs: [id]);
    final pointsJson = row['area_points_json'] as String?;
    return {
      if (pointsJson != null && pointsJson.isNotEmpty) 'workedAreaPoints': jsonDecode(pointsJson),
      'date': row['date'],
      'blockId': row['block_id'],
      'activityType': row['activity_type'],
      'notes': (row['notes'] as String?)?.isEmpty ?? true ? null : row['notes'],
      'supervisorId': row['supervisor_id'],
      'createdOffline': true,
      'workers': workers.map((w) => {'workerId': w['worker_id'], 'hoursWorked': w['hours_worked']}).toList(),
      'inputs': inputs.map((i) => {'inventoryItemId': i['inventory_item_id'], 'quantity': i['quantity']}).toList(),
    };
  }

  static Future<void> markRowSynced(String table, String id) async {
    final db = await database;
    await db.update(
      table,
      {'sync_status': SyncStatus.synced.index, 'synced_at': DateTime.now().toIso8601String()},
      where: 'id = ?', whereArgs: [id],
    );
  }
}
