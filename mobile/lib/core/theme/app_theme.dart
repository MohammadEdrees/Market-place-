import 'package:flutter/material.dart';

/// Material 3 theme for the mobile app.
class AppTheme {
  AppTheme._();

  static const Color seed = Color(0xFF3F51B5);

  static ThemeData get light => ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(seedColor: seed),
        scaffoldBackgroundColor: const Color(0xFFF7F7FB),
        snackBarTheme: const SnackBarThemeData(
          behavior: SnackBarBehavior.floating,
        ),
      );
}
