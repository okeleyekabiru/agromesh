import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart' show Geolocator, LocationAccuracy, LocationPermission;
import '../blocks/map_pick_screen.dart';
import '../../services/api_client.dart' show ApiException;
import '../../services/geo_check.dart';
import 'package:provider/provider.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../models/models.dart';
import '../../services/api_client.dart';
import '../../stores/app_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/geo_sketch.dart';
import '../../widgets/widgets.dart';

class LogActivityScreen extends StatefulWidget {
  const LogActivityScreen({super.key});

  @override
  State<LogActivityScreen> createState() => _LogActivityScreenState();
}

class _LogActivityScreenState extends State<LogActivityScreen> {
  int _step = 0; // index into _steps
  final _formKey = GlobalKey<FormState>();

  // Weeding, spraying, fertilizer and harvest cover part of a block at a time: the supervisor walks the edge
  // of what was worked and taps a point at each corner (8 or more), so the block can show done vs left.
  static const _areaTypes = {
    ActivityType.weeding, ActivityType.spraying, ActivityType.fertilizerApplication, ActivityType.harvesting,
  };
  static const _minAreaPoints = 8;
  final List<GeoPoint> _areaPoints = [];
  bool _capturing = false;
  String? _areaError;

  bool get _needsArea => _activityType != null && _areaTypes.contains(_activityType);
  List<String> get _steps =>
      _needsArea ? ['Location', 'Activity', 'Area', 'Workers & Inputs'] : ['Location', 'Activity', 'Workers & Inputs'];
  String get _stepName => _steps[_step];

  // Form state
  Block? _block;
  ActivityType? _activityType;
  final _notesCtrl = TextEditingController();
  final List<WorkerLog> _workers = [];
  final List<InputUsed> _inputs = [];
  String? _supervisorId;

  // Work already done this cycle on the picked block, drawn under the area picker so a new area does not overlap it.
  BlockCoverage? _existing;

  // Input builders — workers and inputs are picked from real data (the farm's workers and
  // its stock), never typed in: the server only accepts known worker ids and stock items.
  String? _workerPickId;
  final _hoursCtrl = TextEditingController(text: '8');
  String? _stockItemId;
  final _inputQtyCtrl = TextEditingController();
  String? _inputError;

  bool _isSubmitting = false;

