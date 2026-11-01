import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../models/billing_models.dart';
import '../../services/api_client.dart';
import '../../services/database_service.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import 'billing_common.dart';
import 'common.dart';
import 'ops_common.dart';

class _LineEdit {
  final description = TextEditingController();
  final rate = TextEditingController();
  final quantity = TextEditingController(text: '1');

  _LineEdit();
  _LineEdit.from(InvoiceLine l) {
    description.text = l.description;
    rate.text = _plain(l.rate);
    quantity.text = _plain(l.quantity);
  }

  static String _plain(double v) => v == v.roundToDouble() ? v.toStringAsFixed(0) : v.toString();

  /// The line as typed, or null while the rate or quantity is not a number yet.
  InvoiceLine? get parsed {
    final r = parseNumber(rate.text);
    final q = parseNumber(quantity.text);
    if (r == null || q == null) return null;
    return InvoiceLine(description: description.text.trim(), rate: r, quantity: q);
  }

  void dispose() {
    description.dispose();
    rate.dispose();
    quantity.dispose();
  }
}

/// Create or edit a sales invoice. Pops with the saved [Invoice] (the server's figures are the truth).
class InvoiceFormScreen extends StatefulWidget {
  final String farmId;
  final Invoice? existing;
  const InvoiceFormScreen({super.key, required this.farmId, this.existing});

  @override
  State<InvoiceFormScreen> createState() => _InvoiceFormScreenState();
}

class _InvoiceFormScreenState extends State<InvoiceFormScreen> {
  final _billName = TextEditingController();
  final _billPhone = TextEditingController();
  final _billEmail = TextEditingController();
  final _billAddress = TextEditingController();
  final _vat = TextEditingController(text: '0');
  final _payName = TextEditingController();
  final _payNumber = TextEditingController();
  final _payBank = TextEditingController();
  final _terms = TextEditingController();
  final _signatory = TextEditingController();
  final _lines = <_LineEdit>[];

  late DateTime _date;
  late DateTime _due;
  List<InvoiceTemplate> _templates = const [InvoiceTemplate(key: 'ngf', name: 'NGF - Invoice', isDefault: true)];
  String _templateKey = 'ngf';
  bool _loading = true;
  bool _saving = false;
  String? _error;

  bool get _editing => widget.existing != null;

  @override
  void initState() {
    super.initState();
    final e = widget.existing;
    if (e != null) {
      _date = e.date ?? DateTime.now();
      _due = e.dueDate ?? _date;
      _billName.text = e.billToName;
      _billPhone.text = e.billToPhone ?? '';
      _billEmail.text = e.billToEmail ?? '';
      _billAddress.text = e.billToAddress ?? '';
      _vat.text = _LineEdit._plain(e.vatPercent);
      _payName.text = e.paymentAccountName ?? '';
      _payNumber.text = e.paymentAccountNumber ?? '';
      _payBank.text = e.paymentBankName ?? '';
      _terms.text = e.termsAndNotes ?? '';
      _signatory.text = e.signatoryName ?? '';
      _templateKey = e.templateKey;
      _lines.addAll(e.lineItems.map(_LineEdit.from));
    } else {
      final now = DateTime.now();
      _date = DateTime(now.year, now.month, now.day);
      _due = _date.add(const Duration(days: 14));
    }
    if (_lines.isEmpty) _lines.add(_LineEdit());
    _loadSetup();
  }

  @override
  void dispose() {
    for (final c in [_billName, _billPhone, _billEmail, _billAddress, _vat, _payName, _payNumber, _payBank, _terms, _signatory]) {
      c.dispose();
    }
    for (final l in _lines) {
      l.dispose();
    }
    super.dispose();
  }

  /// Templates (from the server) and, for a new invoice, the farm's bank details to prefill the payment block.
  Future<void> _loadSetup() async {
    try {
      final j = await ApiClient.get('/api/farms/${widget.farmId}/invoice-templates');
      final list = asList(j).map(InvoiceTemplate.fromJson).toList();
      if (list.isNotEmpty) {
        _templates = list;
        if (!_editing || !list.any((t) => t.key == _templateKey)) {
          _templateKey = (list.firstWhere((t) => t.isDefault, orElse: () => list.first)).key;
        }
      }
    } catch (_) {/* keep the built-in default template */}

    if (!_editing) {
      BusinessProfile? profile;
      try {
        final j = await ApiClient.get('/api/farms/${widget.farmId}/business-profile');
        if (j is Map) {
          await DatabaseService.saveCache('bizprofile:${widget.farmId}', [j.cast<String, dynamic>()]);
          profile = BusinessProfile.fromJson(j.cast<String, dynamic>());
        }
      } catch (_) {
        final cached = await DatabaseService.readCacheList('bizprofile:${widget.farmId}');
        if (cached.isNotEmpty) profile = BusinessProfile.fromJson(cached.first);
      }
      if (profile != null && _payName.text.isEmpty && _payNumber.text.isEmpty && _payBank.text.isEmpty) {
        _payName.text = profile.bankAccountName ?? '';
        _payNumber.text = profile.bankAccountNumber ?? '';
        _payBank.text = profile.bankName ?? '';
      }
    }
    if (mounted) setState(() => _loading = false);
  }

