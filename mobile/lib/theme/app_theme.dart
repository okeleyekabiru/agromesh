import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';

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
  static const Color stone   = Color(0xFF6B7C72);
  static const Color ink     = Color(0xFF0A1A10);
  static const Color data    = Color(0xFF00C896);
  static const Color alert   = Color(0xFFDC3C3C);
  static const Color white   = Color(0xFFFFFFFF);

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
      background: AgroColors.mist,
      error:     AgroColors.alert,
      brightness: Brightness.light,
    ),
    textTheme: GoogleFonts.interTextTheme().copyWith(
      displayLarge:  GoogleFonts.syne(fontWeight: FontWeight.w800, fontSize: 32, color: AgroColors.forest, letterSpacing: -1),
      displayMedium: GoogleFonts.syne(fontWeight: FontWeight.w800, fontSize: 24, color: AgroColors.forest, letterSpacing: -0.5),
      displaySmall:  GoogleFonts.syne(fontWeight: FontWeight.w700, fontSize: 20, color: AgroColors.forest, letterSpacing: -0.5),
      headlineMedium:GoogleFonts.syne(fontWeight: FontWeight.w700, fontSize: 18, color: AgroColors.forest),
      headlineSmall: GoogleFonts.syne(fontWeight: FontWeight.w700, fontSize: 15, color: AgroColors.forest),
      titleLarge:    GoogleFonts.inter(fontWeight: FontWeight.w600, fontSize: 14, color: AgroColors.forest),
      titleMedium:   GoogleFonts.inter(fontWeight: FontWeight.w600, fontSize: 13, color: AgroColors.forest),
      bodyLarge:     GoogleFonts.inter(fontWeight: FontWeight.w400, fontSize: 14, color: AgroColors.ink),
      bodyMedium:    GoogleFonts.inter(fontWeight: FontWeight.w400, fontSize: 13, color: AgroColors.ink),
      bodySmall:     GoogleFonts.inter(fontWeight: FontWeight.w400, fontSize: 12, color: AgroColors.stone),
      labelLarge:    GoogleFonts.jetBrainsMono(fontWeight: FontWeight.w500, fontSize: 11, color: AgroColors.stone),
      labelMedium:   GoogleFonts.jetBrainsMono(fontWeight: FontWeight.w500, fontSize: 10, color: AgroColors.stone, letterSpacing: 0.5),
      labelSmall:    GoogleFonts.jetBrainsMono(fontWeight: FontWeight.w400, fontSize: 9,  color: AgroColors.stone, letterSpacing: 1),
    ),
    appBarTheme: AppBarTheme(
      backgroundColor: AgroColors.forest,
      foregroundColor: AgroColors.white,
      elevation: 0,
      centerTitle: false,
      titleTextStyle: GoogleFonts.syne(
        fontWeight: FontWeight.w800,
        fontSize: 18,
        color: AgroColors.white,
        letterSpacing: -0.5,
      ),
    ),
    bottomNavigationBarTheme: const BottomNavigationBarThemeData(
      backgroundColor: AgroColors.forest,
      selectedItemColor: AgroColors.palm,
      unselectedItemColor: AgroColors.stone,
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
        textStyle: GoogleFonts.inter(fontWeight: FontWeight.w600, fontSize: 14),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        foregroundColor: AgroColors.forest,
        side: const BorderSide(color: AgroColors.fog),
        minimumSize: const Size(double.infinity, 44),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
        textStyle: GoogleFonts.inter(fontWeight: FontWeight.w600, fontSize: 13),
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
      hintStyle: GoogleFonts.inter(color: AgroColors.stone, fontSize: 13),
      labelStyle: GoogleFonts.jetBrainsMono(
        color: AgroColors.stone, fontSize: 9,
        letterSpacing: 1, fontWeight: FontWeight.w500,
      ),
    ),
    cardTheme: CardTheme(
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
      labelStyle: GoogleFonts.inter(fontSize: 11, fontWeight: FontWeight.w500, color: AgroColors.forest),
      side: const BorderSide(color: AgroColors.fog),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
    ),
  );
}