  @override
  void dispose() {
    _notesCtrl.dispose();
    _hoursCtrl.dispose();
    _inputQtyCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadExisting() async {
    final block = _block, type = _activityType;
    setState(() => _existing = null);
    if (block == null || type == null || !_areaTypes.contains(type)) return;
    try {
      final json = await ApiClient.get('/api/farms/${block.farmId}/blocks/${block.id}/coverage?activityType=${type.index}');
      if (!mounted || block != _block || type != _activityType) return;
      setState(() => _existing = BlockCoverage.fromApiJson(json as Map<String, dynamic>));
    } catch (_) {
      // Offline: the picker works without the overlay.
    }
  }

  bool get _canProceed {
    if (_step == 0) return _block != null;
    if (_step == 1) return _activityType != null;
    if (_stepName == 'Area') return _areaPoints.length >= _minAreaPoints && areaPointsProblem(_areaPoints, blockRings: geoJsonRings(_block?.boundaryGeoJson)) == null;
    if (_supervisorId == null) return false;
    // Who did the work and what it used are both required.
    if (_workers.isEmpty || _inputs.isEmpty) return false;
    // Spraying / pest control must use a chemical from stock.
    if (_activityType == ActivityType.spraying || _activityType == ActivityType.pestControl) {
      final store = context.read<AppStore>();
      return _inputs.any((inp) {
        final m = store.stockItems.where((s) => s.id == inp.inventoryItemId);
        return m.isNotEmpty && m.first.categoryName.trim().toLowerCase() == 'chemical';
      });
    }
    return true;
  }

  Future<void> _submit() async {
    if (_isSubmitting) return;
    setState(() => _isSubmitting = true);

    final store = context.read<AppStore>();
    final supervisor = store.supervisorOptions.firstWhere((w) => w.id == _supervisorId);

    try {
    await store.logActivity(
      blockId: _block!.id,
      blockName: _block!.name,
      activityType: _activityType!,
      workers: _workers,
      inputs: _inputs,
      supervisorId: supervisor.id,
      supervisorName: supervisor.name,
      notes: _notesCtrl.text,
      areaPoints: _needsArea ? List<GeoPoint>.from(_areaPoints) : const [],
    );
    } on ApiException catch (e) {
      // The server refused it: stay on the form with the reason so it can be fixed.
      if (mounted) {
        setState(() => _isSubmitting = false);
        showDialog<void>(
          context: context,
          builder: (ctx) => AlertDialog(
            title: const Text('The activity was not saved'),
            content: Text(e.message),
            actions: [TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('OK'))],
          ),
        );
      }
      return;
    }

    if (mounted) {
      Navigator.pop(context);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('${_activityType!.label} logged for ${_block!.name}'),
          backgroundColor: AgroColors.leaf,
          behavior: SnackBarBehavior.floating,
        ),
      );
    }
  }

  Future<void> _capturePoint() async {
    setState(() { _capturing = true; _areaError = null; });
    try {
      if (!await Geolocator.isLocationServiceEnabled()) {
        setState(() => _areaError = 'Turn on device location/GPS to mark a point.');
        return;
      }
      var permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) permission = await Geolocator.requestPermission();
      if (permission != LocationPermission.always && permission != LocationPermission.whileInUse) {
        setState(() => _areaError = 'Location permission is needed to mark points.');
        return;
      }
      final position = await Geolocator.getCurrentPosition(
        desiredAccuracy: LocationAccuracy.high,
        timeLimit: const Duration(seconds: 20),
      );
      setState(() => _areaPoints.add(GeoPoint(lat: position.latitude, lng: position.longitude)));
    } catch (_) {
      setState(() => _areaError = 'Could not read your location. Try again in the open.');
    } finally {
      if (mounted) setState(() => _capturing = false);
    }
  }

  /// Usable stock for this block after what's already been added in this form.
  double _availableFor(AppStore store, String itemId) {
    final used = _inputs.where((i) => i.inventoryItemId == itemId).fold(0.0, (sum, i) => sum + i.quantity);
    final left = store.availableFor(itemId, _block?.id ?? '') - used;
    return left < 0 ? 0 : left;
  }

  @override
  Widget build(BuildContext context) {
    final store = context.watch<AppStore>();

    return Scaffold(
      backgroundColor: AgroColors.mist,
      appBar: AppBar(
        title: const Text('Log Activity'),
        leading: IconButton(
          icon: const Icon(Icons.close),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      body: Column(
        children: [
          // Stepper
          _Stepper(currentStep: _step, steps: _steps),

          Expanded(
            child: Form(
              key: _formKey,
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(16),
                child: [
                  _StepLocation(
                    blocks: store.blocks,
                    selectedBlock: _block,
                    onBlockChanged: (s) { setState(() => _block = s); _loadExisting(); },
                  ),
                  _StepActivity(
                    selected: _activityType,
                    notes: _notesCtrl,
                    onSelected: (t) { setState(() {
                      _activityType = t;
                      // Drop picked inputs that don't fit the new activity.
                      _inputs.removeWhere((inp) {
                        final m = store.stockItems.where((s) => s.id == inp.inventoryItemId);
                        return m.isNotEmpty && !m.first.usableIn(t);
                      });
                      final sel = store.stockItems.where((s) => s.id == _stockItemId);
                      if (sel.isNotEmpty && !sel.first.usableIn(t)) _stockItemId = null;
                    }); _loadExisting(); },
                  ),
                  if (_needsArea)
                    _StepArea(
                      block: _block,
                      existing: _existing,
                      points: _areaPoints,
                      capturing: _capturing,
                      error: _areaError,
                      minPoints: _minAreaPoints,
                      onAdd: _capturePoint,
                      onPickMap: () async {
                        final picked = await Navigator.push<List<GeoPoint>>(
                          context,
                          MaterialPageRoute(
                            builder: (_) => MapPickScreen(
                              title: 'Pick the worked area',
                              minPoints: _minAreaPoints,
                              blockGeoJson: _block?.boundaryGeoJson,
                              coveredGeoJson: _existing?.coveredGeoJson,
                              remainingGeoJson: _existing?.remainingGeoJson,
                              initial: List.of(_areaPoints),
                            ),
                          ),
                        );
                        if (picked != null) setState(() { _areaPoints..clear()..addAll(picked); _areaError = null; });
                      },
                      onUndo: () => setState(() { if (_areaPoints.isNotEmpty) _areaPoints.removeLast(); }),
                      onClear: () => setState(_areaPoints.clear),
                    ),
                  _StepWorkersInputs(
                    workers: _workers,
                    inputs: _inputs,
                    availableWorkers: store.workers.where((w) => w.active).toList(),
                    stockItems: store.stockItems.where((i) => _activityType == null || i.usableIn(_activityType!)).toList(),
                    block: _block,
                    // Supervisor workers (everyone when none is one) plus the farm managers.
                    supervisors: store.supervisorOptions,
                    selectedSupervisorId: _supervisorId,
                    workerPickId: _workerPickId,
                    hoursCtrl: _hoursCtrl,
                    stockItemId: _stockItemId,
                    inputQtyCtrl: _inputQtyCtrl,
                    inputError: _inputError,
                    availableFor: (itemId) => _availableFor(store, itemId),
                    pickedFor: (itemId) => store.pickedFor(itemId, _block?.id ?? ''),
                    onWorkerPicked: (id) => setState(() => _workerPickId = id),
                    onStockItemPicked: (id) => setState(() { _stockItemId = id; _inputError = null; }),
                    onSupervisorChanged: (id) => setState(() => _supervisorId = id),
                    onAddWorker: () {
                      final picked = store.workers.where((w) => w.id == _workerPickId);
                      final hours = double.tryParse(_hoursCtrl.text);
                      if (picked.isEmpty || hours == null || hours <= 0) return;
                      final w = picked.first;
                      if (_workers.any((x) => x.workerId == w.id)) return;
                      setState(() {
                        _workers.add(WorkerLog(
                          workerId: w.id,
                          workerName: w.name,
                          workerType: w.type,
                          hoursWorked: hours,
                        ));
                        _workerPickId = null;
                        _hoursCtrl.text = '8';
                      });
                    },
                    onRemoveWorker: (i) => setState(() => _workers.removeAt(i)),
                    onAddInput: () {
                      final matches = store.stockItems.where((i) => i.id == _stockItemId);
                      final qty = double.tryParse(_inputQtyCtrl.text);
                      if (matches.isEmpty || qty == null || qty <= 0) return;
                      final item = matches.first;
                      final available = _availableFor(store, item.id);
                      if (qty > available) {
                        setState(() => _inputError =
                            'Only ${formatQty(available)} ${item.unit} of ${item.name} available.');
                        return;
                      }
                      setState(() {
                        _inputs.add(InputUsed(
                          inventoryItemId: item.id,
                          item: item.name,
                          quantity: qty,
                          unit: item.unit,
                        ));
                        _stockItemId = null;
                        _inputQtyCtrl.clear();
                        _inputError = null;
                      });
                    },
                    onRemoveInput: (i) => setState(() => _inputs.removeAt(i)),
                  ),
                ][_step],
              ),
            ),
          ),

          // Footer navigation
          _Footer(
            step: _step,
            isLast: _step == _steps.length - 1,
            canProceed: _canProceed,
            isSubmitting: _isSubmitting,
            onBack: () {
              if (_step > 0) {
                setState(() => _step--);
              } else {
                Navigator.pop(context);
              }
            },
            onNext: () {
              if (_step < _steps.length - 1) {
                setState(() => _step++);
              } else {
                _submit();
              }
            },
          ),
        ],
      ),
    );
  }
}

