import 'dart:convert';

// ── ENUMS ──

// Ordinals mirror the backend's `CropType` enum exactly (OilPalm, Maize, Mixed,
// Cocoa, Cassava) since the API serializes enums as raw integers.
enum CropType { oilPalm, maize, mixed, cocoa, cassava }

extension CropTypeExt on CropType {
  String get label {
    switch (this) {
      case CropType.oilPalm: return 'Oil Palm';
      case CropType.maize:   return 'Maize';
      case CropType.mixed:   return 'Mixed';
      case CropType.cocoa:   return 'Cocoa';
      case CropType.cassava: return 'Cassava';
    }
  }
}

// Ordinals mirror the backend's `BlockStatus` enum (Active, Idle, HarvestSoon).
enum BlockStatus { active, idle, harvestSoon }

extension BlockStatusExt on BlockStatus {
  String get label {
    switch (this) {
      case BlockStatus.active:      return 'Active';
      case BlockStatus.idle:        return 'Idle';
      case BlockStatus.harvestSoon: return 'Harvest Soon';
    }
  }
}

// Ordinals mirror the backend's `FarmSetupStatus` enum (Draft, Configured).
enum FarmSetupStatus { draft, configured }

enum ActivityType {
  weeding,
  spraying,
  fertilizerApplication,
  maintenance,
  harvesting,
  clearing,
  pruning,
  pestControl,
}

extension ActivityTypeExt on ActivityType {
  String get label {
    switch (this) {
      case ActivityType.weeding:              return 'Weeding';
      case ActivityType.spraying:             return 'Spraying';
      case ActivityType.fertilizerApplication:return 'Fertilizer Application';
      case ActivityType.maintenance:          return 'Maintenance';
      case ActivityType.harvesting:           return 'Harvesting';
      case ActivityType.clearing:             return 'Clearing';
      case ActivityType.pruning:              return 'Pruning';
      case ActivityType.pestControl:          return 'Pest Control';
    }
  }

  String get icon {
    switch (this) {
      case ActivityType.weeding:              return '🌿';
      case ActivityType.spraying:             return '💧';
      case ActivityType.fertilizerApplication:return '🌱';
      case ActivityType.maintenance:          return '🔧';
      case ActivityType.harvesting:           return '🌾';
      case ActivityType.clearing:             return '🪚';
      case ActivityType.pruning:              return '✂️';
      case ActivityType.pestControl:          return '🛡️';
    }
  }
}

enum WorkerType { employee, contractor }

extension WorkerTypeExt on WorkerType {
  String get label => this == WorkerType.employee ? 'Employee' : 'Contractor';
}

enum SyncStatus { synced, pending, failed }

// ── AUTH / SESSION MODELS ──

class AppUser {
  final String id;
  final String email;
  final String fullName;
  final String? companyId;

  const AppUser({
    required this.id,
    required this.email,
    required this.fullName,
    this.companyId,
  });

  factory AppUser.fromJson(Map<String, dynamic> j) => AppUser(
    id: j['id'] as String,
    email: j['email'] as String,
    fullName: j['fullName'] as String? ?? '',
    companyId: j['companyId'] as String?,
  );

  Map<String, dynamic> toJson() => {
    'id': id, 'email': email, 'fullName': fullName, 'companyId': companyId,
  };
}

// ── AREA ──

class AreaValue {
  final double hectares;
  final double acres;
  final String displayValue;
  final String displayUnit;

  const AreaValue({
    required this.hectares,
    required this.acres,
    required this.displayValue,
    required this.displayUnit,
  });

  static const zero = AreaValue(hectares: 0, acres: 0, displayValue: '0', displayUnit: 'ha');

  factory AreaValue.fromJson(Map<String, dynamic> j) => AreaValue(
    hectares: (j['hectares'] as num?)?.toDouble() ?? 0,
    acres: (j['acres'] as num?)?.toDouble() ?? 0,
    displayValue: j['displayValue']?.toString() ?? '0',
    displayUnit: j['displayUnit']?.toString() ?? 'ha',
  );

  Map<String, dynamic> toJson() => {
    'hectares': hectares, 'acres': acres, 'displayValue': displayValue, 'displayUnit': displayUnit,
  };

