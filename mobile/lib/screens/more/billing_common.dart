import 'dart:io';
import 'package:flutter/material.dart';
import 'package:open_filex/open_filex.dart';
import 'package:path_provider/path_provider.dart';
import '../../models/billing_models.dart';
import '../../services/api_client.dart';
import '../../services/file_picking.dart';
import '../../theme/app_theme.dart';
import 'common.dart';

// Helpers shared by the logo / invoice / vendor-invoice screens.

/// The message to show for a failed call. The API's own `detail` (422 rule, 413 too large, 415 wrong type, 403 role)
/// is passed through; anything else means the server could not be reached.
String failText(Object e) {
  if (e is ApiException) {
    if (e.statusCode == 403) return e.message.startsWith('Request failed') ? 'Your role is not allowed to do this.' : e.message;
    if (e.statusCode == 413) return e.message.startsWith('Request failed') ? 'That file is too large for the server.' : e.message;
    if (e.statusCode == 415) return e.message.startsWith('Request failed') ? 'That file type is not accepted.' : e.message;
    return e.message;
  }
  if (e is PickFailure) return e.message;
  return 'Could not reach the server. Check your connection and try again.';
}

/// [failText] for the template preview: 401 (signed out), 404 (unknown farm or template) and 422 (bad colour) get their own words.
String previewFailText(Object e) {
  if (e is ApiException) {
    if (e.statusCode == 401) return 'Your session has expired. Sign in again to see the preview.';
    if (e.statusCode == 404) return 'That template is not available for this farm. Pick another template.';
    if (e.statusCode == 422) return e.message.startsWith('Request failed') ? 'The colours were not accepted. Use the form #RRGGBB.' : e.message;
  }
  return failText(e);
}

/// Writes [bytes] to a temporary file and opens it in another app (a PDF viewer, the gallery). Returns an error message,
/// or null when it opened.
Future<String?> saveAndOpen(List<int> bytes, String fileName) async {
  try {
    final dir = Directory('${(await getTemporaryDirectory()).path}/agromesh_open');
    await dir.create(recursive: true);
    await _purgeOldOpenedFiles(dir);
    final safe = fileName.replaceAll(RegExp(r'[^A-Za-z0-9._-]'), '_');
    final file = File('${dir.path}/$safe');
    await file.writeAsBytes(bytes, flush: true);
    final r = await OpenFilex.open(file.path);
    if (r.type != ResultType.done) {
      return 'Saved ${file.path.split(RegExp(r'[\\/]')).last}, but no app could open it. Install a viewer for this file type and try again.';
    }
    return null;
  } catch (_) {
    return 'The file could not be saved on this phone.';
  }
}

/// Invoices and bills are opened from a temp copy; copies older than a day are removed the next time one is opened.
Future<void> _purgeOldOpenedFiles(Directory dir) async {
  try {
    final cutoff = DateTime.now().subtract(const Duration(days: 1));
    await for (final e in dir.list()) {
      if (e is File && (await e.lastModified()).isBefore(cutoff)) await e.delete();
    }
  } catch (_) {/* best effort */}
}

/// A money amount at the end of a row: gets up to 40% of the row and shrinks to fit instead of overflowing
/// (large fonts, 9-digit totals). Pair it with an `Expanded(flex: 3)` for the text before it.
Widget amountCell(String text, TextStyle style) => Flexible(
      flex: 2,
      child: FittedBox(
        fit: BoxFit.scaleDown,
        alignment: Alignment.centerRight,
        child: Text(text, style: style),
      ),
    );

Widget errorLine(String? text) => text == null
    ? const SizedBox.shrink()
    : Padding(
        padding: const EdgeInsets.only(bottom: 8),
        child: Text(text, style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13, color: AgroColors.alert)),
      );

Widget hintLine(String text) => Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Text(text, style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 12.5, color: AgroColors.stone)),
    );

Widget sectionLabel(String text) => Padding(
      padding: const EdgeInsets.only(top: 6, bottom: 8),
      child: Text(text, style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 14, fontWeight: FontWeight.w700, color: AgroColors.forest)),
    );

