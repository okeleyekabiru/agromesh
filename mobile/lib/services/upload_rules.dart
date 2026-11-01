/// Client-side checks for files sent to the API (farm logo, vendor invoices), mirroring the server's rules so a bad
/// file is refused on the phone instead of after a slow upload. The server stays the authority (it checks the bytes too).
enum UploadKind { pdf, png, jpeg, webp }

class UploadRule {
  final String label;
  final int maxBytes;
  final Set<UploadKind> allowed;
  const UploadRule(this.label, this.maxBytes, this.allowed);

  /// Farm logo: PNG, JPEG or WebP, up to 2 MB.
  static const logo = UploadRule('The logo', 2 * 1024 * 1024, {UploadKind.png, UploadKind.jpeg, UploadKind.webp});

  /// Vendor invoice: PDF, PNG or JPEG, up to 10 MB.
  static const vendorInvoice = UploadRule('The invoice file', 10 * 1024 * 1024, {UploadKind.pdf, UploadKind.png, UploadKind.jpeg});

  String get allowedText => allowed.map((k) => k.label).join(', ');
  String get maxText => '${maxBytes ~/ (1024 * 1024)} MB';
}

extension UploadKindInfo on UploadKind {
  String get label => switch (this) { UploadKind.pdf => 'PDF', UploadKind.png => 'PNG', UploadKind.jpeg => 'JPEG', UploadKind.webp => 'WebP' };
  String get contentType => switch (this) {
        UploadKind.pdf => 'application/pdf',
        UploadKind.png => 'image/png',
        UploadKind.jpeg => 'image/jpeg',
        UploadKind.webp => 'image/webp',
      };
  String get extension => switch (this) { UploadKind.pdf => 'pdf', UploadKind.png => 'png', UploadKind.jpeg => 'jpg', UploadKind.webp => 'webp' };
}

/// Works out the real file type from its first bytes (the name or a declared type can lie). Null when unknown.
UploadKind? sniffUpload(List<int> b) {
  bool starts(List<int> sig, [int at = 0]) {
    if (b.length < at + sig.length) return false;
    for (var i = 0; i < sig.length; i++) {
      if (b[at + i] != sig[i]) return false;
    }
    return true;
  }

  if (starts([0x25, 0x50, 0x44, 0x46, 0x2D])) return UploadKind.pdf; // %PDF-
  if (starts([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])) return UploadKind.png;
  if (starts([0xFF, 0xD8, 0xFF])) return UploadKind.jpeg;
  if (starts([0x52, 0x49, 0x46, 0x46]) && starts([0x57, 0x45, 0x42, 0x50], 8)) return UploadKind.webp; // RIFF....WEBP
  return null;
}

/// Returns the message to show when [bytes] cannot be sent under [rule], or null when it is fine.
String? uploadProblem(UploadRule rule, List<int> bytes) {
  if (bytes.isEmpty) return '${rule.label} is empty. Choose another file.';
  if (bytes.length > rule.maxBytes) {
    return '${rule.label} is ${formatBytes(bytes.length)}. The limit is ${rule.maxText}. Choose a smaller file.';
  }
  final kind = sniffUpload(bytes);
  if (kind == null || !rule.allowed.contains(kind)) {
    return '${rule.label} must be a ${rule.allowedText} file.';
  }
  return null;
}

String formatBytes(int n) {
  if (n >= 1024 * 1024) return '${(n / (1024 * 1024)).toStringAsFixed(1)} MB';
  if (n >= 1024) return '${(n / 1024).toStringAsFixed(0)} KB';
  return '$n B';
}