  /// Defensively parses either the full `{hectares, acres, ...}` DTO shape or a
  /// bare numeric hectares value — the exact shape of nested `area` fields on
  /// crops/blocks isn't pinned down as tightly as the top-level Farm DTO.
  static AreaValue fromDynamic(dynamic v) {
    if (v is Map<String, dynamic>) return AreaValue.fromJson(v);
    if (v is num) return AreaValue(hectares: v.toDouble(), acres: 0, displayValue: v.toString(), displayUnit: 'ha');
    return AreaValue.zero;
  }
}

// ── FARM ──

class Farm {
  final String id;
  final String name;
  final String location;
  final AreaValue area;
  final String? ownerId;
  final String? companyId;
  final String? countryCode;
  final String? stateId;
  final String? lgaFreetext;
  final FarmSetupStatus setupStatus;
  final int preferredAreaUnit;
  final double? centroidLat;
  final double? centroidLng;
  final int blockCount;
  final int workerCount;

  const Farm({
    required this.id,
    required this.name,
    required this.location,
    required this.area,
    this.ownerId,
    this.companyId,
    this.countryCode,
    this.stateId,
    this.lgaFreetext,
    required this.setupStatus,
    this.preferredAreaUnit = 0,
    this.centroidLat,
    this.centroidLng,
    this.blockCount = 0,
    this.workerCount = 0,
  });

  double get totalHectares => area.hectares;

  static FarmSetupStatus _parseSetupStatus(dynamic v) {
    if (v is int) return v == 1 ? FarmSetupStatus.configured : FarmSetupStatus.draft;
    if (v is String) return v.toLowerCase() == 'configured' ? FarmSetupStatus.configured : FarmSetupStatus.draft;
    return FarmSetupStatus.draft;
  }

  /// Full shape returned by `GET /api/farms`.
  factory Farm.fromJson(Map<String, dynamic> j) => Farm(
    id: j['id'] as String,
    name: j['name'] as String,
    location: j['location']?.toString() ?? '',
    area: AreaValue.fromDynamic(j['area']),
    ownerId: j['ownerId'] as String?,
    companyId: j['companyId'] as String?,
    countryCode: j['countryCode'] as String?,
    stateId: j['stateId'] as String?,
    lgaFreetext: j['lgaFreetext'] as String?,
    setupStatus: _parseSetupStatus(j['setupStatus']),
    preferredAreaUnit: (j['preferredAreaUnit'] as num?)?.toInt() ?? 0,
    centroidLat: (j['centroidLat'] as num?)?.toDouble(),
    centroidLng: (j['centroidLng'] as num?)?.toDouble(),
    blockCount: (j['blockCount'] as num?)?.toInt() ?? 0,
    workerCount: (j['workerCount'] as num?)?.toInt() ?? 0,
  );

  /// Thin shape returned inside `POST /api/auth/login`'s `farms` array — just
  /// `{id, name, setupStatus}`. The rest gets backfilled later via `GET /api/farms`.
  factory Farm.fromLoginSummary(Map<String, dynamic> j) => Farm(
    id: j['id'] as String,
    name: j['name'] as String,
    location: '',
    area: AreaValue.zero,
    setupStatus: _parseSetupStatus(j['setupStatus']),
  );

  Map<String, dynamic> toJson() => {
    'id': id, 'name': name, 'location': location, 'area': area.toJson(),
    'ownerId': ownerId, 'companyId': companyId, 'countryCode': countryCode,
    'stateId': stateId, 'lgaFreetext': lgaFreetext,
    'setupStatus': setupStatus == FarmSetupStatus.configured ? 1 : 0,
    'preferredAreaUnit': preferredAreaUnit,
    'centroidLat': centroidLat, 'centroidLng': centroidLng,
    'blockCount': blockCount, 'workerCount': workerCount,
  };
}

// ── BLOCK ──

class BlockCrop {
  final CropType cropType;
  final double? hectares;
  final double percentage;

  const BlockCrop({required this.cropType, this.hectares, required this.percentage});

  static CropType _parseCropType(dynamic v) {
    final i = (v as num?)?.toInt() ?? CropType.mixed.index;
    if (i < 0 || i >= CropType.values.length) return CropType.mixed;
    return CropType.values[i];
  }

