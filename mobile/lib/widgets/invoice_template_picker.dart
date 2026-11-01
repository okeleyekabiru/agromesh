import 'package:flutter/material.dart';
import '../models/billing_models.dart';
import '../theme/app_theme.dart';

/// A horizontally scrolling row of selectable template cards. Each shows a small page sketch in the colours the invoice would
/// use ([primaryOverride] / [accentOverride] when set, else the template's own defaults), the name and a short description.
class InvoiceTemplatePicker extends StatelessWidget {
  final List<InvoiceTemplate> templates;
  final String selectedKey;
  final ValueChanged<String> onSelected;
  final Color? primaryOverride;
  final Color? accentOverride;

  const InvoiceTemplatePicker({
    super.key,
    required this.templates,
    required this.selectedKey,
    required this.onSelected,
    this.primaryOverride,
    this.accentOverride,
  });

  @override
  Widget build(BuildContext context) {
    final scale = MediaQuery.textScalerOf(context).scale(1).clamp(1.0, 1.6);
    return SizedBox(
      height: 168 + 40 * (scale - 1),
      child: Semantics(
        container: true,
        label: 'Invoice template, ${templates.length} to choose from',
        child: ListView.separated(
          scrollDirection: Axis.horizontal,
          padding: const EdgeInsets.symmetric(vertical: 4),
          itemCount: templates.length,
          separatorBuilder: (context, index) => const SizedBox(width: 10),
          itemBuilder: (_, i) {
            final t = templates[i];
            return TemplateCard(
              template: t,
              selected: t.key == selectedKey,
              primary: primaryOverride ?? Color(hexColorValue(t.defaultPrimaryColor) ?? 0xFF138A44),
              accent: accentOverride ?? Color(hexColorValue(t.defaultAccentColor) ?? 0xFFF5C90C),
              onTap: () => onSelected(t.key),
            );
          },
        ),
      ),
    );
  }
}

class TemplateCard extends StatelessWidget {
  final InvoiceTemplate template;
  final bool selected;
  final Color primary;
  final Color accent;
  final VoidCallback onTap;
  const TemplateCard({super.key, required this.template, required this.selected, required this.primary, required this.accent, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final desc = template.description;
    return Semantics(
      button: true,
      selected: selected,
      label: '${template.name}${template.isDefault ? ', default' : ''}${desc == null ? '' : '. $desc'}',
      excludeSemantics: true,
      onTap: onTap,
      child: Material(
        color: selected ? AgroColors.mist : Colors.white,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: BorderSide(color: selected ? AgroColors.leaf : AgroColors.fog, width: selected ? 2 : 1),
        ),
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: onTap,
          child: SizedBox(
            width: 150,
            child: Padding(
              padding: const EdgeInsets.all(10),
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                SizedBox(height: 56, child: _PageSketch(primary: primary, accent: accent)),
                const SizedBox(height: 8),
                Row(children: [
                  Expanded(
                    child: Text(template.name,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 13, fontWeight: FontWeight.w800, color: AgroColors.forest)),
                  ),
                  if (selected) const Icon(Icons.check_circle, size: 16, color: AgroColors.leaf),
                ]),
                if (desc != null)
                  Expanded(
                    child: Text(desc,
                        maxLines: 3,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 11.5, color: AgroColors.stone)),
                  ),
              ]),
            ),
          ),
        ),
      ),
    );
  }
}

/// A tiny invoice page: a coloured header band with an accent stripe and a few grey rows.
class _PageSketch extends StatelessWidget {
  final Color primary;
  final Color accent;
  const _PageSketch({required this.primary, required this.accent});

  @override
  Widget build(BuildContext context) {
    Widget row(double w) => Container(height: 4, width: w, margin: const EdgeInsets.only(bottom: 4), decoration: BoxDecoration(color: AgroColors.fog, borderRadius: BorderRadius.circular(2)));
    return ExcludeSemantics(
      child: DecoratedBox(
        decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(6), border: Border.all(color: AgroColors.fog)),
        child: ClipRRect(
          borderRadius: BorderRadius.circular(6),
          child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
            Container(height: 16, color: primary),
            Container(height: 3, color: accent),
            Padding(
              padding: const EdgeInsets.fromLTRB(6, 6, 6, 0),
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [row(70), row(48), row(60)]),
            ),
          ]),
        ),
      ),
    );
  }
}
