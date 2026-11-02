import 'package:ilevora_mobile/models/billing_models.dart';
import 'package:ilevora_mobile/screens/more/billing_common.dart';
import 'package:ilevora_mobile/services/api_client.dart';
import 'package:ilevora_mobile/widgets/brand_color_field.dart';
import 'package:ilevora_mobile/widgets/invoice_template_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

Map<String, dynamic> _tpl(String key, String name, {bool isDefault = false, String p = '#111111', String a = '#222222'}) => {
      'key': key,
      'name': name,
      'description': 'About $name',
      'isDefault': isDefault,
      'defaultPrimaryColor': p,
      'defaultAccentColor': a,
      'previewUrl': '/api/farms/f1/invoice-templates/$key/preview',
    };

Map<String, dynamic> _body({String? primary, String? accent}) => invoiceRequestBody(
      date: DateTime(2026, 11, 1),
      dueDate: DateTime(2026, 11, 15),
      billToName: 'Ada',
      lineItems: const [InvoiceLine(description: 'Eggs', rate: 100, quantity: 2)],
      vatPercent: 0,
      templateKey: 'band',
      primaryColor: primary,
      accentColor: accent,
    );

void main() {
  group('hex colour validation', () {
    test('accepts #RRGGBB in any case', () {
      for (final ok in ['#138A44', '#138a44', '#AbCdEf', '#000000', ' #FFFFFF ']) {
        expect(isHexColor(ok), isTrue, reason: ok);
        expect(hexColorProblem('primary colour', ok), isNull, reason: ok);
      }
    });

    test('rejects short, named, rgb(), missing # and other forms', () {
      for (final bad in ['#FFF', 'red', 'rgb(1,2,3)', '138A44', 'FFFFFF', '#12345G', '#11223344', '##112233', '# 112233']) {
        expect(isHexColor(bad), isFalse, reason: bad);
        expect(normalizeHexColor(bad), isNull, reason: bad);
        expect(hexColorProblem('primary colour', bad), contains('#138A44'), reason: bad);
      }
    });

    test('blank means not set', () {
      expect(hexColorProblem('accent colour', ''), isNull);
      expect(hexColorProblem('accent colour', '   '), isNull);
      expect(hexColorProblem('accent colour', null), isNull);
      expect(normalizeHexColor('  '), isNull);
    });

    test('normalizes to upper case and parses to ARGB', () {
      expect(normalizeHexColor('#b91c1c'), '#B91C1C');
      expect(hexColorValue('#b91c1c'), 0xFFB91C1C);
      expect(hexColorValue('red'), isNull);
    });

    test('the palette has 12 valid, distinct colours', () {
      expect(kBrandPalette.length, 12);
      expect(kBrandPalette.map((p) => p.hex).toSet().length, 12);
      expect(kBrandPalette.every((p) => isHexColor(p.hex)), isTrue);
    });
  });

  group('request building', () {
    test('no colours sends explicit nulls (inherit)', () {
      final b = _body();
      expect(b.containsKey('primaryColor'), isTrue);
      expect(b['primaryColor'], isNull);
      expect(b['accentColor'], isNull);
    });

    test('custom colours are sent upper-case', () {
      final b = _body(primary: '#b91c1c', accent: '#f5c90c');
      expect(b['primaryColor'], '#B91C1C');
      expect(b['accentColor'], '#F5C90C');
    });

    test('one custom colour leaves the other null; blank is null', () {
      final b = _body(primary: '#4F46E5', accent: '  ');
      expect(b['primaryColor'], '#4F46E5');
      expect(b['accentColor'], isNull);
    });

    test('an invalid colour is refused before sending', () {
      expect(() => _body(primary: '#FFF'), throwsArgumentError);
      expect(() => _body(accent: 'red'), throwsArgumentError);
    });

    test('an edit that loaded colours sends them back (PUT would clear them otherwise)', () {
      final loaded = Invoice.fromJson({
        'id': '1',
        'primaryColor': '#B91C1C',
        'accentColor': null,
        'resolvedPrimaryColor': '#B91C1C',
        'resolvedAccentColor': '#F5C90C',
        'primaryColorSource': 'invoice',
        'accentColorSource': 'default',
      });
      expect(loaded.primaryColor, '#B91C1C');
      expect(loaded.accentColor, isNull);
      expect(loaded.resolvedAccentColor, '#F5C90C');
      expect(loaded.primaryColorSource, 'invoice');
      expect(loaded.accentColorSource, 'default');
      final b = _body(primary: loaded.primaryColor, accent: loaded.accentColor);
      expect(b['primaryColor'], '#B91C1C');
      expect(b['accentColor'], isNull);
    });

    test('an old invoice reply without the new fields still parses', () {
      final inv = Invoice.fromJson({'id': '1', 'reference': 'INV-1'});
      expect(inv.primaryColor, isNull);
      expect(inv.primaryColorSource, 'default');
    });
  });

  group('business profile', () {
    test('parses and writes the brand colours; withFields keeps them, withBrandColors keeps the fields', () {
      final p = BusinessProfile.fromJson({'address': 'Farm Rd', 'bankName': 'GTB', 'brandPrimaryColor': '#b91c1c', 'brandAccentColor': null});
      expect(p.brandPrimaryColor, '#B91C1C');
      expect(p.brandAccentColor, isNull);
      final j = p.toJson();
      expect(j['brandPrimaryColor'], '#B91C1C');
      expect(j.containsKey('brandAccentColor'), isTrue);

      final fields = p.withFields(address: 'New Rd', bankName: 'GTB');
      expect(fields.address, 'New Rd');
      expect(fields.brandPrimaryColor, '#B91C1C');

      final colours = p.withBrandColors(primary: null, accent: '#0D9488');
      expect(colours.address, 'Farm Rd');
      expect(colours.bankName, 'GTB');
      expect(colours.brandPrimaryColor, isNull);
      expect(colours.brandAccentColor, '#0D9488');
    });
  });

  group('templates', () {
    test('parses the new fields and orders ngf first, then by name', () {
      final list = parseTemplates([
        _tpl('sidebar', 'Sidebar'),
        _tpl('anchor', 'Anchor'),
        _tpl('ngf', 'NGF - Invoice', isDefault: true, p: '#138A44', a: '#F5C90C'),
        _tpl('band', 'Band'),
        {'name': 'no key'},
      ]);
      expect(list.map((t) => t.key), ['ngf', 'anchor', 'band', 'sidebar']);
      expect(list.first.isDefault, isTrue);
      expect(list.first.defaultPrimaryColor, '#138A44');
      expect(list.first.defaultAccentColor, '#F5C90C');
      expect(list.last.description, 'About Sidebar');
      expect(list.last.previewUrl, endsWith('/sidebar/preview'));
    });

    test('missing default colours fall back to the ngf green and yellow; a bad list parses to empty', () {
      final t = InvoiceTemplate.fromJson({'key': 'x', 'name': 'X'});
      expect(t.defaultPrimaryColor, kDefaultPrimaryColor);
      expect(t.defaultAccentColor, kDefaultAccentColor);
      expect(parseTemplates(null), isEmpty);
      expect(parseTemplates({'a': 1}), isEmpty);
    });

    test('survives a round trip through the offline cache', () {
      final list = parseTemplates([_tpl('ngf', 'NGF', isDefault: true), _tpl('duo', 'Duo', p: '#1D4ED8')]);
      final again = parseTemplates([for (final t in list) t.toJson()]);
      expect(again.map((t) => t.key), ['ngf', 'duo']);
      expect(again.last.defaultPrimaryColor, '#1D4ED8');
    });
  });

  group('preview path', () {
    test('has no query when no colour is given', () {
      expect(invoicePreviewPath('f1', 'band'), '/api/farms/f1/invoice-templates/band/preview');
    });

    test('encodes # as %23 and upper-cases', () {
      expect(
        invoicePreviewPath('f1', 'band', primaryColor: '#b91c1c', accentColor: '#F5C90C'),
        '/api/farms/f1/invoice-templates/band/preview?primaryColor=%23B91C1C&accentColor=%23F5C90C',
      );
    });

    test('leaves out a blank or invalid colour', () {
      expect(invoicePreviewPath('f1', 'duo', primaryColor: '#1D4ED8'), endsWith('?primaryColor=%231D4ED8'));
      expect(invoicePreviewPath('f1', 'duo', primaryColor: 'red', accentColor: ' '), '/api/farms/f1/invoice-templates/duo/preview');
    });
  });

  group('preview errors', () {
    test('401, 404 and 422 have their own text; others fall back to failText', () {
      expect(previewFailText(const ApiException('Request failed (HTTP 401)', 401)), contains('Sign in'));
      expect(previewFailText(const ApiException('Request failed (HTTP 404)', 404)), contains('not available'));
      expect(previewFailText(const ApiException('primaryColor must be a colour in the form #RRGGBB (for example #138A44).', 422)), contains('#RRGGBB'));
      expect(previewFailText(const ApiException('Request failed (HTTP 422)', 422)), contains('#RRGGBB'));
      expect(previewFailText(Exception('socket')), contains('Could not reach the server'));
    });

    test('colour source wording', () {
      expect(colorSourceLabel('invoice'), contains('invoice'));
      expect(colorSourceLabel('farm'), contains('farm'));
      expect(colorSourceLabel('default'), contains('default'));
    });
  });

  group('template picker', () {
    final templates = parseTemplates([
      for (var i = 0; i < 10; i++) _tpl(i == 0 ? 'ngf' : 'tpl$i', i == 0 ? 'NGF - Invoice' : 'Template $i', isDefault: i == 0),
    ]);

    testWidgets('shows cards, marks the selected one and reports taps', (tester) async {
      String selected = 'ngf';
      String? tapped;
      await tester.pumpWidget(MaterialApp(
        home: Scaffold(
          body: StatefulBuilder(
            builder: (context, setState) => InvoiceTemplatePicker(
              templates: templates,
              selectedKey: selected,
              onSelected: (k) => setState(() { tapped = k; selected = k; }),
            ),
          ),
        ),
      ));
      expect(find.text('NGF - Invoice'), findsOneWidget);
      expect(find.byIcon(Icons.check_circle), findsOneWidget);
      expect(tester.getSemantics(find.bySemanticsLabel(RegExp('NGF - Invoice, default'))), isSemantics(isSelected: true, isButton: true));
      expect(tester.getSemantics(find.bySemanticsLabel(RegExp('Template 2'))), isSemantics(isSelected: false, isButton: true));

      await tester.tap(find.text('Template 2'));
      await tester.pump();
      expect(tapped, 'tpl2');
      expect(find.byIcon(Icons.check_circle), findsOneWidget);
      expect(find.bySemanticsLabel(RegExp('Template 2')), findsOneWidget);
    });

    testWidgets('cards are at least 44dp tall and a card scrolls into view', (tester) async {
      await tester.pumpWidget(MaterialApp(
        home: Scaffold(body: InvoiceTemplatePicker(templates: templates, selectedKey: 'ngf', onSelected: (_) {})),
      ));
      expect(tester.getSize(find.byType(TemplateCard).first).height, greaterThanOrEqualTo(44));
      await tester.scrollUntilVisible(find.text('Template 9'), 200, scrollable: find.byType(Scrollable).first);
      expect(find.text('Template 9'), findsOneWidget);
    });
  });

  group('colour field', () {
    testWidgets('palette tap fills the hex box; invalid text shows the error; reset clears', (tester) async {
      final c = TextEditingController();
      await tester.pumpWidget(MaterialApp(
        home: Scaffold(
          body: SingleChildScrollView(
            child: StatefulBuilder(
              builder: (context, setState) => BrandColorField(
                controller: c,
                label: 'Primary colour',
                defaultHex: kDefaultPrimaryColor,
                onChanged: () => setState(() {}),
              ),
            ),
          ),
        ),
      ));
      expect(find.textContaining('Not set'), findsOneWidget);

      await tester.tap(find.bySemanticsLabel('Red #B91C1C'));
      await tester.pump();
      expect(c.text, '#B91C1C');
      expect(find.textContaining('Reset to default'), findsOneWidget);

      await tester.enterText(find.byType(TextField), '#FFF');
      await tester.pump();
      expect(find.textContaining('must look like #138A44'), findsOneWidget);

      await tester.enterText(find.byType(TextField), '#0d9488');
      await tester.pump();
      expect(find.textContaining('must look like'), findsNothing);
      expect(normalizeHexColor(c.text), '#0D9488');

      await tester.tap(find.textContaining('Reset to default'));
      await tester.pump();
      expect(c.text, '');
      expect(find.textContaining('Not set'), findsOneWidget);
    });
  });
}