  factory BlockCrop.fromJson(Map<String, dynamic> j) => BlockCrop(
    cropType: _parseCropType(j['cropType']),
    hectares: j['area'] == null ? null : AreaValue.fromDynamic(j['area']).hectares,
    percentage: (j['percentage'] as num?)?.toDouble() ?? 0,
  );

  Map<String, dynamic> toJson() => {
    'cropType': cropType.index, 'hectares': hectares, 'percentage': percentage,
  };
}

class Block {
  final String id;
  final String farmId;
  final String name;
  final List<BlockCrop> crops;
  final double hectares;
  final BlockStatus status;
  final String? lastActivityDate;
  final int? treeCount;
  final String? boundaryGeoJson;
  final double? centroidLat;
  final double? centroidLng;

  const Block({
    required this.id,
    required this.farmId,
    required this.name,
    required this.crops,
    required this.hectares,
    required this.status,
    this.lastActivityDate,
    this.treeCount,
    this.boundaryGeoJson,
    this.centroidLat,
    this.centroidLng,
  });

  /// Back-compat accessor for older UI code written against a single-crop
  /// prototype model — surfaces the dominant (first) crop.
  CropType get crop => crops.isNotEmpty ? crops.first.cropType : CropType.mixed;

  bool get hasBoundary => boundaryGeoJson != null && boundaryGeoJson!.isNotEmpty;

  Block copyWith({BlockStatus? status, String? lastActivityDate}) => Block(
    id: id, farmId: farmId, name: name, crops: crops,
    hectares: hectares, treeCount: treeCount,
    boundaryGeoJson: boundaryGeoJson, centroidLat: centroidLat, centroidLng: centroidLng,
    status: status ?? this.status,
    lastActivityDate: lastActivityDate ?? this.lastActivityDate,
  );

  static BlockStatus _parseStatus(dynamic v) {
    if (v is int && v >= 0 && v < BlockStatus.values.length) return BlockStatus.values[v];
    if (v is String) {
      for (final s in BlockStatus.values) {
        if (s.name.toLowerCase() == v.toLowerCase()) return s;
      }
    }
    return BlockStatus.active;
  }

  /// Hydrates from `BlockDto` as returned by
  /// `GET /api/farms/{farmId}/blocks` / `POST /api/farms/{farmId}/blocks`.
  factory Block.fromApiJson(Map<String, dynamic> j) {
    final cropsJson = (j['crops'] as List?) ?? const [];
    final crops = cropsJson.map((c) => BlockCrop.fromJson(c as Map<String, dynamic>)).toList();
    return Block(
      id: j['id'] as String,
      farmId: j['farmId'] as String,
      name: j['name'] as String,
      crops: crops.isEmpty ? const [BlockCrop(cropType: CropType.mixed, percentage: 100)] : crops,
      hectares: AreaValue.fromDynamic(j['area']).hectares,
      status: _parseStatus(j['status']),
      lastActivityDate: j['lastActivityDate']?.toString(),
      treeCount: (j['treeCount'] as num?)?.toInt(),
      boundaryGeoJson: j['boundaryGeoJson'] as String?,
      centroidLat: (j['centroidLat'] as num?)?.toDouble(),
      centroidLng: (j['centroidLng'] as num?)?.toDouble(),
    );
  }

  Map<String, dynamic> toMap() => {
    'id': id, 'farm_id': farmId, 'name': name,
    'crop': crop.index, 'hectares': hectares,
    'status': status.index, 'last_activity_date': lastActivityDate,
    'tree_count': treeCount,
    'crops_json': jsonEncode(crops.map((c) => c.toJson()).toList()),
    'boundary_geojson': boundaryGeoJson,
    'centroid_lat': centroidLat,
    'centroid_lng': centroidLng,
  };