  double get _vatPercent => (parseNumber(_vat.text) ?? 0).clamp(0, 100).toDouble();

  List<InvoiceLine> get _typedLines => [for (final l in _lines) if (l.parsed != null) l.parsed!];

  Future<void> _save() async {
    final name = _billName.text.trim();
    if (name.isEmpty) return _fail('Enter who the invoice is billed to.');
    if (_lines.length > 200) return _fail('An invoice can have at most 200 items.');
    final lines = <InvoiceLine>[];
    for (var i = 0; i < _lines.length; i++) {
      final l = _lines[i].parsed;
      final n = i + 1;
      if (_lines[i].description.text.trim().isEmpty) return _fail('Item $n needs a description.');
      if (l == null) return _fail('Item $n needs a rate and a quantity.');
      final lineProblem = invoiceLineProblem(n, l);
      if (lineProblem != null) return _fail(lineProblem);
      lines.add(l);
    }
    final subtotalProblem = invoiceSubtotalProblem(lines);
    if (subtotalProblem != null) return _fail(subtotalProblem);
    final vat = _vat.text.trim().isEmpty ? 0.0 : parseNumber(_vat.text);
    if (vat == null || vat < 0 || vat > 100) return _fail('VAT must be a number between 0 and 100 percent.');
    final dateProblem = dateRangeProblem('The invoice date', _date) ?? dateRangeProblem('The due date', _due);
    if (dateProblem != null) return _fail(dateProblem);
    if (_due.isBefore(_date)) return _fail('The due date cannot be before the invoice date.');

    setState(() { _saving = true; _error = null; });
    try {
      final body = invoiceRequestBody(
      date: _date,
      dueDate: _due,
      billToName: name,
      billToPhone: _billPhone.text,
      billToEmail: _billEmail.text,
      billToAddress: _billAddress.text,
      lineItems: lines,
      vatPercent: vat,
      paymentAccountName: _payName.text,
      paymentAccountNumber: _payNumber.text,
      paymentBankName: _payBank.text,
      termsAndNotes: _terms.text,
      signatoryName: _signatory.text,
      templateKey: _templateKey,
      );
      final path = '/api/farms/${widget.farmId}/invoices';
      final res = _editing ? await ApiClient.put('$path/${widget.existing!.id}', body) : await ApiClient.post(path, body);
      if (!mounted) return;
      Navigator.pop(context, Invoice.fromJson((res as Map).cast<String, dynamic>()));
    } catch (e) {
      if (mounted) setState(() { _error = failText(e); _saving = false; });
    }
  }

  void _fail(String m) => setState(() => _error = m);

  Widget _text(TextEditingController c, String label, {int max = 200, TextInputType? type, int lines = 1}) => Padding(
        padding: const EdgeInsets.only(bottom: 10),
        child: TextField(
          controller: c,
          keyboardType: type,
          maxLines: lines,
          inputFormatters: [LengthLimitingTextInputFormatter(max)],
          decoration: InputDecoration(labelText: label, border: const OutlineInputBorder()),
        ),
      );

