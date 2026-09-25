import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:market_workplace/core/shell/app_shell.dart';
import 'package:market_workplace/core/i18n/l10n_ext.dart';

/// English localizations for the pure `navTabsFor` unit tests (no widget
/// pump, so no `MaterialApp`/delegates are involved).
late final AppLocalizations _l10n;

void main() {
  setUpAll(() async {
    _l10n = await AppLocalizations.delegate.load(const Locale('en'));
  });

  test('clients get four tabs without listing management', () {
    final tabs = navTabsFor(isProvider: false, l10n: _l10n);

    expect(tabs.map((t) => t.label), ['Browse', 'Services', 'Orders', 'Profile']);
    expect(tabs.map((t) => t.branch), [0, 1, 3, 4]);
    expect(tabs.map((t) => t.label), isNot(contains('Listings')));
    // branches stay aligned with the shell's five pages
    expect(tabs.map((t) => t.branch), everyElement(isIn([0, 1, 2, 3, 4])));
  });

  test('providers get the Listings tab between Services and Orders', () {
    final tabs = navTabsFor(isProvider: true, l10n: _l10n);

    expect(tabs.map((t) => t.label),
        ['Browse', 'Services', 'Listings', 'Orders', 'Profile']);
    expect(tabs.map((t) => t.branch), [0, 1, 2, 3, 4]);
    expect(tabs[2].branch, 2);
  });

  test('tabs expose distinct Material icons', () {
    for (final tab in navTabsFor(isProvider: true, l10n: _l10n)) {
      expect(tab.icon, isA<IconData>());
      expect(tab.selectedIcon, isA<IconData>());
    }
  });
}