  factory Block.fromMap(Map<String, dynamic> m) {
    List<BlockCrop> crops = [];
    final cj = m['crops_json'] as String?;
    if (cj != null && cj.isNotEmpty) {
      try {
        crops = (jsonDecode(cj) as List)
            .map((e) => BlockCrop.fromJson(e as Map<String, dynamic>))
            .toList();
      } catch (_) {
        crops = [];
      }
    }
    if (crops.isEmpty) {
      final idx = (m['crop'] as int?) ?? CropType.mixed.index;
      crops = [BlockCrop(cropType: CropType.values[idx.clamp(0, CropType.values.length - 1)], percentage: 100)];
    }
    return Block(
      id: m['id'] as String,
      farmId: m['farm_id'] as String,
      name: m['name'] as String,
      crops: crops,
      hectares: (m['hectares'] as num).toDouble(),
      status: BlockStatus.values[((m['status'] as int?) ?? 0).clamp(0, BlockStatus.values.length - 1)],
      lastActivityDate: m['last_activity_date'] as String?,
      treeCount: m['tree_count'] as int?,
      boundaryGeoJson: m['boundary_geojson'] as String?,
      centroidLat: (m['centroid_lat'] as num?)?.toDouble(),
      centroidLng: (m['centroid_lng'] as num?)?.toDouble(),
    );
  }
}

class Worker {
  final String id;
  final String farmId;
  final String name;
  final String initials;
  final String role;
  final WorkerType type;
  final bool active;
  final double dailyRate;

  const Worker({
    required this.id,
    required this.farmId,
    required this.name,
    required this.initials,
    required this.role,
    required this.type,
    required this.active,
    required this.dailyRate,
  });

  Map<String, dynamic> toMap() => {
    'id': id, 'farm_id': farmId, 'name': name, 'initials': initials,
    'role': role, 'type': type.index, 'active': active ? 1 : 0,
    'daily_rate': dailyRate,
  };

  factory Worker.fromMap(Map<String, dynamic> m) => Worker(
    id: m['id'], farmId: m['farm_id'], name: m['name'],
    initials: m['initials'], role: m['role'],
    type: WorkerType.values[m['type'] as int],
    active: (m['active'] as int) == 1,
    dailyRate: (m['daily_rate'] as num).toDouble(),
  );

  /// Hydrates from the backend's `WorkerDto` (enums are ordinals; pay is per the role's
  /// frequency: 0=Daily, 1=Weekly, 2=Monthly — converted to a daily equivalent here).
  factory Worker.fromApiJson(Map<String, dynamic> j) {
    final rate = (j['payRate'] as num?)?.toDouble() ?? 0;
    final freq = (j['payFrequency'] as num?)?.toInt() ?? 0;
    final typeIdx = ((j['type'] as num?)?.toInt() ?? 0).clamp(0, WorkerType.values.length - 1);
    return Worker(
      id: j['id'] as String,
      farmId: j['farmId'] as String,
      name: j['name'] as String,
      initials: (j['initials'] as String?) ?? '',
      role: (j['roleTitle'] as String?) ?? '',
      type: WorkerType.values[typeIdx],
      active: (j['active'] as bool?) ?? true,
      dailyRate: freq == 1 ? rate / 6 : freq == 2 ? rate / 26 : rate,
    );
  }
}

class WorkerLog {
  final String workerId;
  final String workerName;
  final WorkerType workerType;
  final double hoursWorked;

  const WorkerLog({
    required this.workerId,
    required this.workerName,
    required this.workerType,
    required this.hoursWorked,
  });

  Map<String, dynamic> toMap() => {
    'worker_id': workerId, 'worker_name': workerName,
    'worker_type': workerType.index, 'hours_worked': hoursWorked,
  };

  factory WorkerLog.fromMap(Map<String, dynamic> m) => WorkerLog(
    workerId: m['worker_id'], workerName: m['worker_name'],
    workerType: WorkerType.values[m['worker_type'] as int],
    hoursWorked: (m['hours_worked'] as num).toDouble(),
  );
}

/// An input used in an activity. Inputs are always real stock items: [inventoryItemId] is what
/// the server deducts stock from; [item]/[unit] are just the display name and unit.
class InputUsed {
  final String inventoryItemId;
  final String item;
  final double quantity;
  final String unit;

  const InputUsed({
    required this.inventoryItemId,
    required this.item,
    required this.quantity,
    required this.unit,
  });

  Map<String, dynamic> toMap() => {
    'inventory_item_id': inventoryItemId, 'item': item, 'quantity': quantity, 'unit': unit,
  };

