// Models for the farm business profile, sales invoices and vendor invoices (bills received from suppliers).
// They are plain JSON maps from the API; money is held as double and rounded to 2 decimals like the server does.

double _num(dynamic v) => v is num ? v.toDouble() : double.tryParse('${v ?? ''}') ?? 0;
String? _str(dynamic v) {
  final s = v?.toString().trim();
  return (s == null || s.isEmpty) ? null : s;
}

/// A decimal read from a double's shortest text form (1.005 stays 1.005, not 1.00499999...), so rounding matches the
/// server's decimal arithmetic.
class _Dec {
  final BigInt n; // signed digits
  final int scale; // value = n / 10^scale
  const _Dec(this.n, this.scale);

  factory _Dec.of(double v) {
    var s = v.abs().toString();
    if (s.contains('e') || s.contains('E')) s = v.abs().toStringAsFixed(10);
    final dot = s.indexOf('.');
    final scale = dot < 0 ? 0 : s.length - dot - 1;
    final n = BigInt.parse(s.replaceAll('.', ''));
    return _Dec(v < 0 ? -n : n, scale);
  }

  _Dec operator *(_Dec o) => _Dec(n * o.n, scale + o.scale);

  /// Rounds to 2 decimals, halves away from zero.
  double toRounded2() {
    final neg = n.isNegative;
    var a = n.abs();
    if (scale > 2) {
      final div = BigInt.from(10).pow(scale - 2);
      var q = a ~/ div;
      if ((a % div) * BigInt.two >= div) q += BigInt.one;
      a = q;
    } else {
      a = a * BigInt.from(10).pow(2 - scale);
    }
    return (neg ? -a : a).toInt() / 100;
  }
}

/// Rounds to 2 decimals, halves away from zero (the server's rule), on the decimal value.
double round2(double v) => v.isFinite ? _Dec.of(v).toRounded2() : v;

/// `yyyy-MM-dd`, the API's date format.
String dateOnly(DateTime d) =>
    '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

DateTime? parseDateOnly(dynamic v) {
  if (v is! String || v.length < 10) return null;
  return DateTime.tryParse(v.substring(0, 10));
}

// ── BUSINESS PROFILE (printed on invoices) ──
class BusinessProfile {
  final String? address;
  final String? phone;
  final String? email;
  final String? website;
  final String? bankAccountName;
  final String? bankAccountNumber;
  final String? bankName;

  /// Farm brand colours (`#RRGGBB`, upper case) used by invoices that have no colour of their own; null = not set.
  final String? brandPrimaryColor;
  final String? brandAccentColor;

  const BusinessProfile({
    this.address,
    this.phone,
    this.email,
    this.website,
    this.bankAccountName,
    this.bankAccountNumber,
    this.bankName,
    this.brandPrimaryColor,
    this.brandAccentColor,
  });

  static const empty = BusinessProfile();

  factory BusinessProfile.fromJson(Map<String, dynamic> j) => BusinessProfile(
        address: _str(j['address']),
        phone: _str(j['phone']),
        email: _str(j['email']),
        website: _str(j['website']),
        bankAccountName: _str(j['bankAccountName']),
        bankAccountNumber: _str(j['bankAccountNumber']),
        bankName: _str(j['bankName']),
        brandPrimaryColor: normalizeHexColor(_str(j['brandPrimaryColor'])),
        brandAccentColor: normalizeHexColor(_str(j['brandAccentColor'])),
      );

  /// A copy with other brand colours (null clears one). Everything else is kept, so a PUT never drops a field.
  BusinessProfile withBrandColors({String? primary, String? accent}) => BusinessProfile(
        address: address,
        phone: phone,
        email: email,
        website: website,
        bankAccountName: bankAccountName,
        bankAccountNumber: bankAccountNumber,
        bankName: bankName,
        brandPrimaryColor: primary,
        brandAccentColor: accent,
      );

  /// A copy with other text fields; the brand colours are kept.
  BusinessProfile withFields({
    String? address,
    String? phone,
    String? email,
    String? website,
    String? bankAccountName,
    String? bankAccountNumber,
    String? bankName,
  }) =>
      BusinessProfile(
        address: address,
        phone: phone,
        email: email,
        website: website,
        bankAccountName: bankAccountName,
        bankAccountNumber: bankAccountNumber,
        bankName: bankName,
        brandPrimaryColor: brandPrimaryColor,
        brandAccentColor: brandAccentColor,
      );

