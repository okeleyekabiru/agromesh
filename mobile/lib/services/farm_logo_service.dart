import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:path_provider/path_provider.dart';
import 'api_client.dart';

/// The farm logo kept on disk, so it still shows with no connection. One image plus the version it was fetched at
/// (the farm's `logoUpdatedAt`) per farm; when the version changes the image is fetched again.
class FarmLogoCache {
  final Future<Directory> Function() _dir;
  FarmLogoCache({Future<Directory> Function()? dirProvider}) : _dir = dirProvider ?? getApplicationSupportDirectory;

  static String _safe(String farmId) => farmId.replaceAll(RegExp(r'[^A-Za-z0-9_-]'), '_');

  Future<File> _image(String farmId) async => File('${(await _dir()).path}/farm_logo_${_safe(farmId)}.bin');
  Future<File> _version(String farmId) async => File('${(await _dir()).path}/farm_logo_${_safe(farmId)}.ver');

  Future<({Uint8List bytes, String? version})?> read(String farmId) async {
    try {
      final img = await _image(farmId);
      if (!await img.exists()) return null;
      final bytes = await img.readAsBytes();
      if (bytes.isEmpty) return null;
      final v = await _version(farmId);
      final version = await v.exists() ? await v.readAsString() : null;
      return (bytes: bytes, version: (version == null || version.isEmpty) ? null : version);
    } catch (_) {
      return null;
    }
  }

  Future<void> write(String farmId, List<int> bytes, String? version) async {
    try {
      final dir = await _dir();
      if (!await dir.exists()) await dir.create(recursive: true);
      await (await _image(farmId)).writeAsBytes(bytes, flush: true);
      await (await _version(farmId)).writeAsString(version ?? '', flush: true);
    } catch (e) {
      debugPrint('caching the farm logo failed: $e'); // non-fatal: the logo just is not kept for offline
    }
  }

  Future<void> clear(String farmId) async {
    try {
      final img = await _image(farmId);
      if (await img.exists()) await img.delete();
      final v = await _version(farmId);
      if (await v.exists()) await v.delete();
    } catch (_) {/* nothing to clear */}
  }
}

/// Fetches the farm logo with the bearer token and decides, from the farm's `hasLogo` / `logoUpdatedAt`, whether the
/// disk copy is still good.
class FarmLogoService {
  final FarmLogoCache cache;
  final Future<List<int>> Function(String farmId) _fetch;

  FarmLogoService({FarmLogoCache? cache, Future<List<int>> Function(String farmId)? fetch})
      : cache = cache ?? FarmLogoCache(),
        _fetch = fetch ?? ((farmId) => ApiClient.getBytes('/api/farms/$farmId/logo'));

  /// What is on disk for the farm (works offline). Null when none.
  Future<Uint8List?> loadCached(String farmId) async => (await cache.read(farmId))?.bytes;

  /// Called once the server has told us whether the farm has a logo. Returns the bytes to show, or null for "no logo".
  /// If the logo has to be fetched and that fails, the older disk copy (if any) is kept rather than dropping the logo.
  Future<Uint8List?> resolve(String farmId, {required bool hasLogo, String? updatedAt}) async {
    if (!hasLogo) {
      await cache.clear(farmId);
      return null;
    }
    final cached = await cache.read(farmId);
    if (cached != null && cached.version == updatedAt) return cached.bytes;
    try {
      final bytes = Uint8List.fromList(await _fetch(farmId));
      if (bytes.isEmpty) return cached?.bytes;
      await cache.write(farmId, bytes, updatedAt);
      return bytes;
    } catch (_) {
      return cached?.bytes;
    }
  }

  /// Remembers a logo we just uploaded.
  Future<void> store(String farmId, List<int> bytes, String? updatedAt) => cache.write(farmId, bytes, updatedAt);

  Future<void> clear(String farmId) => cache.clear(farmId);
}