  factory InputUsed.fromMap(Map<String, dynamic> m) => InputUsed(
    inventoryItemId: (m['inventory_item_id'] as String?) ?? '',
    item: m['item'], quantity: (m['quantity'] as num).toDouble(), unit: m['unit'],
  );

  @override
  String toString() => '${formatQty(quantity)} $unit $item';
}

/// Trims trailing zeros: 12.0 -> "12", 2.5 -> "2.5", 0.125 -> "0.125".
String formatQty(double q) {
  final s = q.toStringAsFixed(3);
  return s.replaceFirst(RegExp(r'\.?0+$'), '');
}

// ── STOCK ──

/// A stock item as cached from `GET /api/farms/{farmId}/inventory`.
class StockItem {
  final String id;
  final String name;
  final String unit;
  final double currentStock;
  final String categoryName;

  const StockItem({required this.id, required this.name, required this.unit, required this.currentStock, this.categoryName = ''});

  factory StockItem.fromApiJson(Map<String, dynamic> j) => StockItem(
    id: j['id'] as String,
    name: j['name'] as String,
    unit: j['unit'] as String,
    currentStock: (j['currentStock'] as num).toDouble(),
    categoryName: j['categoryName'] as String? ?? '',
  );

  Map<String, dynamic> toJson() =>
      {'id': id, 'name': name, 'unit': unit, 'currentStock': currentStock, 'categoryName': categoryName};

  StockItem copyWith({double? currentStock}) => StockItem(
      id: id, name: name, unit: unit, currentStock: currentStock ?? this.currentStock, categoryName: categoryName);

  /// Same rule as the API: chemicals only for spraying / pest control, fertilizer only for
  /// fertilizer application (weeding may use chemicals), and neither for the other activities.
  bool usableIn(ActivityType type) {
    final cat = categoryName.trim().toLowerCase();
    if (cat.isEmpty) return true;
    final chemical = cat == 'chemical';
    final fertilizer = cat == 'fertilizer';
    switch (type) {
      case ActivityType.spraying:
      case ActivityType.pestControl:
        return chemical;
      case ActivityType.fertilizerApplication:
        return fertilizer;
      case ActivityType.weeding:
        return !fertilizer;
      default:
        return !chemical && !fertilizer;
    }
  }
}

/// Stock that was picked out of the store for a block and not yet used or returned
/// (`GET /api/farms/{farmId}/inventory/outstanding`).
class OutstandingStock {
  final String pickId;
  final String inventoryItemId;
  final String inventoryItemName;
  final String unit;
  final String blockId;
  final double quantityPicked;
  final double quantityOutstanding;

  const OutstandingStock({
    required this.pickId,
    required this.inventoryItemId,
    required this.inventoryItemName,
    required this.unit,
    required this.blockId,
    required this.quantityPicked,
    required this.quantityOutstanding,
  });

  factory OutstandingStock.fromApiJson(Map<String, dynamic> j) => OutstandingStock(
    pickId: j['pickId'] as String,
    inventoryItemId: j['inventoryItemId'] as String,
    inventoryItemName: j['inventoryItemName'] as String,
    unit: j['unit'] as String,
    blockId: j['blockId'] as String,
    quantityPicked: (j['quantityPicked'] as num).toDouble(),
    quantityOutstanding: (j['quantityOutstanding'] as num).toDouble(),
  );

  Map<String, dynamic> toJson() => {
    'pickId': pickId, 'inventoryItemId': inventoryItemId, 'inventoryItemName': inventoryItemName,
    'unit': unit, 'blockId': blockId, 'quantityPicked': quantityPicked,
    'quantityOutstanding': quantityOutstanding,
  };

  OutstandingStock copyWith({double? quantityOutstanding}) => OutstandingStock(
    pickId: pickId, inventoryItemId: inventoryItemId, inventoryItemName: inventoryItemName,
    unit: unit, blockId: blockId, quantityPicked: quantityPicked,
    quantityOutstanding: quantityOutstanding ?? this.quantityOutstanding,
  );
}

class ActivityLog {
  final String id;
  final String farmId;
  final String blockId;
  final String blockName;
  final ActivityType activityType;
  final DateTime date;
  final List<WorkerLog> workers;
  final List<InputUsed> inputs;
  final String notes;
  final String supervisorId;
  final String supervisorName;
  final double labourCost;
  final double inputCost;
  final SyncStatus syncStatus;
  final DateTime createdAt;
  final DateTime? syncedAt;

