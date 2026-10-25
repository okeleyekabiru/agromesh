import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../services/api_client.dart';
import '../../stores/app_store.dart';
import '../../stores/auth_store.dart';
import '../../theme/app_theme.dart';

// Shared helpers for the "More" screens. They read the API directly (JSON maps, no local cache), so each
// screen shows a loading state, an error with Retry, and pull-to-refresh.

String farmIdFrom(BuildContext c) => c.read<AppStore>().farmId;

bool isManager(BuildContext c) => c.read<AuthStore>().user?.isManager ?? false;
bool isAdmin(BuildContext c) => c.read<AuthStore>().user?.isAdmin ?? false;

String naira(num? v) {
  final n = (v ?? 0).round();
  final s = n.abs().toString().replaceAllMapped(RegExp(r'\B(?=(\d{3})+(?!\d))'), (_) => ',');
  return '${n < 0 ? '-' : ''}₦$s';
}

String num1(num? v, [int d = 1]) => (v ?? 0).toStringAsFixed(d);

const _months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

String dateText(dynamic iso) {
  final d = iso is String ? DateTime.tryParse(iso)?.toLocal() : null;
  return d == null ? '—' : '${d.day} ${_months[d.month - 1]} ${d.year}';
}

String errText(Object e) =>
    e is ApiException ? e.message : 'Could not reach the server. Check your connection and try again.';

void toast(BuildContext c, String msg) => ScaffoldMessenger.of(c).showSnackBar(SnackBar(content: Text(msg)));

List<Map<String, dynamic>> asList(dynamic v) =>
    (v is List ? v : const []).whereType<Map>().map((m) => m.cast<String, dynamic>()).toList();

/// Page chrome for a More screen: app bar, loading / error / data, pull-to-refresh.
class LoadScaffold<T> extends StatefulWidget {
  final String title;
  final Future<T> Function() load;
  final Widget Function(BuildContext context, T data, Future<void> Function() reload) builder;
  final Widget? fab;
  final List<Widget>? actions;
  const LoadScaffold({super.key, required this.title, required this.load, required this.builder, this.fab, this.actions});

  @override
  State<LoadScaffold<T>> createState() => _LoadScaffoldState<T>();
}

class _LoadScaffoldState<T> extends State<LoadScaffold<T>> {
  T? _data;
  String? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _reload();
  }

  Future<void> _reload() async {
    setState(() { _loading = _data == null; _error = null; });
    try {
      final d = await widget.load();
      if (mounted) setState(() { _data = d; _loading = false; });
    } catch (e) {
      if (mounted) setState(() { _error = errText(e); _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    Widget body;
    if (_loading) {
      body = const Center(child: CircularProgressIndicator(color: AgroColors.palm));
    } else if (_data == null) {
      body = ListView(children: [
        Padding(
          padding: const EdgeInsets.all(32),
          child: Column(children: [
            const Icon(Icons.cloud_off_outlined, color: AgroColors.stone, size: 36),
            const SizedBox(height: 10),
            Text(_error ?? 'Nothing to show.', textAlign: TextAlign.center,
                style: GoogleFonts.inter(fontSize: 13, color: AgroColors.stone)),
            const SizedBox(height: 12),
            OutlinedButton(onPressed: _reload, child: const Text('Retry')),
          ]),
        ),
      ]);
    } else {
      body = widget.builder(context, _data as T, _reload);
    }
    return Scaffold(
      backgroundColor: AgroColors.mist,
      appBar: AppBar(
        backgroundColor: AgroColors.forest,
        foregroundColor: AgroColors.white,
        title: Text(widget.title, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
        actions: widget.actions,
      ),
      floatingActionButton: widget.fab,
      body: RefreshIndicator(color: AgroColors.palm, onRefresh: _reload, child: body),
    );
  }
}

/// A simple label / value row.
class KvRow extends StatelessWidget {
  final String k;
  final String v;
  final Color? vColor;
  const KvRow(this.k, this.v, {super.key, this.vColor});

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Expanded(child: Text(k, style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone))),
          const SizedBox(width: 12),
          Flexible(
            child: Text(v, textAlign: TextAlign.right,
                style: GoogleFonts.inter(fontSize: 12, fontWeight: FontWeight.w600, color: vColor ?? AgroColors.ink)),
          ),
        ]),
      );
}

Widget emptyNote(String text) => Padding(
      padding: const EdgeInsets.all(28),
      child: Center(
        child: Text(text, textAlign: TextAlign.center, style: GoogleFonts.inter(fontSize: 12.5, color: AgroColors.stone)),
      ),
    );

/// Bottom-sheet form helper: shows [child] with keyboard padding.
Future<T?> showSheet<T>(BuildContext c, Widget child) => showModalBottomSheet<T>(
      context: c,
      isScrollControlled: true,
      backgroundColor: AgroColors.white,
      shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(16))),
      builder: (ctx) => Padding(
        padding: EdgeInsets.fromLTRB(16, 16, 16, 16 + MediaQuery.of(ctx).viewInsets.bottom),
        child: SingleChildScrollView(child: child),
      ),
    );

const _kWebUrl = 'agromesh_web_url';
const _webUrlDefine = String.fromEnvironment('WEB_APP_URL');

/// Opens a page of the AgroMesh web app in the browser. The web address is asked for once and remembered.
Future<void> openWebApp(BuildContext context, String path) async {
  const storage = FlutterSecureStorage();
  var base = _webUrlDefine.isNotEmpty ? _webUrlDefine : (await storage.read(key: _kWebUrl)) ?? '';
  if (base.isEmpty && context.mounted) {
    final ctrl = TextEditingController();
    final entered = await showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Web app address'),
        content: TextField(
          controller: ctrl,
          keyboardType: TextInputType.url,
          decoration: const InputDecoration(hintText: 'https://your-agromesh-web-address'),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cancel')),
          TextButton(onPressed: () => Navigator.pop(ctx, ctrl.text.trim()), child: const Text('Open')),
        ],
      ),
    );
    if (entered == null || entered.isEmpty) return;
    base = entered.startsWith('http') ? entered : 'https://$entered';
    await storage.write(key: _kWebUrl, value: base);
  }
  final uri = Uri.tryParse('${base.endsWith('/') ? base.substring(0, base.length - 1) : base}$path');
  if (uri == null || !await launchUrl(uri, mode: LaunchMode.externalApplication)) {
    if (context.mounted) toast(context, 'Could not open the web app.');
  }
}
