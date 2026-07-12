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

// Ordinals mirror the backend's `SectionStatus` enum (Active, Idle, HarvestSoon).
enum SectionStatus { active, idle, harvestSoon }

extension SectionStatusExt on SectionStatus {
  String get label {
    switch (this) {
      case SectionStatus.active:      return 'Active';
      case SectionStatus.idle:        return 'Idle';
      case SectionStatus.harvestSoon: return 'Harvest Soon';
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
  /// crops/sections isn't pinned down as tightly as the top-level Farm DTO.
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
  final int sectionCount;
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
    this.sectionCount = 0,
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
    sectionCount: (j['sectionCount'] as num?)?.toInt() ?? 0,
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
    'sectionCount': sectionCount, 'workerCount': workerCount,
  };
}

// ── SECTION ──

class SectionCrop {
  final CropType cropType;
  final double? hectares;
  final double percentage;

  const SectionCrop({required this.cropType, this.hectares, required this.percentage});

  static CropType _parseCropType(dynamic v) {
    final i = (v as num?)?.toInt() ?? CropType.mixed.index;
    if (i < 0 || i >= CropType.values.length) return CropType.mixed;
    return CropType.values[i];
  }

  factory SectionCrop.fromJson(Map<String, dynamic> j) => SectionCrop(
    cropType: _parseCropType(j['cropType']),
    hectares: j['area'] == null ? null : AreaValue.fromDynamic(j['area']).hectares,
    percentage: (j['percentage'] as num?)?.toDouble() ?? 0,
  );

  Map<String, dynamic> toJson() => {
    'cropType': cropType.index, 'hectares': hectares, 'percentage': percentage,
  };
}

class Section {
  final String id;
  final String farmId;
  final String name;
  final List<SectionCrop> crops;
  final double hectares;
  final SectionStatus status;
  final String? lastActivityDate;
  final int blockCount;
  final String? boundaryGeoJson;
  final double? centroidLat;
  final double? centroidLng;

  const Section({
    required this.id,
    required this.farmId,
    required this.name,
    required this.crops,
    required this.hectares,
    required this.status,
    this.lastActivityDate,
    required this.blockCount,
    this.boundaryGeoJson,
    this.centroidLat,
    this.centroidLng,
  });

  /// Back-compat accessor for older UI code written against a single-crop
  /// prototype model — surfaces the dominant (first) crop.
  CropType get crop => crops.isNotEmpty ? crops.first.cropType : CropType.mixed;

  bool get hasBoundary => boundaryGeoJson != null && boundaryGeoJson!.isNotEmpty;

  Section copyWith({SectionStatus? status, String? lastActivityDate}) => Section(
    id: id, farmId: farmId, name: name, crops: crops,
    hectares: hectares, blockCount: blockCount,
    boundaryGeoJson: boundaryGeoJson, centroidLat: centroidLat, centroidLng: centroidLng,
    status: status ?? this.status,
    lastActivityDate: lastActivityDate ?? this.lastActivityDate,
  );

  static SectionStatus _parseStatus(dynamic v) {
    if (v is int && v >= 0 && v < SectionStatus.values.length) return SectionStatus.values[v];
    if (v is String) {
      for (final s in SectionStatus.values) {
        if (s.name.toLowerCase() == v.toLowerCase()) return s;
      }
    }
    return SectionStatus.active;
  }

  /// Hydrates from `SectionDto` as returned by
  /// `GET /api/farms/{farmId}/sections` / `POST /api/farms/{farmId}/sections`.
  factory Section.fromApiJson(Map<String, dynamic> j) {
    final cropsJson = (j['crops'] as List?) ?? const [];
    final crops = cropsJson.map((c) => SectionCrop.fromJson(c as Map<String, dynamic>)).toList();
    final blocksJson = (j['blocks'] as List?) ?? const [];
    return Section(
      id: j['id'] as String,
      farmId: j['farmId'] as String,
      name: j['name'] as String,
      crops: crops.isEmpty ? const [SectionCrop(cropType: CropType.mixed, percentage: 100)] : crops,
      hectares: AreaValue.fromDynamic(j['area']).hectares,
      status: _parseStatus(j['status']),
      lastActivityDate: j['lastActivityDate']?.toString(),
      blockCount: blocksJson.length,
      boundaryGeoJson: j['boundaryGeoJson'] as String?,
      centroidLat: (j['centroidLat'] as num?)?.toDouble(),
      centroidLng: (j['centroidLng'] as num?)?.toDouble(),
    );
  }