  const ActivityLog({
    required this.id,
    required this.farmId,
    required this.blockId,
    required this.blockName,
    required this.activityType,
    required this.date,
    required this.workers,
    required this.inputs,
    this.notes = '',
    required this.supervisorId,
    required this.supervisorName,
    this.labourCost = 0,
    this.inputCost = 0,
    this.syncStatus = SyncStatus.pending,
    required this.createdAt,
    this.syncedAt,
  });

  double get totalCost => labourCost + inputCost;

  /// Builds a synced activity from `GET /api/farms/{farmId}/activities`. The API only returns
  /// ids, so block and supervisor names are looked up by the caller.
  factory ActivityLog.fromApiJson(
    Map<String, dynamic> j, {
    required String blockName,
    required String supervisorName,
  }) {
    final date = DateTime.parse(j['date'] as String).toLocal();
    return ActivityLog(
      id: j['id'] as String,
      farmId: j['farmId'] as String,
      blockId: j['blockId'] as String,
      blockName: blockName,
      activityType: ActivityType.values[(j['activityType'] as num).toInt()],
      date: date,
      workers: ((j['workers'] as List?) ?? const []).map((w) {
        final m = w as Map<String, dynamic>;
        return WorkerLog(
          workerId: m['workerId'] as String,
          workerName: m['workerName'] as String? ?? '',
          workerType: WorkerType.values[(m['workerType'] as num?)?.toInt() ?? 0],
          hoursWorked: (m['hoursWorked'] as num).toDouble(),
        );
      }).toList(),
      inputs: ((j['inputs'] as List?) ?? const []).map((i) {
        final m = i as Map<String, dynamic>;
        return InputUsed(
          inventoryItemId: '',
          item: m['item'] as String? ?? '',
          quantity: (m['quantity'] as num).toDouble(),
          unit: m['unit'] as String? ?? '',
        );
      }).toList(),
      notes: j['notes'] as String? ?? '',
      supervisorId: j['supervisorId'] as String? ?? '',
      supervisorName: supervisorName,
      labourCost: (j['labourCost'] as num?)?.toDouble() ?? 0,
      inputCost: (j['inputCost'] as num?)?.toDouble() ?? 0,
      syncStatus: SyncStatus.synced,
      createdAt: date,
      syncedAt: DateTime.tryParse(j['syncedAt'] as String? ?? '')?.toLocal() ?? date,
    );
  }

  Map<String, dynamic> toMap() => {
    'id': id,
    'farm_id': farmId,
    'block_id': blockId,
    'block_name': blockName,
    'activity_type': activityType.index,
    'date': date.toIso8601String(),
    'notes': notes,
    'supervisor_id': supervisorId,
    'supervisor_name': supervisorName,
    'labour_cost': labourCost,
    'input_cost': inputCost,
    'sync_status': syncStatus.index,
    'created_at': createdAt.toIso8601String(),
    'synced_at': syncedAt?.toIso8601String(),
  };
}

// ── BOUNDARY CAPTURE ──

class GeoPoint {
  final double lat;
  final double lng;

  const GeoPoint({required this.lat, required this.lng});

  Map<String, dynamic> toJson() => {'lat': lat, 'lng': lng};

  factory GeoPoint.fromJson(Map<String, dynamic> j) => GeoPoint(
    lat: (j['lat'] as num).toDouble(),
    lng: (j['lng'] as num).toDouble(),
  );
}

/// Local outbox row for a GPS corner-capture session. `id` is the client-generated
/// operation id (a uuid.v4 minted once when the user taps "Finish boundary") and is
/// reused verbatim as `operationId` on every sync retry — this is what makes
/// resubmission after a dropped connection idempotent on the server.
class BoundarySubmission {
  final String id;
  final String blockId;
  final String farmId;
  final List<GeoPoint> points;
  final SyncStatus syncStatus;
  final DateTime createdAt;
  final DateTime? syncedAt;

  const BoundarySubmission({
    required this.id,
    required this.blockId,
    required this.farmId,
    required this.points,
    this.syncStatus = SyncStatus.pending,
    required this.createdAt,
    this.syncedAt,
  });

