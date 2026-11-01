import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../models/billing_models.dart';
import '../../services/api_client.dart';
import '../../services/file_picking.dart';
import '../../services/upload_rules.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import 'billing_common.dart';
import 'ops_common.dart';

/// Attach the invoice a supplier sent for a received order. Pops with the saved [VendorInvoice].
class VendorInvoiceUploadScreen extends StatefulWidget {
  final String farmId;
  final String orderId;
  final String orderLabel;
  final double orderTotal;
  final String supplierName;
  const VendorInvoiceUploadScreen({
    super.key,
    required this.farmId,
    required this.orderId,
    required this.orderLabel,
    required this.orderTotal,
    required this.supplierName,
  });

  @override
  State<VendorInvoiceUploadScreen> createState() => _VendorInvoiceUploadScreenState();
}

class _VendorInvoiceUploadScreenState extends State<VendorInvoiceUploadScreen> {
  final _number = TextEditingController();
  final _amount = TextEditingController();
  final _vendor = TextEditingController();
  final _notes = TextEditingController();
  late DateTime _invoiceDate;
  DateTime? _dueDate;

  PickedUpload? _file;
  UploadKind? _kind;
  bool _busy = false;
  bool _picking = false;
  String? _fileError;
  String? _error;

  @override
  void initState() {
    super.initState();
    final now = DateTime.now();
    _invoiceDate = DateTime(now.year, now.month, now.day);
    _vendor.text = widget.supplierName;
  }

  @override
  void dispose() {
    _number.dispose();
    _amount.dispose();
    _vendor.dispose();
    _notes.dispose();
    super.dispose();
  }

  Future<void> _pick(PickSource source) async {
    setState(() { _picking = true; _fileError = null; });
    try {
      final picked = await FilePicking.pick(source, allowPdf: true);
      if (picked != null) {
        final problem = uploadProblem(UploadRule.vendorInvoice, picked.bytes);
        if (problem != null) {
          _fileError = problem;
        } else {
          _file = picked;
          _kind = sniffUpload(picked.bytes);
        }
      }
    } catch (e) {
      _fileError = failText(e);
    }
    if (mounted) setState(() => _picking = false);
  }

  Future<void> _submit() async {
    final file = _file;
    final kind = _kind;
    if (file == null || kind == null) return setState(() => _error = 'Choose the invoice file first (PDF, PNG or JPEG).');
    final number = _number.text.trim();
    if (number.isEmpty) return setState(() => _error = 'Enter the vendor\'s invoice number.');
    final amount = parseNumber(_amount.text);
    if (amount == null) return setState(() => _error = 'Enter the invoice amount as a number, for example 125000.50.');
    final amountProblem = billAmountProblem(amount);
    if (amountProblem != null) return setState(() => _error = amountProblem);
    final dateProblem = dateRangeProblem('The invoice date', _invoiceDate) ?? (_dueDate == null ? null : dateRangeProblem('The due date', _dueDate!));
    if (dateProblem != null) return setState(() => _error = dateProblem);
    if (_dueDate != null && _dueDate!.isBefore(_invoiceDate)) return setState(() => _error = 'The due date cannot be before the invoice date.');
    final problem = uploadProblem(UploadRule.vendorInvoice, file.bytes);
    if (problem != null) return setState(() => _error = problem);

    setState(() { _busy = true; _error = null; });
    try {
      final res = await ApiClient.sendMultipart(
        'POST',
        '/api/farms/${widget.farmId}/orders/${widget.orderId}/vendor-invoices',
        fileField: 'file',
        fileBytes: file.bytes,
        fileName: _safeName(file.name, kind),
        contentType: kind.contentType,
        fields: {
          'invoiceNumber': number,
          'amount': amount.toStringAsFixed(2),
          'invoiceDate': dateOnly(_invoiceDate),
          'dueDate': _dueDate == null ? null : dateOnly(_dueDate!),
          'vendorName': _vendor.text,
          'notes': _notes.text,
        },
      );
      if (!mounted) return;
      Navigator.pop(context, VendorInvoice.fromJson((res as Map).cast<String, dynamic>()));
    } catch (e) {
      if (mounted) setState(() { _error = failText(e); _busy = false; });
    }
  }

