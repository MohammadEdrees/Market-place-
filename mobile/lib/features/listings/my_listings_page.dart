import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/format/formatters.dart';
import '../../core/i18n/l10n_ext.dart';
import '../../core/widgets/listing_image.dart';
import '../../core/widgets/state_views.dart';
import '../../core/widgets/status_badge.dart';
import '../catalog/catalog_providers.dart';
import '../catalog/catalog_repository.dart';
import '../catalog/models.dart';

/// Provider workspace: own products and services with edit/delete and a
/// create action.
class MyListingsPage extends StatelessWidget {
  const MyListingsPage({super.key});

  void _showCreateSheet(BuildContext context) {
    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.shopping_bag_outlined),
              title: Text(sheetContext.l10n.myListingsNewProduct),
              onTap: () {
                Navigator.of(sheetContext).pop();
                context.go('/listings/product/new');
              },
            ),
            ListTile(
              leading: const Icon(Icons.home_repair_service_outlined),
              title: Text(sheetContext.l10n.myListingsNewService),
              onTap: () {
                Navigator.of(sheetContext).pop();
                context.go('/listings/service/new');
              },
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = context.l10n;
    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: Text(l10n.myListingsTitle),
          bottom: TabBar(
            tabs: [
              Tab(text: l10n.myListingsProductsTab),
              Tab(text: l10n.myListingsServicesTab),
            ],
          ),
        ),
        floatingActionButton: FloatingActionButton.extended(
          onPressed: () => _showCreateSheet(context),
          icon: const Icon(Icons.add),
          label: Text(l10n.myListingsCreate),
        ),
        body: const TabBarView(
          children: [_MyProductsTab(), _MyServicesTab()],
        ),
      ),
    );
  }
}

class _MyProductsTab extends ConsumerWidget {
  const _MyProductsTab();

  Future<void> _delete(
    BuildContext context,
    WidgetRef ref,
    Product product,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(dialogContext.l10n.myListingsDeleteProductTitle),
        content: Text(
          dialogContext.l10n.myListingsDeleteProductMessage(product.name),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: Text(dialogContext.l10n.commonCancel),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: Text(dialogContext.l10n.commonDelete),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;

    try {
      await ref.read(catalogRepositoryProvider).deleteProduct(product.id);
      ref.invalidate(myProductsProvider);
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              context.l10n.myListingsDeletedProduct(product.name),
            ),
          ),
        );
      }
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(errorMessage(context, e)),
            backgroundColor: Theme.of(context).colorScheme.error,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return AsyncValueView<List<Product>>(
      value: ref.watch(myProductsProvider),
      onRetry: () => ref.invalidate(myProductsProvider),
      data: (products) {
        if (products.isEmpty) {
          return EmptyView(
            icon: Icons.inventory_2_outlined,
            message: context.l10n.myListingsEmptyProducts,
          );
        }
        return ListView.builder(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 88),
          itemCount: products.length,
          itemBuilder: (context, index) {
            final product = products[index];
            return Card(
              margin: const EdgeInsets.only(bottom: 10),
              clipBehavior: Clip.antiAlias,
              child: ListTile(
                contentPadding: const EdgeInsets.fromLTRB(8, 4, 4, 4),
                leading: SizedBox(
                  width: 56,
                  height: 56,
                  child: ListingImage(
                    url:
                        product.images.isEmpty ? null : product.images.first.path,
                  ),
                ),
                title: Text(
                  product.name,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                subtitle: Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Row(
                    children: [
                      Text(
                        formatMoney(product.price),
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          context.l10n.myListingsStockAndSold(
                            product.stock.toString(),
                            product.sold.toString(),
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: Theme.of(context).textTheme.bodySmall,
                        ),
                      ),
                      StatusBadge(status: product.status),
                    ],
                  ),
                ),
                trailing: PopupMenuButton<String>(
                  onSelected: (action) {
                    if (action == 'edit') {
                      context.go('/listings/product/${product.id}');
                    } else {
                      _delete(context, ref, product);
                    }
                  },
                  itemBuilder: (popupContext) => [
                    PopupMenuItem(
                      value: 'edit',
                      child: ListTile(
                        leading: const Icon(Icons.edit_outlined),
                        title: Text(popupContext.l10n.commonEdit),
                        contentPadding: EdgeInsets.zero,
                      ),
                    ),
                    PopupMenuItem(
                      value: 'delete',
                      child: ListTile(
                        leading: const Icon(Icons.delete_outline),
                        title: Text(popupContext.l10n.commonDelete),
                        contentPadding: EdgeInsets.zero,
                      ),
                    ),
                  ],
                ),
              ),
            );
          },
        );
      },
    );
  }
}

class _MyServicesTab extends ConsumerWidget {
  const _MyServicesTab();

  Future<void> _delete(
    BuildContext context,
    WidgetRef ref,
    Service service,
  ) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(dialogContext.l10n.myListingsDeleteServiceTitle),
        content: Text(
          dialogContext.l10n.myListingsDeleteServiceMessage(service.title),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: Text(dialogContext.l10n.commonCancel),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: Text(dialogContext.l10n.commonDelete),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;

    try {
      await ref.read(catalogRepositoryProvider).deleteService(service.id);
      ref.invalidate(myServicesProvider);
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              context.l10n.myListingsDeletedService(service.title),
            ),
          ),
        );
      }
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(errorMessage(context, e)),
            backgroundColor: Theme.of(context).colorScheme.error,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return AsyncValueView<List<Service>>(
      value: ref.watch(myServicesProvider),
      onRetry: () => ref.invalidate(myServicesProvider),
      data: (services) {
        if (services.isEmpty) {
          return EmptyView(
            icon: Icons.home_repair_service_outlined,
            message: context.l10n.myListingsEmptyServices,
          );
        }
        return ListView.builder(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 88),
          itemCount: services.length,
          itemBuilder: (context, index) {
            final service = services[index];
            return Card(
              margin: const EdgeInsets.only(bottom: 10),
              clipBehavior: Clip.antiAlias,
              child: ListTile(
                contentPadding: const EdgeInsets.fromLTRB(8, 4, 4, 4),
                leading: SizedBox(
                  width: 56,
                  height: 56,
                  child: ListingImage(
                    url: service.images.isEmpty
                        ? null
                        : service.images.first.path,
                  ),
                ),
                title: Text(
                  service.title,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
                subtitle: Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Row(
                    children: [
                      Text(
                        formatMoney(service.cost),
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          service.location,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: Theme.of(context).textTheme.bodySmall,
                        ),
                      ),
                      StatusBadge(
                        status: service.isActive ? 'Active' : 'Inactive',
                      ),
                    ],
                  ),
                ),
                trailing: PopupMenuButton<String>(
                  onSelected: (action) {
                    if (action == 'edit') {
                      context.go('/listings/service/${service.id}');
                    } else {
                      _delete(context, ref, service);
                    }
                  },
                  itemBuilder: (popupContext) => [
                    PopupMenuItem(
                      value: 'edit',
                      child: ListTile(
                        leading: const Icon(Icons.edit_outlined),
                        title: Text(popupContext.l10n.commonEdit),
                        contentPadding: EdgeInsets.zero,
                      ),
                    ),
                    PopupMenuItem(
                      value: 'delete',
                      child: ListTile(
                        leading: const Icon(Icons.delete_outline),
                        title: Text(popupContext.l10n.commonDelete),
                        contentPadding: EdgeInsets.zero,
                      ),
                    ),
                  ],
                ),
              ),
            );
          },
        );
      },
    );
  }
}
