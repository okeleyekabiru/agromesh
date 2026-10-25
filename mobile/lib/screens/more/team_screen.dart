import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../../services/api_client.dart';
import '../../theme/app_theme.dart';
import '../../widgets/widgets.dart';
import 'common.dart';

String _roleLabel(String r) => r == 'FarmManager' ? 'Farm manager' : r;

AgroBadgeVariant _roleVariant(String r) {
  switch (r) {
    case 'Admin':
      return AgroBadgeVariant.blue;
    case 'FarmManager':
      return AgroBadgeVariant.green;
    default:
      return AgroBadgeVariant.stone;
  }
}

/// Managers and supervisors. Admin and farm manager only; only an admin can disable accounts.
class TeamScreen extends StatefulWidget {
  const TeamScreen({super.key});

  @override
  State<TeamScreen> createState() => _TeamScreenState();
}

class _TeamScreenState extends State<TeamScreen> {
  int _version = 0;
  String? _busy;

  @override
  Widget build(BuildContext context) {
    if (!isManager(context)) {
      return Scaffold(
        appBar: AppBar(backgroundColor: AgroColors.forest, foregroundColor: AgroColors.white, title: const Text('Team')),
        body: emptyNote('Only admins and farm managers can see the team.'),
      );
    }
    final admin = isAdmin(context);
    return LoadScaffold<List<Map<String, dynamic>>>(
      key: ValueKey('team-$_version'),
      title: 'Team',
      load: () async => asList(await ApiClient.get('/api/users')),
      fab: FloatingActionButton.extended(
        backgroundColor: AgroColors.leaf,
        foregroundColor: AgroColors.white,
        onPressed: () async {
          final added = await showSheet<bool>(context, _AddMember(admin: admin));
          if (added == true && mounted) setState(() => _version++);
        },
        icon: const Icon(Icons.person_add_alt_1_outlined),
        label: const Text('Add member'),
      ),
      builder: (ctx, members, reload) {
        final active = members.where((m) => m['isActive'] != false).length;
        return ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          children: [
            Row(children: [
              Expanded(child: KpiCard(label: 'Members', value: '${members.length}', delta: '$active active', deltaType: KpiDeltaType.mute, dark: true)),
              const SizedBox(width: 10),
              Expanded(child: KpiCard(label: 'Disabled', value: '${members.length - active}', delta: 'Cannot sign in', deltaType: KpiDeltaType.mute)),
            ]),
            const SizedBox(height: 14),
            AgroCard(
              child: Column(
                children: [
                  AgroCardHeader(title: 'People', subtitle: admin ? 'You can disable accounts' : 'You can add supervisors'),
                  if (members.isEmpty) emptyNote('No team members yet.'),
                  for (final m in members) _row(ctx, m, admin, reload),
                ],
              ),
            ),
            const SizedBox(height: 90),
          ],
        );
      },
    );
  }

  Widget _row(BuildContext ctx, Map<String, dynamic> m, bool admin, Future<void> Function() reload) {
    final role = '${m['role'] ?? ''}';
    final active = m['isActive'] != false;
    final id = '${m['id']}';
    return Opacity(
      opacity: active ? 1 : 0.55,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: const BoxDecoration(border: Border(bottom: BorderSide(color: AgroColors.fog))),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('${m['fullName'] ?? ''}', style: GoogleFonts.inter(fontSize: 13, fontWeight: FontWeight.w600, color: AgroColors.ink)),
                  Text('${m['email'] ?? ''}', style: GoogleFonts.inter(fontSize: 11, color: AgroColors.stone), overflow: TextOverflow.ellipsis),
                  const SizedBox(height: 4),
                  Wrap(spacing: 6, children: [
                    AgroBadge(label: _roleLabel(role), variant: _roleVariant(role)),
                    if (!active) const AgroBadge(label: 'Disabled', variant: AgroBadgeVariant.red),
                  ]),
                ],
              ),
            ),
            if (admin && role != 'Admin')
              TextButton(
                onPressed: _busy == id ? null : () => _toggle(ctx, id, !active, reload),
                child: Text(active ? 'Disable' : 'Enable',
                    style: TextStyle(color: active ? AgroColors.alert : AgroColors.leaf, fontWeight: FontWeight.w600)),
              ),
          ],
        ),
      ),
    );
  }

  Future<void> _toggle(BuildContext ctx, String id, bool makeActive, Future<void> Function() reload) async {
    if (!makeActive) {
      final ok = await showDialog<bool>(
        context: ctx,
        builder: (c) => AlertDialog(
          title: const Text('Disable this account?'),
          content: const Text('They will be signed out and will not be able to sign in until you enable the account again.'),
          actions: [
            TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Cancel')),
            TextButton(onPressed: () => Navigator.pop(c, true), child: const Text('Disable')),
          ],
        ),
      );
      if (ok != true) return;
    }
    setState(() => _busy = id);
    try {
      await ApiClient.patch('/api/users/$id/status', {'isActive': makeActive});
      await reload();
    } catch (e) {
      if (ctx.mounted) toast(ctx, errText(e));
    } finally {
      if (mounted) setState(() => _busy = null);
    }
  }
}