  Map<String, dynamic> toMap() => {
    'id': id,
    'block_id': blockId,
    'farm_id': farmId,
    'points_json': jsonEncode(points.map((p) => p.toJson()).toList()),
    'sync_status': syncStatus.index,
    'created_at': createdAt.toIso8601String(),
    'synced_at': syncedAt?.toIso8601String(),
  };

  factory BoundarySubmission.fromMap(Map<String, dynamic> m) => BoundarySubmission(
    id: m['id'] as String,
    blockId: m['block_id'] as String,
    farmId: m['farm_id'] as String,
    points: (jsonDecode(m['points_json'] as String) as List)
        .map((e) => GeoPoint.fromJson(e as Map<String, dynamic>))
        .toList(),
    syncStatus: SyncStatus.values[(m['sync_status'] as int?) ?? 0],
    createdAt: DateTime.parse(m['created_at'] as String),
    syncedAt: m['synced_at'] != null ? DateTime.parse(m['synced_at'] as String) : null,
  );
}

// ── WEATHER ──

/// One day of `GET /api/farms/{farmId}/weather`'s forecast.
class DailyForecast {
  final DateTime date;
  final double precipitationMm;
  final int rainChance;
  final double tempMaxC;
  final double tempMinC;
  final double windKmh;
  final int weatherCode;
  final bool goodSprayDay;

  const DailyForecast({
    required this.date,
    required this.precipitationMm,
    required this.rainChance,
    required this.tempMaxC,
    required this.tempMinC,
    required this.windKmh,
    required this.weatherCode,
    required this.goodSprayDay,
  });

  factory DailyForecast.fromJson(Map<String, dynamic> j) => DailyForecast(
    date: DateTime.parse(j['date'] as String),
    precipitationMm: (j['precipitationMm'] as num?)?.toDouble() ?? 0,
    rainChance: (j['rainChance'] as num?)?.toInt() ?? 0,
    tempMaxC: (j['tempMaxC'] as num?)?.toDouble() ?? 0,
    tempMinC: (j['tempMinC'] as num?)?.toDouble() ?? 0,
    windKmh: (j['windKmh'] as num?)?.toDouble() ?? 0,
    weatherCode: (j['weatherCode'] as num?)?.toInt() ?? 0,
    goodSprayDay: j['goodSprayDay'] as bool? ?? false,
  );

  Map<String, dynamic> toJson() => {
    'date': date.toIso8601String(),
    'precipitationMm': precipitationMm,
    'rainChance': rainChance,
    'tempMaxC': tempMaxC,
    'tempMinC': tempMinC,
    'windKmh': windKmh,
    'weatherCode': weatherCode,
    'goodSprayDay': goodSprayDay,
  };

  /// WMO weather code (as returned by Open-Meteo) to an emoji.
  String get icon {
    final c = weatherCode;
    if (c == 0) return '☀️';
    if (c <= 2) return '🌤️';
    if (c == 3) return '☁️';
    if (c == 45 || c == 48) return '🌫️';
    if (c >= 51 && c <= 57) return '🌦️';
    if (c >= 61 && c <= 67) return '🌧️';
    if (c >= 80 && c <= 82) return '🌦️';
    if (c >= 95) return '⛈️';
    return '🌥️';
  }
}

class WeatherInfo {
  final String? location;
  final String season;
  final double rainfallLast30Days;
  final DateTime? nextDryWindowStart;
  final DateTime? nextDryWindowEnd;
  final List<DailyForecast> forecast;

  const WeatherInfo({
    this.location,
    required this.season,
    required this.rainfallLast30Days,
    this.nextDryWindowStart,
    this.nextDryWindowEnd,
    required this.forecast,
  });

  factory WeatherInfo.fromApiJson(Map<String, dynamic> j) => WeatherInfo(
    location: j['location'] as String?,
    season: j['currentSeason'] as String? ?? '',
    rainfallLast30Days: (j['rainfallLast30Days'] as num?)?.toDouble() ?? 0,
    nextDryWindowStart: DateTime.tryParse(j['nextDryWindowStart'] as String? ?? ''),
    nextDryWindowEnd: DateTime.tryParse(j['nextDryWindowEnd'] as String? ?? ''),
    forecast: ((j['forecast'] as List?) ?? const [])
        .map((d) => DailyForecast.fromJson(d as Map<String, dynamic>))
        .toList(),
  );

