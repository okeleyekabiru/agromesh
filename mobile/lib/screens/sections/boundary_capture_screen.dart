import 'dart:math';
import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';
import '../../models/models.dart';
import '../../stores/app_store.dart';
import '../../theme/app_theme.dart';

/// GPS corner-marking boundary capture — the primary, mobile-first way to
/// capture a section's boundary (map-drawing was explicitly rejected). Walk to
/// each corner of the section and tap "Mark Corner Here"; once at least 3
/// points are captured, "Finish Boundary" writes the session to the local
/// offline outbox (works with zero connectivity) for best-effort sync later.
class BoundaryCaptureScreen extends StatefulWidget {
  final String sectionId;
  final String sectionName;

  const BoundaryCaptureScreen({super.key, required this.sectionId, required this.sectionName});

  @override
  State<BoundaryCaptureScreen> createState() => _BoundaryCaptureScreenState();
}

class _BoundaryCaptureScreenState extends State<BoundaryCaptureScreen> {
  final List<GeoPoint> _points = [];
  bool _isCapturing = false;
  bool _isFinishing = false;
  String? _error;

  Future<void> _markCorner() async {
    setState(() {
      _isCapturing = true;
      _error = null;
    });

    try {
      final hasPermission = await _ensurePermission();
      if (!hasPermission) {
        setState(() => _error = 'Location permission is required to mark corners. Enable it in Settings.');
        return;
      }

      final serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled) {
        setState(() => _error = 'Turn on device location/GPS to capture a corner.');
        return;
      }

      final position = await Geolocator.getCurrentPosition(
        desiredAccuracy: LocationAccuracy.high,
        timeLimit: const Duration(seconds: 20),
      );

      setState(() {
        _points.add(GeoPoint(lat: position.latitude, lng: position.longitude));
      });
    } catch (_) {
      setState(() => _error = 'Could not get a GPS fix. Move to open sky and try again.');
    } finally {
      if (mounted) setState(() => _isCapturing = false);
    }
  }

  Future<bool> _ensurePermission() async {
    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }
    return permission == LocationPermission.always || permission == LocationPermission.whileInUse;
  }

  void _undoLastPoint() {
    if (_points.isEmpty) return;
    setState(() => _points.removeLast());
  }

  Future<void> _finishBoundary() async {
    if (_points.length < 3) return;
    setState(() => _isFinishing = true);

    try {
      await context.read<AppStore>().finishBoundaryCapture(widget.sectionId, List.of(_points));
      if (mounted) {
        Navigator.pop(context);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Boundary saved for ${widget.sectionName} (${_points.length} points)'),
            backgroundColor: AgroColors.leaf,
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isFinishing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final canFinish = _points.length >= 3 && !_isFinishing;

    return Scaffold(
      backgroundColor: AgroColors.mist,
      appBar: AppBar(title: Text('Mark Boundary · ${widget.sectionName}')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
            child: Text(
              'Walk to each corner of the section and tap "Mark Corner Here", in order '
              'around the boundary. No map or connectivity needed — this works fully offline.',
              style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone),
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
              child: _points.isEmpty
                ? Center(
                    child: Text('No corners marked yet', style: GoogleFonts.inter(color: AgroColors.stone, fontSize: 13)),
                  )
                : SizedBox.expand(
                    child: CustomPaint(painter: BoundarySchematicPainter(points: _points)),
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
                        '${_points.length} corner${_points.length == 1 ? '' : 's'} marked',
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
                ElevatedButton.icon(
                  onPressed: _isCapturing ? null : _markCorner,
                  icon: _isCapturing
                    ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : const Icon(Icons.add_location_alt_outlined, size: 18),
                  label: Text(_isCapturing ? 'Getting location…' : 'Mark Corner Here'),
                ),
                const SizedBox(height: 8),
                OutlinedButton(
                  onPressed: canFinish ? _finishBoundary : null,
                  child: _isFinishing
                    ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
                    : Text(_points.length < 3
                        ? 'Finish Boundary (need ${3 - _points.length} more)'
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

/// Draws a simple schematic polygon connecting captured GPS points in their
/// relative position — a local equirectangular projection around the first
/// point, scaled to fit the canvas. This is deliberately NOT a real map (no
/// basemap/tiles), since the boundary preview must work with zero
/// connectivity — a real map is a nice-to-have, never a requirement here.
class BoundarySchematicPainter extends CustomPainter {
  final List<GeoPoint> points;

  const BoundarySchematicPainter({required this.points});

  @override
  void paint(Canvas canvas, Size size) {
    if (points.isEmpty) return;

    final lat0Rad = points.first.lat * (pi / 180);
    final projected = points.map((p) {
      final x = (p.lng - points.first.lng) * cos(lat0Rad);
      final y = -(p.lat - points.first.lat); // screen y grows downward; lat grows north
      return Offset(x, y);
    }).toList();

    if (projected.length == 1) {
      _drawDot(canvas, size.center(Offset.zero), 1);
      return;
    }

    final minX = projected.map((p) => p.dx).reduce(min);
    final maxX = projected.map((p) => p.dx).reduce(max);
    final minY = projected.map((p) => p.dy).reduce(min);
    final maxY = projected.map((p) => p.dy).reduce(max);
    final spanX = (maxX - minX) == 0 ? 1.0 : (maxX - minX);
    final spanY = (maxY - minY) == 0 ? 1.0 : (maxY - minY);

    const padding = 36.0;
    final availableW = max(size.width - padding * 2, 1.0);
    final availableH = max(size.height - padding * 2, 1.0);
    final scale = min(availableW / spanX, availableH / spanY);

    Offset toCanvas(Offset p) {
      final cx = (p.dx - minX) * scale + (size.width - spanX * scale) / 2;
      final cy = (p.dy - minY) * scale + (size.height - spanY * scale) / 2;
      return Offset(cx, cy);
    }

    final canvasPoints = projected.map(toCanvas).toList();

    final fillPaint = Paint()
      ..color = AgroColors.palm.withOpacity(0.12)
      ..style = PaintingStyle.fill;
    final strokePaint = Paint()
      ..color = AgroColors.leaf
      ..style = PaintingStyle.stroke
      ..strokeWidth = 2;

    final path = Path()..moveTo(canvasPoints.first.dx, canvasPoints.first.dy);
    for (final p in canvasPoints.skip(1)) {
      path.lineTo(p.dx, p.dy);
    }

    if (canvasPoints.length >= 3) {
      path.close();
      canvas.drawPath(path, fillPaint);
    }
    canvas.drawPath(path, strokePaint);

    for (var i = 0; i < canvasPoints.length; i++) {
      _drawDot(canvas, canvasPoints[i], i + 1);
    }
  }

  void _drawDot(Canvas canvas, Offset center, int label) {
    canvas.drawCircle(center, 7, Paint()..color = AgroColors.forest);
    final textPainter = TextPainter(
      text: TextSpan(
        text: '$label',
        style: const TextStyle(color: Colors.white, fontSize: 9, fontWeight: FontWeight.bold),
      ),
      textDirection: TextDirection.ltr,
    )..layout();
    textPainter.paint(canvas, center - Offset(textPainter.width / 2, textPainter.height / 2));
  }

  @override
  bool shouldRepaint(covariant BoundarySchematicPainter oldDelegate) {
    // `points` is the same mutable list instance across rebuilds (the caller
    // appends/removes in place), so reference/content comparison here can't
    // reliably detect changes — always repaint. Cheap for a handful of points.
    return true;
  }
}
