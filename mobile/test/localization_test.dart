import 'dart:convert';
import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:market_workplace/core/i18n/l10n_ext.dart';
import 'package:market_workplace/core/i18n/locale_provider.dart';
import 'package:market_workplace/features/auth/login_page.dart';
import 'package:market_workplace/features/profile/profile_page.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// `Market Workplace` ships exactly two languages; the endonyms used by the
/// switcher are the same in both catalogs (they are never translated).
const String kEnglishEndonym = 'English';
const String kArabicEndonym = 'العربية';

/// A persisted, unexpired client session so `ProfilePage` renders its read
/// view instead of the loading spinner.
Map<String, Object> signedInPrefs() => <String, Object>{
      'session.token': 'test-token',
      'session.expiresAt':
          DateTime.now().toUtc().add(const Duration(hours: 2)).toIso8601String(),
      'session.user': jsonEncode(<String, Object?>{
        'id': 7,
        'email': 'cara@test.dev',
        'name': 'Cara Client',
        'role': 'Client',
        'type': 'Mobile',
      }),
    };

/// A `MaterialApp` wired exactly like `MarketWorkplaceApp`: it watches the
/// locale provider so a switch flips the whole tree (and its direction).
Widget appUnderTest(Widget home) => ProviderScope(
      child: Consumer(
        builder: (context, ref, _) => MaterialApp(
          locale: ref.watch(localeProvider),
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
          home: home,
        ),
      ),
    );

TextDirection directionOf(WidgetTester tester) => tester
    .widget<Directionality>(find.byType(Directionality).first)
    .textDirection;

/// Reads one of the two ARB catalogs from disk (the test runner's working
/// directory is the package root).
Map<String, String> readArb(String relativePath) {
  final file = File(relativePath).existsSync()
      ? File(relativePath)
      : File('mobile/$relativePath');
  if (!file.existsSync()) {
    fail('Could not locate $relativePath from ${Directory.current.path}');
  }
  final decoded = jsonDecode(file.readAsStringSync());
  final raw = Map<String, dynamic>.from(decoded as Map<String, dynamic>);
  return <String, String>{
    for (final entry in raw.entries)
      if (!entry.key.startsWith('@') && entry.value is String)
        entry.key: entry.value as String,
  };
}

