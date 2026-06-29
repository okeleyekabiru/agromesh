import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../models/models.dart';
import '../../stores/app_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';

class LogActivityScreen extends StatefulWidget {
  const LogActivityScreen({super.key});

  @override
  State<LogActivityScreen> createState() => _LogActivityScreenState();
}

class _LogActivityScreenState extends State<LogActivityScreen> {
  int _step = 0; // 0=Location, 1=Activity, 2=Workers & Inputs
  final _formKey = GlobalKey<FormState>();

  // Form state
  Section? _section;
  Block? _block;
  ActivityType? _activityType;
  final _notesCtrl = TextEditingController();
  final List<WorkerLog> _workers = [];
  final List<InputUsed> _inputs = [];
  String? _supervisorId;

  // Input builders
  final _workerNameCtrl = TextEditingController();
  WorkerType _workerType = WorkerType.employee;
  final _inputItemCtrl = TextEditingController();
  final _inputQtyCtrl = TextEditingController();
  String _inputUnit = 'kg';

  bool _isSubmitting = false;

  @override
  void dispose() {
    _notesCtrl.dispose();
    _workerNameCtrl.dispose();
    _inputItemCtrl.dispose();
    _inputQtyCtrl.dispose();
    super.dispose();
  }

  bool get _canProceed {
    if (_step == 0) return _section != null && _block != null;
    if (_step == 1) return _activityType != null;
    return true;
  }

