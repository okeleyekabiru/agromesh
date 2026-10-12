import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:agromesh_mobile/models/models.dart';
import 'package:agromesh_mobile/widgets/geo_sketch.dart';

void main() {
  test('polygon and multipolygon GeoJSON rings are read', () {
    const poly = '{"type":"Polygon","coordinates":[[[3.9,7.4],[3.901,7.4],[3.901,7.401],[3.9,7.4]]]}';
    expect(geoJsonRings(poly).length, 1);
    expect(geoJsonRings(poly).first.first.lat, 7.4);

    const multi = '{"type":"MultiPolygon","coordinates":[[[[3.9,7.4],[3.9,7.5],[4.0,7.5],[3.9,7.4]]],[[[5,5],[5,6],[6,6],[5,5]]]]}';
    expect(geoJsonRings(multi).length, 2);
  });

  test('unreadable GeoJSON gives no rings instead of throwing', () {
    expect(geoJsonRings(null), isEmpty);
    expect(geoJsonRings(''), isEmpty);
    expect(geoJsonRings('not json'), isEmpty);
    expect(geoJsonRings('{"type":"Point","coordinates":[1,2]}'), isEmpty);
  });

  test('an activity keeps the points of the area worked for the offline outbox', () {
    final log = ActivityLog(
      id: 'a1', farmId: 'f', blockId: 'b', blockName: 'B', activityType: ActivityType.weeding,
      date: DateTime(2026, 10, 12), workers: const [], inputs: const [], supervisorId: 's', supervisorName: 'S',
      createdAt: DateTime(2026, 10, 12),
      areaPoints: const [GeoPoint(lat: 7.4, lng: 3.9), GeoPoint(lat: 7.401, lng: 3.9)],
    );
    final stored = jsonDecode(log.toMap()['area_points_json'] as String) as List;
    expect(stored.length, 2);
    expect((stored.first as Map)['lat'], 7.4);

    final without = ActivityLog(
      id: 'a2', farmId: 'f', blockId: 'b', blockName: 'B', activityType: ActivityType.maintenance,
      date: DateTime(2026, 10, 12), workers: const [], inputs: const [], supervisorId: 's', supervisorName: 'S',
      createdAt: DateTime(2026, 10, 12),
    );
    expect(without.toMap()['area_points_json'], isNull);
  });

  test('coverage response is read', () {
    final c = BlockCoverage.fromApiJson({
      'status': 'InProgress', 'hasBoundary': true, 'blockHectares': 1.0, 'coveredHectares': 0.5,
      'remainingHectares': 0.5, 'percent': 50.0, 'activityCount': 1,
      'cycleStartedOn': '2026-10-10T08:00:00Z', 'lastWorkedOn': '2026-10-10T08:00:00Z',
      'coveredGeoJson': null, 'remainingGeoJson': null, 'blockGeoJson': null,
    });
    expect(c.percent, 50);
    expect(c.status, 'InProgress');
    expect(c.remainingHectares, 0.5);
  });
}
