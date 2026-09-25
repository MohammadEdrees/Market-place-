import 'package:flutter_test/flutter_test.dart';
import 'package:market_workplace/core/api/api_exception.dart';

void main() {
  group('ApiException.fromResponse', () {
    test('parses RFC 7807 problem details', () {
      final error = ApiException.fromResponse(409, {
        'title': 'Role in use',
        'detail': 'Role in use. Unassign its users first.',
        'status': 409,
      });

      expect(error.statusCode, 409);
      expect(error.title, 'Role in use');
      expect(error.detail, 'Role in use. Unassign its users first.');
      expect(error.message, 'Role in use. Unassign its users first.');
      expect(error.isConflict, isTrue);
    });

    test('parses field-level validation errors', () {
      final error = ApiException.fromResponse(400, {
        'title': 'Validation failed',
        'errors': {
          'Name': ['Name is required.'],
          'Email': ['Email is invalid.', 'Email is already taken.'],
        },
      });

      expect(error.isValidation, isTrue);
      expect(error.message, contains('Name is required.'));
      expect(error.message, contains('Email is already taken.'));
      expect(error.fieldError('Email'), 'Email is invalid.');
      expect(error.fieldError('Missing'), isNull);
    });

    test('falls back to status-based titles', () {
      expect(ApiException.fromResponse(404, null).title, 'Not found');
      expect(ApiException.fromResponse(401, null).title, 'Sign-in required');
      expect(ApiException.fromResponse(0, null).title, 'Connection failed');
      expect(ApiException.fromResponse(503, null).title, 'Server error');
    });

    test('uses the title when no detail is present', () {
      final error = ApiException.fromResponse(409, {'title': 'Duplicate SKU'});
      expect(error.message, 'Duplicate SKU');
    });

    test('handles non-JSON bodies', () {
      final error = ApiException.fromResponse(500, 'gateway exploded');
      expect(error.statusCode, 500);
      expect(error.detail, 'gateway exploded');
      expect(error.title, 'Server error');
    });
  });
}