// ── STEPPER ──
class _Stepper extends StatelessWidget {
  final int currentStep;
  final List<String> steps;
  const _Stepper({required this.currentStep, required this.steps});

  @override
  Widget build(BuildContext context) {
    return Container(
      color: AgroColors.white,
      padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 24),
      child: Row(
        children: List.generate(steps.length * 2 - 1, (i) {
          if (i.isOdd) {
            return Expanded(child: Container(height: 1, color: AgroColors.fog));
          }
          final stepIndex = i ~/ 2;
          final isActive = stepIndex == currentStep;
          final isDone = stepIndex < currentStep;
          return Column(
            children: [
              Container(
                width: 24, height: 24,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: isDone ? AgroColors.palm : isActive ? AgroColors.leaf : Colors.transparent,
                  border: Border.all(color: isDone || isActive ? Colors.transparent : AgroColors.fog, width: 1.5),
                ),
                child: Center(
                  child: isDone
                    ? const Icon(Icons.check, color: Colors.white, size: 12)
                    : Text('${stepIndex + 1}',
                        style: GoogleFonts.jetBrainsMono(fontSize: 10, color: isActive ? Colors.white : AgroColors.stone)),
                ),
              ),
              const SizedBox(height: 4),
              Text(steps[stepIndex],
                style: GoogleFonts.jetBrainsMono(
                  fontSize: 8, letterSpacing: 0.5,
                  color: isActive ? AgroColors.leaf : AgroColors.stone)),
            ],
          );
        }),
      ),
    );
  }
}

