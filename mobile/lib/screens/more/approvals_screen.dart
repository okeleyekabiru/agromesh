import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../services/api_client.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import 'common.dart';
import 'ops_common.dart';

/// Pending price changes and stock adjustments, to approve or reject (admins).
class ApprovalsScreen extends StatelessWidget {
  const ApprovalsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final farm = farmIdFrom(context);
    final admin = isAdmin(context);
    return LoadScaffold<List<List<Map<String, dynamic>>>>(
      title: 'Approvals',
      load: () async => [
        asList(await ApiClient.get('/api/farms/$farm/inventory-price-changes?status=Pending')),
        asList(await ApiClient.get('/api/farms/$farm/inventory-adjustments?status=Pending')),
      ],
      builder: (context, data, reload) {
        final prices = data[0], adjustments = data[1];
        return ListView(padding: const EdgeInsets.all(16), children: [
          if (!admin)
            Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: Text('Only an admin can approve or reject these.', style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone)),
            ),
          AgroCard(
            child: Column(children: [
              const AgroCardHeader(title: 'Pending price changes'),
              if (prices.isEmpty) emptyNote('Nothing waiting.'),
              for (final p in prices)
                _Request(
                  title: '${p['inventoryItemName']}',
                  lines: [
                    'Now ${naira(dbl(p['currentUnitCost']))} per ${p['unit']}',
                    'Asked ${naira(dbl(p['requestedPricePerPack']))} per ${p['requestedPackLabel']} of ${qty(p['requestedPackSize'])} (${naira(dbl(p['requestedPricePerUnit']))} per ${p['unit']})',
                    if (p['backfillActivities'] == true) 'Also costs past activities',
                    if ((p['notes'] ?? '') != '') '${p['notes']}',
                  ],
                  when: p['createdAt'],
                  canReview: admin,
                  base: '/api/farms/$farm/inventory-price-changes/${p['id']}',
                  reload: reload,
                ),
            ]),
          ),
          const SizedBox(height: 14),
          AgroCard(
            child: Column(children: [
              const AgroCardHeader(title: 'Pending stock adjustments'),
              if (adjustments.isEmpty) emptyNote('Nothing waiting.'),
              for (final a in adjustments)
                _Request(
                  title: '${a['inventoryItemName']}',
                  lines: [
                    '${qty(a['currentStock'])} → ${qty(a['requestedQuantity'])} ${a['unit']}',
                    if ((a['notes'] ?? '') != '') '${a['notes']}',
                  ],
                  when: a['createdAt'],
                  canReview: admin,
                  base: '/api/farms/$farm/inventory-adjustments/${a['id']}',
                  reload: reload,
                ),
            ]),
          ),
          const SizedBox(height: 40),
        ]);
      },
    );
  }
}

class _Request extends StatefulWidget {
  final String title;
  final List<String> lines;
  final dynamic when;
  final bool canReview;
  final String base;
  final Future<void> Function() reload;
  const _Request({required this.title, required this.lines, required this.when, required this.canReview, required this.base, required this.reload});
  @override
  State<_Request> createState() => _RequestState();
}

class _RequestState extends State<_Request> {
  bool _busy = false;

  Future<void> _decide(bool approve) async {
    String? notes;
    if (!approve) {
      final c = TextEditingController();
      notes = await showDialog<String>(
        context: context,
        builder: (ctx) => AlertDialog(
          title: const Text('Reject this request?'),
          content: TextField(controller: c, decoration: const InputDecoration(labelText: 'Reason (optional)')),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cancel')),
            TextButton(onPressed: () => Navigator.pop(ctx, c.text.trim()), child: const Text('Reject')),
          ],
        ),
      );
      if (notes == null) return;
    }
    setState(() => _busy = true);
    try {
      await ApiClient.patch('${widget.base}/${approve ? 'approve' : 'reject'}', {'reviewNotes': (notes ?? '').isEmpty ? null : notes});
      if (mounted) toast(context, approve ? 'Approved.' : 'Rejected.');
      await widget.reload();
    } catch (e) {
      if (mounted) {
        toast(context, errText(e));
        setState(() => _busy = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: AgroColors.fog))),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Row(children: [
          Expanded(child: Text(widget.title, style: GoogleFonts.inter(fontSize: 13.5, fontWeight: FontWeight.w600, color: AgroColors.ink))),
          Text(dateText(widget.when), style: GoogleFonts.inter(fontSize: 11, color: AgroColors.stone)),
        ]),
        const SizedBox(height: 4),
        for (final l in widget.lines) Text(l, style: GoogleFonts.inter(fontSize: 12, color: AgroColors.stone)),
        if (widget.canReview) ...[
          const SizedBox(height: 10),
          Row(children: [
            Expanded(child: OutlinedButton(onPressed: _busy ? null : () => _decide(false), child: const Text('Reject'))),
            const SizedBox(width: 10),
            Expanded(
              child: ElevatedButton(
                onPressed: _busy ? null : () => _decide(true),
                style: ElevatedButton.styleFrom(backgroundColor: AgroColors.leaf, foregroundColor: Colors.white),
                child: const Text('Approve'),
              ),
            ),
          ]),
        ],
      ]),
    );
  }
}
