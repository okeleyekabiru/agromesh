// ── ENUMS ──

enum CropType { oilPalm, maize, mixed }

extension CropTypeExt on CropType {
  String get label {
    switch (this) {
      case CropType.oilPalm: return 'Oil Palm';
      case CropType.maize:   return 'Maize';
      case CropType.mixed:   return 'Mixed';
    }
  }
}

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

// ── MODELS ──

class Farm {
  final String id;
  final String name;
  final String location;
  final double totalHectares;
  final List<CropType> crops;

  const Farm({
    required this.id,
    required this.name,
    required this.location,
    required this.totalHectares,
    required this.crops,
  });
}

class Section {
  final String id;
  final String farmId;
  final String name;
  final CropType crop;
  final double hectares;
  final SectionStatus status;
  final String? lastActivityDate;
  final int blockCount;

  const Section({
    required this.id,
    required this.farmId,
    required this.name,
    required this.crop,
    required this.hectares,
    required this.status,
    this.lastActivityDate,
    required this.blockCount,
  });

  Section copyWith({SectionStatus? status, String? lastActivityDate}) => Section(
    id: id, farmId: farmId, name: name, crop: crop,
    hectares: hectares, blockCount: blockCount,
    status: status ?? this.status,
    lastActivityDate: lastActivityDate ?? this.lastActivityDate,
  );

  Map<String, dynamic> toMap() => {
    'id': id, 'farm_id': farmId, 'name': name,
    'crop': crop.index, 'hectares': hectares,
    'status': status.index, 'last_activity_date': lastActivityDate,
    'block_count': blockCount,
  };

  factory Section.fromMap(Map<String, dynamic> m) => Section(
    id: m['id'], farmId: m['farm_id'], name: m['name'],
    crop: CropType.values[m['crop'] as int],
    hectares: (m['hectares'] as num).toDouble(),
    status: SectionStatus.values[m['status'] as int],
    lastActivityDate: m['last_activity_date'],
    blockCount: m['block_count'] as int,
  );
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