  Widget _lineCard(int i) {
    final l = _lines[i];
    final parsed = l.parsed;
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(color: AgroColors.mist, borderRadius: BorderRadius.circular(10), border: Border.all(color: AgroColors.fog)),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          Text('Item ${i + 1}', style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13, fontWeight: FontWeight.w700, color: AgroColors.forest)),
          const Spacer(),
          if (_lines.length > 1)
            IconButton(
              tooltip: 'Remove item ${i + 1}',
              constraints: const BoxConstraints(minWidth: 44, minHeight: 44),
              icon: const Icon(Icons.delete_outline, color: AgroColors.alert, size: 20),
              onPressed: () => setState(() => _lines.removeAt(i).dispose()),
            ),
        ]),
        TextField(
          controller: l.description,
          maxLines: 2,
          minLines: 1,
          inputFormatters: [LengthLimitingTextInputFormatter(500)],
          onChanged: (_) => setState(() {}),
          decoration: const InputDecoration(labelText: 'Description', border: OutlineInputBorder(), filled: true, fillColor: Colors.white),
        ),
        const SizedBox(height: 10),
        Row(children: [
          Expanded(
            child: TextField(
              controller: l.rate,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              onChanged: (_) => setState(() {}),
              decoration: const InputDecoration(labelText: 'Rate (₦)', border: OutlineInputBorder(), filled: true, fillColor: Colors.white),
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: TextField(
              controller: l.quantity,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              onChanged: (_) => setState(() {}),
              decoration: const InputDecoration(labelText: 'Quantity', border: OutlineInputBorder(), filled: true, fillColor: Colors.white),
            ),
          ),
        ]),
        const SizedBox(height: 8),
        Align(
          alignment: Alignment.centerRight,
          child: Text('Amount ${nairaExact(parsed?.amount ?? 0)}',
              style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13, fontWeight: FontWeight.w700, color: AgroColors.ink)),
        ),
      ]),
    );
  }

  @override
  Widget build(BuildContext context) {
    final totals = computeInvoiceTotals(_typedLines, _vatPercent);
    return Scaffold(
      backgroundColor: AgroColors.mist,
      appBar: AppBar(
        backgroundColor: AgroColors.forest,
        foregroundColor: AgroColors.white,
        title: Text(_editing ? 'Edit ${widget.existing!.reference}' : 'New invoice', style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: AgroColors.palm))
          : ListView(padding: const EdgeInsets.all(16), children: [
              AgroCard(
                padding: const EdgeInsets.all(16),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  sectionLabel('Billed to'),
                  _text(_billName, 'Customer name'),
                  _text(_billPhone, 'Phone (optional)', max: 60, type: TextInputType.phone),
                  _text(_billEmail, 'Email (optional)', type: TextInputType.emailAddress),
                  _text(_billAddress, 'Address (optional)', max: 500, lines: 2),
                  Row(children: [
                    Expanded(
                      child: DateField(
                        label: 'Invoice date',
                        value: _date,
                        onChanged: (d) => setState(() {
                          _date = d!;
                          if (_due.isBefore(_date)) _due = _date;
                        }),
                      ),
                    ),
                    const SizedBox(width: 10),
                    Expanded(child: DateField(label: 'Due date', value: _due, first: _date, onChanged: (d) => setState(() => _due = d!))),
                  ]),
                ]),
              ),
              const SizedBox(height: 14),
              AgroCard(
                padding: const EdgeInsets.all(16),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  sectionLabel('Items'),
                  for (var i = 0; i < _lines.length; i++) _lineCard(i),
                  TextButton.icon(
                    style: TextButton.styleFrom(minimumSize: const Size(44, 44)),
                    onPressed: _lines.length >= 200 ? null : () => setState(() => _lines.add(_LineEdit())),
                    icon: const Icon(Icons.add),
                    label: const Text('Add item'),
                  ),
                  const SizedBox(height: 4),
                  SizedBox(
                    width: 160,
                    child: TextField(
                      controller: _vat,
                      keyboardType: const TextInputType.numberWithOptions(decimal: true),
                      onChanged: (_) => setState(() {}),
                      decoration: const InputDecoration(labelText: 'VAT %', border: OutlineInputBorder()),
                    ),
                  ),
                  InvoiceTotalsCard(totals: totals, vatPercent: _vatPercent),
                  hintLine('A preview. The saved invoice uses the server\'s figures.'),
                ]),
              ),
              const SizedBox(height: 14),
              AgroCard(
                padding: const EdgeInsets.all(16),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  sectionLabel('Payment details'),
                  if (!_editing) hintLine('Filled in from the farm business profile. Change them here for this invoice only.'),
                  _text(_payName, 'Account name', max: 150),
                  _text(_payNumber, 'Account number', max: 40, type: TextInputType.number),
                  _text(_payBank, 'Bank name', max: 100),
                  sectionLabel('Terms and signature'),
                  _text(_terms, 'Terms & conditions / notes', max: 2000, lines: 3),
                  _text(_signatory, 'Signatory name', max: 150),
                  DropdownButtonFormField<String>(
                    initialValue: _templates.any((t) => t.key == _templateKey) ? _templateKey : _templates.first.key,
                    isExpanded: true,
                    decoration: const InputDecoration(labelText: 'Template', border: OutlineInputBorder()),
                    items: [for (final t in _templates) DropdownMenuItem(value: t.key, child: Text(t.name, overflow: TextOverflow.ellipsis))],
                    onChanged: (v) => setState(() => _templateKey = v ?? _templateKey),
                  ),
                ]),
              ),
              const SizedBox(height: 14),
              errorLine(_error),
              primaryButton(_editing ? 'Save changes' : 'Create invoice', _save, busy: _saving),
              const SizedBox(height: 24),
            ]),
    );
  }
}

/// Subtotal, VAT and total of an invoice.
class InvoiceTotalsCard extends StatelessWidget {
  final InvoiceTotals totals;
  final double vatPercent;
  const InvoiceTotalsCard({super.key, required this.totals, required this.vatPercent});

  @override
  Widget build(BuildContext context) {
    final vatLabel = vatPercent == vatPercent.roundToDouble() ? vatPercent.toStringAsFixed(0) : vatPercent.toString();
    return Container(
      margin: const EdgeInsets.only(top: 12, bottom: 6),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(color: AgroColors.mist, borderRadius: BorderRadius.circular(10)),
      child: Column(children: [
        KvRow('Subtotal', nairaExact(totals.subtotal)),
        KvRow('VAT ($vatLabel%)', nairaExact(totals.vatAmount)),
        const Divider(height: 12),
        KvRow('Total', nairaExact(totals.total), vColor: AgroColors.forest),
      ]),
    );
  }
}
