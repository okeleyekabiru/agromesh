import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import '../../models/billing_models.dart';
import '../../services/api_client.dart';
import '../../services/database_service.dart';
import '../../services/file_picking.dart';
import '../../services/upload_rules.dart';
import '../../stores/app_store.dart';
import '../../theme/app_theme.dart';
import '../../widgets/brand_color_field.dart';
import '../../widgets/farm_mark.dart';
import '../../widgets/widgets.dart';
import 'billing_common.dart';
import 'common.dart';
import 'ops_common.dart';

/// Farm logo (upload / replace / remove) and the business profile printed on invoices.
/// Managers and the owner can change both; everyone else sees them read-only.
class FarmBrandingScreen extends StatefulWidget {
  const FarmBrandingScreen({super.key});

  @override
  State<FarmBrandingScreen> createState() => _FarmBrandingScreenState();
}

class _FarmBrandingScreenState extends State<FarmBrandingScreen> {
  final _address = TextEditingController();
  final _phone = TextEditingController();
  final _email = TextEditingController();
  final _website = TextEditingController();
  final _bankAccountName = TextEditingController();
  final _bankAccountNumber = TextEditingController();
  final _bankName = TextEditingController();
  final _brandPrimary = TextEditingController();
  final _brandAccent = TextEditingController();

  /// The profile as last loaded or saved. PUT is a full replace, so each save starts from this and changes only its own part.
  BusinessProfile _saved = BusinessProfile.empty;

  bool _loading = true;
  bool _offline = false;
  bool _savingProfile = false;
  bool _savingColors = false;
  String? _colorsError;
  bool _logoBusy = false;
  String? _loadError;
  String? _logoError;
  String? _profileError;

  late final String _farm;
  late final bool _canEdit;

  @override
  void initState() {
    super.initState();
    _farm = farmIdFrom(context);
    _canEdit = isManager(context);
    _loadProfile();
  }

  @override
  void dispose() {
    for (final c in [_address, _phone, _email, _website, _bankAccountName, _bankAccountNumber, _bankName, _brandPrimary, _brandAccent]) {
      c.dispose();
    }
    super.dispose();
  }

  void _fillColors(BusinessProfile p) {
    _brandPrimary.text = p.brandPrimaryColor ?? '';
    _brandAccent.text = p.brandAccentColor ?? '';
  }

  void _fill(BusinessProfile p) {
    _saved = p;
    _fillColors(p);
    _fillFields(p);
  }

  void _fillFields(BusinessProfile p) {
    _address.text = p.address ?? '';
    _phone.text = p.phone ?? '';
    _email.text = p.email ?? '';
    _website.text = p.website ?? '';
    _bankAccountName.text = p.bankAccountName ?? '';
    _bankAccountNumber.text = p.bankAccountNumber ?? '';
    _bankName.text = p.bankName ?? '';
  }

  Future<void> _loadProfile() async {
    setState(() { _loading = true; _loadError = null; });
    try {
      final j = await ApiClient.get('/api/farms/$_farm/business-profile');
      final map = (j is Map ? j : {}).cast<String, dynamic>();
      _fill(BusinessProfile.fromJson(map));
      _offline = false;
      await DatabaseService.saveCache('bizprofile:$_farm', [map]);
    } catch (e) {
      final cached = await DatabaseService.readCacheList('bizprofile:$_farm');
      if (cached.isNotEmpty) {
        _fill(BusinessProfile.fromJson(cached.first));
        _offline = true;
      } else {
        _loadError = failText(e);
      }
    }
    if (mounted) setState(() => _loading = false);
  }

  Future<void> _pickLogo(PickSource source) async {
    setState(() => _logoError = null);
    PickedUpload? picked;
    try {
      picked = await FilePicking.pick(source, shrink: true);
    } catch (e) {
      setState(() => _logoError = failText(e));
      return;
    }
    if (picked == null || !mounted) return;
    final problem = uploadProblem(UploadRule.logo, picked.bytes);
    if (problem != null) {
      setState(() => _logoError = problem);
      return;
    }
    final kind = sniffUpload(picked.bytes)!;
    final store = context.read<AppStore>();
    setState(() => _logoBusy = true);
    try {
      await store.uploadFarmLogo(picked.bytes, 'logo.${kind.extension}', kind.contentType);
      if (mounted) toast(context, 'Farm logo saved.');
    } catch (e) {
      if (mounted) setState(() => _logoError = failText(e));
    }
    if (mounted) setState(() => _logoBusy = false);
  }

