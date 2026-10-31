import 'package:flutter/material.dart';

// ── AGROMESH COLOR SYSTEM ──
class AgroColors {
  AgroColors._();

  static const Color forest  = Color(0xFF0D2B1F);
  static const Color canopy  = Color(0xFF1A4A32);
  static const Color leaf    = Color(0xFF2D7A4F);
  static const Color palm    = Color(0xFF3DAA6A);
  static const Color harvest = Color(0xFFE8A838);
  static const Color mist    = Color(0xFFF0F4F1);
  static const Color fog     = Color(0xFFE2EBE5);
  static const Color stone   = Color(0xFF4F5F56);
  static const Color stoneOnDark = Color(0xFFA9BDB0); // muted text on forest/canopy surfaces
  static const Color ink     = Color(0xFF0A1A10);
  static const Color data    = Color(0xFF00C896);
  static const Color alert   = Color(0xFFDC3C3C);
  static const Color white   = Color(0xFFFFFFFF);

  // Work-progress map: blue/orange stays distinguishable for colour-blind users (Okabe-Ito).
  static const Color coverageDone = Color(0xFF0072B2);
  static const Color coverageLeft = Color(0xFFE69F00);
  // Marked sections are also numbered on the map, so colour is never the only cue.
  static const List<Color> sectionPalette = [
    Color(0xFFD55E00), Color(0xFF56B4E9), Color(0xFFCC79A7), Color(0xFF009E73),
    Color(0xFF0072B2), Color(0xFFE69F00), Color(0xFF8E44AD), Color(0xFF444444),
  ];

  // Semantic
  static const Color success = palm;
  static const Color warning = harvest;
  static const Color error   = alert;
  static const Color online  = data;
}

// ── AGROMESH THEME ──
class AgroTheme {
  AgroTheme._();

  static ThemeData get theme => ThemeData(
    useMaterial3: true,
    scaffoldBackgroundColor: AgroColors.mist,
    colorScheme: ColorScheme.fromSeed(
      seedColor: AgroColors.leaf,
      primary:   AgroColors.leaf,
      secondary: AgroColors.palm,
      surface:   AgroColors.white,
      error:     AgroColors.alert,
      brightness: Brightness.light,
    ),
    textTheme: ThemeData.light().textTheme.apply(fontFamily: 'PlusJakartaSans').copyWith(
      displayLarge:  const TextStyle(fontFamily: 'PlusJakartaSans', fontWeight: FontWeight.w800, fontSize: 32, color: AgroColors.forest, letterSpacing: -1),
      displayMedium: const TextStyle(fontFamily: 'PlusJakartaSans', fontWeight: FontWeight.w800, fontSize: 24, color: AgroColors.forest, letterSpacing: -0.5),
      displaySmall:  const TextStyle(fontFamily: 'PlusJakartaSans', fontWeight: FontWeight.w700, fontSize: 20, color: AgroColors.forest, letterSpacing: -0.5),
      headlineMedium:const TextStyle(fontFamily: 'PlusJakartaSans', fontWeight: FontWeight.w700, fontSize: 18, color: AgroColors.forest),
      headlineSmall: const TextStyle(fontFamily: 'PlusJakartaSans', fontWeight: FontWeight.w700, fontSize: 15, color: AgroColors.forest),
      titleLarge:    const TextStyle(fontFamily: 'PlusJakartaSans', fontWeight: FontWeight.w600, fontSize: 14, color: AgroColors.forest),
      titleMedium:   const TextStyle(fontFamily: 'PlusJakartaSans', fontWeight: FontWeight.w600, fontSize: 14, color: AgroColors.forest),
      bodyLarge:     const TextStyle(fontFamily: 'PlusJakartaSans', fontWeight: FontWeight.w400, fontSize: 16, color: AgroColors.ink),
      bodyMedium:    const TextStyle(fontFamily: 'PlusJakartaSans', fontWeight: FontWeight.w400, fontSize: 16, color: AgroColors.ink),
      bodySmall:     const TextStyle(fontFamily: 'PlusJakartaSans', fontWeight: FontWeight.w400, fontSize: 14, color: AgroColors.stone),
      labelLarge:    const TextStyle(fontFamily: 'PlusJakartaSans', fontWeight: FontWeight.w500, fontSize: 14, color: AgroColors.stone),
      labelMedium:   const TextStyle(fontFamily: 'PlusJakartaSans', fontWeight: FontWeight.w500, fontSize: 13, color: AgroColors.stone, letterSpacing: 0.5),
      labelSmall:    const TextStyle(fontFamily: 'PlusJakartaSans', fontWeight: FontWeight.w400, fontSize: 12,  color: AgroColors.stone, letterSpacing: 1),
    ),
    appBarTheme: const AppBarTheme(
      backgroundColor: AgroColors.forest,
      foregroundColor: AgroColors.white,
      elevation: 0,
      centerTitle: false,
      titleTextStyle: TextStyle(fontFamily: 'PlusJakartaSans', 
        fontWeight: FontWeight.w800,
        fontSize: 18,
        color: AgroColors.white,
        letterSpacing: -0.5,
      ),
    ),
    bottomNavigationBarTheme: const BottomNavigationBarThemeData(
      backgroundColor: AgroColors.forest,
      selectedItemColor: AgroColors.palm,
      unselectedItemColor: AgroColors.stoneOnDark,
      showSelectedLabels: true,
      showUnselectedLabels: true,
      type: BottomNavigationBarType.fixed,
      elevation: 0,
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        backgroundColor: AgroColors.leaf,
        foregroundColor: AgroColors.white,
        minimumSize: const Size(double.infinity, 48),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
        textStyle: const TextStyle(fontFamily: 'PlusJakartaSans', fontWeight: FontWeight.w600, fontSize: 14),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        foregroundColor: AgroColors.forest,
        side: const BorderSide(color: AgroColors.fog),
        minimumSize: const Size(double.infinity, 48),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
        textStyle: const TextStyle(fontFamily: 'PlusJakartaSans', fontWeight: FontWeight.w600, fontSize: 14),
      ),
    ),
    textButtonTheme: TextButtonThemeData(
      style: TextButton.styleFrom(
        minimumSize: const Size(48, 48),
        tapTargetSize: MaterialTapTargetSize.padded,
        textStyle: const TextStyle(fontFamily: 'PlusJakartaSans', fontWeight: FontWeight.w600, fontSize: 14),
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: AgroColors.white,
      contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(10),
        borderSide: const BorderSide(color: AgroColors.fog),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(10),
        borderSide: const BorderSide(color: AgroColors.fog),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(10),
        borderSide: const BorderSide(color: AgroColors.leaf, width: 1.5),
      ),
      hintStyle: const TextStyle(fontFamily: 'PlusJakartaSans', color: AgroColors.stone, fontSize: 14),
      labelStyle: const TextStyle(fontFamily: 'PlusJakartaSans', 
        color: AgroColors.stone, fontSize: 12,
        letterSpacing: 1, fontWeight: FontWeight.w500,
      ),
    ),
    cardTheme: CardThemeData(
      color: AgroColors.white,
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: const BorderSide(color: AgroColors.fog),
      ),
      margin: EdgeInsets.zero,
    ),
    dividerTheme: const DividerThemeData(
      color: AgroColors.fog,
      thickness: 1,
      space: 0,
    ),
    chipTheme: ChipThemeData(
      backgroundColor: AgroColors.mist,
      labelStyle: const TextStyle(fontFamily: 'PlusJakartaSans', fontSize: 12, fontWeight: FontWeight.w500, color: AgroColors.forest),
      side: const BorderSide(color: AgroColors.fog),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
    ),
  );
}