// ── STEP 1: LOCATION ──
class _StepLocation extends StatelessWidget {
  final List<Block> blocks;
  final Block? selectedBlock;
  final void Function(Block?) onBlockChanged;

  const _StepLocation({
    required this.blocks,
    required this.selectedBlock,
    required this.onBlockChanged,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const MonoLabel('Block'),
        const SizedBox(height: 6),
        DropdownButtonFormField<Block>(
          value: selectedBlock,
          hint: Text('Choose block...', style: GoogleFonts.inter(fontSize: 13, color: AgroColors.stone)),
          decoration: const InputDecoration(),
          items: blocks.map((s) => DropdownMenuItem(
            value: s,
            child: Text('${s.name} — ${s.crop.label}', style: GoogleFonts.inter(fontSize: 13)),
          )).toList(),
          onChanged: onBlockChanged,
        ),
        if (selectedBlock != null) ...[
          const SizedBox(height: 4),
          Text('${selectedBlock!.hectares.toStringAsFixed(0)} ha · Last activity: ${selectedBlock!.lastActivityDate ?? 'Never'}',
            style: GoogleFonts.jetBrainsMono(fontSize: 9, color: AgroColors.stone)),
        ],
      ],
    );
  }
}

// ── STEP 2: ACTIVITY ──
class _StepActivity extends StatelessWidget {
  final ActivityType? selected;
  final TextEditingController notes;
  final void Function(ActivityType) onSelected;

  const _StepActivity({required this.selected, required this.notes, required this.onSelected});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const MonoLabel('Activity Type'),
        const SizedBox(height: 10),
        GridView.count(
          crossAxisCount: 2,
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          crossAxisSpacing: 8,
          mainAxisSpacing: 8,
          childAspectRatio: 2.2,
          children: ActivityType.values.map((type) {
            final isSelected = type == selected;
            return GestureDetector(
              onTap: () => onSelected(type),
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                decoration: BoxDecoration(
                  color: isSelected ? AgroColors.leaf.withOpacity(0.06) : Colors.transparent,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(
                    color: isSelected ? AgroColors.leaf : AgroColors.fog,
                    width: isSelected ? 1.5 : 1,
                  ),
                ),
                child: Row(
                  children: [
                    Text(type.icon, style: const TextStyle(fontSize: 18)),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(type.label,
                        style: GoogleFonts.inter(
                          fontSize: 11, fontWeight: FontWeight.w600,
                          color: isSelected ? AgroColors.leaf : AgroColors.forest),
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                  ],
                ),
              ),
            );
          }).toList(),
        ),
        if (selected != null) ...[
          const SizedBox(height: 16),
          const MonoLabel('Notes (optional)'),
          const SizedBox(height: 6),
          TextField(
            controller: notes,
            maxLines: 2,
            style: GoogleFonts.inter(fontSize: 13, color: AgroColors.forest),
            decoration: InputDecoration(
              hintText: 'Any details about this activity...',
              hintStyle: GoogleFonts.inter(fontSize: 13, color: AgroColors.stone),
            ),
          ),
        ],
      ],
    );
  }
}