  Map<String, dynamic> toMap() => {
    'id': id, 'farm_id': farmId, 'name': name,
    'crop': crop.index, 'hectares': hectares,
    'status': status.index, 'last_activity_date': lastActivityDate,
    'block_count': blockCount,
    'crops_json': jsonEncode(crops.map((c) => c.toJson()).toList()),
    'boundary_geojson': boundaryGeoJson,
    'centroid_lat': centroidLat,
    'centroid_lng': centroidLng,
  };

  factory Section.fromMap(Map<String, dynamic> m) {
    List<SectionCrop> crops = [];
    final cj = m['crops_json'] as String?;
    if (cj != null && cj.isNotEmpty) {
      try {
        crops = (jsonDecode(cj) as List)
            .map((e) => SectionCrop.fromJson(e as Map<String, dynamic>))
            .toList();
      } catch (_) {
        crops = [];
      }
    }
    if (crops.isEmpty) {
      final idx = (m['crop'] as int?) ?? CropType.mixed.index;
      crops = [SectionCrop(cropType: CropType.values[idx.clamp(0, CropType.values.length - 1)], percentage: 100)];
    }
    return Section(
      id: m['id'] as String,
      farmId: m['farm_id'] as String,
      name: m['name'] as String,
      crops: crops,
      hectares: (m['hectares'] as num).toDouble(),
      status: SectionStatus.values[((m['status'] as int?) ?? 0).clamp(0, SectionStatus.values.length - 1)],
      lastActivityDate: m['last_activity_date'] as String?,
      blockCount: (m['block_count'] as int?) ?? 0,
      boundaryGeoJson: m['boundary_geojson'] as String?,
      centroidLat: (m['centroid_lat'] as num?)?.toDouble(),
      centroidLng: (m['centroid_lng'] as num?)?.toDouble(),
    );
  }
}

class Block {
  final String id;
  final String sectionId;
  final String name;
  final double hectares;
  final int? treeCount;

  const Block({
    required this.id,
    required this.sectionId,
    required this.name,
    required this.hectares,
    this.treeCount,
  });

  /// Hydrates from `BlockDto` (`{id, sectionId, name, area, treeCount}`).
  factory Block.fromApiJson(Map<String, dynamic> j) => Block(
    id: j['id'] as String,
    sectionId: j['sectionId'] as String,
    name: j['name'] as String,
    hectares: AreaValue.fromDynamic(j['area']).hectares,
    treeCount: (j['treeCount'] as num?)?.toInt(),
  );
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

class InputUsed {
  final String item;
  final double quantity;
  final String unit;

  const InputUsed({required this.item, required this.quantity, required this.unit});

  Map<String, dynamic> toMap() => {'item': item, 'quantity': quantity, 'unit': unit};

  factory InputUsed.fromMap(Map<String, dynamic> m) => InputUsed(
    item: m['item'], quantity: (m['quantity'] as num).toDouble(), unit: m['unit'],
  );

  @override
  String toString() => '${quantity.toStringAsFixed(0)}$unit $item';
}

class ActivityLog {
  final String id;
  final String farmId;
  final String sectionId;
  final String sectionName;
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
    required this.sectionId,
    required this.sectionName,
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

  Map<String, dynamic> toMap() => {
    'id': id,
    'farm_id': farmId,
    'section_id': sectionId,
    'section_name': sectionName,
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
  final String sectionId;
  final String farmId;
  final List<GeoPoint> points;
  final SyncStatus syncStatus;
  final DateTime createdAt;
  final DateTime? syncedAt;

  const BoundarySubmission({
    required this.id,
    required this.sectionId,
    required this.farmId,
    required this.points,
    this.syncStatus = SyncStatus.pending,
    required this.createdAt,
    this.syncedAt,
  });

  Map<String, dynamic> toMap() => {
    'id': id,
    'section_id': sectionId,
    'farm_id': farmId,
    'points_json': jsonEncode(points.map((p) => p.toJson()).toList()),
    'sync_status': syncStatus.index,
    'created_at': createdAt.toIso8601String(),
    'synced_at': syncedAt?.toIso8601String(),
  };

  factory BoundarySubmission.fromMap(Map<String, dynamic> m) => BoundarySubmission(
    id: m['id'] as String,
    sectionId: m['section_id'] as String,
    farmId: m['farm_id'] as String,
    points: (jsonDecode(m['points_json'] as String) as List)
        .map((e) => GeoPoint.fromJson(e as Map<String, dynamic>))
        .toList(),
    syncStatus: SyncStatus.values[(m['sync_status'] as int?) ?? 0],
    createdAt: DateTime.parse(m['created_at'] as String),
    syncedAt: m['synced_at'] != null ? DateTime.parse(m['synced_at'] as String) : null,
  );
}
