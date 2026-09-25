import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:market_workplace/core/api/api_exception.dart';
import 'package:market_workplace/core/auth/session.dart';
import 'package:market_workplace/features/auth/auth_controller.dart';
import 'package:market_workplace/features/auth/auth_repository.dart';

import 'helpers.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  Map<String, Object?> loginResponse({
    String token = 'jwt-token',
    String role = 'Client',
    int id = 3,
  }) =>
      {
        'token': token,
        'expiresAt': '2026-09-26T12:00:00.000Z',
        'user': {
          'id': id,
          'email': 'client@test.dev',
          'name': 'Cara Client',
          'role': role,
          'type': 'Mobile',
          'phone': null,
          'location': 'Amman',
          'bio': null,
          'imagePath': null,
        },
      };

  group('AuthRepository.login', () {
    test('posts credentials and parses the session', () async {
      late RequestOptions captured;
      final container = containerWith((options, stream) async {
        captured = options;
        return jsonResponse(loginResponse());
      });

      final session = await container.read(authRepositoryProvider).login(
            email: 'client@test.dev',
            password: 'Client123!',
          );

      expect(captured.path, '/api/auth/login');
      expect(captured.data, {
        'email': 'client@test.dev',
        'password': 'Client123!',
      });
      expect(session.token, 'jwt-token');
      expect(session.user.id, 3);
      expect(session.user.role, 'Client');
      expect(session.expiresAt.isUtc, isTrue);
    });

    test('turns a 401 into ApiException with the problem title', () async {
      final container = containerWith(
        (options, stream) async => jsonResponse(
          {'title': 'Sign-in failed', 'detail': 'Invalid email or password.'},
          status: 401,
        ),
      );

      await expectLater(
        container.read(authRepositoryProvider).login(
              email: 'x@y.dev',
              password: 'wrong',
            ),
        throwsA(
          isA<ApiException>()
              .having((e) => e.statusCode, 'statusCode', 401)
              .having((e) => e.message, 'message', 'Invalid email or password.'),
        ),
      );
    });
  });

  group('AuthRepository.register', () {
    test('sends the mobile account payload', () async {
      late RequestOptions captured;
      final container = containerWith((options, stream) async {
        captured = options;
        return jsonResponse(loginResponse(role: 'Provider', id: 9));
      });

      final session = await container.read(authRepositoryProvider).register(
            name: 'Sam Seller',
            email: 'sam@test.dev',
            password: 'Seller123!',
            role: 'Provider',
            phone: '555-999',
            location: '',
          );

      expect(captured.path, '/api/auth/register');
      expect(captured.data, {
        'name': 'Sam Seller',
        'email': 'sam@test.dev',
        'password': 'Seller123!',
        'role': 'Provider',
        'phone': '555-999',
        // empty location is omitted, not sent
      });
      expect(session.user.role, 'Provider');
    });
  });

  group('AuthController', () {
    test('successful sign-in persists the session', () async {
      final container = containerWith(
        (options, stream) async => jsonResponse(loginResponse()),
      );
      final controller = container.read(authControllerProvider.notifier);
      await container.read(authControllerProvider.future);

      final user = await controller.signIn(
        email: 'client@test.dev',
        password: 'Client123!',
      );

      expect(user.name, 'Cara Client');
      final state = container.read(authControllerProvider);
      expect(state.value?.token, 'jwt-token');
      expect(container.read(sessionStoreProvider).token, 'jwt-token');
    });

    test('rejects dashboard roles with a clear message', () async {
      final container = containerWith(
        (options, stream) async =>
            jsonResponse(loginResponse(role: 'Admin', id: 4)),
      );
      final controller = container.read(authControllerProvider.notifier);
      await container.read(authControllerProvider.future);

      await expectLater(
        controller.signIn(email: 'admin@test.dev', password: 'Admin123!'),
        throwsA(
          isA<ApiException>()
              .having((e) => e.statusCode, 'statusCode', 403)
              .having(
                (e) => e.detail,
                'detail',
                contains('web dashboard'),
              ),
        ),
      );

      // The rejected session must not be stored.
      expect(container.read(authControllerProvider).value, isNull);
      expect(container.read(sessionStoreProvider).token, isNull);
    });

    test('signOut clears state and storage', () async {
      final container = containerWith(
        (options, stream) async => jsonResponse(loginResponse()),
      );
      final controller = container.read(authControllerProvider.notifier);
      await container.read(authControllerProvider.future);

      await controller.signIn(
        email: 'client@test.dev',
        password: 'Client123!',
      );
      await controller.signOut();

      expect(container.read(authControllerProvider).value, isNull);
      expect(container.read(sessionStoreProvider).token, isNull);
    });
  });
}
