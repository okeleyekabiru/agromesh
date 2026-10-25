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
}
