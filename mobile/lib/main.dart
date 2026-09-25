import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app.dart';
import 'core/i18n/locale_provider.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  // Restore the persisted language choice before the first frame; the app
  // never follows the system locale, so this is the only source of truth.
  final locale = await loadLocale();
  runApp(
    ProviderScope(
      overrides: [localeProvider.overrideWith(() => LocaleController(locale))],
      child: const MarketWorkplaceApp(),
    ),
  );
}