  /// PUT is a full replace: blank / missing clears the field.
  Map<String, dynamic> toJson() => {
        'address': address,
        'phone': phone,
        'email': email,
        'website': website,
        'bankAccountName': bankAccountName,
        'bankAccountNumber': bankAccountNumber,
        'bankName': bankName,
        'brandPrimaryColor': brandPrimaryColor,
        'brandAccentColor': brandAccentColor,
      };
}

// ── BRAND COLOURS ──
// The server accepts exactly `#RRGGBB` (any case), answers in UPPER case and treats blank as "not set".

const String kDefaultPrimaryColor = '#138A44';
const String kDefaultAccentColor = '#F5C90C';

final RegExp _hexColor = RegExp(r'^#[0-9A-Fa-f]{6}$');

/// True when [s] (surrounding spaces ignored) is exactly `#RRGGBB`.
bool isHexColor(String? s) => s != null && _hexColor.hasMatch(s.trim());

/// `#rrggbb` -> `#RRGGBB`. Null for blank (= not set) and for anything that is not a valid colour.
String? normalizeHexColor(String? s) {
  final t = s?.trim();
  if (t == null || t.isEmpty) return null;
  return _hexColor.hasMatch(t) ? t.toUpperCase() : null;
}

/// Why [s] cannot be sent as the [label] (e.g. "primary colour"), or null when it is blank or valid.
String? hexColorProblem(String label, String? s) {
  final t = s?.trim() ?? '';
  if (t.isEmpty || _hexColor.hasMatch(t)) return null;
  return 'The $label must look like #138A44: a # followed by 6 digits or letters A to F.';
}

/// The packed ARGB value of a valid `#RRGGBB`, or null.
int? hexColorValue(String? s) {
  final n = normalizeHexColor(s);
  return n == null ? null : 0xFF000000 | int.parse(n.substring(1), radix: 16);
}

/// Where a resolved invoice colour came from, in words.
String colorSourceLabel(String? source) {
  switch (source) {
    case 'invoice':
      return 'set on this invoice';
    case 'farm':
      return "your farm's brand colour";
    default:
      return "the template's default";
  }
}

// ── INVOICE TEMPLATES ──
class InvoiceTemplate {
  final String key;
  final String name;
  final String? description;
  final bool isDefault;
  final String defaultPrimaryColor;
  final String defaultAccentColor;
  final String? previewUrl;
  const InvoiceTemplate({
    required this.key,
    required this.name,
    this.description,
    this.isDefault = false,
    this.defaultPrimaryColor = kDefaultPrimaryColor,
    this.defaultAccentColor = kDefaultAccentColor,
    this.previewUrl,
  });

  factory InvoiceTemplate.fromJson(Map<String, dynamic> j) => InvoiceTemplate(
        key: '${j['key']}',
        name: '${j['name'] ?? j['key']}',
        description: _str(j['description']),
        isDefault: j['isDefault'] == true,
        defaultPrimaryColor: normalizeHexColor(_str(j['defaultPrimaryColor'])) ?? kDefaultPrimaryColor,
        defaultAccentColor: normalizeHexColor(_str(j['defaultAccentColor'])) ?? kDefaultAccentColor,
        previewUrl: _str(j['previewUrl']),
      );

  Map<String, dynamic> toJson() => {
        'key': key,
        'name': name,
        'description': description,
        'isDefault': isDefault,
        'defaultPrimaryColor': defaultPrimaryColor,
        'defaultAccentColor': defaultAccentColor,
        'previewUrl': previewUrl,
      };
}

/// The server's order: the default template (ngf) first, then by name. Applied again on the phone so an older saved copy
/// or a reordered reply still shows the same order.
List<InvoiceTemplate> sortTemplates(Iterable<InvoiceTemplate> templates) {
  final list = templates.toList();
  list.sort((a, b) {
    if (a.isDefault != b.isDefault) return a.isDefault ? -1 : 1;
    return a.name.toLowerCase().compareTo(b.name.toLowerCase());
  });
  return list;
}

/// The template list from the API reply (entries without a key are ignored).
List<InvoiceTemplate> parseTemplates(dynamic json) => sortTemplates([
      for (final t in (json is List ? json : const []))
        if (t is Map && '${t['key'] ?? ''}'.isNotEmpty) InvoiceTemplate.fromJson(t.cast<String, dynamic>()),
    ]);

