import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/router/app_router.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/auth_controller.dart';

class MarketWorkplaceApp extends ConsumerWidget {
  const MarketWorkplaceApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Re-run the router redirects whenever the session appears, changes or disappears.
    ref.listen(
      authControllerProvider,
      (_, _) => ref.read(routerRefreshProvider).notify(),
    );
    final router = ref.watch(routerProvider);

    return MaterialApp.router(
      title: 'Market Workplace',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      routerConfig: router,
    );
  }
}
