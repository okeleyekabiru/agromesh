import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../services/api_client.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import 'common.dart';

// Wire format: enums arrive as ordinals. Category order matches the API's TransactionCategory enum.
const _categories = [
  'Labour', 'Fertilizer', 'Chemicals', 'Fuel', 'Equipment', 'Maintenance', 'Transportation', 'Seeds', 'Sales', 'Other',
  'TaxAndCharges',
];
const _kinds = ['Expense', 'Income', 'Capital'];

String _catLabel(String c) => c == 'TaxAndCharges' ? 'Tax & Charges' : c;

String _enumName(dynamic v, List<String> names) {
  if (v is int) return v >= 0 && v < names.length ? names[v] : '$v';
  return v?.toString() ?? '';
}

class _FinanceData {
  final Map<String, dynamic> summary;
  final Map<String, dynamic> bank;
  final List<Map<String, dynamic>> queue;
  final List<Map<String, dynamic>> txns;
  const _FinanceData(this.summary, this.bank, this.queue, this.txns);
}

class FinanceScreen extends StatefulWidget {
  const FinanceScreen({super.key});

  @override
  State<FinanceScreen> createState() => _FinanceScreenState();
}

class _FinanceScreenState extends State<FinanceScreen> {
  String _filter = 'All';
  static const _filters = ['All', 'Needs review', 'Income', 'Expense'];
  bool _syncing = false;
  String? _resolving;

  String _txQuery() {
    switch (_filter) {
      case 'Needs review':
        return '&pendingReview=true';
      case 'Income':
        return '&kind=1';
      case 'Expense':
        return '&kind=0';
      default:
        return '';
    }
  }

  Future<_FinanceData> _load(String farm) async {
    final summary = await ApiClient.get('/api/farms/$farm/transactions/summary');
    Map<String, dynamic> bank = const {};
    try {
      final b = await ApiClient.get('/api/farms/$farm/bank-account');
      if (b is Map) bank = b.cast<String, dynamic>();
    } catch (_) {/* the bank card just stays empty */}
    List<Map<String, dynamic>> queue = const [];
    try {
      queue = asList(await ApiClient.get('/api/farms/$farm/transactions/needs-review'));
    } catch (_) {}
    final page = await ApiClient.get('/api/farms/$farm/transactions?page=1&pageSize=50${_txQuery()}');
    final txns = asList(page is Map ? page['items'] : page);
    return _FinanceData(summary is Map ? summary.cast<String, dynamic>() : const {}, bank, queue, txns);
  }

