import 'dart:convert';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'api_client.dart';
import 'database_service.dart';

/// Best-effort offline-outbox sync: for each pending row in a local table,
/// POST it to the server and mark it `synced` on 2xx. On failure the row is
/// left `pending` for the next sync attempt (triggered on reconnect or pull-
/// to-refresh) — no retry backoff, same idiom the original activity-only sync
/// used, just now shared across both outbox tables.
class SyncService {
  SyncService._();

  static Future<bool> isOnline() async {
    final result = await Connectivity().checkConnectivity();
    return result != ConnectivityResult.none;
  }

  static Future<SyncResult> syncPending(String farmId) async {
    if (!await isOnline()) {
      return const SyncResult(success: false, synced: 0, message: 'Offline — queued for later');
    }

    final activityRows = await DatabaseService.getPendingRows('activity_logs', farmId: farmId);
    final boundaryRows = await DatabaseService.getPendingRows('boundary_submissions', farmId: farmId);

    if (activityRows.isEmpty && boundaryRows.isEmpty) {
      return const SyncResult(success: true, synced: 0, message: 'Nothing to sync');
    }

    // The local row is SQLite-shaped; the API needs a LogActivityRequest (camelCase, workers
    // and inputs inlined) — posting the raw row never matched it.
    final syncedActivities = await _syncRows(
      table: 'activity_logs',
      rows: activityRows,
      endpointFor: (row) => '/api/farms/${row['farm_id']}/activities',
      payloadFor: DatabaseService.buildActivityPayload,
    );

    final syncedBoundaries = await _syncRows(
      table: 'boundary_submissions',
      rows: boundaryRows,
      endpointFor: (row) => '/api/blocks/${row['block_id']}/boundary/points',
      payloadFor: (row) async => {
        // `id` IS the client operation id — reused verbatim on every retry so
        // the server can dedupe resubmissions instead of double-counting them.
        'operationId': row['id'],
        'points': jsonDecode(row['points_json'] as String),
      },
    );

    final total = syncedActivities.synced + syncedBoundaries.synced;
    final rejected = syncedActivities.rejected + syncedBoundaries.rejected;
    final attempted = activityRows.length + boundaryRows.length;
    return SyncResult(
      success: true,
      synced: total,
      message: rejected > 0
          ? 'Synced $total of $attempted item(s); $rejected rejected by the server'
          : 'Synced $total of $attempted item(s)',
    );
  }

  /// Shared per-item POST loop used for every outbox table — deliberately not
  /// a generic polymorphic "outbox" abstraction, just a small helper the two
  /// concrete tables both call with their own endpoint/payload shaping.
  static Future<_RowSyncResult> _syncRows({
    required String table,
    required List<Map<String, dynamic>> rows,
    required String Function(Map<String, dynamic> row) endpointFor,
    required Future<Map<String, dynamic>> Function(Map<String, dynamic> row) payloadFor,
  }) async {
    int synced = 0;
    int rejected = 0;
    for (final row in rows) {
      try {
        await ApiClient.post(endpointFor(row), await payloadFor(row));
        await DatabaseService.markRowSynced(table, row['id'] as String);
        synced++;
      } on ApiException catch (e) {
        // A 4xx like "not enough stock" will never succeed on retry — mark it failed instead
        // of re-sending it forever. Auth (401/403) and rate-limit errors stay pending.
        if (_isPermanentRejection(e.statusCode)) {
          await DatabaseService.markRowFailed(table, row['id'] as String);
          rejected++;
        }
        continue;
      } catch (_) {
        // Network failure / timeout — leave pending, continue with the rest.
        continue;
      }
    }
    return _RowSyncResult(synced, rejected);
  }

  static bool _isPermanentRejection(int? status) =>
      status == 400 || status == 404 || status == 409 || status == 422;

  // Listen for connectivity changes and auto-sync
  static Stream<bool> get connectivityStream => Connectivity()
      .onConnectivityChanged
      .map((result) => result != ConnectivityResult.none);
}

class _RowSyncResult {
  final int synced;
  final int rejected;
  const _RowSyncResult(this.synced, this.rejected);
}

class SyncResult {
  final bool success;
  final int synced;
  final String message;

  const SyncResult({
    required this.success,
    required this.synced,
    required this.message,
  });
}
