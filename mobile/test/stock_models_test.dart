import 'package:flutter_test/flutter_test.dart';
import 'package:agromesh_mobile/models/models.dart';
import 'package:agromesh_mobile/stores/app_store.dart';

void main() {
  weatherTests();
  cycleTests();
  group('formatQty', () {
    test('trims trailing zeros but keeps real decimals', () {
      expect(formatQty(12), '12');
      expect(formatQty(12.0), '12');
      expect(formatQty(2.5), '2.5');
      expect(formatQty(0.125), '0.125');
      expect(formatQty(100), '100');
      expect(formatQty(0), '0');
      expect(formatQty(50.5), '50.5');
    });
  });

  group('InputUsed', () {
    test('round-trips the stock item id and prints a readable label', () {
      const input = InputUsed(inventoryItemId: 'item-1', item: 'NPK Fertilizer', quantity: 38, unit: 'kg');
      final copy = InputUsed.fromMap(input.toMap());
      expect(copy.inventoryItemId, 'item-1');
      expect(copy.quantity, 38);
      expect(input.toString(), '38 kg NPK Fertilizer');
    });

    test('rows saved before inputs were stock-based have no item id', () {
      final old = InputUsed.fromMap({'item': 'Salt', 'quantity': 1, 'unit': 'kg'});
      expect(old.inventoryItemId, '');
    });
  });

  group('Worker.fromApiJson', () {
    Map<String, dynamic> dto({num rate = 5000, int freq = 0, int type = 0}) => {
      'id': 'w1', 'farmId': 'f1', 'name': 'Ade Bello', 'initials': 'AB',
      'roleTitle': 'Harvester', 'payRate': rate, 'payFrequency': freq,
      'type': type, 'active': true,
    };

    test('daily pay is used as is', () {
      final w = Worker.fromApiJson(dto());
      expect(w.role, 'Harvester');
      expect(w.type, WorkerType.employee);
      expect(w.dailyRate, 5000);
    });

    test('weekly and monthly pay convert to a daily equivalent (6-day week, 26-day month)', () {
      expect(Worker.fromApiJson(dto(rate: 6000, freq: 1)).dailyRate, 1000);
      expect(Worker.fromApiJson(dto(rate: 26000, freq: 2)).dailyRate, 1000);
    });

    test('contractor type maps from its ordinal', () {
      expect(Worker.fromApiJson(dto(type: 1)).type, WorkerType.contractor);
    });
  });

  group('stock models', () {
    test('StockItem and OutstandingStock survive the JSON cache round-trip', () {
      const item = StockItem(id: 'i1', name: 'NPK', unit: 'kg', currentStock: 162.5);
      final item2 = StockItem.fromApiJson(item.toJson());
      expect(item2.currentStock, 162.5);

      const out = OutstandingStock(
        pickId: 'p1', inventoryItemId: 'i1', inventoryItemName: 'NPK', unit: 'kg',
        blockId: 'b1', quantityPicked: 50, quantityOutstanding: 12,
      );
      final out2 = OutstandingStock.fromApiJson(out.toJson());
      expect(out2.quantityOutstanding, 12);
      expect(out.copyWith(quantityOutstanding: 4).quantityOutstanding, 4);
    });
  });

  group('AppStore stock helpers', () {
    AppStore store() {
      final s = AppStore();
      s.stockItems = const [StockItem(id: 'i1', name: 'NPK', unit: 'kg', currentStock: 150)];
      s.outstanding = const [
        OutstandingStock(pickId: 'p1', inventoryItemId: 'i1', inventoryItemName: 'NPK', unit: 'kg',
            blockId: 'A', quantityPicked: 50, quantityOutstanding: 12),
        OutstandingStock(pickId: 'p2', inventoryItemId: 'i1', inventoryItemName: 'NPK', unit: 'kg',
            blockId: 'B', quantityPicked: 20, quantityOutstanding: 20),
      ];
      return s;
    }

    test('pickedFor only counts stock picked out for that block', () {
      expect(store().pickedFor('i1', 'A'), 12);
      expect(store().pickedFor('i1', 'B'), 20);
      expect(store().pickedFor('i1', 'C'), 0);
    });

    test('availableFor = picked for the block + what is in the store', () {
      expect(store().availableFor('i1', 'A'), 162);
      expect(store().availableFor('i1', 'C'), 150);
      expect(store().availableFor('unknown', 'A'), 0);
    });
  });
}

void weatherTests() {
  group('WeatherInfo', () {
    test('parses the API shape and survives the cache round-trip', () {
      final info = WeatherInfo.fromApiJson({
        'location': 'Surulere, Lagos', 'currentSeason': 'Late Rains', 'rainfallLast30Days': 120.4,
        'nextDryWindowStart': '2026-10-14T00:00:00', 'nextDryWindowEnd': null,
        'forecast': [
          {'date': '2026-10-11T00:00:00', 'precipitationMm': 2.2, 'rainChance': 83, 'tempMaxC': 30.7,
           'tempMinC': 25.1, 'windKmh': 10.6, 'weatherCode': 55, 'goodSprayDay': false},
        ],
      });
      final copy = WeatherInfo.fromApiJson(info.toJson());
      expect(copy.location, 'Surulere, Lagos');
      expect(copy.forecast.single.rainChance, 83);
      expect(copy.forecast.single.icon, '🌦️');
      expect(copy.nextDryWindowEnd, isNull);
    });
  });
}

void cycleTests() {
  group('WorkCycle', () {
    test('parses the API shape and explains itself', () {
      final w = BlockWork.fromApiJson({
        'nextWeedingPrediction': {
          'status': 'DueSoon', 'daysUntilDue': 5, 'reasoning': 'Weeding due in 5 days',
          'factors': ['Late Rains: 249 mm'], 'recommendedSprayDate': null,
        },
        'nextSprayingPrediction': {'status': 'NotNeeded', 'daysUntilDue': 120, 'reasoning': 'x', 'factors': []},
        'nextFertilizingPrediction': {'status': 'OnTrack', 'daysUntilDue': 30, 'reasoning': 'Fertilizer due in 4 weeks', 'factors': []},
      });
      expect(w.fertilizing!.shortText, 'in 4 wks');
      expect(BlockWork.fromJson(w.toJson()).fertilizing!.daysUntilDue, 30);
      expect(w.weeding!.shortText, 'in 5d');
      expect(w.weeding!.needsAttention, isTrue);
      expect(w.spraying!.shortText, 'Not needed (dry)');
      expect(w.spraying!.needsAttention, isFalse);
      expect(BlockWork.fromJson(w.toJson()).weeding!.factors, ['Late Rains: 249 mm']);
    });

    test('overdue and far-off wording', () {
      const overdue = WorkCycle(status: 'Overdue', daysUntilDue: -3, headline: '');
      const far = WorkCycle(status: 'OnTrack', daysUntilDue: 28, headline: '');
      expect(overdue.shortText, 'Overdue 3d');
      expect(far.shortText, 'in 4 wks');
    });
  });
}
