import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/api_exception.dart';
import '../../core/format/formatters.dart';
import '../../core/i18n/l10n_ext.dart';
import '../../core/widgets/gallery.dart';
import '../../core/widgets/state_views.dart';
import '../../core/widgets/status_badge.dart';
import '../auth/auth_controller.dart';
import '../orders/orders_repository.dart';
import 'catalog_providers.dart';
import 'models.dart';

/// Full product detail with gallery, stock info and the buy action.
class ProductDetailPage extends ConsumerWidget {
  const ProductDetailPage({super.key, required this.id});

  final int id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      appBar: AppBar(title: Text(context.l10n.productDetailTitle)),
      body: AsyncValueView<Product>(
        value: ref.watch(productProvider(id)),
        onRetry: () => ref.invalidate(productProvider(id)),
        data: (product) => _ProductDetail(product: product),
      ),
    );
  }
}

class _ProductDetail extends ConsumerWidget {
  const _ProductDetail({required this.product});

  final Product product;

  Future<void> _buy(BuildContext context, WidgetRef ref) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(dialogContext.l10n.productDetailConfirmTitle),
        content: Text(
          dialogContext.l10n.productDetailConfirmMessage(
            product.name,
            formatMoney(product.price),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: Text(dialogContext.l10n.commonCancel),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: Text(dialogContext.l10n.productDetailBuy),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;

    try {
      await ref
          .read(ordersRepositoryProvider)
          .placeOrder(kind: 'Product', productId: product.id);
      ref.invalidate(productProvider(product.id));
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(context.l10n.productDetailOrderPlaced),
          ),
        );
      }
    } on ApiException catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(context.localizeApiMessage(e.message)),
            backgroundColor: Theme.of(context).colorScheme.error,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final l10n = context.l10n;
    final session = ref.watch(authControllerProvider).value;
    final mine = session != null && product.sellerId == session.user.id;

    return Column(
      children: [
        Expanded(
          child: ListView(
            padding: const EdgeInsets.only(bottom: 16),
            children: [
              Gallery(
                images: [for (final image in product.images) image.path],
              ),
              Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      product.name,
                      style: theme.textTheme.headlineSmall
                          ?.copyWith(fontWeight: FontWeight.w700),
                    ),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        Text(
                          formatMoney(product.price),
                          style: theme.textTheme.headlineSmall?.copyWith(
                            fontWeight: FontWeight.w700,
                            color: theme.colorScheme.primary,
                          ),
                        ),
                        const Spacer(),
                        StatusBadge(status: product.status),
                      ],
                    ),
                    const SizedBox(height: 16),
                    Wrap(
                      spacing: 8,
                      runSpacing: 8,
                      children: [
                        _InfoChip(
                          icon: Icons.category_outlined,
                          label: product.category,
                        ),
                        _InfoChip(
                          icon: Icons.inventory_2_outlined,
                          label: product.inStock
                              ? l10n.productDetailInStock(
                                  product.stock.toString())
                              : l10n.productDetailNoneLeft,
                        ),
                        if (product.sold > 0)
                          _InfoChip(
                            icon: Icons.sell_outlined,
                            label:
                                l10n.productDetailSold(product.sold.toString()),
                          ),
                        _InfoChip(
                          icon: Icons.qr_code_outlined,
                          label: product.sku,
                        ),
                      ],
                    ),
                    if (mine) ...[
                      const SizedBox(height: 16),
                      Text(
                        l10n.productDetailYours,
                        style: theme.textTheme.bodyMedium?.copyWith(
                          color: theme.colorScheme.onSurfaceVariant,
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ],
          ),
        ),
        SafeArea(
          top: false,
          child: Container(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
            decoration: BoxDecoration(
              color: theme.colorScheme.surface,
              border: Border(top: BorderSide(color: theme.dividerColor)),
            ),
            child: mine
                ? FilledButton.icon(
                    onPressed: () =>
                        context.go('/listings/product/${product.id}'),
                    icon: const Icon(Icons.edit_outlined),
                    label: Text(l10n.productDetailEditListing),
                  )
                : FilledButton.icon(
                    onPressed:
                        product.inStock ? () => _buy(context, ref) : null,
                    icon: const Icon(Icons.shopping_cart_outlined),
                    label: Text(
                      product.inStock
                          ? l10n.productDetailBuyNow
                          : l10n.statusOutOfStock,
                    ),
                  ),
          ),
        ),
      ],
    );
  }
}

class _InfoChip extends StatelessWidget {
  const _InfoChip({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: theme.colorScheme.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 15, color: theme.colorScheme.onSurfaceVariant),
          const SizedBox(width: 6),
          Text(label, style: theme.textTheme.bodySmall),
        ],
      ),
    );
  }
}
