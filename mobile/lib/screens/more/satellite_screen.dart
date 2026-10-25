import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../services/api_client.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import 'common.dart';

/// Crop condition per block from Sentinel-2 (greenness NDVI, leaf water NDMI), and the pictures behind it.
class SatelliteScreen extends StatefulWidget {
  const SatelliteScreen({super.key});

  @override
  State<SatelliteScreen> createState() => _SatelliteScreenState();
}

class _SatelliteScreenState extends State<SatelliteScreen> {
  int _version = 0;
  bool _refreshing = false;

  Future<Map<String, dynamic>> _load() async {
    final json = await ApiClient.get('/api/farms/${farmIdFrom(context)}/blocks/satellite');
    return (json as Map).cast<String, dynamic>();
  }

  Future<void> _refresh(Future<void> Function() reload) async {
    setState(() => _refreshing = true);
    try {
      await ApiClient.post('/api/farms/${farmIdFrom(context)}/blocks/satellite/refresh', {});
      await reload();
    } catch (e) {
      if (mounted) toast(context, errText(e));
    } finally {
      if (mounted) setState(() => _refreshing = false);
    }
  }

  AgroBadgeVariant _variant(String c) => switch (c) {
        'Improving' || 'Stable' => AgroBadgeVariant.green,
        'Building' => AgroBadgeVariant.blue,
        'Declining' => AgroBadgeVariant.harvest,
        'DryOrBare' => AgroBadgeVariant.red,
        _ => AgroBadgeVariant.stone,
      };

  @override
  Widget build(BuildContext context) {
    final manager = isManager(context);
    return LoadScaffold<Map<String, dynamic>>(
      key: ValueKey(_version),
      title: 'Satellite',
      load: _load,
      builder: (ctx, data, reload) {
        final blocks = asList(data['blocks']);
        final configured = data['configured'] == true;
        return ListView(
          padding: const EdgeInsets.all(16),
          physics: const AlwaysScrollableScrollPhysics(),
          children: [
            if (!configured)
              AgroCard(child: emptyNote('Satellite monitoring is not connected yet. Add the Copernicus client id and secret to the API settings.'))
            else ...[
              Row(
                children: [
                  Expanded(
                    child: Text('Last updated ${dateText(data['lastSyncAt'])}',
                        style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone)),
                  ),
                  if (manager)
                    OutlinedButton(
                      onPressed: _refreshing ? null : () => _refresh(reload),
                      child: Text(_refreshing ? 'Fetching…' : 'Refresh'),
                    ),
                ],
              ),
              const SizedBox(height: 10),
              if (blocks.isEmpty) AgroCard(child: emptyNote('No blocks yet.')),
              for (final b in blocks) ...[
                _BlockCard(block: b, variant: _variant('${b['condition']}'), onTap: () {
                  Navigator.push(ctx, MaterialPageRoute(builder: (_) => _PictureScreen(blockId: '${b['blockId']}', name: '${b['blockName']}')));
                }),
                const SizedBox(height: 10),
              ],
            ],
            const SizedBox(height: 80),
          ],
        );
      },
    );
  }
}

class _BlockCard extends StatelessWidget {
  final Map<String, dynamic> block;
  final AgroBadgeVariant variant;
  final VoidCallback onTap;
  const _BlockCard({required this.block, required this.variant, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final points = asList(block['points']);
    final ndvi = block['ndvi'] as num?;
    final ndmi = block['ndmi'] as num?;
    return AgroCard(
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text('${block['blockName']}',
                        style: GoogleFonts.syne(fontSize: 14, fontWeight: FontWeight.w700, color: AgroColors.forest)),
                  ),
                  AgroBadge(label: '${block['label'] ?? block['condition']}', variant: variant),
                ],
              ),
              if ('${block['summary'] ?? ''}'.isNotEmpty) ...[
                const SizedBox(height: 6),
                Text('${block['summary']}', style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone, height: 1.4)),
              ],
              const SizedBox(height: 10),
              Row(
                children: [
                  _Metric('Greenness (NDVI)', ndvi == null ? '—' : ndvi.toStringAsFixed(2)),
                  const SizedBox(width: 20),
                  _Metric('Leaf water (NDMI)', ndmi == null ? '—' : ndmi.toStringAsFixed(2)),
                  const Spacer(),
                  if (points.length > 1)
                    SizedBox(width: 90, height: 30, child: CustomPaint(painter: _Spark(points.map((p) => (p['ndvi'] as num?)?.toDouble() ?? 0).toList()))),
                ],
              ),
              const SizedBox(height: 8),
              Text(block['hasBoundary'] == false ? 'No usable boundary: redraw it on the web app.' : 'Tap to see the pictures',
                  style: GoogleFonts.inter(fontSize: 11, color: AgroColors.leaf, fontWeight: FontWeight.w600)),
            ],
          ),
        ),
      ),
    );
  }
}

class _Metric extends StatelessWidget {
  final String label;
  final String value;
  const _Metric(this.label, this.value);

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(value, style: GoogleFonts.jetBrainsMono(fontSize: 15, fontWeight: FontWeight.w600, color: AgroColors.forest)),
          Text(label, style: GoogleFonts.inter(fontSize: 10, color: AgroColors.stone)),
        ],
      );
}

class _Spark extends CustomPainter {
  final List<double> v;
  _Spark(this.v);

  @override
  void paint(Canvas canvas, Size size) {
    if (v.length < 2) return;
    final lo = v.reduce((a, b) => a < b ? a : b);
    final hi = v.reduce((a, b) => a > b ? a : b);
    final span = (hi - lo) == 0 ? 1.0 : hi - lo;
    final path = Path();
    for (var i = 0; i < v.length; i++) {
      final x = size.width * i / (v.length - 1);
      final y = size.height - (v[i] - lo) / span * size.height;
      i == 0 ? path.moveTo(x, y) : path.lineTo(x, y);
    }
    canvas.drawPath(path, Paint()..color = AgroColors.palm..style = PaintingStyle.stroke..strokeWidth = 2);
  }