// ── STEP 3: WORKERS & INPUTS ──
class _StepWorkersInputs extends StatelessWidget {
  final List<WorkerLog> workers;
  final List<InputUsed> inputs;
  final List<Worker> availableWorkers;
  final List<StockItem> stockItems;
  final Block? block;
  final List<Worker> supervisors;
  final String? selectedSupervisorId;
  final String? workerPickId;
  final TextEditingController hoursCtrl;
  final String? stockItemId;
  final TextEditingController inputQtyCtrl;
  final String? inputError;
  final double Function(String itemId) availableFor;
  final double Function(String itemId) pickedFor;
  final void Function(String?) onWorkerPicked;
  final void Function(String?) onStockItemPicked;
  final void Function(String?) onSupervisorChanged;
  final VoidCallback onAddWorker;
  final void Function(int) onRemoveWorker;
  final VoidCallback onAddInput;
  final void Function(int) onRemoveInput;

  const _StepWorkersInputs({
    required this.workers, required this.inputs, required this.availableWorkers,
    required this.stockItems, required this.block, required this.supervisors,
    required this.selectedSupervisorId, required this.workerPickId,
    required this.hoursCtrl, required this.stockItemId, required this.inputQtyCtrl,
    required this.inputError, required this.availableFor, required this.pickedFor,
    required this.onWorkerPicked, required this.onStockItemPicked,
    required this.onSupervisorChanged, required this.onAddWorker,
    required this.onRemoveWorker, required this.onAddInput, required this.onRemoveInput,
  });