Set<String> placeholdersOf(String message) =>
    RegExp(r'\{[a-zA-Z0-9_]+\}').allMatches(message).map((m) => m.group(0)!).toSet();

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  group('language switcher', () {
    testWidgets('Profile page switches the app to Arabic (RTL)', (tester) async {
      SharedPreferences.setMockInitialValues(signedInPrefs());

      await tester.pumpWidget(appUnderTest(const ProfilePage()));
      await tester.pumpAndSettle();

      expect(find.text('Profile'), findsOneWidget);
      expect(directionOf(tester), TextDirection.ltr);
      expect(find.widgetWithText(ListTile, 'Language'), findsOneWidget);

      await tester.tap(find.widgetWithText(ListTile, 'Language'));
      await tester.pumpAndSettle();

      // Both endonyms are offered, each in its own language.
      final dialog = find.byType(AlertDialog);
      expect(
        find.descendant(of: dialog, matching: find.text(kEnglishEndonym)),
        findsOneWidget,
      );
      expect(
        find.descendant(of: dialog, matching: find.text(kArabicEndonym)),
        findsOneWidget,
      );

      await tester.tap(
        find.descendant(of: dialog, matching: find.text(kArabicEndonym)),
      );
      await tester.pumpAndSettle();

      expect(directionOf(tester), TextDirection.rtl);
      final arabic = await AppLocalizations.delegate.load(const Locale('ar'));
      expect(find.text(arabic.profileTitle), findsOneWidget);
      expect(find.text('Profile'), findsNothing);
    });

    testWidgets('Login page compact switcher flips to RTL before sign-in',
        (tester) async {
      SharedPreferences.setMockInitialValues({});

      await tester.pumpWidget(appUnderTest(const LoginPage()));
      await tester.pumpAndSettle();

      expect(directionOf(tester), TextDirection.ltr);
      expect(find.byTooltip('Language'), findsOneWidget);

      await tester.tap(find.byTooltip('Language'));
      await tester.pumpAndSettle();
      expect(find.text(kArabicEndonym), findsOneWidget);

      await tester.tap(find.text(kArabicEndonym));
      await tester.pumpAndSettle();

      expect(directionOf(tester), TextDirection.rtl);
      final arabic = await AppLocalizations.delegate.load(const Locale('ar'));
      expect(find.text(arabic.loginSignIn), findsOneWidget);
      // The brand name stays untouched in every language.
      expect(find.text('Market Workplace'), findsOneWidget);
    });
  });

  group('persistence', () {
    test('switching the language stores it under marketplace.locale',
        () async {
      SharedPreferences.setMockInitialValues({});
      final container = ProviderContainer();
      addTearDown(container.dispose);

      // Nothing chosen yet: the app boots in English (it never follows the
      // system locale).
      expect(await loadLocale(), const Locale('en'));

      await container.read(localeProvider.notifier).setLocale(const Locale('ar'));

      expect(container.read(localeProvider), const Locale('ar'));
      final prefs = await SharedPreferences.getInstance();
      expect(prefs.getString(kLocalePreferenceKey), 'ar');
      expect(await loadLocale(), const Locale('ar'));

      await container.read(localeProvider.notifier).setLocale(const Locale('en'));
      expect(prefs.getString(kLocalePreferenceKey), 'en');
      expect(await loadLocale(), const Locale('en'));
    });
  });

  group('catalog completeness', () {
    test('Arabic carries every English key with matching placeholders', () {
      final en = readArb('lib/l10n/app_en.arb');
      final ar = readArb('lib/l10n/app_ar.arb');

      expect(en, hasLength(197));
      expect(ar.keys.toSet(), en.keys.toSet(),
          reason: 'every English key must have an Arabic counterpart');

      final untranslated = <String>[];
      final placeholderDrift = <String>[];
      for (final key in en.keys) {
        final value = ar[key]!;
        if (value.trim().isEmpty) untranslated.add(key);
        if (placeholdersOf(value).length != placeholdersOf(en[key]!).length ||
            !placeholdersOf(en[key]!)
                .every(placeholdersOf(value).contains)) {
          placeholderDrift.add(key);
        }
      }

      expect(untranslated, isEmpty, reason: 'empty Arabic values');
      expect(placeholderDrift, isEmpty, reason: 'ICU placeholders must match');

      // Only the two language endonyms may be byte-identical: they are
      // deliberately never translated. Everything else must be translated.
      final identical = en.keys
          .where((key) => ar[key] == en[key])
          .toList()
        ..sort();
      expect(identical, ['commonLanguageArabic', 'commonLanguageEnglish']);
    });

    test('the generated Arabic lookup resolves real Arabic text', () async {
      final ar = await AppLocalizations.delegate.load(const Locale('ar'));
      final en = await AppLocalizations.delegate.load(const Locale('en'));

      expect(ar.localeName, 'ar');
      expect(en.navBrowse, 'Browse');
      expect(ar.navBrowse, isNot(en.navBrowse));
      expect(
        ar.navBrowse.codeUnits.where((unit) => unit >= 0x0600 && unit <= 0x06FF),
        isNotEmpty,
        reason: 'Arabic string must contain Arabic-block code points',
      );
      expect(ar.commonMaxCharacters('120'), contains('120'));
      expect(en.commonMaxCharacters('120'), 'Max 120 characters.');
    });
  });

  group('error allowlist', () {
    testWidgets('ten API problem details translate; anything else passes',
        (tester) async {
      late BuildContext ctx;
      await tester.pumpWidget(MaterialApp(
        locale: const Locale('ar'),
        localizationsDelegates: AppLocalizations.localizationsDelegates,
        supportedLocales: AppLocalizations.supportedLocales,
        home: Builder(builder: (context) {
          ctx = context;
          return const SizedBox();
        }),
      ));
      await tester.pumpAndSettle();

      final arabic = AppLocalizations.of(ctx)!;
      expect(apiMessageMap, hasLength(10));
      for (final entry in apiMessageMap.entries) {
        expect(ctx.localizeApiMessage(entry.key), entry.value(arabic),
            reason: '${entry.key} must render its Arabic translation');
        expect(ctx.localizeApiMessage(entry.key), isNot(entry.key));
      }

      // Client-side fallbacks are mapped too (generic + network).
      expect(ctx.localizeApiMessage('Something went wrong. Please try again.'),
          arabic.commonGenericError);
      expect(
          ctx.localizeApiMessage('Something went wrong.'),
          arabic.commonSomethingWentWrong);
      expect(ctx.localizeApiMessage('The server took too long to respond.'),
          arabic.networkTimeout);
      expect(ctx.localizeApiMessage('Could not reach the server. Check your connection.'),
          arabic.networkUnreachable);
      expect(ctx.localizeApiMessage('Request failed (422)'),
          arabic.networkRequestFailed);
      expect(clientMessageMap, hasLength(15));

      // Anything the backend may invent later is shown verbatim.
      expect(
          ctx.localizeApiMessage('Quota exceeded for this tenant.'),
          'Quota exceeded for this tenant.');
    });
  });
}