/// Path of the sample-PDF preview of [templateKey]. A colour that is null or invalid is left out, so the server falls back
/// to the farm brand colour and then the template default. The `#` is encoded as `%23`.
String invoicePreviewPath(String farmId, String templateKey, {String? primaryColor, String? accentColor}) {
  final primary = normalizeHexColor(primaryColor);
  final accent = normalizeHexColor(accentColor);
  final query = <String, String>{
    if (primary != null) 'primaryColor': primary,
    if (accent != null) 'accentColor': accent,
  };
  return Uri(
    path: '/api/farms/$farmId/invoice-templates/$templateKey/preview',
    queryParameters: query.isEmpty ? null : query,
  ).toString();
}

// ── SALES INVOICES ──

/// One row of the invoice list (`GET /invoices`).
class InvoiceSummary {
  final String id;
  final String reference;
  final String billToName;
  final DateTime? date;
  final DateTime? dueDate;
  final double total;
  final String templateKey;

  const InvoiceSummary({required this.id, required this.reference, required this.billToName, this.date, this.dueDate, required this.total, this.templateKey = 'ngf'});

  factory InvoiceSummary.fromJson(Map<String, dynamic> j) => InvoiceSummary(
        id: '${j['id']}',
        reference: '${j['reference'] ?? ''}',
        billToName: '${j['billToName'] ?? ''}',
        date: parseDateOnly(j['date']),
        dueDate: parseDateOnly(j['dueDate']),
        total: _num(j['total']),
        templateKey: '${j['templateKey'] ?? 'ngf'}',
      );

  Map<String, dynamic> toJson() => {
        'id': id, 'reference': reference, 'billToName': billToName,
        'date': date == null ? null : dateOnly(date!), 'dueDate': dueDate == null ? null : dateOnly(dueDate!),
        'total': total, 'templateKey': templateKey,
      };
}

class InvoiceLine {
  final String description;
  final double rate;
  final double quantity;
  const InvoiceLine({required this.description, required this.rate, required this.quantity});

  /// rate x quantity, 2 decimals (the server's value is the truth; this is the live preview).
  double get amount => (rate.isFinite && quantity.isFinite) ? (_Dec.of(rate) * _Dec.of(quantity)).toRounded2() : double.nan;

  factory InvoiceLine.fromJson(Map<String, dynamic> j) =>
      InvoiceLine(description: '${j['description'] ?? ''}', rate: _num(j['rate']), quantity: _num(j['quantity']));

  Map<String, dynamic> toJson() => {'description': description, 'rate': rate, 'quantity': quantity};
}

class InvoiceTotals {
  final double subtotal;
  final double vatAmount;
  final double total;
  const InvoiceTotals(this.subtotal, this.vatAmount, this.total);
}

/// Subtotal = sum of rounded line amounts, VAT on the subtotal, total = both. Same rules as the server.
InvoiceTotals computeInvoiceTotals(Iterable<InvoiceLine> lines, double vatPercent) {
  final subtotal = round2(lines.fold(0.0, (s, l) => s + l.amount));
  // subtotal x percent / 100 on decimals (the extra /100 is two more digits of scale).
  final vatRaw = _Dec.of(subtotal) * _Dec.of(vatPercent);
  final vat = _Dec(vatRaw.n, vatRaw.scale + 2).toRounded2();
  return InvoiceTotals(subtotal, vat, round2(subtotal + vat));
}

/// A whole invoice (`GET /invoices/{id}`); also the shape the form edits.
class Invoice {
  final String id;
  final String reference;
  final DateTime? date;
  final DateTime? dueDate;
  final String billToName;
  final String? billToPhone;
  final String? billToEmail;
  final String? billToAddress;
  final List<InvoiceLine> lineItems;
  final double vatPercent;
  final double subtotal;
  final double vatAmount;
  final double total;
  final String? paymentAccountName;
  final String? paymentAccountNumber;
  final String? paymentBankName;
  final String? termsAndNotes;
  final String? signatoryName;
  final String templateKey;

  /// Colours stored on the invoice (null = inherit the farm brand colour, then the template default).
  final String? primaryColor;
  final String? accentColor;

  /// What the PDF uses now, and where each came from: 'invoice' | 'farm' | 'default'.
  final String? resolvedPrimaryColor;
  final String? resolvedAccentColor;
  final String primaryColorSource;
  final String accentColorSource;