  @override
  Widget build(BuildContext context) {
    final selectedStock = stockItems.where((i) => i.id == stockItemId);
    final stock = selectedStock.isEmpty ? null : selectedStock.first;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Workers
        MonoLabel('Workers (${workers.length} added)'),
        const SizedBox(height: 8),
        if (workers.isNotEmpty) ...[
          Wrap(
            spacing: 6, runSpacing: 6,
            children: workers.asMap().entries.map((e) =>
              WorkerChip(
                label: '${e.value.workerName} · ${formatQty(e.value.hoursWorked)}h',
                onRemove: () => onRemoveWorker(e.key),
              )
            ).toList(),
          ),
          const SizedBox(height: 10),
        ],
        if (availableWorkers.isEmpty)
          Text('No workers yet — they sync from the farm when you are online.',
            style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone))
        else
          Row(
            children: [
              Expanded(
                child: DropdownButtonFormField<String>(
                  value: workerPickId,
                  isExpanded: true,
                  hint: Text('Choose worker...', style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone)),
                  decoration: const InputDecoration(),
                  items: availableWorkers.map((w) => DropdownMenuItem(
                    value: w.id,
                    child: Text('${w.name} (${w.type.label})',
                      style: GoogleFonts.inter(fontSize: 13), overflow: TextOverflow.ellipsis),
                  )).toList(),
                  onChanged: onWorkerPicked,
                ),
              ),
              const SizedBox(width: 6),
              SizedBox(
                width: 56,
                child: TextField(
                  controller: hoursCtrl,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  style: GoogleFonts.inter(fontSize: 13, color: AgroColors.forest),
                  decoration: InputDecoration(
                    hintText: 'Hrs',
                    hintStyle: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone),
                  ),
                ),
              ),
              const SizedBox(width: 6),
              ElevatedButton(
                onPressed: onAddWorker,
                style: ElevatedButton.styleFrom(minimumSize: const Size(48, 42), padding: EdgeInsets.zero),
                child: const Icon(Icons.add, size: 18),
              ),
            ],
          ),

        const SizedBox(height: 20),
        const Divider(color: AgroColors.fog),
        const SizedBox(height: 12),

        // Inputs — chosen from stock; using them reduces stock
        MonoLabel('Inputs Used (${inputs.length} added)'),
        const SizedBox(height: 8),
        if (inputs.isNotEmpty) ...[
          Wrap(
            spacing: 6, runSpacing: 6,
            children: inputs.asMap().entries.map((e) =>
              WorkerChip(label: '📦 ${e.value}', onRemove: () => onRemoveInput(e.key))
            ).toList(),
          ),
          const SizedBox(height: 10),
        ],
        if (stockItems.isEmpty)
          Text('No suitable stock for this activity — add stock on the web app, then sync.',
            style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone))
        else ...[
          Row(
            children: [
              Expanded(
                child: DropdownButtonFormField<String>(
                  value: stockItemId,
                  isExpanded: true,
                  hint: Text('Choose from stock...', style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone)),
                  decoration: const InputDecoration(),
                  items: stockItems.map((i) {
                    final left = availableFor(i.id);
                    return DropdownMenuItem(
                      value: i.id,
                      enabled: left > 0,
                      child: Text('${i.name} — ${formatQty(left)} ${i.unit}',
                        style: GoogleFonts.inter(fontSize: 13, color: left > 0 ? AgroColors.forest : AgroColors.stone),
                        overflow: TextOverflow.ellipsis),
                    );
                  }).toList(),
                  onChanged: onStockItemPicked,
                ),
              ),
              const SizedBox(width: 6),
              SizedBox(
                width: 72,
                child: TextField(
                  controller: inputQtyCtrl,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  style: GoogleFonts.inter(fontSize: 13, color: AgroColors.forest),
                  decoration: InputDecoration(
                    hintText: stock == null ? 'Qty' : stock.unit,
                    hintStyle: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone),
                  ),
                ),
              ),
              const SizedBox(width: 6),
              ElevatedButton(
                onPressed: onAddInput,
                style: ElevatedButton.styleFrom(minimumSize: const Size(48, 42), padding: EdgeInsets.zero),
                child: const Icon(Icons.add, size: 18),
              ),
            ],
          ),
          if (stock != null) ...[
            const SizedBox(height: 6),
            Text(
              '${formatQty(pickedFor(stock.id))} ${stock.unit} picked out for ${block?.name ?? 'this block'} · '
              '${formatQty(stock.currentStock)} ${stock.unit} in the store. Picked stock is used first.',
              style: GoogleFonts.jetBrainsMono(fontSize: 9, color: AgroColors.stone),
            ),
          ],
          if (inputError != null) ...[
            const SizedBox(height: 6),
            Text(inputError!, style: GoogleFonts.inter(fontSize: 12, color: AgroColors.alert)),
          ],
        ],

        const SizedBox(height: 20),
        const Divider(color: AgroColors.fog),
        const SizedBox(height: 12),

        // Supervisor
        const MonoLabel('Supervisor'),
        const SizedBox(height: 6),
        DropdownButtonFormField<String>(
          value: selectedSupervisorId,
          hint: Text('Select supervisor...', style: GoogleFonts.inter(fontSize: 13, color: AgroColors.stone)),
          decoration: const InputDecoration(),
          items: supervisors.map((w) => DropdownMenuItem(
            value: w.id,
            child: Text(w.name, style: GoogleFonts.inter(fontSize: 13)),
          )).toList(),
          onChanged: onSupervisorChanged,
        ),

        const SizedBox(height: 80),
      ],
    );
  }
}

// ── AREA (mark what was worked) ──
class _StepArea extends StatelessWidget {
  final Block? block;
  final BlockCoverage? existing;
  final List<GeoPoint> points;
  final bool capturing;
  final String? error;
  final int minPoints;
  final VoidCallback onAdd;
  final VoidCallback onPickMap;
  final VoidCallback onUndo;
  final VoidCallback onClear;

  const _StepArea({
    required this.block, required this.existing, required this.points, required this.capturing, required this.error,
    required this.minPoints, required this.onAdd, required this.onPickMap, required this.onUndo, required this.onClear,
  });

