import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';
import '../../services/api_client.dart';
import '../../services/auth_service.dart';
import '../../stores/app_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';

// Helpers shared by the activities / inventory / orders / approvals screens.

double dbl(dynamic v) => v is num ? v.toDouble() : double.tryParse('${v ?? ''}') ?? 0;

String qty(dynamic v) {
  final d = dbl(v);
  return d == d.roundToDouble() ? d.toStringAsFixed(0) : d.toStringAsFixed(2);
}

String blockName(BuildContext c, String? id) {
  for (final b in c.read<AppStore>().blocks) {
    if (b.id == id) return b.name;
  }
  return 'Block';
}

const activityNames = [
  'Weeding', 'Spraying', 'Fertilizer application', 'Maintenance', 'Harvesting', 'Clearing', 'Pruning', 'Pest control'
];

String activityName(dynamic t) {
  if (t is int && t >= 0 && t < activityNames.length) return activityNames[t];
  return t is String && t.isNotEmpty ? t : 'Activity';
}

/// ApiClient has no PUT; the pricing endpoint needs one.
Future<dynamic> apiPut(String path, Map<String, dynamic> body) async {
  final token = await AuthService.getToken();
  final res = await http
      .put(Uri.parse('${ApiClient.baseUrl}$path'),
          headers: {'Content-Type': 'application/json', if (token != null) 'Authorization': 'Bearer $token'},
          body: jsonEncode(body))
      .timeout(const Duration(seconds: 120));
  if (res.statusCode >= 200 && res.statusCode < 300) return res.body.isEmpty ? null : jsonDecode(res.body);
  var message = 'Request failed (HTTP ${res.statusCode})';
  try {
    final b = jsonDecode(res.body);
    if (b is Map && b['detail'] is String && (b['detail'] as String).isNotEmpty) message = b['detail'] as String;
  } catch (_) {}
  throw ApiException(message, res.statusCode);
}

Widget numField(TextEditingController c, String label, {String? hint}) => Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: TextField(
        controller: c,
        keyboardType: const TextInputType.numberWithOptions(decimal: true),
        decoration: InputDecoration(labelText: label, hintText: hint, border: const OutlineInputBorder()),
      ),
    );

Widget textField(TextEditingController c, String label, {String? hint, int lines = 1}) => Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: TextField(
        controller: c,
        maxLines: lines,
        decoration: InputDecoration(labelText: label, hintText: hint, border: const OutlineInputBorder()),
      ),
    );

Widget sheetTitle(String t, [String? sub]) => Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(t, style: GoogleFonts.syne(fontSize: 16, fontWeight: FontWeight.w700, color: AgroColors.forest)),
        if (sub != null) Text(sub, style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone)),
      ]),
    );

Widget primaryButton(String label, VoidCallback? onTap, {bool busy = false}) => SizedBox(
      width: double.infinity,
      child: ElevatedButton(
        onPressed: busy ? null : onTap,
        style: ElevatedButton.styleFrom(backgroundColor: AgroColors.leaf, foregroundColor: Colors.white, padding: const EdgeInsets.symmetric(vertical: 14)),
        child: busy
            ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
            : Text(label),
      ),
    );

AgroBadge statusBadge(String status) => AgroBadge(
      label: status,
      variant: switch (status) {
        'Approved' || 'Received' => AgroBadgeVariant.green,
        'Pending' || 'Placed' => AgroBadgeVariant.harvest,
        'Rejected' => AgroBadgeVariant.red,
        _ => AgroBadgeVariant.stone,
      },
    );