  @override
  Widget build(BuildContext context) {
    final farm = farmIdFrom(context);
    return LoadScaffold<_FinanceData>(
      key: ValueKey('fin-$_filter'),
      title: 'Finance',
      load: () => _load(farm),
      builder: (ctx, d, reload) => ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(16),
        children: [
          Row(children: [
            Expanded(child: KpiCard(label: 'Income', value: naira(d.summary['income'] as num?), delta: 'This month', deltaType: KpiDeltaType.up, dark: true)),
            const SizedBox(width: 10),
            Expanded(child: KpiCard(label: 'Expenses', value: naira(d.summary['expenses'] as num?), delta: 'This month', deltaType: KpiDeltaType.mute)),
          ]),
          const SizedBox(height: 10),
          Row(children: [
            Expanded(child: KpiCard(label: 'Capital', value: naira(d.summary['capital'] as num?), delta: 'Owner investment', deltaType: KpiDeltaType.mute)),
            const SizedBox(width: 10),
            Expanded(
              child: KpiCard(
                label: 'Pending review',
                value: '${d.summary['pendingReview'] ?? 0}',
                delta: 'Not yet checked',
                deltaType: ((d.summary['pendingReview'] as num?) ?? 0) > 0 ? KpiDeltaType.warn : KpiDeltaType.up,
              ),
            ),
          ]),
          const SizedBox(height: 14),
          _bankCard(ctx, farm, d.bank, reload),
          if (d.queue.isNotEmpty) ...[
            const SizedBox(height: 14),
            _queueCard(ctx, farm, d.queue, reload),
          ],
          const SizedBox(height: 14),
          SizedBox(
            height: 36,
            child: ListView(
              scrollDirection: Axis.horizontal,
              children: [
                for (final f in _filters)
                  Padding(
                    padding: const EdgeInsets.only(right: 8),
                    child: ChoiceChip(
                      label: Text(f, style: GoogleFonts.inter(fontSize: 12)),
                      selected: _filter == f,
                      onSelected: (_) => setState(() => _filter = f),
                    ),
                  ),
              ],
            ),
          ),
          const SizedBox(height: 10),
          AgroCard(
            child: Column(
              children: [
                const AgroCardHeader(title: 'Transactions'),
                if (d.txns.isEmpty) emptyNote('No transactions to show.'),
                for (final t in d.txns) _txRow(ctx, farm, t, reload),
              ],
            ),
          ),
          const SizedBox(height: 40),
        ],
      ),
    );
  }

  Widget _bankCard(BuildContext ctx, String farm, Map<String, dynamic> bank, Future<void> Function() reload) {
    final linked = bank['linked'] == true;
    return AgroCard(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(children: [
              const Icon(Icons.account_balance_outlined, color: AgroColors.leaf, size: 20),
              const SizedBox(width: 10),
              Expanded(
                child: Text(linked ? 'Bank account linked' : 'No bank account linked',
                    style: GoogleFonts.inter(fontSize: 13, fontWeight: FontWeight.w600, color: AgroColors.ink)),
              ),
              AgroBadge(label: linked ? 'Linked' : 'Not linked', variant: linked ? AgroBadgeVariant.green : AgroBadgeVariant.stone),
            ]),
            if (linked) KvRow('Last sync', dateText(bank['lastSyncAt'])),
            const SizedBox(height: 8),
            if (linked)
              OutlinedButton.icon(
                onPressed: _syncing
                    ? null
                    : () async {
                        setState(() => _syncing = true);
                        try {
                          final r = await ApiClient.post('/api/farms/$farm/bank-account/sync', {});
                          final n = r is Map ? r['newTransactions'] : null;
                          if (ctx.mounted) toast(ctx, n == null ? 'Bank synced.' : 'Bank synced: $n new transactions.');
                          await reload();
                        } catch (e) {
                          if (ctx.mounted) toast(ctx, errText(e));
                        } finally {
                          if (mounted) setState(() => _syncing = false);
                        }
                      },
                icon: const Icon(Icons.sync, size: 16),
                label: Text(_syncing ? 'Syncing…' : 'Sync now'),
              )
            else
              Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text('Linking a bank account is done in the browser (Mono Connect). Sign in there, link the account, then come back and sync here.',
                    style: GoogleFonts.inter(fontSize: 11.5, color: AgroColors.stone)),
                const SizedBox(height: 8),
                OutlinedButton.icon(
                  onPressed: () => openWebApp(ctx, '/finance'),
                  icon: const Icon(Icons.open_in_new, size: 16),
                  label: const Text('Link bank account on the web'),
                ),
              ]),
          ],
        ),
      ),
    );
  }

  Widget _queueCard(BuildContext ctx, String farm, List<Map<String, dynamic>> queue, Future<void> Function() reload) {
    return AgroCard(
      child: Column(
        children: [
          AgroCardHeader(title: 'Transfers to confirm', subtitle: '${queue.length} naming an order'),
          for (final t in queue)
            Container(
              padding: const EdgeInsets.fromLTRB(16, 10, 16, 12),
              decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: AgroColors.fog))),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('${t['narration'] ?? ''}', maxLines: 2, overflow: TextOverflow.ellipsis,
                      style: GoogleFonts.inter(fontSize: 12.5, fontWeight: FontWeight.w600, color: AgroColors.ink)),
                  const SizedBox(height: 2),
                  Text('${naira((t['amount'] as num?)?.abs())} · ${dateText(t['date'])}',
                      style: GoogleFonts.jetBrainsMono(fontSize: 11, color: AgroColors.stone)),
                  if ((t['reconciliationNote'] as String?)?.isNotEmpty ?? false)
                    Padding(
                      padding: const EdgeInsets.only(top: 4),
                      child: Text(t['reconciliationNote'] as String, style: GoogleFonts.inter(fontSize: 11, color: AgroColors.stone)),
                    ),
                  const SizedBox(height: 8),
                  Row(children: [
                    FilledButton(
                      onPressed: _resolving == t['id'] ? null : () => _resolve(ctx, farm, t['id'] as String, true, reload),
                      child: const Text('Confirm paid'),
                    ),
                    const SizedBox(width: 8),
                    OutlinedButton(
                      onPressed: _resolving == t['id'] ? null : () => _resolve(ctx, farm, t['id'] as String, false, reload),
                      child: const Text('Not an order payment'),
                    ),
                  ]),
                ],
              ),
            ),
        ],
      ),
    );
  }

  Future<void> _resolve(BuildContext ctx, String farm, String id, bool confirm, Future<void> Function() reload) async {
    setState(() => _resolving = id);
    try {
      await ApiClient.post('/api/farms/$farm/transactions/$id/reconcile', {'confirm': confirm});
      await reload();
    } catch (e) {
      if (ctx.mounted) toast(ctx, errText(e));
    } finally {
      if (mounted) setState(() => _resolving = null);
    }
  }

  Widget _txRow(BuildContext ctx, String farm, Map<String, dynamic> t, Future<void> Function() reload) {
    final amount = (t['amount'] as num?) ?? 0;
    final out = amount < 0;
    final kind = _enumName(t['kind'], _kinds);
    final cat = _enumName(t['category'], _categories);
    final reviewed = t['reviewedByUser'] == true;
    return InkWell(
      onTap: () => _openDetail(ctx, farm, t, reload),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: AgroColors.fog))),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('${t['narration'] ?? ''}', maxLines: 2, overflow: TextOverflow.ellipsis,
                      style: GoogleFonts.inter(fontSize: 12.5, fontWeight: FontWeight.w600, color: AgroColors.ink)),
                  const SizedBox(height: 4),
                  Wrap(spacing: 6, runSpacing: 4, crossAxisAlignment: WrapCrossAlignment.center, children: [
                    Text(dateText(t['date']), style: GoogleFonts.inter(fontSize: 11, color: AgroColors.stone)),
                    AgroBadge(label: _catLabel(cat), variant: AgroBadgeVariant.stone),
                    if (kind == 'Capital') const AgroBadge(label: 'Capital', variant: AgroBadgeVariant.harvest),
                    if (!reviewed) const AgroBadge(label: 'To review', variant: AgroBadgeVariant.harvest),
                  ]),
                ],
              ),
            ),
            const SizedBox(width: 10),
            Text('${out ? '-' : '+'}${naira(amount.abs())}',
                style: GoogleFonts.jetBrainsMono(fontSize: 12.5, fontWeight: FontWeight.w600, color: out ? AgroColors.alert : AgroColors.leaf)),
          ],
        ),
      ),
    );
  }

  void _openDetail(BuildContext ctx, String farm, Map<String, dynamic> t, Future<void> Function() reload) {
    showSheet(ctx, _TxDetail(farm: farm, txn: t)).then((changed) {
      if (changed == true) reload();
    });
  }
}