  @override
  Widget build(BuildContext context) {
    final enough = points.length >= minPoints;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        MonoLabel('Area worked — ${points.length} / $minPoints points${enough ? ' ✓' : ''}'),
        const SizedBox(height: 8),
        Text(
          'Walk around the edge of the area that was worked and tap Add point at each corner, in order. At least $minPoints points.',
          style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone),
        ),
        const SizedBox(height: 12),
        GeoSketch(
          block: geoJsonRings(block?.boundaryGeoJson),
          covered: geoJsonRings(existing?.coveredGeoJson),
          remaining: geoJsonRings(existing?.remainingGeoJson),
          marking: points,
        ),
        if (existing != null && existing!.status != 'NotStarted')
          Padding(
            padding: const EdgeInsets.only(top: 8),
            child: Text(
              existing!.status == 'Complete'
                  ? 'The whole block is already covered this cycle: this will start a new cycle.'
                  : 'Green is already done this cycle (${existing!.coveredHectares.toStringAsFixed(2)} ha). Mark only the amber part so the work does not overlap.',
              style: GoogleFonts.inter(fontSize: 11.5, color: AgroColors.forest),
            ),
          ),
        if (block != null && !block!.hasBoundary)
          Padding(
            padding: const EdgeInsets.only(top: 8),
            child: Text('This block has no boundary yet, so the area cannot be checked against its edge.',
              style: GoogleFonts.inter(fontSize: 11, color: AgroColors.harvest)),
          ),
        if (areaPointsProblem(points, blockRings: geoJsonRings(block?.boundaryGeoJson)) case final problem?)
          Padding(
            padding: const EdgeInsets.only(top: 8),
            child: Text(problem, style: const TextStyle(color: AgroColors.alert, fontSize: 12, fontWeight: FontWeight.w600)),
          ),
        if (error != null)
          Padding(
            padding: const EdgeInsets.only(top: 8),
            child: Text(error!, style: const TextStyle(color: AgroColors.alert, fontSize: 12)),
          ),
        const SizedBox(height: 12),
        SizedBox(
          width: double.infinity,
          child: OutlinedButton.icon(
            onPressed: onPickMap,
            icon: const Icon(Icons.touch_app_outlined, size: 18),
            label: const Text('Pick on map (tap the corners)'),
          ),
        ),
        const SizedBox(height: 8),
        Row(
          children: [
            Expanded(
              child: ElevatedButton.icon(
                onPressed: capturing ? null : onAdd,
                icon: capturing
                    ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : const Icon(Icons.my_location, size: 18),
                label: Text(capturing ? 'Reading GPS…' : 'Add point here'),
              ),
            ),
            const SizedBox(width: 8),
            OutlinedButton(onPressed: points.isEmpty ? null : onUndo, child: const Text('Undo')),
            const SizedBox(width: 8),
            OutlinedButton(onPressed: points.isEmpty ? null : onClear, child: const Text('Clear')),
          ],
        ),
      ],
    );
  }
}

// ── FOOTER ──
class _Footer extends StatelessWidget {
  final int step;
  final bool isLast;
  final bool canProceed;
  final bool isSubmitting;
  final VoidCallback onBack;
  final VoidCallback onNext;

  const _Footer({
    required this.step, required this.isLast, required this.canProceed,
    required this.isSubmitting, required this.onBack, required this.onNext,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.fromLTRB(16, 12, 16, MediaQuery.of(context).padding.bottom + 12),
      decoration: const BoxDecoration(
        color: AgroColors.white,
        border: Border(top: BorderSide(color: AgroColors.fog)),
      ),
      child: Row(
        children: [
          OutlinedButton(
            onPressed: onBack,
            style: OutlinedButton.styleFrom(minimumSize: const Size(100, 44)),
            child: Text(step == 0 ? 'Cancel' : '← Back', style: GoogleFonts.inter(fontWeight: FontWeight.w600, fontSize: 13)),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: ElevatedButton(
              onPressed: canProceed && !isSubmitting ? onNext : null,
              child: isSubmitting
                ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                : Text(isLast ? '✓ Submit Activity' : 'Continue →',
                    style: GoogleFonts.inter(fontWeight: FontWeight.w600, fontSize: 14)),
            ),
          ),
        ],
      ),
    );
  }
}