  /// Keeps the name the user knows but makes sure its extension matches the real type.
  static String _safeName(String name, UploadKind kind) {
    final base = name.contains('.') ? name.substring(0, name.lastIndexOf('.')) : name;
    return '${base.isEmpty ? 'invoice' : base}.${kind.extension}';
  }

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

  @override
  Widget build(BuildContext context) {
    final file = _file;
    return Scaffold(
      backgroundColor: AgroColors.mist,
      appBar: AppBar(
        backgroundColor: AgroColors.forest,
        foregroundColor: AgroColors.white,
        title: const Text('Upload vendor invoice', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
      ),
      body: ListView(padding: const EdgeInsets.all(16), children: [
        AgroCard(
          padding: const EdgeInsets.all(16),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            sectionLabel('For order ${widget.orderLabel}'),
            hintLine('Order total ${nairaExact(widget.orderTotal)}. Details cannot be edited after upload; delete and upload again to fix a mistake.'),
            sectionLabel('Invoice file'),
            if (file != null && _kind != null)
              Container(
                margin: const EdgeInsets.only(bottom: 10),
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(color: AgroColors.mist, borderRadius: BorderRadius.circular(10), border: Border.all(color: AgroColors.fog)),
                child: Row(children: [
                  _Preview(kind: _kind!, bytes: file.bytes),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                      Text(file.name, maxLines: 2, overflow: TextOverflow.ellipsis, style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13, fontWeight: FontWeight.w600, color: AgroColors.ink)),
                      Text('${_kind!.label} · ${formatBytes(file.bytes.length)}', style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 12.5, color: AgroColors.stone)),
                    ]),
                  ),
                ]),
              ),
            Wrap(spacing: 8, runSpacing: 8, children: [
              _pickButton(Icons.folder_open_outlined, file == null ? 'Choose file' : 'Change file', PickSource.files),
              _pickButton(Icons.photo_library_outlined, 'Gallery', PickSource.gallery),
              _pickButton(Icons.photo_camera_outlined, 'Camera', PickSource.camera),
            ]),
            const SizedBox(height: 6),
            hintLine('PDF, PNG or JPEG, up to ${UploadRule.vendorInvoice.maxText}.'),
            errorLine(_fileError),
            sectionLabel('Details'),
            _text(_number, 'Invoice number', max: 100),
            _text(_amount, 'Amount (₦)', type: const TextInputType.numberWithOptions(decimal: true)),
            _text(_vendor, 'Vendor name'),
            Row(children: [
              Expanded(child: DateField(label: 'Invoice date', value: _invoiceDate, onChanged: (d) => setState(() => _invoiceDate = d!))),
              const SizedBox(width: 10),
              Expanded(child: DateField(label: 'Due date', value: _dueDate, first: _invoiceDate, clearable: true, onChanged: (d) => setState(() => _dueDate = d))),
            ]),
            _text(_notes, 'Notes (optional)', max: 1000, lines: 3),
            errorLine(_error),
            primaryButton('Upload invoice', _submit, busy: _busy),
          ]),
        ),
        const SizedBox(height: 24),
      ]),
    );
  }

  Widget _pickButton(IconData icon, String label, PickSource source) => OutlinedButton.icon(
        onPressed: (_picking || _busy) ? null : () => _pick(source),
        style: OutlinedButton.styleFrom(minimumSize: const Size(44, 44), foregroundColor: AgroColors.leaf, side: const BorderSide(color: AgroColors.leaf)),
        icon: Icon(icon, size: 18),
        label: Text(label),
      );
}

class _Preview extends StatelessWidget {
  final UploadKind kind;
  final Uint8List bytes;
  const _Preview({required this.kind, required this.bytes});

  @override
  Widget build(BuildContext context) {
    if (kind == UploadKind.pdf) {
      return Container(
        width: 56,
        height: 56,
        decoration: BoxDecoration(color: AgroColors.alert.withValues(alpha: 0.08), borderRadius: BorderRadius.circular(8)),
        child: const Icon(Icons.picture_as_pdf_outlined, color: AgroColors.alert),
      );
    }
    return ClipRRect(
      borderRadius: BorderRadius.circular(8),
      child: Image.memory(bytes, width: 56, height: 56, fit: BoxFit.cover, cacheWidth: 112, errorBuilder: (_, __, ___) => const Icon(Icons.image_outlined, size: 40)),
    );
  }
}
