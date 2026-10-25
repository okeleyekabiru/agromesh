import '../models/models.dart';

/// Shown when marked points would draw a figure of eight. Same wording as the server's check.
const pointsCrossMessage =
    'The points cross each other. Mark them in order around the edge of the area, without crossing back over.';

/// True when any two sides of the shape drawn by [pts] (closed back to the first point) cross each other.
/// Sides that share a corner are not counted.
bool pointsCross(List<GeoPoint> pts) {
  final n = pts.length;
  if (n < 4) return false;
  for (var i = 0; i < n; i++) {
    final a1 = pts[i], a2 = pts[(i + 1) % n];
    for (var j = i + 1; j < n; j++) {
      // Skip the same side and its neighbours.
      if (j == i || (j + 1) % n == i || (i + 1) % n == j) continue;
      final b1 = pts[j], b2 = pts[(j + 1) % n];
      if (_segmentsCross(a1, a2, b1, b2)) return true;
    }
  }
  return false;
}

double _orient(GeoPoint a, GeoPoint b, GeoPoint c) =>
    (b.lng - a.lng) * (c.lat - a.lat) - (b.lat - a.lat) * (c.lng - a.lng);

bool _segmentsCross(GeoPoint p1, GeoPoint p2, GeoPoint p3, GeoPoint p4) {
  final d1 = _orient(p3, p4, p1), d2 = _orient(p3, p4, p2);
  final d3 = _orient(p1, p2, p3), d4 = _orient(p1, p2, p4);
  return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
}

const pointsSameSpotMessage =
    'The points are all in the same spot, so they do not mark an area. Spread them around the edge of what was worked.';
const pointsOutsideMessage =
    "The area you marked is outside this block. Check that the points are inside the block's boundary.";

/// Area (square metres) of the shape drawn by [pts], on a flat local frame.
double areaSquareMetres(List<GeoPoint> pts) {
  if (pts.length < 3) return 0;
  const mPerDegLat = 110574.0;
  final o = pts.first;
  final mPerDegLng = 111320.0 * _cosDeg(o.lat);
  final xy = [for (final p in pts) ((p.lng - o.lng) * mPerDegLng, (p.lat - o.lat) * mPerDegLat)];
  var s = 0.0;
  for (var i = 0; i < xy.length; i++) {
    final a = xy[i], b = xy[(i + 1) % xy.length];
    s += a.$1 * b.$2 - b.$1 * a.$2;
  }
  return s.abs() / 2;
}

double _cosDeg(double d) => _cos(d * 3.141592653589793 / 180);
double _cos(double r) {
  // Taylor series is plenty accurate for latitudes on a farm and avoids another import.
  final x = r * r;
  return 1 - x / 2 + x * x / 24 - x * x * x / 720 + x * x * x * x / 40320;
}

bool _inside(GeoPoint p, List<GeoPoint> ring) {
  var inside = false;
  for (var i = 0, j = ring.length - 1; i < ring.length; j = i++) {
    final a = ring[i], b = ring[j];
    if ((a.lat > p.lat) != (b.lat > p.lat) && p.lng < (b.lng - a.lng) * (p.lat - a.lat) / (b.lat - a.lat) + a.lng) {
      inside = !inside;
    }
  }
  return inside;
}

/// Why these points cannot be sent as a worked area, in the server's own words; null when they are fine.
/// [blockRings] is the block's outline (empty when it has none, which skips the "outside the block" check).
String? areaPointsProblem(List<GeoPoint> pts, {List<List<GeoPoint>> blockRings = const []}) {
  if (pts.length < 3) return null; // too few points is already shown as a count
  // Crossing first: a figure of eight has no net area, but "cross" is the more useful thing to tell the user.
  if (pointsCross(pts)) return pointsCrossMessage;
  if (areaSquareMetres(pts) < 1) return pointsSameSpotMessage;
  if (blockRings.isNotEmpty) {
    final outer = blockRings.first;
    if (outer.length >= 3) {
      final anyPointInside = pts.any((p) => _inside(p, outer));
      final blockInsideWorked = outer.any((p) => _inside(p, pts));
      var edgesTouch = false;
      for (var i = 0; i < pts.length && !edgesTouch; i++) {
        for (var j = 0; j < outer.length; j++) {
          if (_segmentsCross(pts[i], pts[(i + 1) % pts.length], outer[j], outer[(j + 1) % outer.length])) {
            edgesTouch = true;
            break;
          }
        }
      }
      if (!anyPointInside && !blockInsideWorked && !edgesTouch) return pointsOutsideMessage;
    }
  }
  return null;
}