  Map<String, dynamic> toJson() => {
    'location': location,
    'currentSeason': season,
    'rainfallLast30Days': rainfallLast30Days,
    'nextDryWindowStart': nextDryWindowStart?.toIso8601String(),
    'nextDryWindowEnd': nextDryWindowEnd?.toIso8601String(),
    'forecast': forecast.map((d) => d.toJson()).toList(),
  };
}

// ── WORK CYCLE (next weeding / spraying, from the weather) ──

/// When one kind of work is next due on a block (`nextWeedingPrediction` / `nextSprayingPrediction`
/// in `GET /api/farms/{farmId}/blocks`).
class WorkCycle {
  /// NotNeeded | Unknown | OnTrack | DueSoon | Overdue
  final String status;
  final int daysUntilDue;
  final String headline;
  final List<String> factors;
  final DateTime? recommendedSprayDate;

  const WorkCycle({
    required this.status,
    required this.daysUntilDue,
    required this.headline,
    this.factors = const [],
    this.recommendedSprayDate,
  });

  factory WorkCycle.fromApiJson(Map<String, dynamic> j) => WorkCycle(
    status: j['status'] as String? ?? 'Unknown',
    daysUntilDue: (j['daysUntilDue'] as num?)?.toInt() ?? 0,
    headline: j['reasoning'] as String? ?? '',
    factors: ((j['factors'] as List?) ?? const []).map((e) => e.toString()).toList(),
    recommendedSprayDate: DateTime.tryParse(j['recommendedSprayDate'] as String? ?? ''),
  );

  Map<String, dynamic> toJson() => {
    'status': status,
    'daysUntilDue': daysUntilDue,
    'reasoning': headline,
    'factors': factors,
    'recommendedSprayDate': recommendedSprayDate?.toIso8601String(),
  };

  /// Short text for a tile: "in 5 days", "Overdue 3d", "Not needed (dry)".
  String get shortText {
    switch (status) {
      case 'NotNeeded': return 'Not needed (dry)';
      case 'Unknown': return 'No history yet';
      default:
        if (daysUntilDue < 0) return 'Overdue ${-daysUntilDue}d';
        if (daysUntilDue == 0) return 'Today';
        if (daysUntilDue >= 14) return 'in ${(daysUntilDue / 7).round()} wks';
        return 'in ${daysUntilDue}d';
    }
  }

  bool get needsAttention => status == 'Overdue' || (status != 'NotNeeded' && status != 'Unknown' && daysUntilDue <= 7);
}

class BlockWork {
  final WorkCycle? weeding;
  final WorkCycle? spraying;
  final WorkCycle? fertilizing;
  const BlockWork({this.weeding, this.spraying, this.fertilizing});

  factory BlockWork.fromApiJson(Map<String, dynamic> blockJson) => BlockWork(
    weeding: blockJson['nextWeedingPrediction'] == null
        ? null : WorkCycle.fromApiJson(blockJson['nextWeedingPrediction'] as Map<String, dynamic>),
    spraying: blockJson['nextSprayingPrediction'] == null
        ? null : WorkCycle.fromApiJson(blockJson['nextSprayingPrediction'] as Map<String, dynamic>),
    fertilizing: blockJson['nextFertilizingPrediction'] == null
        ? null : WorkCycle.fromApiJson(blockJson['nextFertilizingPrediction'] as Map<String, dynamic>),
  );

  Map<String, dynamic> toJson() =>
      {'weeding': weeding?.toJson(), 'spraying': spraying?.toJson(), 'fertilizing': fertilizing?.toJson()};

  factory BlockWork.fromJson(Map<String, dynamic> j) => BlockWork(
    weeding: j['weeding'] == null ? null : WorkCycle.fromApiJson(j['weeding'] as Map<String, dynamic>),
    spraying: j['spraying'] == null ? null : WorkCycle.fromApiJson(j['spraying'] as Map<String, dynamic>),
    fertilizing: j['fertilizing'] == null ? null : WorkCycle.fromApiJson(j['fertilizing'] as Map<String, dynamic>),
  );
}
