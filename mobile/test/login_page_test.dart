import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:market_workplace/core/api/api_exception.dart';
import 'package:market_workplace/core/auth/session.dart';
import 'package:market_workplace/features/auth/auth_controller.dart';
import 'package:market_workplace/features/auth/auth_repository.dart';
import 'package:market_workplace/features/auth/login_page.dart';
import 'package:market_workplace/l10n/generated/app_localizations.dart';
import 'package:shared_preferences/shared_preferences.dart';

class FakeAuthRepository implements AuthRepository {
  FakeAuthRepository({this.session, this.error});

  Session? session;
  Object? error;
  int loginCalls = 0;
  String? lastEmail;
  String? lastPassword;

  @override
  Future<Session> login({required String email, required String password}) {
    loginCalls++;
    lastEmail = email;
    lastPassword = password;
    final failure = error;
    if (failure != null) return Future<Session>.error(failure);
    return Future<Session>.value(session!);
  }

  @override
  Future<Session> register({
    required String name,
    required String email,
    required String password,
    String? role,
    String? phone,
    String? location,
  }) =>
      throw UnimplementedError();

  @override
  Future<UserProfile> me() => throw UnimplementedError();
}

class _HomeStub extends StatelessWidget {
  const _HomeStub();

  @override
  Widget build(BuildContext context) =>
      const Scaffold(body: Center(child: Text('home')));
}

Session clientSession() => Session(
      token: 'tok-1',
      expiresAt: DateTime.now().toUtc().add(const Duration(hours: 12)),
      user: const UserProfile(
        id: 3,
        email: 'client@test.dev',
        name: 'Cara Client',
        role: 'Client',
        type: 'Mobile',
      ),
    );

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  GoRouter buildRouter() => GoRouter(
        initialLocation: '/login',
        routes: [
          GoRoute(
            path: '/',
            builder: (context, state) => const _HomeStub(),
          ),
          GoRoute(
            path: '/login',
            builder: (context, state) => const LoginPage(),
          ),
        ],
      );

  Future<FakeAuthRepository> pumpLogin(
    WidgetTester tester, {
    FakeAuthRepository? repo,
  }) async {
    final fake = repo ?? FakeAuthRepository();
    await tester.pumpWidget(
      ProviderScope(
        overrides: [authRepositoryProvider.overrideWithValue(fake)],
        child: MaterialApp.router(
          routerConfig: buildRouter(),
          localizationsDelegates: AppLocalizations.localizationsDelegates,
          supportedLocales: AppLocalizations.supportedLocales,
        ),
      ),
    );
    await tester.pumpAndSettle();
    return fake;
  }

  Future<void> submit(
    WidgetTester tester, {
    required String email,
    required String password,
  }) async {
    await tester.enterText(find.byType(TextFormField).at(0), email);
    await tester.enterText(find.byType(TextFormField).at(1), password);
    await tester.tap(find.widgetWithText(FilledButton, 'Sign in'));
    await tester.pumpAndSettle();
  }

  testWidgets('empty submit shows validation errors and calls no API',
      (tester) async {
    final fake = await pumpLogin(tester);

    await tester.tap(find.widgetWithText(FilledButton, 'Sign in'));
    await tester.pump();

    expect(find.text('Email is required.'), findsOneWidget);
    expect(find.text('Password is required.'), findsOneWidget);
    expect(fake.loginCalls, 0);
  });

  testWidgets('invalid email is rejected client-side', (tester) async {
    final fake = await pumpLogin(tester);

    await submit(tester, email: 'not-an-email', password: 'secret1');

    expect(find.text('Enter a valid email address.'), findsOneWidget);
    expect(fake.loginCalls, 0);
  });

  testWidgets('valid credentials sign in and navigate home', (tester) async {
    final fake = await pumpLogin(
      tester,
      repo: FakeAuthRepository(session: clientSession()),
    );

    await submit(tester, email: '  client@test.dev  ', password: 'Client123!');

    expect(fake.loginCalls, 1);
    expect(fake.lastEmail, 'client@test.dev');
    expect(fake.lastPassword, 'Client123!');
    expect(find.text('home'), findsOneWidget);
  });

  testWidgets('backend errors are shown on the form', (tester) async {
    final fake = await pumpLogin(
      tester,
      repo: FakeAuthRepository(
        error: const ApiException(
          statusCode: 401,
          title: 'Sign-in failed',
          detail: 'Invalid email or password.',
        ),
      ),
    );

    await submit(tester, email: 'x@y.dev', password: 'nope12');

    expect(fake.loginCalls, 1);
    expect(find.text('Invalid email or password.'), findsOneWidget);
    expect(find.text('home'), findsNothing);
  });

  testWidgets('signing in stores the session in the controller',
      (tester) async {
    final fake = await pumpLogin(
      tester,
      repo: FakeAuthRepository(session: clientSession()),
    );

    final element = tester.element(find.byType(LoginPage));
    final container = ProviderScope.containerOf(element);

    await submit(tester, email: 'client@test.dev', password: 'Client123!');

    expect(fake.loginCalls, 1);
    final state = container.read(authControllerProvider);
    expect(state.value?.token, 'tok-1');
    expect(state.value?.user.role, 'Client');
  });

  setUp(() => SharedPreferences.setMockInitialValues({}));
}
