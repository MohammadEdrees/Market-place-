import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// SharedPreferences key for the persisted language choice (`en` / `ar`).
const String kLocalePreferenceKey = 'marketplace.locale';

/// The app's explicit UI locale.
///
/// The app deliberately does **not** follow the system locale: the choice is
/// always the one made in the in-app switcher (defaulting to English), so
/// behaviour stays deterministic across restarts and devices. The choice is
/// loaded from [SharedPreferences] in `main()` before `runApp` and persisted
/// again on every switch under [kLocalePreferenceKey].
class LocaleController extends Notifier<Locale> {
  LocaleController([this._initial = const Locale('en')]);

  final Locale _initial;

  @override
  Locale build() => _initial;

  /// Switches the UI language and persists the choice (`en` / `ar`).
  Future<void> setLocale(Locale locale) async {
    state = locale;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(kLocalePreferenceKey, locale.languageCode);
  }
}

/// The locale the whole app renders in; `Locale('ar')` flips the UI to RTL
/// through `GlobalWidgetsLocalizations`.
final localeProvider =
    NotifierProvider<LocaleController, Locale>(LocaleController.new);

/// Reads the persisted locale before `runApp`. Anything but a stored `ar`
/// (including nothing at all) resolves to English.
Future<Locale> loadLocale() async {
  final prefs = await SharedPreferences.getInstance();
  return prefs.getString(kLocalePreferenceKey) == 'ar'
      ? const Locale('ar')
      : const Locale('en');
}