  const Invoice({
    required this.id,
    required this.reference,
    this.date,
    this.dueDate,
    required this.billToName,
    this.billToPhone,
    this.billToEmail,
    this.billToAddress,
    required this.lineItems,
    this.vatPercent = 0,
    this.subtotal = 0,
    this.vatAmount = 0,
    this.total = 0,
    this.paymentAccountName,
    this.paymentAccountNumber,
    this.paymentBankName,
    this.termsAndNotes,
    this.signatoryName,
    this.templateKey = 'ngf',
    this.primaryColor,
    this.accentColor,
    this.resolvedPrimaryColor,
    this.resolvedAccentColor,
    this.primaryColorSource = 'default',
    this.accentColorSource = 'default',
  });

  factory Invoice.fromJson(Map<String, dynamic> j) => Invoice(
        id: '${j['id']}',
        reference: '${j['reference'] ?? ''}',
        date: parseDateOnly(j['date']),
        dueDate: parseDateOnly(j['dueDate']),
        billToName: '${j['billToName'] ?? ''}',
        billToPhone: _str(j['billToPhone']),
        billToEmail: _str(j['billToEmail']),
        billToAddress: _str(j['billToAddress']),
        lineItems: [for (final l in (j['lineItems'] is List ? j['lineItems'] as List : const [])) if (l is Map) InvoiceLine.fromJson(l.cast<String, dynamic>())],
        vatPercent: _num(j['vatPercent']),
        subtotal: _num(j['subtotal']),
        vatAmount: _num(j['vatAmount']),
        total: _num(j['total']),
        paymentAccountName: _str(j['paymentAccountName']),
        paymentAccountNumber: _str(j['paymentAccountNumber']),
        paymentBankName: _str(j['paymentBankName']),
        termsAndNotes: _str(j['termsAndNotes']),
        signatoryName: _str(j['signatoryName']),
        templateKey: '${j['templateKey'] ?? 'ngf'}',
        primaryColor: normalizeHexColor(_str(j['primaryColor'])),
        accentColor: normalizeHexColor(_str(j['accentColor'])),
        resolvedPrimaryColor: normalizeHexColor(_str(j['resolvedPrimaryColor'])),
        resolvedAccentColor: normalizeHexColor(_str(j['resolvedAccentColor'])),
        primaryColorSource: _str(j['primaryColorSource']) ?? 'default',
        accentColorSource: _str(j['accentColorSource']) ?? 'default',
      );
}

/// The request body for create (POST) and update (PUT).
Map<String, dynamic> invoiceRequestBody({
  required DateTime date,
  required DateTime dueDate,
  required String billToName,
  String? billToPhone,
  String? billToEmail,
  String? billToAddress,
  required List<InvoiceLine> lineItems,
  required double vatPercent,
  String? paymentAccountName,
  String? paymentAccountNumber,
  String? paymentBankName,
  String? termsAndNotes,
  String? signatoryName,
  required String templateKey,
  String? primaryColor,
  String? accentColor,
}) {
  // PUT is a full replace: leaving a colour out clears it, so an edit passes the colours it loaded. Null = inherit.
  final problem = hexColorProblem('primary colour', primaryColor) ?? hexColorProblem('accent colour', accentColor);
  if (problem != null) throw ArgumentError(problem);
  return {
      'date': dateOnly(date),
      'dueDate': dateOnly(dueDate),
      'billToName': billToName.trim(),
      'billToPhone': _str(billToPhone),
      'billToEmail': _str(billToEmail),
      'billToAddress': _str(billToAddress),
      'lineItems': [for (final l in lineItems) l.toJson()],
      'vatPercent': vatPercent,
      'paymentAccountName': _str(paymentAccountName),
      'paymentAccountNumber': _str(paymentAccountNumber),
      'paymentBankName': _str(paymentBankName),
      'termsAndNotes': _str(termsAndNotes),
      'signatoryName': _str(signatoryName),
      'templateKey': templateKey,
      'primaryColor': normalizeHexColor(primaryColor),
      'accentColor': normalizeHexColor(accentColor),
    };
}

// ── VENDOR INVOICES ──
class VendorInvoice {
  final String id;
  final String orderId;
  final String orderReference;
  final double orderTotal;
  final bool orderIsPaid;
  final String vendorName;
  final String invoiceNumber;
  final double amount;
  final DateTime? invoiceDate;
  final DateTime? dueDate;
  final bool isPaid;
  final DateTime? paidAt;
  final bool isOverdue;
  final String? notes;
  final String fileName;
  final String contentType;
  final int fileSize;

