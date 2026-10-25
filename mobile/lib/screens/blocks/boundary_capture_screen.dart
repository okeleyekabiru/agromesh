import 'dart:async';
import 'dart:math';
import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';
import '../../models/models.dart';
import '../../services/api_client.dart';
import '../../stores/app_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/geo_sketch.dart' show geoJsonRings;
import 'map_pick_screen.dart';

/// GPS corner-marking boundary capture. Walk to each corner of the block and tap "Mark Corner Here", in order
/// around the edge. A live GPS fix is shown with its accuracy; a corner is taken from the best fix seen in the
/// last few seconds, so a weak first fix does not skew it. The drawing is to scale, with the length of every
/// side. Needs at least 4 corners. When online the boundary is sent straight away and the server's answer is
/// shown (so a rejection is never silent); offline it is queued and sent when the connection returns.
class BoundaryCaptureScreen extends StatefulWidget {
  final String blockId;
  final String blockName;
  final String? existingGeoJson;

  const BoundaryCaptureScreen({super.key, required this.blockId, required this.blockName, this.existingGeoJson});

  @override
  State<BoundaryCaptureScreen> createState() => _BoundaryCaptureScreenState();
}

class _BoundaryCaptureScreenState extends State<BoundaryCaptureScreen> {
  static const _minPoints = 4;
  static const _goodAccuracyM = 10.0;
  static const _maxWaitSeconds = 20;

  final List<GeoPoint> _points = [];
  final List<double> _accuracies = [];
  StreamSubscription<Position>? _sub;
  Position? _live;
  bool _isCapturing = false;
  bool _isFinishing = false;
  String? _error;
  late final List<List<GeoPoint>> _existing = geoJsonRings(widget.existingGeoJson);

  @override
  void initState() {
    super.initState();
    _startLive();
  }

  @override
  void dispose() {
    _sub?.cancel();
    super.dispose();
  }

  Future<void> _startLive() async {
    if (!await _ensurePermission() || !await Geolocator.isLocationServiceEnabled()) return;
    _sub = Geolocator.getPositionStream(
      locationSettings: const LocationSettings(accuracy: LocationAccuracy.best, distanceFilter: 0),
    ).listen((p) {
      if (mounted) setState(() => _live = p);
    }, onError: (_) {});
  }

