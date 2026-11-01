import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../models/billing_models.dart';
import '../theme/app_theme.dart';

/// A curated set of brand-friendly colours offered as one-tap swatches (name, #RRGGBB).
const List<({String name, String hex})> kBrandPalette = [
  (name: 'Farm green', hex: '#138A44'),
  (name: 'Deep green', hex: '#065F46'),
  (name: 'Teal', hex: '#0D9488'),
  (name: 'Blue', hex: '#1D4ED8'),
  (name: 'Indigo', hex: '#4F46E5'),
  (name: 'Purple', hex: '#7C3AED'),
  (name: 'Pink', hex: '#DB2777'),
  (name: 'Red', hex: '#B91C1C'),
  (name: 'Orange', hex: '#EA580C'),
  (name: 'Gold', hex: '#F5C90C'),
  (name: 'Brown', hex: '#78350F'),
  (name: 'Charcoal', hex: '#1F2937'),
];

/// Black or white, whichever reads better on [c] (for the tick drawn on a selected swatch).
Color onColor(Color c) => c.computeLuminance() > 0.5 ? Colors.black : Colors.white;

/// A colour field without a colour-picker package: a row of palette swatches plus a `#RRGGBB` text box.
///
/// The text in [controller] is the value: blank means "not set" (the [defaultHex] or the inherited colour is used), anything
/// else must be a valid `#RRGGBB`. The owner reads the value with [normalizeHexColor] and checks it with [hexColorProblem].
class BrandColorField extends StatelessWidget {
  final TextEditingController controller;
  final String label;

  /// What is used while the field is blank (shown as the preview and in the hint).
  final String defaultHex;

  /// Words for [defaultHex], e.g. "template default" or "default".
  final String defaultLabel;
  final bool enabled;

  /// Called after every change (swatch tap, typing, reset) so the owner can rebuild.
  final VoidCallback onChanged;

  /// Show "Reset to default".
  final bool showReset;

  const BrandColorField({
    super.key,
    required this.controller,
    required this.label,
    required this.defaultHex,
    required this.onChanged,
    this.defaultLabel = 'default',
    this.enabled = true,
    this.showReset = true,
  });

  void _set(String hex) {
    controller.text = hex;
    controller.selection = TextSelection.collapsed(offset: hex.length);
    onChanged();
  }

  @override
  Widget build(BuildContext context) {
    final text = controller.text.trim();
    final problem = hexColorProblem(label.toLowerCase(), text);
    final chosen = normalizeHexColor(text);
    final shownHex = chosen ?? defaultHex;
    final shown = Color(hexColorValue(shownHex) ?? 0xFF138A44);
    return Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Text(label, style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13, fontWeight: FontWeight.w700, color: AgroColors.ink)),
      const SizedBox(height: 8),
      Wrap(spacing: 6, runSpacing: 6, children: [
        for (final p in kBrandPalette) _Swatch(name: p.name, hex: p.hex, selected: chosen == p.hex, enabled: enabled, onTap: () => _set(p.hex)),
      ]),
      const SizedBox(height: 10),
      Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Semantics(
          label: '$label preview, $shownHex${chosen == null ? ', $defaultLabel' : ''}',
          excludeSemantics: true,
          child: Container(
            width: 48,
            height: 48,
            margin: const EdgeInsets.only(top: 4),
            decoration: BoxDecoration(color: shown, borderRadius: BorderRadius.circular(10), border: Border.all(color: AgroColors.fog)),
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: TextField(
            controller: controller,
            enabled: enabled,
            autocorrect: false,
            enableSuggestions: false,
            textCapitalization: TextCapitalization.characters,
            keyboardType: TextInputType.visiblePassword,
            inputFormatters: [LengthLimitingTextInputFormatter(9)],
            onChanged: (_) => onChanged(),
            decoration: InputDecoration(
              labelText: '$label (#RRGGBB)',
              hintText: defaultHex,
              helperText: chosen == null && problem == null ? 'Not set: uses the $defaultLabel $defaultHex' : null,
              helperMaxLines: 2,
              errorText: problem,
              errorMaxLines: 3,
              border: const OutlineInputBorder(),
            ),
          ),
        ),
      ]),
      if (showReset && enabled && text.isNotEmpty)
        Align(
          alignment: Alignment.centerLeft,
          child: TextButton.icon(
            style: TextButton.styleFrom(minimumSize: const Size(44, 44)),
            onPressed: () => _set(''),
            icon: const Icon(Icons.restart_alt, size: 18),
            label: Text('Reset to default ($defaultHex)'),
          ),
        ),
    ]);
  }
}

class _Swatch extends StatelessWidget {
  final String name;
  final String hex;
  final bool selected;
  final bool enabled;
  final VoidCallback onTap;
  const _Swatch({required this.name, required this.hex, required this.selected, required this.enabled, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final color = Color(hexColorValue(hex)!);
    return Semantics(
      button: true,
      selected: selected,
      enabled: enabled,
      label: '$name $hex',
      excludeSemantics: true,
      onTap: enabled ? onTap : null,
      child: InkWell(
        onTap: enabled ? onTap : null,
        customBorder: const CircleBorder(),
        child: SizedBox(
          width: 44,
          height: 44,
          child: Center(
            child: Container(
              width: 34,
              height: 34,
              decoration: BoxDecoration(
                color: color,
                shape: BoxShape.circle,
                border: Border.all(color: selected ? AgroColors.ink : AgroColors.fog, width: selected ? 2.5 : 1),
              ),
              child: selected ? Icon(Icons.check, size: 18, color: onColor(color)) : null,
            ),
          ),
        ),
      ),
    );
  }
}
