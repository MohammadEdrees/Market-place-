import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/auth_controller.dart';
import '../../features/auth/login_page.dart';
import '../../features/auth/register_page.dart';
import '../../features/catalog/product_detail_page.dart';
import '../../features/catalog/products_page.dart';
import '../../features/catalog/service_detail_page.dart';
import '../../features/catalog/services_page.dart';
import '../../features/listings/listing_form_page.dart';
import '../../features/listings/my_listings_page.dart';
import '../../features/orders/orders_page.dart';
import '../../features/profile/profile_page.dart';
import '../shell/app_shell.dart';
import '../shell/splash_page.dart';

/// Bumped whenever the session changes so go_router re-runs its redirects.
class RouterRefresh extends ChangeNotifier {
  void notify() => notifyListeners();
}

final routerRefreshProvider = Provider<RouterRefresh>((ref) => RouterRefresh());

/// The app's single navigator: auth screens, the bottom-nav shell and the
/// full-screen detail/form pages pushed on top of it.
final routerProvider = Provider<GoRouter>((ref) {
  final refresh = ref.read(routerRefreshProvider);

  return GoRouter(
    initialLocation: '/splash',
    refreshListenable: refresh,
    redirect: (context, state) => _redirect(ref, state),
    routes: [
      GoRoute(
        path: '/splash',
        builder: (context, state) => const SplashPage(),
      ),
      GoRoute(
        path: '/login',
        builder: (context, state) => const LoginPage(),
      ),
      GoRoute(
        path: '/register',
        builder: (context, state) => const RegisterPage(),
      ),
      StatefulShellRoute.indexedStack(
        builder: (context, state, navigationShell) =>
            AppShell(navigationShell: navigationShell),
        branches: [
          StatefulShellBranch(routes: [
            GoRoute(
              path: '/',
              builder: (context, state) => const ProductsPage(),
            ),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(
              path: '/services',
              builder: (context, state) => const ServicesPage(),
            ),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(
              path: '/listings',
              builder: (context, state) => const MyListingsPage(),
            ),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(
              path: '/orders',
              builder: (context, state) => const OrdersPage(),
            ),
          ]),
          StatefulShellBranch(routes: [
            GoRoute(
              path: '/profile',
              builder: (context, state) => const ProfilePage(),
            ),
          ]),
        ],
      ),
      GoRoute(
        path: '/products/:id',
        builder: (context, state) {
          final id = int.tryParse(state.pathParameters['id'] ?? '');
          return id == null
              ? const ProductsPage()
              : ProductDetailPage(id: id);
        },
      ),
      GoRoute(
        path: '/services/:id',
        builder: (context, state) {
          final id = int.tryParse(state.pathParameters['id'] ?? '');
          return id == null
              ? const ServicesPage()
              : ServiceDetailPage(id: id);
        },
      ),
      GoRoute(
        path: '/listings/:kind/new',
        builder: (context, state) => ListingFormPage(
          kind: state.pathParameters['kind'] ?? 'product',
        ),
      ),
      GoRoute(
        path: '/listings/:kind/:id',
        builder: (context, state) => ListingFormPage(
          kind: state.pathParameters['kind'] ?? 'product',
          id: int.tryParse(state.pathParameters['id'] ?? ''),
        ),
      ),
    ],
  );
});

/// Access rules: signed-out users only reach the auth screens, signed-in
/// users never see them, and listing management is provider-only.
String? _redirect(Ref ref, GoRouterState state) {
  final path = state.matchedLocation;
  final auth = ref.read(authControllerProvider);

  if (auth.isLoading) return path == '/splash' ? null : '/splash';

  final session = auth.value;
  final onAuthRoute = path == '/login' || path == '/register';

  if (session == null) return onAuthRoute ? null : '/login';
  if (path == '/splash' || onAuthRoute) return '/';
  if (path == '/listings' && !session.user.isProvider) return '/';
  return null;
}