class _AddMember extends StatefulWidget {
  final bool admin;
  const _AddMember({required this.admin});

  @override
  State<_AddMember> createState() => _AddMemberState();
}

class _AddMemberState extends State<_AddMember> {
  final _name = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  late String _role = widget.admin ? 'FarmManager' : 'Supervisor';
  bool _saving = false;
  String? _error;

  @override
  void dispose() {
    _name.dispose();
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (_name.text.trim().isEmpty || _email.text.trim().isEmpty || _password.text.length < 6) {
      setState(() => _error = 'Enter a name, an email and a password of at least 6 characters.');
      return;
    }
    setState(() { _saving = true; _error = null; });
    try {
      await ApiClient.post('/api/users', {
        'fullName': _name.text.trim(),
        'email': _email.text.trim(),
        'password': _password.text,
        'role': _role,
      });
      if (mounted) Navigator.pop(context, true);
    } catch (e) {
      if (mounted) setState(() { _error = errText(e); _saving = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text('Add a team member', style: GoogleFonts.syne(fontSize: 15, fontWeight: FontWeight.w700, color: AgroColors.forest)),
        const SizedBox(height: 12),
        TextField(controller: _name, textCapitalization: TextCapitalization.words,
            decoration: const InputDecoration(labelText: 'Full name', isDense: true, border: OutlineInputBorder())),
        const SizedBox(height: 10),
        TextField(controller: _email, keyboardType: TextInputType.emailAddress,
            decoration: const InputDecoration(labelText: 'Email', isDense: true, border: OutlineInputBorder())),
        const SizedBox(height: 10),
        TextField(controller: _password, obscureText: true,
            decoration: const InputDecoration(labelText: 'Temporary password', isDense: true, border: OutlineInputBorder())),
        const SizedBox(height: 12),
        if (widget.admin)
          SegmentedButton<String>(
            segments: const [
              ButtonSegment(value: 'FarmManager', label: Text('Farm manager')),
              ButtonSegment(value: 'Supervisor', label: Text('Supervisor')),
            ],
            selected: {_role},
            onSelectionChanged: _saving ? null : (s) => setState(() => _role = s.first),
          )
        else
          Text('Farm managers can add supervisors.', style: GoogleFonts.inter(fontSize: 11.5, color: AgroColors.stone)),
        if (_error != null)
          Padding(
            padding: const EdgeInsets.only(top: 8),
            child: Text(_error!, style: GoogleFonts.inter(fontSize: 11.5, color: AgroColors.alert)),
          ),
        const SizedBox(height: 14),
        Row(mainAxisAlignment: MainAxisAlignment.end, children: [
          TextButton(onPressed: _saving ? null : () => Navigator.pop(context), child: const Text('Cancel')),
          const SizedBox(width: 8),
          FilledButton(onPressed: _saving ? null : _save, child: Text(_saving ? 'Adding…' : 'Add')),
        ]),
      ],
    );
  }
}
