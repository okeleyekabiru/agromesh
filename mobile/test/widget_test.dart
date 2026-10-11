// Basic widget tests for the auth/GPS-capture UI added in Phase 4.
//
// Per the project's testing approach, this GPS/UI/hardware-interaction code
// is implemented first and covered with lightweight widget tests after — not
// exhaustive coverage, just enough to catch obvious render/wiring regressions.

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';

import 'package:agromesh_mobile/screens/auth/login_screen.dart';
import 'package:agromesh_mobile/screens/blocks/boundary_capture_screen.dart';
import 'package:agromesh_mobile/stores/auth_store.dart';
import 'package:agromesh_mobile/theme/app_theme.dart';
import 'package:agromesh_mobile/widgets/widgets.dart';

void main() {
  // Avoid GoogleFonts trying to fetch font files over the network during tests.
  setUpAll(() {
    GoogleFonts.config.allowRuntimeFetching = false;
  });

  testWidgets('LoginScreen renders email/password fields and a Log In button', (tester) async {
    await tester.pumpWidget(
      ChangeNotifierProvider<AuthStore>(
        create: (_) => AuthStore(),
        child: MaterialApp(theme: AgroTheme.theme, home: const LoginScreen()),
      ),
    );

    expect(find.byType(AgroTextField), findsNWidgets(2));
    expect(find.widgetWithText(ElevatedButton, 'Log In'), findsOneWidget);
  });

  testWidgets('BoundaryCaptureScreen starts with zero corners marked and Finish Boundary disabled', (tester) async {
    await tester.pumpWidget(
      MaterialApp(
        theme: AgroTheme.theme,
        home: BoundaryCaptureScreen(blockId: 'sec_1', blockName: 'Test Section'),
      ),
    );

    expect(find.text('0 corners marked'), findsOneWidget);
    // `ElevatedButton.icon` builds a private subclass, so match by text alone
    // rather than `widgetWithText(ElevatedButton, ...)` (exact-type lookup).
    expect(find.text('Mark Corner Here'), findsOneWidget);

    final finishButton = tester.widget<OutlinedButton>(
      find.widgetWithText(OutlinedButton, 'Finish Boundary (need 3 more)'),
    );
    expect(finishButton.onPressed, isNull);
  });
}
