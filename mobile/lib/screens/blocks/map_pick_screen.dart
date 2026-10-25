import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart' hide Path;
import 'package:geolocator/geolocator.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:latlong2/latlong.dart' hide Path;
import '../../models/models.dart';
import '../../theme/app_theme.dart';
import '../../widgets/geo_sketch.dart' show geoJsonRings;

/// Pick the corners of an area by tapping a satellite map (the same way the web app does), instead of standing at
/// each one. The block's outline is drawn for reference; for worked areas, what is already done (green) and what is
/// left (amber) are drawn too. "My location" adds the GPS position as a point. Pops with the chosen points.
class MapPickScreen extends StatefulWidget {
  final String title;
  final int minPoints;
  final String? blockGeoJson;
  final String? coveredGeoJson;
  final String? remainingGeoJson;
  final List<GeoPoint> initial;

  const MapPickScreen({
    super.key,
    required this.title,
    required this.minPoints,
    this.blockGeoJson,
    this.coveredGeoJson,
    this.remainingGeoJson,
    this.initial = const [],
  });

  @override
  State<MapPickScreen> createState() => _MapPickScreenState();
}

class _MapPickScreenState extends State<MapPickScreen> {
  final _map = MapController();
  late final List<LatLng> _points = [for (final p in widget.initial) LatLng(p.lat, p.lng)];
  late final List<List<LatLng>> _block = _rings(widget.blockGeoJson);
  late final List<List<LatLng>> _covered = _rings(widget.coveredGeoJson);
  late final List<List<LatLng>> _remaining = _rings(widget.remainingGeoJson);
  LatLng? _me;
  bool _locating = false;
  String? _error;

  static List<List<LatLng>> _rings(String? json) =>
      geoJsonRings(json).map((r) => [for (final p in r) LatLng(p.lat, p.lng)]).toList();

  LatLngBounds? get _startBounds {
    final all = [..._points, for (final r in _block) ...r];
    if (all.isEmpty) return null;
    return LatLngBounds.fromPoints(all);
  }

  @override
  void initState() {
    super.initState();
    // With no outline or points to centre on, start at the phone's position.
    if (_startBounds == null) _locate(move: true);
  }

