import 'package:flutter_test/flutter_test.dart';
import 'package:agromesh_mobile/models/models.dart';
import 'package:agromesh_mobile/services/geo_check.dart';

GeoPoint p(double lat, double lng) => GeoPoint(lat: lat, lng: lng);

void main() {
  test('a square in order does not cross', () {
    expect(pointsCross([p(0, 0), p(0, 1), p(1, 1), p(1, 0)]), isFalse);
  });
  test('a figure of eight crosses', () {
    expect(pointsCross([p(0, 0), p(1, 1), p(1, 0), p(0, 1)]), isTrue);
  });
  test('a longer ring in order does not cross', () {
    expect(pointsCross([p(0, 0), p(0, 1), p(0, 2), p(1, 2), p(2, 2), p(2, 1), p(2, 0), p(1, 0)]), isFalse);
  });
  test('walking back over an edge crosses', () {
    expect(pointsCross([p(0, 0), p(0, 2), p(2, 2), p(2, 0), p(-1, 1), p(1, -1)]), isTrue);
  });

  group('areaPointsProblem', () {
    final block = [[p(7.0, 3.0), p(7.0, 3.01), p(7.01, 3.01), p(7.01, 3.0)]];

    test('points all in one spot', () {
      final one = p(7.005, 3.005);
      expect(areaPointsProblem([one, one, one, one], blockRings: block), pointsSameSpotMessage);
    });
    test('points crossing', () {
      expect(areaPointsProblem([p(7.002, 3.002), p(7.008, 3.008), p(7.008, 3.002), p(7.002, 3.008)], blockRings: block), pointsCrossMessage);
    });
    test('area outside the block', () {
      expect(areaPointsProblem([p(8.0, 4.0), p(8.0, 4.01), p(8.01, 4.01), p(8.01, 4.0)], blockRings: block), pointsOutsideMessage);
    });
    test('a good area inside the block', () {
      expect(areaPointsProblem([p(7.002, 3.002), p(7.002, 3.008), p(7.008, 3.008), p(7.008, 3.002)], blockRings: block), isNull);
    });
    test('an area bigger than the block that covers it is fine', () {
      expect(areaPointsProblem([p(6.99, 2.99), p(6.99, 3.02), p(7.02, 3.02), p(7.02, 2.99)], blockRings: block), isNull);
    });
  });
}