  @override
  bool shouldRepaint(covariant _Spark old) => old.v != v;
}

/// The picture for one block: layer chips and a date stepper.
class _PictureScreen extends StatefulWidget {
  final String blockId;
  final String name;
  const _PictureScreen({required this.blockId, required this.name});

  @override
  State<_PictureScreen> createState() => _PictureScreenState();
}

class _PictureScreenState extends State<_PictureScreen> {
  static const _layers = [('TrueColour', 'True colour'), ('Ndvi', 'Greenness'), ('Ndmi', 'Leaf water')];

  Map<String, dynamic>? _view;
  List<Map<String, dynamic>> _dates = [];
  String? _error;
  String? _imgError;
  bool _loading = true;
  bool _imgLoading = false;
  String _layer = 'Ndvi';
  int _idx = 0;
  Uint8List? _bytes;
  int _seq = 0;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _load());
  }

  String get _base => '/api/farms/${farmIdFrom(context)}/blocks/${widget.blockId}/satellite';

  Future<void> _load() async {
    try {
      final json = (await ApiClient.get(_base) as Map).cast<String, dynamic>();
      _view = json;
      _dates = asList(json['dates']);
      final clear = _dates.indexWhere((d) => ((d['clearShare'] as num?) ?? 0) >= 0.3);
      _idx = clear >= 0 ? clear : 0;
      if (mounted) setState(() => _loading = false);
      await _show();
    } catch (e) {
      if (mounted) setState(() { _error = errText(e); _loading = false; });
    }
  }

  Future<void> _show() async {
    if (_dates.isEmpty || _view?['hasBoundary'] != true) return;
    final mine = ++_seq;
    setState(() { _imgLoading = true; _imgError = null; });
    try {
      final date = '${_dates[_idx]['date']}'.substring(0, 10);
      final bytes = await ApiClient.getBytes('$_base/image?layer=$_layer&date=$date');
      if (mine != _seq || !mounted) return;
      setState(() => _bytes = Uint8List.fromList(bytes));
    } catch (e) {
      if (mine == _seq && mounted) setState(() { _imgError = errText(e); _bytes = null; });
    } finally {
      if (mine == _seq && mounted) setState(() => _imgLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    Widget body;
    if (_loading) {
      body = const Center(child: CircularProgressIndicator(color: AgroColors.palm));
    } else if (_error != null) {
      body = emptyNote(_error!);
    } else if (_view?['configured'] != true) {
      body = emptyNote('Satellite monitoring is not connected yet.');
    } else if (_view?['hasBoundary'] != true) {
      body = emptyNote('This block has no usable boundary, so there is no picture to show. Redraw its boundary on the web app.');
    } else if (_dates.isEmpty) {
      body = emptyNote('No satellite readings for this block yet. A manager can press Refresh on the Satellite page.');
    } else {
      final d = _dates[_idx];
      body = ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Wrap(
            spacing: 8,
            children: [
              for (final l in _layers)
                ChoiceChip(
                  label: Text(l.$2),
                  selected: _layer == l.$1,
                  onSelected: (_) { setState(() => _layer = l.$1); _show(); },
                ),
            ],
          ),
          const SizedBox(height: 10),
          Row(
            children: [
              IconButton(
                onPressed: _idx < _dates.length - 1 ? () { setState(() => _idx++); _show(); } : null,
                icon: const Icon(Icons.chevron_left),
              ),
              Expanded(
                child: Text(
                  '${dateText(d['date'])} · ${(((d['clearShare'] as num?) ?? 0) * 100).round()}% clear',
                  textAlign: TextAlign.center,
                  style: GoogleFonts.inter(fontSize: 13, fontWeight: FontWeight.w600),
                ),
              ),
              IconButton(
                onPressed: _idx > 0 ? () { setState(() => _idx--); _show(); } : null,
                icon: const Icon(Icons.chevron_right),
              ),
            ],
          ),
          AspectRatio(
            aspectRatio: 1,
            child: Container(
              decoration: BoxDecoration(color: AgroColors.forest, borderRadius: BorderRadius.circular(10)),
              clipBehavior: Clip.antiAlias,
              child: Stack(
                fit: StackFit.expand,
                children: [
                  if (_bytes != null) Image.memory(_bytes!, fit: BoxFit.contain),
                  if (_imgLoading) const Center(child: CircularProgressIndicator(color: AgroColors.palm)),
                ],
              ),
            ),
          ),
          if (_imgError != null)
            Padding(padding: const EdgeInsets.only(top: 8), child: Text(_imgError!, style: GoogleFonts.inter(fontSize: 12, color: AgroColors.alert))),
          const SizedBox(height: 8),
          Text(
            _layer == 'Ndvi'
                ? 'Greenness (NDVI): how dense and healthy the crop is.'
                : _layer == 'Ndmi'
                    ? 'Leaf water (NDMI): how much water the leaves hold.'
                    : 'Natural colour, 10 m detail. White patches are cloud.',
            style: GoogleFonts.inter(fontSize: 11.5, color: AgroColors.stone),
          ),
          Text('One picture covers a 5-day window.', style: GoogleFonts.inter(fontSize: 11.5, color: AgroColors.stone)),
        ],
      );
    }
    return Scaffold(
      backgroundColor: AgroColors.mist,
      appBar: AppBar(
        backgroundColor: AgroColors.forest,
        foregroundColor: AgroColors.white,
        title: Text('${widget.name} · satellite', style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
      ),
      body: body,
    );
  }
}
