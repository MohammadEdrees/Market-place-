import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:market_workplace/core/auth/session.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  Session buildSession({
    String token = 'jwt-abc',
    DateTime? expiresAt,
    String role = 'Client',
  }) =>
      Session(
        token: token,
        expiresAt: expiresAt ??
            DateTime.now().toUtc().add(const Duration(hours: 12)),
        user: UserProfile(
          id: 5,
          email: 'cara@test.dev',
          name: 'Cara Client',
          role: role,
          type: 'Mobile',
          phone: '555-1234',
          location: 'Amman',
        ),
      );

  group('SessionStore', () {
    test('save → restore round-trips an unexpired session', () async {
      SharedPreferences.setMockInitialValues({});

      final store = SessionStore();
      await store.save(buildSession());
      expect(store.token, 'jwt-abc');

      final restored = await SessionStore().restore();
      expect(restored, isNotNull);
      expect(restored!.token, 'jwt-abc');
      expect(restored.user.name, 'Cara Client');
      expect(restored.user.role, 'Client');
      expect(restored.user.phone, '555-1234');
      expect(restored.isExpired, isFalse);
    });

    test('restore clears an expired session', () async {
      SharedPreferences.setMockInitialValues({
        'session.token': 'stale',
        'session.expiresAt': '2020-01-01T00:00:00.000Z',
        'session.user': jsonEncode({
          'id': 1,
          'email': 'old@test.dev',
          'name': 'Old',
          'role': 'Client',
          'type': 'Mobile',
        }),
      });

      final store = SessionStore();
      final restored = await store.restore();

      expect(restored, isNull);
      expect(store.token, isNull);
      final prefs = await SharedPreferences.getInstance();
      expect(prefs.getString('session.token'), isNull);
    });

    test('restore returns null when nothing was persisted', () async {
      SharedPreferences.setMockInitialValues({});
      expect(await SessionStore().restore(), isNull);
    });

    test('expire clears the session and notifies listeners', () async {
      SharedPreferences.setMockInitialValues({});
      final store = SessionStore();
      await store.save(buildSession());

      var notified = false;
      store.onUnauthorized = () => notified = true;

      await store.expire();

      expect(notified, isTrue);
      expect(store.token, isNull);
      expect(store.current, isNull);
      final prefs = await SharedPreferences.getInstance();
      expect(prefs.getString('session.token'), isNull);
    });

    test('expire does not notify when already signed out', () async {
      SharedPreferences.setMockInitialValues({});
      final store = SessionStore();
      var notified = false;
      store.onUnauthorized = () => notified = true;

      await store.expire();

      expect(notified, isFalse);
    });
  });

  group('UserProfile', () {
    test('derives mobile capabilities from the role', () {
      const provider = UserProfile(
        id: 1,
        email: 'p@t.dev',
        name: 'P',
        role: 'Provider',
      );
      const client = UserProfile(
        id: 2,
        email: 'c@t.dev',
        name: 'C',
        role: 'Client',
      );
      const admin = UserProfile(
        id: 3,
        email: 'a@t.dev',
        name: 'A',
        role: 'Admin',
      );

      expect(provider.isProvider, isTrue);
      expect(provider.canUseMobileApp, isTrue);
      expect(client.isProvider, isFalse);
      expect(client.canUseMobileApp, isTrue);
      expect(admin.isProvider, isFalse);
      expect(admin.canUseMobileApp, isFalse);
    });

    test('round-trips through JSON', () {
      const user = UserProfile(
        id: 7,
        email: 'x@y.dev',
        name: 'X',
        role: 'Provider',
        phone: '1',
        location: 'Zarqa',
        bio: 'Hello',
        imagePath: 'http://localhost:5240/images/users/a.png',
      );
      final copy = UserProfile.fromJson(user.toJson());
      expect(copy.id, 7);
      expect(copy.role, 'Provider');
      expect(copy.bio, 'Hello');
      expect(copy.imagePath, contains('http://localhost:5240'));
    });
  });
}
