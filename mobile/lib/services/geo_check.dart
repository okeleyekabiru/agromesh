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
