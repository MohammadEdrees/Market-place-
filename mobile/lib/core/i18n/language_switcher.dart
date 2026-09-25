import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'l10n_ext.dart';
import 'locale_provider.dart';

/// The two languages the app ships, as endonyms: each name is written in its
/// own language and is never translated.
List<(Locale locale, String label)> appLocales(BuildContext context) => [
      (const Locale('en'), context.l10n.commonLanguageEnglish),
      (const Locale('ar'), context.l10n.commonLanguageArabic),
    ];

/// Name of the currently selected language (an endonym).
String currentLanguageLabel(BuildContext context, Locale locale) =>
    locale.languageCode == 'ar'
        ? context.l10n.commonLanguageArabic
        : context.l10n.commonLanguageEnglish;

/// Opens the language picker used by the Profile page and the compact switcher
/// on the login screen. Picking a language rebuilds the app in that locale
/// (RTL for Arabic) and persists the choice.
Future<void> showLanguagePicker(BuildContext context, WidgetRef ref) {
  return showDialog<void>(
    context: context,
    builder: (dialogContext) {
      final current = ref.read(localeProvider);
      return AlertDialog(
        title: Text(dialogContext.l10n.commonLanguage),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            for (final (locale, label) in appLocales(dialogContext))
              ListTile(
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.language_outlined),
                title: Text(label),
                trailing: locale.languageCode == current.languageCode
                    ? const Icon(Icons.check_circle_outline)
                    : null,
                onTap: () async {
                  Navigator.of(dialogContext).pop();
                  await ref
                      .read(localeProvider.notifier)
                      .setLocale(locale);
                },
              ),
          ],
        ),
      );
    },
  );
}
