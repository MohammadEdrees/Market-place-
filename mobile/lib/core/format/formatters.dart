import 'package:intl/intl.dart';

/// en-US display formatters, matching the dashboard's formatting rules.
final NumberFormat _money = NumberFormat.simpleCurrency(locale: 'en_US');
final DateFormat _day = DateFormat.yMMMd('en_US');
final DateFormat _dayTime = DateFormat.yMMMd('en_US').add_jm();

/// `$1,234.50` — always en-US, whatever the device locale.
String formatMoney(num value) => _money.format(value);

/// `Sep 25, 2026`
String formatDate(DateTime value) => _day.format(value.toLocal());

/// `Sep 25, 2026, 3:45 PM`
String formatDateTime(DateTime value) => _dayTime.format(value.toLocal());
