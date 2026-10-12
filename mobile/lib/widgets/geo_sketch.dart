import 'dart:convert';
import 'dart:math' as math;
import 'package:flutter/material.dart';
import '../models/models.dart';
import '../theme/app_theme.dart';

/// Reads the rings of a GeoJSON Polygon / MultiPolygon (outer rings and holes, flattened) so they can be
/// drawn together with the even-odd rule. Returns an empty list for anything it cannot read.
List<List<GeoPoint>> geoJsonRings(String? geoJson) {
  if (geoJson == null || geoJson.isEmpty) return const [];
  try {
    final g = jsonDecode(geoJson) as Map<String, dynamic>;
    List<List<GeoPoint>> ringsOf(List polygon) => polygon
        .map((ring) => (ring as List)
            .map((c) => GeoPoint(lat: (c[1] as num).toDouble(), lng: (c[0] as num).toDouble()))
            .toList())
        .toList();

    switch (g['type']) {
      case 'Polygon':
        return ringsOf(g['coordinates'] as List);
      case 'MultiPolygon':
        return [for (final p in g['coordinates'] as List) ...ringsOf(p as List)];
    }
  } catch (_) {/* fall through */}
  return const [];
}

/// A map-free sketch of a block: its outline, what is done (green), what is left (amber), and, while the
/// supervisor is marking an area, the numbered points walked so far. Everything is projected into the same
/// frame so the pieces line up.
class GeoSketch extends StatelessWidget {
  final List<List<GeoPoint>> block;
  final List<List<GeoPoint>> covered;
  final List<List<GeoPoint>> remaining;
  final List<GeoPoint> marking;
  /// Each job's marked area this cycle, oldest first: drawn as numbered outlines with a dot on every picked corner.
  final List<List<List<GeoPoint>>> sections;
  final double height;

  const GeoSketch({
    super.key,
    this.block = const [],
    this.covered = const [],
    this.remaining = const [],
    this.marking = const [],
    this.sections = const [],
    this.height = 240,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      height: height,
      width: double.infinity,
      decoration: BoxDecoration(
        color: AgroColors.mist,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: AgroColors.fog),
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(10),
        child: CustomPaint(painter: _SketchPainter(block, covered, remaining, marking, sections)),
      ),
    );
  }
}

class _SketchPainter extends CustomPainter {
  final List<List<GeoPoint>> block, covered, remaining;
  final List<GeoPoint> marking;
  final List<List<List<GeoPoint>>> sections;
  _SketchPainter(this.block, this.covered, this.remaining, this.marking, this.sections);

  @override
  void paint(Canvas canvas, Size size) {
    final all = [...block.expand((r) => r), ...covered.expand((r) => r), ...remaining.expand((r) => r), ...marking, ...sections.expand((s) => s.expand((r) => r))];
    if (all.isEmpty) return;

    var minLat = all.first.lat, maxLat = all.first.lat, minLng = all.first.lng, maxLng = all.first.lng;
    for (final p in all) {
      minLat = math.min(minLat, p.lat);
      maxLat = math.max(maxLat, p.lat);
      minLng = math.min(minLng, p.lng);
      maxLng = math.max(maxLng, p.lng);
    }
    // Longitude degrees are shorter than latitude degrees away from the equator.
    final cosLat = math.cos(((minLat + maxLat) / 2) * math.pi / 180);
    final w = math.max((maxLng - minLng) * cosLat, 1e-9);
    final h = math.max(maxLat - minLat, 1e-9);
    const pad = 16.0;
    final scale = math.min((size.width - 2 * pad) / w, (size.height - 2 * pad) / h);
    final offX = (size.width - w * scale) / 2;
    final offY = (size.height - h * scale) / 2;

    Offset at(GeoPoint p) => Offset(offX + (p.lng - minLng) * cosLat * scale, offY + (maxLat - p.lat) * scale);

    Path pathOf(List<List<GeoPoint>> rings) {
      final path = Path()..fillType = PathFillType.evenOdd;
      for (final ring in rings) {
        if (ring.length < 3) continue;
        path.addPolygon(ring.map(at).toList(), true);
      }
      return path;
    }

    if (remaining.isNotEmpty) {
      canvas.drawPath(pathOf(remaining), Paint()..color = const Color(0x73F5A623));
    }
    if (covered.isNotEmpty) {
      canvas.drawPath(pathOf(covered), Paint()..color = const Color(0x8C3FB56D));
    }
    if (block.isNotEmpty) {
      canvas.drawPath(
        pathOf(block),
        Paint()
          ..style = PaintingStyle.stroke
          ..strokeWidth = 2
          ..color = AgroColors.forest,
      );
    }

    const palette = [Color(0xFFE4572E), Color(0xFF17BEBB), Color(0xFF9B5DE5), Color(0xFFF15BB5), Color(0xFF3A86FF), Color(0xFFFB8500), Color(0xFF2A9D8F), Color(0xFF8338EC)];
    for (var i = 0; i < sections.length; i++) {
      final col = palette[i % palette.length];
      Offset? centre;
      for (final ring in sections[i]) {
        if (ring.length < 3) continue;
        final pts = ring.map(at).toList();
        canvas.drawPath(Path()..addPolygon(pts, true), Paint()..style = PaintingStyle.stroke..strokeWidth = 2..color = col);
        // The ring repeats its first point, so the last one is not a corner of its own.
        for (final p in pts.take(pts.length - 1)) {
          canvas.drawCircle(p, 3.5, Paint()..color = Colors.white);
          canvas.drawCircle(p, 2.5, Paint()..color = col);
        }
        centre ??= Offset(pts.map((p) => p.dx).reduce((a, b) => a + b) / pts.length, pts.map((p) => p.dy).reduce((a, b) => a + b) / pts.length);
      }
      if (centre != null) {
        canvas.drawCircle(centre, 10, Paint()..color = col);
        final tp = TextPainter(
          text: TextSpan(text: '${i + 1}', style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.w700)),
          textDirection: TextDirection.ltr,
        )..layout();
        tp.paint(canvas, centre - Offset(tp.width / 2, tp.height / 2));
      }
    }

    if (marking.isNotEmpty) {
      final pts = marking.map(at).toList();
      if (pts.length >= 3) {
        canvas.drawPath(Path()..addPolygon(pts, true), Paint()..color = const Color(0x402D7A4F));
      }
      canvas.drawPath(
        Path()..addPolygon(pts, pts.length >= 3),
        Paint()
          ..style = PaintingStyle.stroke
          ..strokeWidth = 2
          ..color = AgroColors.palm,
      );
      for (var i = 0; i < pts.length; i++) {
        canvas.drawCircle(pts[i], 10, Paint()..color = AgroColors.palm);
        final tp = TextPainter(
          text: TextSpan(text: '${i + 1}', style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.w700)),
          textDirection: TextDirection.ltr,
        )..layout();
        tp.paint(canvas, pts[i] - Offset(tp.width / 2, tp.height / 2));
      }
    }
  }

  @override
  bool shouldRepaint(covariant _SketchPainter old) => true;
}