  Future<void> _submit() async {
    if (_isSubmitting) return;
    setState(() => _isSubmitting = true);

    final store = context.read<AppStore>();
    final supervisor = store.workers.firstWhere(
      (w) => w.id == _supervisorId,
      orElse: () => store.workers.first,
    );

    await store.logActivity(
      sectionId: _section!.id,
      sectionName: _section!.name,
      blockId: _block!.id,
      blockName: _block!.name,
      activityType: _activityType!,
      workers: _workers,
      inputs: _inputs,
      supervisorId: supervisor.id,
      supervisorName: supervisor.name,
      notes: _notesCtrl.text,
    );

    if (mounted) {
      Navigator.pop(context);
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('${_activityType!.label} logged for ${_section!.name}'),
          backgroundColor: AgroColors.leaf,
          behavior: SnackBarBehavior.floating,
        ),
      );
    }
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
          _Stepper(currentStep: _step),

          Expanded(
            child: Form(
              key: _formKey,
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(16),
                child: [
                  _StepLocation(
                    sections: store.sections,
                    blocks: _section != null ? store.blocksForSection(_section!.id) : [],
                    selectedSection: _section,
                    selectedBlock: _block,
                    onSectionChanged: (s) => setState(() { _section = s; _block = null; }),
                    onBlockChanged:   (b) => setState(() => _block = b),
                  ),
                  _StepActivity(
                    selected: _activityType,
                    notes: _notesCtrl,
                    onSelected: (t) => setState(() => _activityType = t),
                  ),
                  _StepWorkersInputs(
                    workers: _workers,
                    inputs: _inputs,
                    supervisors: store.supervisors,
                    selectedSupervisorId: _supervisorId,
                    workerNameCtrl: _workerNameCtrl,
                    workerType: _workerType,
                    inputItemCtrl: _inputItemCtrl,
                    inputQtyCtrl: _inputQtyCtrl,
                    inputUnit: _inputUnit,
                    onWorkerTypeChanged: (t) => setState(() => _workerType = t),
                    onInputUnitChanged:  (u) => setState(() => _inputUnit = u),
                    onSupervisorChanged: (id) => setState(() => _supervisorId = id),
                    onAddWorker: () {
                      if (_workerNameCtrl.text.trim().isEmpty) return;
                      setState(() {
                        _workers.add(WorkerLog(
                          workerId: 'manual_${DateTime.now().millisecondsSinceEpoch}',
                          workerName: _workerNameCtrl.text.trim(),
                          workerType: _workerType,
                          hoursWorked: 8,
                        ));
                        _workerNameCtrl.clear();
                      });
                    },
                    onRemoveWorker: (i) => setState(() => _workers.removeAt(i)),
                    onAddInput: () {
                      final item = _inputItemCtrl.text.trim();
                      final qty = double.tryParse(_inputQtyCtrl.text);
                      if (item.isEmpty || qty == null) return;
                      setState(() {
                        _inputs.add(InputUsed(item: item, quantity: qty, unit: _inputUnit));
                        _inputItemCtrl.clear();
                        _inputQtyCtrl.clear();
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
              if (_step < 2) {
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
  const _Stepper({required this.currentStep});

  @override
  Widget build(BuildContext context) {
    final steps = ['Location', 'Activity', 'Workers & Inputs'];
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
  final List<Section> sections;
  final List<Block> blocks;
  final Section? selectedSection;
  final Block? selectedBlock;
  final void Function(Section?) onSectionChanged;
  final void Function(Block?) onBlockChanged;

  const _StepLocation({
    required this.sections, required this.blocks,
    required this.selectedSection, required this.selectedBlock,
    required this.onSectionChanged, required this.onBlockChanged,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const MonoLabel('Section'),
        const SizedBox(height: 6),
        DropdownButtonFormField<Section>(
          value: selectedSection,
          hint: Text('Choose section...', style: GoogleFonts.inter(fontSize: 13, color: AgroColors.stone)),
          decoration: const InputDecoration(),
          items: sections.map((s) => DropdownMenuItem(
            value: s,
            child: Text('${s.name} — ${s.crop.label}', style: GoogleFonts.inter(fontSize: 13)),
          )).toList(),
          onChanged: onSectionChanged,
        ),
        if (selectedSection != null) ...[
          const SizedBox(height: 4),
          Text('${selectedSection!.hectares.toStringAsFixed(0)} ha · ${selectedSection!.blockCount} blocks · Last activity: ${selectedSection!.lastActivityDate ?? 'Never'}',
            style: GoogleFonts.jetBrainsMono(fontSize: 9, color: AgroColors.stone)),
        ],
        const SizedBox(height: 16),
        const MonoLabel('Block'),
        const SizedBox(height: 6),
        DropdownButtonFormField<Block>(
          value: selectedBlock,
          hint: Text('Choose block...', style: GoogleFonts.inter(fontSize: 13, color: AgroColors.stone)),
          decoration: const InputDecoration(),
          items: blocks.map((b) => DropdownMenuItem(
            value: b,
            child: Text('${b.name} (${b.hectares.toStringAsFixed(0)} ha)', style: GoogleFonts.inter(fontSize: 13)),
          )).toList(),
          onChanged: selectedSection != null ? onBlockChanged : null,
        ),
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
  final List<Worker> supervisors;
  final String? selectedSupervisorId;
  final TextEditingController workerNameCtrl;
  final WorkerType workerType;
  final TextEditingController inputItemCtrl;
  final TextEditingController inputQtyCtrl;
  final String inputUnit;
  final void Function(WorkerType) onWorkerTypeChanged;
  final void Function(String) onInputUnitChanged;
  final void Function(String?) onSupervisorChanged;
  final VoidCallback onAddWorker;
  final void Function(int) onRemoveWorker;
  final VoidCallback onAddInput;
  final void Function(int) onRemoveInput;

  const _StepWorkersInputs({
    required this.workers, required this.inputs, required this.supervisors,
    required this.selectedSupervisorId, required this.workerNameCtrl,
    required this.workerType, required this.inputItemCtrl, required this.inputQtyCtrl,
    required this.inputUnit, required this.onWorkerTypeChanged,
    required this.onInputUnitChanged, required this.onSupervisorChanged,
    required this.onAddWorker, required this.onRemoveWorker,
    required this.onAddInput, required this.onRemoveInput,
  });

  @override
  Widget build(BuildContext context) {
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
              WorkerChip(label: '${e.value.workerName} (${e.value.workerType.label})', onRemove: () => onRemoveWorker(e.key))
            ).toList(),
          ),
          const SizedBox(height: 10),
        ],
        Row(
          children: [
            Expanded(
              child: TextField(
                controller: workerNameCtrl,
                style: GoogleFonts.inter(fontSize: 13, color: AgroColors.forest),
                decoration: InputDecoration(
                  hintText: 'Worker name...',
                  hintStyle: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone),
                ),
                onSubmitted: (_) => onAddWorker(),
              ),
            ),
            const SizedBox(width: 8),
            DropdownButton<WorkerType>(
              value: workerType,
              underline: const SizedBox(),
              style: GoogleFonts.inter(fontSize: 12, color: AgroColors.forest),
              items: WorkerType.values.map((t) =>
                DropdownMenuItem(value: t, child: Text(t.label))).toList(),
              onChanged: (t) => t != null ? onWorkerTypeChanged(t) : null,
            ),
            const SizedBox(width: 8),
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

        // Inputs
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
        Row(
          children: [
            Expanded(
              child: TextField(
                controller: inputItemCtrl,
                style: GoogleFonts.inter(fontSize: 13, color: AgroColors.forest),
                decoration: InputDecoration(
                  hintText: 'e.g. NPK Fertilizer',
                  hintStyle: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone),
                ),
              ),
            ),
            const SizedBox(width: 6),
            SizedBox(
              width: 60,
              child: TextField(
                controller: inputQtyCtrl,
                keyboardType: TextInputType.number,
                style: GoogleFonts.inter(fontSize: 13, color: AgroColors.forest),
                decoration: InputDecoration(
                  hintText: 'Qty',
                  hintStyle: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone),
                ),
              ),
            ),
            const SizedBox(width: 6),
            DropdownButton<String>(
              value: inputUnit,
              underline: const SizedBox(),
              style: GoogleFonts.inter(fontSize: 12, color: AgroColors.forest),
              items: ['kg', 'L', 'bags', 'units', 'ha'].map((u) =>
                DropdownMenuItem(value: u, child: Text(u))).toList(),
              onChanged: (u) => u != null ? onInputUnitChanged(u) : null,
            ),
            const SizedBox(width: 6),
            ElevatedButton(
              onPressed: onAddInput,
              style: ElevatedButton.styleFrom(minimumSize: const Size(48, 42), padding: EdgeInsets.zero),
              child: const Icon(Icons.add, size: 18),
            ),
          ],
        ),

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

// ── FOOTER ──
class _Footer extends StatelessWidget {
  final int step;
  final bool canProceed;
  final bool isSubmitting;
  final VoidCallback onBack;
  final VoidCallback onNext;

  const _Footer({
    required this.step, required this.canProceed,
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
                : Text(step == 2 ? '✓ Submit Activity' : 'Continue →',
                    style: GoogleFonts.inter(fontWeight: FontWeight.w600, fontSize: 14)),
            ),
          ),
        ],
      ),
    );
  }
}