  Future<bool> _ensurePermission() async {
    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }
    return permission == LocationPermission.always || permission == LocationPermission.whileInUse;
  }

  /// Waits (up to a few seconds) for a fix that is accurate enough, and keeps the best one seen.
  Future<void> _markCorner() async {
    setState(() { _isCapturing = true; _error = null; });
    try {
      if (!await _ensurePermission()) {
        setState(() => _error = 'Location permission is required to mark corners. Enable it in Settings.');
        return;
      }
      if (!await Geolocator.isLocationServiceEnabled()) {
        setState(() => _error = 'Turn on device location/GPS to capture a corner.');
        return;
      }
      if (_sub == null) await _startLive();

      Position? best = _live;
      final deadline = DateTime.now().add(const Duration(seconds: _maxWaitSeconds));
      while ((best == null || best.accuracy > _goodAccuracyM) && DateTime.now().isBefore(deadline)) {
        await Future.delayed(const Duration(milliseconds: 400));
        final cur = _live;
        if (cur != null && (best == null || cur.accuracy < best.accuracy)) best = cur;
      }
      if (best == null) {
        setState(() => _error = 'Could not get a GPS fix. Move to open sky and try again.');
        return;
      }
      setState(() {
        _points.add(GeoPoint(lat: best!.latitude, lng: best.longitude));
        _accuracies.add(best.accuracy);
        if (best.accuracy > 25) {
          _error = 'That corner is only accurate to about ${best.accuracy.round()} m. Undo it and try again in the open if you can.';
        }
      });
    } catch (_) {
      setState(() => _error = 'Could not get a GPS fix. Move to open sky and try again.');
    } finally {
      if (mounted) setState(() => _isCapturing = false);
    }
  }

  Future<void> _pickOnMap() async {
    final picked = await Navigator.push<List<GeoPoint>>(
      context,
      MaterialPageRoute(
        builder: (_) => MapPickScreen(
          title: 'Pick the boundary on the map',
          minPoints: _minPoints,
          blockGeoJson: widget.existingGeoJson,
          initial: List.of(_points),
        ),
      ),
    );
    if (picked == null) return;
    setState(() {
      _points..clear()..addAll(picked);
      _accuracies..clear()..addAll(List.filled(picked.length, 0));
      _error = null;
    });
  }

  void _undoLastPoint() {
    if (_points.isEmpty) return;
    setState(() { _points.removeLast(); _accuracies.removeLast(); _error = null; });
  }

  Future<void> _finishBoundary() async {
    if (_points.length < _minPoints) return;
    setState(() { _isFinishing = true; _error = null; });
    final store = context.read<AppStore>();
    try {
      var saved = false;
      if (store.isOnline) {
        saved = await _sendNow(store, confirm: false);
        if (!saved) return; // the error is on screen
      } else {
        await store.finishBoundaryCapture(widget.blockId, List.of(_points));
      }
      if (!mounted) return;
      Navigator.pop(context);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(saved
              ? 'Boundary saved for ${widget.blockName} (${_points.length} corners)'
              : 'Boundary queued for ${widget.blockName}. It is sent when you are back online.'),
          backgroundColor: AgroColors.leaf,
          behavior: SnackBarBehavior.floating,
        ),
      );
    } finally {
      if (mounted) setState(() => _isFinishing = false);
    }
  }

  /// Sends the boundary and shows a rejection. A "looks wrong, confirm to proceed" answer offers to save anyway.
  Future<bool> _sendNow(AppStore store, {required bool confirm}) async {
    try {
      await store.submitBoundaryNow(widget.blockId, List.of(_points), confirm: confirm);
      return true;
    } on ApiException catch (e) {
      final overridable = e.statusCode == 400 && e.message.contains('Confirm to proceed');
      if (overridable && mounted) {
        final yes = await showDialog<bool>(
          context: context,
          builder: (ctx) => AlertDialog(
            title: const Text('This boundary looks odd'),
            content: Text(e.message),
            actions: [
              TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Go back')),
              TextButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Save anyway')),
            ],
          ),
        );
        if (yes == true) return _sendNow(store, confirm: true);
        return false;
      }
      if (mounted) setState(() => _error = e.message);
      return false;
    } catch (_) {
      // No connection after all: keep it in the outbox for later.
      await store.finishBoundaryCapture(widget.blockId, List.of(_points));
      return true;
    }
  }

  double get _areaHa => _points.length < 3 ? 0 : _areaM2(_points) / 10000;

  @override
  Widget build(BuildContext context) {
    final canFinish = _points.length >= _minPoints && !_isFinishing;
    final acc = _live?.accuracy;
    final accColor = acc == null
        ? AgroColors.stone
        : acc <= _goodAccuracyM ? AgroColors.palm : acc <= 25 ? AgroColors.harvest : AgroColors.alert;

    return Scaffold(
      backgroundColor: AgroColors.mist,
      appBar: AppBar(title: Text('Mark Boundary · ${widget.blockName}')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
            child: Row(
              children: [
                Icon(Icons.gps_fixed, size: 14, color: accColor),
                const SizedBox(width: 6),
                Expanded(
                  child: Text(
                    acc == null
                        ? 'Waiting for GPS…'
                        : 'GPS accuracy about ${acc.round()} m${acc <= _goodAccuracyM ? ' · good' : acc <= 25 ? ' · fair, wait a moment' : ' · poor, move to open sky'}',
                    style: GoogleFonts.inter(fontSize: 12, color: accColor, fontWeight: FontWeight.w600),
                  ),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 0),
            child: Text(
              'Walk to each corner, in order around the edge, and tap "Mark Corner Here". Stand still on the corner for a few seconds first. At least $_minPoints corners.',
              style: GoogleFonts.inter(fontSize: 11.5, color: AgroColors.stone),
            ),
          ),
          Expanded(
            child: Container(
              margin: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: AgroColors.white,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: AgroColors.fog),
              ),
              clipBehavior: Clip.antiAlias,
              child: _points.isEmpty && _existing.isEmpty
                  ? Center(child: Text('No corners marked yet', style: GoogleFonts.inter(color: AgroColors.stone, fontSize: 13)))
                  : SizedBox.expand(
                      child: CustomPaint(
                        painter: BoundarySchematicPainter(
                          points: _points,
                          existing: _existing,
                          current: _live == null ? null : GeoPoint(lat: _live!.latitude, lng: _live!.longitude),
                        ),
                      ),
                    ),
            ),
          ),
          if (_error != null)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Text(_error!, style: const TextStyle(color: AgroColors.alert, fontSize: 12)),
            ),
          Container(
            padding: EdgeInsets.fromLTRB(16, 12, 16, MediaQuery.of(context).padding.bottom + 12),
            decoration: const BoxDecoration(
              color: AgroColors.white,
              border: Border(top: BorderSide(color: AgroColors.fog)),
            ),
            child: Column(
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        '${_points.length} corner${_points.length == 1 ? '' : 's'} marked'
                        '${_points.length >= 3 ? ' · about ${_areaHa.toStringAsFixed(2)} ha' : ''}',
                        style: GoogleFonts.jetBrainsMono(fontWeight: FontWeight.w600, fontSize: 12, color: AgroColors.forest),
                      ),
                    ),
                    if (_points.isNotEmpty)
                      TextButton(
                        onPressed: _isCapturing ? null : _undoLastPoint,
                        child: const Text('Undo last', style: TextStyle(fontSize: 12)),
                      ),
                  ],
                ),
                const SizedBox(height: 8),
                OutlinedButton.icon(
                  onPressed: _isCapturing ? null : _pickOnMap,
                  icon: const Icon(Icons.touch_app_outlined, size: 18),
                  label: const Text('Pick on map (tap the corners)'),
                ),
                const SizedBox(height: 8),
                ElevatedButton.icon(
                  onPressed: _isCapturing ? null : _markCorner,
                  icon: _isCapturing
                      ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                      : const Icon(Icons.add_location_alt_outlined, size: 18),
                  label: Text(_isCapturing ? 'Getting a good fix…' : 'Mark Corner Here'),
                ),
                const SizedBox(height: 8),
                OutlinedButton(
                  onPressed: canFinish ? _finishBoundary : null,
                  child: _isFinishing
                      ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
                      : Text(_points.length < _minPoints
                          ? 'Finish Boundary (need ${_minPoints - _points.length} more)'
                          : 'Finish Boundary'),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── geometry (metres, local flat frame around the first point) ──

Offset _toMeters(GeoPoint p, GeoPoint origin) {
  const mPerDegLat = 110574.0;
  final mPerDegLng = 111320.0 * cos(origin.lat * pi / 180);
  return Offset((p.lng - origin.lng) * mPerDegLng, -(p.lat - origin.lat) * mPerDegLat); // y down = north up
}

double _areaM2(List<GeoPoint> pts) {
  final o = pts.first;
  final m = pts.map((p) => _toMeters(p, o)).toList();
  var s = 0.0;
  for (var i = 0; i < m.length; i++) {
    final a = m[i], b = m[(i + 1) % m.length];
    s += a.dx * b.dy - b.dx * a.dy;
  }
  return s.abs() / 2;
}

/// Draws the captured corners to scale (north up) with the length of every side and a scale bar. The block's
/// saved outline, if any, is drawn dashed behind, and the phone's current position as a blue dot.
class BoundarySchematicPainter extends CustomPainter {
  final List<GeoPoint> points;
  final List<List<GeoPoint>> existing;
  final GeoPoint? current;

  const BoundarySchematicPainter({required this.points, this.existing = const [], this.current});

  @override
  void paint(Canvas canvas, Size size) {
    final all = <GeoPoint>[...points, for (final r in existing) ...r, if (current != null) current!];
    if (all.isEmpty) return;
    final origin = all.first;

    final pm = points.map((p) => _toMeters(p, origin)).toList();
    final em = existing.map((r) => r.map((p) => _toMeters(p, origin)).toList()).toList();
    final cm = current == null ? null : _toMeters(current!, origin);
    final every = <Offset>[...pm, for (final r in em) ...r, if (cm != null) cm];

    var minX = every.map((p) => p.dx).reduce(min), maxX = every.map((p) => p.dx).reduce(max);
    var minY = every.map((p) => p.dy).reduce(min), maxY = every.map((p) => p.dy).reduce(max);
    // Keep a sensible minimum extent so one or two close points are not blown up to the whole canvas.
    const minSpan = 20.0;
    if (maxX - minX < minSpan) { final c = (minX + maxX) / 2; minX = c - minSpan / 2; maxX = c + minSpan / 2; }
    if (maxY - minY < minSpan) { final c = (minY + maxY) / 2; minY = c - minSpan / 2; maxY = c + minSpan / 2; }
    final spanX = maxX - minX, spanY = maxY - minY;

    const pad = 40.0;
    final scale = min(max(size.width - pad * 2, 1.0) / spanX, max(size.height - pad * 2, 1.0) / spanY); // px per metre
    Offset toCanvas(Offset p) => Offset(
          (p.dx - minX) * scale + (size.width - spanX * scale) / 2,
          (p.dy - minY) * scale + (size.height - spanY * scale) / 2,
        );

    // Saved outline (dashed grey)
    final ghost = Paint()
      ..color = AgroColors.stone
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.5;
    for (final ring in em) {
      final path = Path();
      for (var i = 0; i < ring.length; i++) {
        final c = toCanvas(ring[i]);
        i == 0 ? path.moveTo(c.dx, c.dy) : path.lineTo(c.dx, c.dy);
      }
      path.close();
      _dash(canvas, path, ghost);
    }

    final cp = pm.map(toCanvas).toList();
    if (cp.isNotEmpty) {
      final path = Path()..moveTo(cp.first.dx, cp.first.dy);
      for (final p in cp.skip(1)) {
        path.lineTo(p.dx, p.dy);
      }
      if (cp.length >= 3) {
        path.close();
        canvas.drawPath(path, Paint()..color = AgroColors.palm.withOpacity(0.14));
      }
      canvas.drawPath(path, Paint()
        ..color = AgroColors.leaf
        ..style = PaintingStyle.stroke
        ..strokeWidth = 2);

      // Side lengths
      if (cp.length >= 2) {
        final sides = cp.length >= 3 ? cp.length : cp.length - 1;
        for (var i = 0; i < sides; i++) {
          final a = pm[i], b = pm[(i + 1) % pm.length];
          final len = (b - a).distance;
          final mid = (cp[i] + cp[(i + 1) % cp.length]) / 2;
          _label(canvas, '${len.toStringAsFixed(len < 100 ? 1 : 0)} m', mid, size, AgroColors.ink, 9.5);
        }
      }
      for (var i = 0; i < cp.length; i++) {
        _dot(canvas, cp[i], '${i + 1}');
      }
    }

    if (cm != null) {
      final c = toCanvas(cm);
      canvas.drawCircle(c, 9, Paint()..color = const Color(0xFF2F80ED).withOpacity(0.22));
      canvas.drawCircle(c, 5, Paint()..color = const Color(0xFF2F80ED));
      canvas.drawCircle(c, 5, Paint()
        ..color = Colors.white
        ..style = PaintingStyle.stroke
        ..strokeWidth = 1.5);
    }

    _scaleBar(canvas, size, scale);
  }

  void _scaleBar(Canvas canvas, Size size, double pxPerM) {
    const nice = [1.0, 2.0, 5.0, 10.0, 20.0, 50.0, 100.0, 200.0, 500.0, 1000.0];
    final target = 90 / pxPerM;
    final m = nice.lastWhere((n) => n <= target, orElse: () => nice.first);
    final w = m * pxPerM;
    final y = size.height - 16;
    const x = 14.0;
    final p = Paint()
      ..color = AgroColors.forest
      ..strokeWidth = 2;
    canvas.drawLine(Offset(x, y), Offset(x + w, y), p);
    canvas.drawLine(Offset(x, y - 4), Offset(x, y + 4), p);
    canvas.drawLine(Offset(x + w, y - 4), Offset(x + w, y + 4), p);
    _label(canvas, '${m.round()} m', Offset(x + w / 2, y - 10), size, AgroColors.forest, 10, plate: false);
    _label(canvas, 'N ↑', Offset(size.width - 22, 16), size, AgroColors.forest, 11, plate: false);
  }

  void _dash(Canvas canvas, Path path, Paint paint) {
    for (final metric in path.computeMetrics()) {
      var d = 0.0;
      while (d < metric.length) {
        canvas.drawPath(metric.extractPath(d, min(d + 6, metric.length)), paint);
        d += 11;
      }
    }
  }

  void _label(Canvas canvas, String text, Offset center, Size size, Color color, double fs, {bool plate = true}) {
    final tp = TextPainter(
      text: TextSpan(text: text, style: TextStyle(color: color, fontSize: fs, fontWeight: FontWeight.w600)),
      textDirection: TextDirection.ltr,
    )..layout();
    final r = Rect.fromCenter(center: center, width: tp.width + 6, height: tp.height + 2);
    if (plate) canvas.drawRRect(RRect.fromRectAndRadius(r, const Radius.circular(3)), Paint()..color = Colors.white.withOpacity(0.85));
    tp.paint(canvas, center - Offset(tp.width / 2, tp.height / 2));
  }

  void _dot(Canvas canvas, Offset center, String label) {
    canvas.drawCircle(center, 8, Paint()..color = AgroColors.forest);
    final tp = TextPainter(
      text: TextSpan(text: label, style: const TextStyle(color: Colors.white, fontSize: 9, fontWeight: FontWeight.bold)),
      textDirection: TextDirection.ltr,
    )..layout();
    tp.paint(canvas, center - Offset(tp.width / 2, tp.height / 2));
  }

  // `points` is the same mutable list instance across rebuilds, so always repaint; cheap for a handful of points.
  @override
  bool shouldRepaint(covariant BoundarySchematicPainter oldDelegate) => true;
}