  const VendorInvoice({
    required this.id,
    required this.orderId,
    this.orderReference = '',
    this.orderTotal = 0,
    this.orderIsPaid = false,
    required this.vendorName,
    required this.invoiceNumber,
    required this.amount,
    this.invoiceDate,
    this.dueDate,
    this.isPaid = false,
    this.paidAt,
    this.isOverdue = false,
    this.notes,
    this.fileName = '',
    this.contentType = '',
    this.fileSize = 0,
  });

  factory VendorInvoice.fromJson(Map<String, dynamic> j) => VendorInvoice(
        id: '${j['id']}',
        orderId: '${j['orderId'] ?? ''}',
        orderReference: '${j['orderReference'] ?? ''}',
        orderTotal: _num(j['orderTotal']),
        orderIsPaid: j['orderIsPaid'] == true,
        vendorName: '${j['vendorName'] ?? ''}',
        invoiceNumber: '${j['invoiceNumber'] ?? ''}',
        amount: _num(j['amount']),
        invoiceDate: parseDateOnly(j['invoiceDate']),
        dueDate: parseDateOnly(j['dueDate']),
        isPaid: '${j['status']}'.toLowerCase() == 'paid',
        paidAt: j['paidAt'] is String ? DateTime.tryParse(j['paidAt'] as String)?.toLocal() : null,
        // A paid bill is never overdue, whatever the server's flag says.
        isOverdue: j['isOverdue'] == true && '${j['status']}'.toLowerCase() != 'paid',
        notes: _str(j['notes']),
        fileName: '${j['fileName'] ?? ''}',
        contentType: '${j['contentType'] ?? ''}',
        fileSize: (j['fileSize'] as num?)?.toInt() ?? 0,
      );

  /// The vendor billed a different amount than the order total (worth a second look).
  bool get amountMismatch => (amount - orderTotal).abs() >= 0.005;

  bool get isPdf => contentType.toLowerCase().contains('pdf') || fileName.toLowerCase().endsWith('.pdf');
}

// ── LIMITS (mirror the API, so a user sees the problem before a server 400 / 422) ──
const int kMinYear = 2000;
const int kMaxYear = 2100;
const double kMaxRate = 100000000; // rate must be below
const double kMaxQuantity = 1000000; // quantity must be below
const double kMaxLineAmount = 1000000000; // a line's amount must be below
const double kMaxSubtotal = 10000000000; // the invoice subtotal must be below
const double kMaxBillAmount = 1000000000000; // a vendor bill amount must be below

String _commas(double v) =>
    v.toStringAsFixed(0).replaceAllMapped(RegExp(r'\B(?=(\d{3})+(?!\d))'), (_) => ',');

/// Why item [n] cannot be sent, or null when it is fine.
String? invoiceLineProblem(int n, InvoiceLine l) {
  if (l.rate < 0) return 'The rate of item $n cannot be negative.';
  if (l.quantity <= 0) return 'The quantity of item $n must be more than zero.';
  if (l.rate >= kMaxRate) return 'The rate of item $n must be below ₦${_commas(kMaxRate)}.';
  if (l.quantity >= kMaxQuantity) return 'The quantity of item $n must be below ${_commas(kMaxQuantity)}.';
  if (l.amount >= kMaxLineAmount) return 'The amount of item $n (rate x quantity) must be below ₦${_commas(kMaxLineAmount)}. Check the rate and quantity.';
  return null;
}

/// Why the invoice total is too big, or null.
String? invoiceSubtotalProblem(Iterable<InvoiceLine> lines) {
  final subtotal = lines.fold(0.0, (s, l) => s + l.amount);
  return subtotal >= kMaxSubtotal ? 'The invoice subtotal must be below ₦${_commas(kMaxSubtotal)}. Split it into two invoices.' : null;
}

/// Why a date cannot be used (the API accepts years 2000 to 2100), or null.
String? dateRangeProblem(String label, DateTime d) =>
    (d.year < kMinYear || d.year > kMaxYear) ? '$label must be between the years $kMinYear and $kMaxYear.' : null;

/// Why a vendor bill amount cannot be sent, or null.
String? billAmountProblem(double amount) {
  // The amount is sent to 2 dp, so anything under half a kobo would go out as 0.00 and be refused.
  if (!amount.isFinite || round2(amount) <= 0) return 'Enter the invoice amount (more than zero).';
  if (amount >= kMaxBillAmount) return 'The amount must be below ₦${_commas(kMaxBillAmount)}.';
  return null;
}