/// A tappable field that opens the date picker. At least 48dp tall.
class DateField extends StatelessWidget {
  final String label;
  final DateTime? value;
  final ValueChanged<DateTime?> onChanged;
  final DateTime? first;
  final bool clearable;
  const DateField({super.key, required this.label, required this.value, required this.onChanged, this.first, this.clearable = false});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: InkWell(
        borderRadius: BorderRadius.circular(4),
        onTap: () async {
          final now = DateTime.now();
          var firstDate = first ?? DateTime(now.year - 5);
          var lastDate = DateTime(now.year + 5);
          // Keep the shown value inside the pickable range (an old invoice, or a due date before a later invoice date).
          if (value != null && value!.isAfter(lastDate)) lastDate = value!;
          if (first == null && value != null && value!.isBefore(firstDate)) firstDate = value!;
          // The API only accepts years 2000 to 2100.
          if (firstDate.year < kMinYear) firstDate = DateTime(kMinYear);
          if (lastDate.year > kMaxYear) lastDate = DateTime(kMaxYear, 12, 31);
          var initial = value ?? now;
          if (initial.isBefore(firstDate)) initial = firstDate;
          if (initial.isAfter(lastDate)) initial = lastDate;
          final picked = await showDatePicker(context: context, initialDate: initial, firstDate: firstDate, lastDate: lastDate);
          if (picked != null) onChanged(picked);
        },
        child: InputDecorator(
          decoration: InputDecoration(
            labelText: label,
            border: const OutlineInputBorder(),
            suffixIcon: clearable && value != null
                ? IconButton(tooltip: 'Clear $label', icon: const Icon(Icons.close, size: 18), onPressed: () => onChanged(null))
                : const Icon(Icons.calendar_today_outlined, size: 18),
          ),
          child: Text(
            value == null ? 'Not set' : dateText(dateOnly(value!)),
            style: TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 14, color: value == null ? AgroColors.stone : AgroColors.ink),
          ),
        ),
      ),
    );
  }
}

/// Money with two decimals for invoices and bills, e.g. ₦1,250.50 (the app's `naira` drops the kobo).
String nairaExact(num? v) {
  final value = (v ?? 0).toDouble();
  final fixed = value.abs().toStringAsFixed(2);
  final parts = fixed.split('.');
  final whole = parts[0].replaceAllMapped(RegExp(r'\B(?=(\d{3})+(?!\d))'), (_) => ',');
  return '${value < 0 ? '-' : ''}₦$whole.${parts[1]}';
}

/// Parses a typed number. Handles "1250.5", "1,250.50" (comma = thousands), "12,50" (a lone comma followed by 1 or 2
/// digits and no dot = decimal comma) and "1.250,50". Null when it is not a plain finite number (so "NaN", "Infinity"
/// and exponents are refused).
double? parseNumber(String s) {
  var t = s.replaceAll(RegExp(r'\s'), '');
  if (t.isEmpty || !RegExp(r'^[+-]?[0-9.,]+$').hasMatch(t)) return null;
  final lastDot = t.lastIndexOf('.');
  final lastComma = t.lastIndexOf(',');
  if (lastDot >= 0 && lastComma >= 0) {
    // Both present: whichever comes last is the decimal mark.
    t = lastComma > lastDot ? t.replaceAll('.', '').replaceAll(',', '.') : t.replaceAll(',', '');
  } else if (lastComma >= 0) {
    final isDecimalComma = ','.allMatches(t).length == 1 && RegExp(r'^[+-]?[0-9]*,[0-9]{1,2}$').hasMatch(t);
    t = isDecimalComma ? t.replaceAll(',', '.') : t.replaceAll(',', '');
  }
  final v = double.tryParse(t);
  return (v != null && v.isFinite) ? v : null;
}

/// True when a failed load should fall back to the saved copy: no connection, or the server/proxy is down or waking
/// (5xx). A real 4xx answer (not allowed, not found) is shown instead.
bool shouldUseOfflineCache(Object e) => e is! ApiException || (e.statusCode ?? 0) >= 500;

Future<bool> confirmDialog(BuildContext context, String title, String body, String action) async {
  final ok = await showDialog<bool>(
    context: context,
    builder: (ctx) => AlertDialog(
      title: Text(title),
      content: Text(body),
      actions: [
        TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancel')),
        TextButton(
          onPressed: () => Navigator.pop(ctx, true),
          style: TextButton.styleFrom(foregroundColor: AgroColors.alert),
          child: Text(action),
        ),
      ],
    ),
  );
  return ok == true;
}

/// Shown when the list is empty because the phone is offline and nothing was saved yet.
Widget offlineNote(String text) => Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: AgroColors.harvest.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: AgroColors.harvest.withValues(alpha: 0.3)),
      ),
      child: Row(children: [
        const Icon(Icons.cloud_off_outlined, size: 16, color: AgroColors.harvest),
        const SizedBox(width: 8),
        Expanded(child: Text(text, style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 12.5, color: AgroColors.ink))),
      ]),
    );

