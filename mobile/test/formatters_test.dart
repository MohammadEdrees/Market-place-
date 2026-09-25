import 'package:flutter_test/flutter_test.dart';
import 'package:market_workplace/core/format/formatters.dart';

void main() {
  test('formats money as en-US currency', () {
    expect(formatMoney(0), r'$0.00');
    expect(formatMoney(59.99), r'$59.99');
    expect(formatMoney(1234.5), r'$1,234.50');
    expect(formatMoney(1000000), r'$1,000,000.00');
  });

  test('formats dates as en-US', () {
    expect(formatDate(DateTime(2026, 8, 25)), 'Aug 25, 2026');
    expect(formatDate(DateTime(2026, 12, 1)), 'Dec 1, 2026');
  });

  test('formats date-times as en-US with a 12-hour clock', () {
    final formatted = formatDateTime(DateTime(2026, 8, 25, 15, 45));
    // ICU may use a narrow no-break space before AM/PM.
    final normalized = formatted.replaceAll(' ', ' ');
    expect(normalized, contains('Aug 25, 2026'));
    expect(normalized, contains('3:45 PM'));
  });
}