class _TxDetail extends StatefulWidget {
  final String farm;
  final Map<String, dynamic> txn;
  const _TxDetail({required this.farm, required this.txn});

  @override
  State<_TxDetail> createState() => _TxDetailState();
}

class _TxDetailState extends State<_TxDetail> {
  late String _category;
  late String _kind;
  late final TextEditingController _notes;
  bool _saving = false;
  String? _error;

  num get _amount => (widget.txn['amount'] as num?) ?? 0;

  @override
  void initState() {
    super.initState();
    final c = _enumName(widget.txn['category'], _categories);
    _category = _categories.contains(c) ? c : 'Other';
    final k = _enumName(widget.txn['kind'], _kinds);
    _kind = k == 'Capital' ? 'Capital' : 'Income';
    _notes = TextEditingController(text: widget.txn['reviewNotes'] as String? ?? '');
  }

  @override
  void dispose() {
    _notes.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    setState(() { _saving = true; _error = null; });
    try {
      final id = widget.txn['id'];
      await ApiClient.patch('/api/farms/${widget.farm}/transactions/$id/review', {
        'category': _categories.indexOf(_category),
        'cropAllocation': null,
        'notes': _notes.text.trim().isEmpty ? null : _notes.text.trim(),
      });
      // Only money in can move between income and capital.
      final current = _enumName(widget.txn['kind'], _kinds);
      if (_amount >= 0 && _kind != (current == 'Capital' ? 'Capital' : 'Income')) {
        await ApiClient.patch('/api/farms/${widget.farm}/transactions/$id/kind', {'kind': _kind == 'Capital' ? 2 : 1});
      }
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) setState(() { _error = errText(e); _saving = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    final t = widget.txn;
    final out = _amount < 0;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text('${out ? '-' : '+'}${naira(_amount.abs())}',
            style: GoogleFonts.jetBrainsMono(fontSize: 22, fontWeight: FontWeight.w600, color: out ? AgroColors.alert : AgroColors.leaf)),
        const SizedBox(height: 6),
        Text('${t['narration'] ?? ''}', style: GoogleFonts.inter(fontSize: 13, fontWeight: FontWeight.w600, color: AgroColors.ink)),
        const SizedBox(height: 10),
        KvRow('Date', dateText(t['date'])),
        KvRow('Confidence', _enumName(t['confidence'], const ['High', 'Medium', 'Low'])),
        KvRow('Categorised by', _enumName(t['source'], const ['Dictionary', 'Pattern', 'Llm', 'Manual'])),
        KvRow('Reviewed', t['reviewedByUser'] == true ? 'Yes' : 'Not yet'),
        const Divider(height: 24),
        Text('Category', style: GoogleFonts.inter(fontSize: 11, color: AgroColors.stone)),
        const SizedBox(height: 4),
        DropdownButtonFormField<String>(
          initialValue: _category,
          isExpanded: true,
          decoration: const InputDecoration(isDense: true, border: OutlineInputBorder()),
          items: [for (final c in _categories) DropdownMenuItem(value: c, child: Text(_catLabel(c)))],
          onChanged: _saving ? null : (v) => setState(() => _category = v ?? _category),
        ),
        if (!out) ...[
          const SizedBox(height: 12),
          Text('Money in is', style: GoogleFonts.inter(fontSize: 11, color: AgroColors.stone)),
          const SizedBox(height: 4),
          SegmentedButton<String>(
            segments: const [ButtonSegment(value: 'Income', label: Text('Income')), ButtonSegment(value: 'Capital', label: Text('Capital'))],
            selected: {_kind},
            onSelectionChanged: _saving ? null : (s) => setState(() => _kind = s.first),
          ),
        ],
        const SizedBox(height: 12),
        TextField(
          controller: _notes,
          maxLines: 2,
          decoration: const InputDecoration(labelText: 'Notes', isDense: true, border: OutlineInputBorder()),
        ),
        if (_error != null)
          Padding(
            padding: const EdgeInsets.only(top: 8),
            child: Text(_error!, style: GoogleFonts.inter(fontSize: 11.5, color: AgroColors.alert)),
          ),
        const SizedBox(height: 14),
        Row(mainAxisAlignment: MainAxisAlignment.end, children: [
          TextButton(onPressed: _saving ? null : () => Navigator.pop(context), child: const Text('Cancel')),
          const SizedBox(width: 8),
          FilledButton(onPressed: _saving ? null : _save, child: Text(_saving ? 'Saving…' : 'Save & mark reviewed')),
        ]),
      ],
    );
  }
}
