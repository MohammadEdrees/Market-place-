import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/auth_controller.dart';

/// One bottom-navigation tab: its shell branch and how it renders.
class NavTab {
  const NavTab({
    required this.branch,
    required this.icon,
    required this.selectedIcon,
    required this.label,
  });

  final int branch;
  final IconData icon;
  final IconData selectedIcon;
  final String label;
}

/// The tabs for a role. Clients never see the listing-management tab;
/// providers get it between Services and Orders.
List<NavTab> navTabsFor({required bool isProvider}) => [
      const NavTab(
        branch: 0,
        icon: Icons.storefront_outlined,
        selectedIcon: Icons.storefront,
        label: 'Browse',
      ),
      const NavTab(
        branch: 1,
        icon: Icons.home_repair_service_outlined,
        selectedIcon: Icons.home_repair_service,
        label: 'Services',
      ),
      if (isProvider)
        const NavTab(
          branch: 2,
          icon: Icons.inventory_2_outlined,
          selectedIcon: Icons.inventory_2,
          label: 'Listings',
        ),
      const NavTab(
        branch: 3,
        icon: Icons.receipt_long_outlined,
        selectedIcon: Icons.receipt_long,
        label: 'Orders',
      ),
      const NavTab(
        branch: 4,
        icon: Icons.person_outline,
        selectedIcon: Icons.person,
        label: 'Profile',
      ),
    ];

/// Root scaffold holding the shell's pages plus the bottom navigation bar.
class AppShell extends ConsumerWidget {
  const AppShell({super.key, required this.navigationShell});

  final StatefulNavigationShell navigationShell;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(authControllerProvider).value;
    final tabs = navTabsFor(isProvider: session?.user.isProvider ?? false);
    final selectedIndex =
        tabs.indexWhere((tab) => tab.branch == navigationShell.currentIndex);

    return Scaffold(
      body: navigationShell,
      bottomNavigationBar: NavigationBar(
        selectedIndex: selectedIndex < 0 ? 0 : selectedIndex,
        onDestinationSelected: (index) {
          final branch = tabs[index].branch;
          navigationShell.goBranch(
            branch,
            initialLocation: branch == navigationShell.currentIndex,
          );
        },
        destinations: [
          for (final tab in tabs)
            NavigationDestination(
              icon: Icon(tab.icon),
              selectedIcon: Icon(tab.selectedIcon),
              label: tab.label,
            ),
        ],
      ),
    );
  }
}