  Future<void> _locate({bool move = false, bool add = false}) async {
    setState(() { _locating = true; _error = null; });
    try {
      var permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) permission = await Geolocator.requestPermission();
      if (permission != LocationPermission.always && permission != LocationPermission.whileInUse) {
        setState(() => _error = 'Location permission is needed to use your position.');
        return;
      }
      if (!await Geolocator.isLocationServiceEnabled()) {
        setState(() => _error = 'Turn on device location/GPS.');
        return;
      }
      final pos = await Geolocator.getCurrentPosition(
        desiredAccuracy: LocationAccuracy.best,
        timeLimit: const Duration(seconds: 15),
      );
      final here = LatLng(pos.latitude, pos.longitude);
      setState(() {
        _me = here;
        if (add) _points.add(here);
      });
      if (move || add) _map.move(here, move ? 18 : _map.camera.zoom);
    } catch (_) {
      if (mounted) setState(() => _error = 'Could not read your location.');
    } finally {
      if (mounted) setState(() => _locating = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final start = _startBounds;
    final enough = _points.length >= widget.minPoints;

    return Scaffold(
      backgroundColor: AgroColors.mist,
      appBar: AppBar(title: Text(widget.title)),
      body: Column(
        children: [
          Expanded(
            child: Stack(
              children: [
                FlutterMap(
                  mapController: _map,
                  options: MapOptions(
                    initialCenter: const LatLng(9.0, 8.0), // Nigeria, until the outline or GPS says otherwise
                    initialZoom: 6,
                    initialCameraFit: start == null
                        ? null
                        : CameraFit.bounds(bounds: start, padding: const EdgeInsets.all(48), maxZoom: 19),
                    maxZoom: 22,
                    interactionOptions: const InteractionOptions(flags: InteractiveFlag.all & ~InteractiveFlag.rotate),
                    onTap: (_, p) => setState(() { _points.add(p); _error = null; }),
                  ),
                  children: [
                    TileLayer(
                      urlTemplate: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
                      maxNativeZoom: 19,
                      maxZoom: 22,
                      userAgentPackageName: 'com.agromesh.mobile',
                    ),
                    PolygonLayer(polygons: [
                      for (final r in _remaining)
                        Polygon(points: r, color: const Color(0xFFF5A623).withOpacity(0.35), borderColor: const Color(0xFFF5A623), borderStrokeWidth: 1),
                      for (final r in _covered)
                        Polygon(points: r, color: const Color(0xFF3FB56D).withOpacity(0.45), borderColor: Colors.white, borderStrokeWidth: 1.5),
                      for (final r in _block)
                        Polygon(points: r, color: Colors.transparent, borderColor: Colors.white, borderStrokeWidth: 2),
                      if (_points.length >= 3)
                        Polygon(points: _points, color: AgroColors.palm.withOpacity(0.3), borderColor: const Color(0xFF00E5A0), borderStrokeWidth: 2.5),
                    ]),
                    if (_points.length == 2)
                      PolylineLayer(polylines: [Polyline(points: _points, color: const Color(0xFF00E5A0), strokeWidth: 2.5)]),
                    MarkerLayer(markers: [
                      if (_me != null)
                        Marker(
                          point: _me!,
                          width: 22,
                          height: 22,
                          child: Container(
                            decoration: BoxDecoration(
                              color: const Color(0xFF2F80ED),
                              shape: BoxShape.circle,
                              border: Border.all(color: Colors.white, width: 2.5),
                            ),
                          ),
                        ),
                      for (var i = 0; i < _points.length; i++)
                        Marker(
                          point: _points[i],
                          width: 26,
                          height: 26,
                          child: Container(
                            decoration: BoxDecoration(
                              color: AgroColors.forest,
                              shape: BoxShape.circle,
                              border: Border.all(color: Colors.white, width: 2),
                            ),
                            alignment: Alignment.center,
                            child: Text('${i + 1}', style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.bold)),
                          ),
                        ),
                    ]),
                    const SimpleAttributionWidget(source: Text('Imagery © Esri')),
                  ],
                ),
                Positioned(
                  left: 10,
                  top: 10,
                  right: 10,
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                    decoration: BoxDecoration(color: Colors.white.withOpacity(0.92), borderRadius: BorderRadius.circular(8)),
                    child: Text(
                      'Tap the map at each corner, in order around the edge. Zoom in for accuracy. At least ${widget.minPoints} points.',
                      style: GoogleFonts.inter(fontSize: 11.5, color: AgroColors.ink),
                    ),
                  ),
                ),
              ],
            ),
          ),
          if (_error != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
              child: Text(_error!, style: const TextStyle(color: AgroColors.alert, fontSize: 12)),
            ),
          Container(
            padding: EdgeInsets.fromLTRB(16, 10, 16, MediaQuery.of(context).padding.bottom + 10),
            decoration: const BoxDecoration(color: AgroColors.white, border: Border(top: BorderSide(color: AgroColors.fog))),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        '${_points.length} / ${widget.minPoints} points${enough ? ' ✓' : ''}',
                        style: GoogleFonts.jetBrainsMono(fontWeight: FontWeight.w600, fontSize: 12, color: AgroColors.forest),
                      ),
                    ),
                    TextButton(
                      onPressed: _points.isEmpty ? null : () => setState(_points.removeLast),
                      child: const Text('Undo'),
                    ),
                    TextButton(
                      onPressed: _points.isEmpty ? null : () => setState(_points.clear),
                      child: const Text('Clear'),
                    ),
                  ],
                ),
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: _locating ? null : () => _locate(add: true),
                        icon: _locating
                            ? const SizedBox(width: 14, height: 14, child: CircularProgressIndicator(strokeWidth: 2))
                            : const Icon(Icons.my_location, size: 16),
                        label: const Text('Add my location'),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: ElevatedButton(
                        onPressed: enough
                            ? () => Navigator.pop(context, [for (final p in _points) GeoPoint(lat: p.latitude, lng: p.longitude)])
                            : null,
                        child: const Text('Use these points'),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
