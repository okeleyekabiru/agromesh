import 'package:flutter_test/flutter_test.dart';
import 'package:ilevora_mobile/models/models.dart';

void main() {
  test('reads cropId and cropName from the API', () {
    final c = BlockCrop.fromJson({'cropId': 'abc', 'cropName': 'Yam', 'cropType': 2, 'percentage': 40});
    expect(c.cropId, 'abc');
    expect(c.cropName, 'Yam');
    expect(c.percentage, 40);
  });

  test('older payloads with only the enum ordinal read as the same crop', () {
    expect(BlockCrop.fromJson({'cropType': 0, 'percentage': 100}).cropId, 'OilPalm');
    expect(BlockCrop.fromJson({'cropType': 4, 'percentage': 100}).cropName, 'Cassava');
    expect(BlockCrop.fromJson({'cropType': 99, 'percentage': 100}).cropId, 'Mixed');
  });

  test('Crop.fromJson', () {
    final c = Crop.fromJson({'id': 'Plantain', 'name': 'Plantain', 'isBuiltIn': true});
    expect(c.isBuiltIn, true);
  });
}
