import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_exception.dart';
import '../../core/auth/session.dart';
import 'auth_repository.dart';

/// The current session: `null` = signed out, data = signed in,
/// loading = the persisted session is being restored at startup.
final authControllerProvider =
    AsyncNotifierProvider<AuthController, Session?>(AuthController.new);

class AuthController extends AsyncNotifier<Session?> {
  SessionStore get _store => ref.read(sessionStoreProvider);

  @override
  Future<Session?> build() async {
    final store = ref.read(sessionStoreProvider);
    store.onUnauthorized = () {
      if (state.value != null) state = const AsyncData(null);
    };
    return store.restore();
  }

  Future<UserProfile> signIn({
    required String email,
    required String password,
  }) async {
    final session = await ref.read(authRepositoryProvider).login(
          email: email,
          password: password,
        );
    return _activate(session);
  }

  Future<UserProfile> signUp({
    required String name,
    required String email,
    required String password,
    required String role,
    String? phone,
    String? location,
  }) async {
    final session = await ref.read(authRepositoryProvider).register(
          name: name,
          email: email,
          password: password,
          role: role,
          phone: phone,
          location: location,
        );
    return _activate(session);
  }

  Future<void> signOut() async {
    await _store.clear();
    state = const AsyncData(null);
  }

  /// Persists an updated profile (from `PUT /api/users/me`) into the session.
  Future<void> applyUser(UserProfile user) async {
    final current = state.value;
    if (current == null) return;
    final updated = Session(
      token: current.token,
      expiresAt: current.expiresAt,
      user: user,
    );
    await _store.save(updated);
    state = AsyncData(updated);
  }

  Future<UserProfile> _activate(Session session) async {
    if (!session.user.canUseMobileApp) {
      throw const ApiException(
        statusCode: 403,
        title: 'Dashboard account',
        detail:
            'This account belongs to the web dashboard. Sign in with a Client or Provider account.',
      );
    }
    await _store.save(session);
    state = AsyncData(session);
    return session.user;
  }
}