  Future<void> _removeLogo() async {
    final ok = await confirmDialog(context, 'Remove the farm logo?', 'New invoices will print the farm name instead of the logo.', 'Remove');
    if (!ok || !mounted) return;
    final store = context.read<AppStore>();
    setState(() { _logoBusy = true; _logoError = null; });
    try {
      await store.removeFarmLogo();
      if (mounted) toast(context, 'Farm logo removed.');
    } catch (e) {
      if (mounted) setState(() => _logoError = failText(e));
    }
    if (mounted) setState(() => _logoBusy = false);
  }

  bool get _emailLooksOk {
    final t = _email.text.trim();
    return t.isEmpty || RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$').hasMatch(t);
  }

  Future<void> _saveProfile() async {
    if (!_emailLooksOk) {
      setState(() => _profileError = 'That email address does not look right.');
      return;
    }
    // The brand colours sent are the last saved ones, so this save never changes them.
    final profile = _saved.withFields(
      address: _address.text.trim(),
      phone: _phone.text.trim(),
      email: _email.text.trim(),
      website: _website.text.trim(),
      bankAccountName: _bankAccountName.text.trim(),
      bankAccountNumber: _bankAccountNumber.text.trim(),
      bankName: _bankName.text.trim(),
    );
    setState(() { _savingProfile = true; _profileError = null; });
    try {
      final saved = await _putProfile(profile);
      if (saved != null) _fillFields(saved);
      if (mounted) toast(context, 'Business profile saved.');
    } catch (e) {
      if (mounted) setState(() => _profileError = failText(e));
    }
    if (mounted) setState(() => _savingProfile = false);
  }

  /// PUT the whole profile (full replace; blank text becomes null), remember and cache the server's answer.
  Future<BusinessProfile?> _putProfile(BusinessProfile profile) async {
    final j = await ApiClient.put('/api/farms/$_farm/business-profile', {
      for (final e in profile.toJson().entries) e.key: (e.value as String?)?.isEmpty == true ? null : e.value,
    });
    if (j is! Map) return null;
    final map = j.cast<String, dynamic>();
    final saved = BusinessProfile.fromJson(map);
    _saved = saved;
    await DatabaseService.saveCache('bizprofile:$_farm', [map]);
    return saved;
  }

  /// Saves only the two brand colours; the other profile fields go back exactly as last saved. Blank = not set (null).
  Future<void> _saveColors() async {
    final problem = hexColorProblem('primary colour', _brandPrimary.text) ?? hexColorProblem('accent colour', _brandAccent.text);
    if (problem != null) {
      setState(() => _colorsError = problem);
      return;
    }
    setState(() { _savingColors = true; _colorsError = null; });
    try {
      final saved = await _putProfile(_saved.withBrandColors(
        primary: normalizeHexColor(_brandPrimary.text),
        accent: normalizeHexColor(_brandAccent.text),
      ));
      if (saved != null) _fillColors(saved);
      if (mounted) toast(context, 'Brand colours saved.');
    } catch (e) {
      if (mounted) setState(() => _colorsError = failText(e));
    }
    if (mounted) setState(() => _savingColors = false);
  }

  Widget _field(TextEditingController c, String label, int max, {TextInputType? type, int lines = 1}) => Padding(
        padding: const EdgeInsets.only(bottom: 10),
        child: TextField(
          controller: c,
          enabled: _canEdit,
          keyboardType: type,
          maxLines: lines,
          inputFormatters: [LengthLimitingTextInputFormatter(max)],
          decoration: InputDecoration(labelText: label, border: const OutlineInputBorder()),
        ),
      );

