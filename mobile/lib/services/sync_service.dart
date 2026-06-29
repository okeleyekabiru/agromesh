import 'dart:convert';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:http/http.dart' as http;
import 'database_service.dart';

class SyncService {
  static const _baseUrl = 'https://api.agromesh.io/v1'; // Replace with real URL
  static const _farmId = 'farm_001'; // Replace with dynamic farm ID

  // Check connectivity
  static Future<bool> isOnline() async {
    final result = await Connectivity().checkConnectivity();
    return result != ConnectivityResult.none;
  }

  // Sync pending activities to server
  static Future<SyncResult> syncPending() async {
    if (!await isOnline()) {
      return const SyncResult(success: false, synced: 0, message: 'Offline — queued for later');
    }

    final pending = await DatabaseService.getPendingSync(_farmId);

    if (pending.isEmpty) {
      return const SyncResult(success: true, synced: 0, message: 'Nothing to sync');
    }

    int synced = 0;

    for (final activity in pending) {
      try {
        final response = await http.post(
          Uri.parse('$_baseUrl/activities'),
          headers: {'Content-Type': 'application/json'},
          body: jsonEncode(activity.toMap()),
        ).timeout(const Duration(seconds: 10));

        if (response.statusCode == 200 || response.statusCode == 201) {
          await DatabaseService.markSynced(activity.id);
          synced++;
        }
      } catch (_) {
        // Individual failure — continue with others
        continue;
      }
    }

    return SyncResult(
      success: true,
      synced: synced,
      message: 'Synced $synced of ${pending.length} activities',
    );
  }

  // Listen for connectivity changes and auto-sync
  static Stream<bool> get connectivityStream => Connectivity()
      .onConnectivityChanged
      .map((result) => result != ConnectivityResult.none);
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