  @override
  Widget build(BuildContext context) {
    final bytes = context.select<AppStore, Uint8List?>((s) => s.farmLogoBytes);
    final farmName = context.select<AppStore, String>((s) => s.farmName);
    return Scaffold(
      backgroundColor: AgroColors.mist,
      appBar: AppBar(
        backgroundColor: AgroColors.forest,
        foregroundColor: AgroColors.white,
        title: const Text('Farm logo & profile', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: AgroColors.palm))
          : ListView(padding: const EdgeInsets.all(16), children: [
              if (_loadError != null)
                AgroCard(
                  child: Column(children: [
                    errorLine(_loadError),
                    OutlinedButton(onPressed: _loadProfile, child: const Text('Retry')),
                  ]),
                )
              else ...[
                if (_offline) offlineNote('Offline: showing the saved business profile. Connect to change it.'),
                AgroCard(
                  padding: const EdgeInsets.all(16),
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    sectionLabel('Farm logo'),
                    Row(children: [
                      Container(
                        width: 88,
                        height: 88,
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(color: AgroColors.fog),
                        ),
                        padding: const EdgeInsets.all(6),
                        child: bytes == null
                            ? const AgroMark(size: 64)
                            : BoundedLogoImage(bytes: bytes, size: 76, fallback: () => const AgroMark(size: 64)),
                      ),
                      const SizedBox(width: 14),
                      Expanded(
                        child: Text(
                          bytes == null
                              ? 'No logo yet. Add one to print it on $farmName invoices.'
                              : 'Printed on your invoices. The app keeps showing the AgroMesh logo.',
                          style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13, color: AgroColors.ink),
                        ),
                      ),
                    ]),
                    const SizedBox(height: 10),
                    hintLine('PNG, JPEG or WebP, up to ${UploadRule.logo.maxText}, at most 4096 x 4096 pixels. Needs a connection.'),
                    errorLine(_logoError),
                    if (_canEdit) ...[
                      Wrap(spacing: 8, runSpacing: 8, children: [
                        _action(Icons.photo_library_outlined, bytes == null ? 'Choose from gallery' : 'Replace from gallery', _logoBusy ? null : () => _pickLogo(PickSource.gallery)),
                        _action(Icons.photo_camera_outlined, 'Take photo', _logoBusy ? null : () => _pickLogo(PickSource.camera)),
                        _action(Icons.folder_open_outlined, 'Browse files', _logoBusy ? null : () => _pickLogo(PickSource.files)),
                        if (bytes != null)
                          _action(Icons.delete_outline, 'Remove logo', _logoBusy ? null : _removeLogo, danger: true),
                      ]),
                      if (_logoBusy)
                        const Padding(
                          padding: EdgeInsets.only(top: 12),
                          child: LinearProgressIndicator(color: AgroColors.palm, backgroundColor: AgroColors.fog),
                        ),
                    ] else
                      hintLine('Only the farm owner or a farm manager can change the logo.'),
                  ]),
                ),
                const SizedBox(height: 14),
                AgroCard(
                  padding: const EdgeInsets.all(16),
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    sectionLabel('Invoice brand colours'),
                    hintLine('Used by every invoice that has no colour of its own. Default: green $kDefaultPrimaryColor and yellow $kDefaultAccentColor. Reset clears a colour; save to apply.'),
                    BrandColorField(
                      controller: _brandPrimary,
                      label: 'Primary colour',
                      defaultHex: kDefaultPrimaryColor,
                      enabled: _canEdit && !_offline,
                      onChanged: () => setState(() {}),
                    ),
                    const SizedBox(height: 14),
                    BrandColorField(
                      controller: _brandAccent,
                      label: 'Accent colour',
                      defaultHex: kDefaultAccentColor,
                      enabled: _canEdit && !_offline,
                      onChanged: () => setState(() {}),
                    ),
                    const SizedBox(height: 6),
                    errorLine(_colorsError),
                    if (_canEdit)
                      primaryButton('Save brand colours', _offline ? null : _saveColors, busy: _savingColors)
                    else
                      hintLine('Only the farm owner or a farm manager can change the brand colours.'),
                  ]),
                ),
                const SizedBox(height: 14),
                AgroCard(
                  padding: const EdgeInsets.all(16),
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    sectionLabel('Business profile'),
                    hintLine('Printed on invoices. The bank details become the default payment details of new invoices.'),
                    _field(_address, 'Address', 300, lines: 2, type: TextInputType.streetAddress),
                    _field(_phone, 'Phone', 60, type: TextInputType.phone),
                    _field(_email, 'Email', 200, type: TextInputType.emailAddress),
                    _field(_website, 'Website', 200, type: TextInputType.url),
                    _field(_bankAccountName, 'Bank account name', 150),
                    _field(_bankAccountNumber, 'Bank account number', 40, type: TextInputType.number),
                    _field(_bankName, 'Bank name', 100),
                    errorLine(_profileError),
                    if (_canEdit) primaryButton('Save business profile', _offline ? null : _saveProfile, busy: _savingProfile),
                  ]),
                ),
              ],
              const SizedBox(height: 24),
            ]),
    );
  }

  Widget _action(IconData icon, String label, VoidCallback? onTap, {bool danger = false}) => OutlinedButton.icon(
        onPressed: onTap,
        style: OutlinedButton.styleFrom(
          minimumSize: const Size(44, 44),
          foregroundColor: danger ? AgroColors.alert : AgroColors.leaf,
          side: BorderSide(color: danger ? AgroColors.alert : AgroColors.leaf),
        ),
        icon: Icon(icon, size: 18),
        label: Text(label),
      );
}
