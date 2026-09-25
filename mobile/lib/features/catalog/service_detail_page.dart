import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/api/api_exception.dart';
import '../../core/format/formatters.dart';
import '../../core/i18n/l10n_ext.dart';
import '../../core/widgets/gallery.dart';
import '../../core/widgets/state_views.dart';
import '../auth/auth_controller.dart';
import '../orders/orders_repository.dart';
import 'catalog_providers.dart';
import 'models.dart';

/// Full service detail with gallery, offers, contact info and reservation.
class ServiceDetailPage extends ConsumerWidget {
  const ServiceDetailPage({super.key, required this.id});

  final int id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      appBar: AppBar(title: Text(context.l10n.serviceDetailTitle)),
      body: AsyncValueView<Service>(
        value: ref.watch(serviceProvider(id)),
        onRetry: () => ref.invalidate(serviceProvider(id)),
        data: (service) => _ServiceDetail(service: service),
      ),
    );
  }
}

class _ServiceDetail extends ConsumerWidget {
  const _ServiceDetail({required this.service});

  final Service service;

  Future<void> _reserve(BuildContext context, WidgetRef ref) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(dialogContext.l10n.serviceDetailConfirmTitle),
        content: Text(
          dialogContext.l10n.serviceDetailConfirmMessage(
            service.title,
            formatMoney(service.cost),
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: Text(dialogContext.l10n.commonCancel),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: Text(dialogContext.l10n.serviceDetailReserve),
          ),
        ],
      ),
    );
    if (confirmed != true || !context.mounted) return;

    try {
      await ref
          .read(ordersRepositoryProvider)
          .placeOrder(kind: 'Service', serviceId: service.id);
      ref.invalidate(serviceProvider(service.id));
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(context.l10n.serviceDetailReserved),
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
    final mine = session != null && service.providerId == session.user.id;

    return Column(
      children: [
        Expanded(
          child: ListView(
            padding: const EdgeInsets.only(bottom: 16),
            children: [
              Gallery(
                images: [for (final image in service.images) image.path],
              ),
              Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      service.title,
                      style: theme.textTheme.headlineSmall
                          ?.copyWith(fontWeight: FontWeight.w700),
                    ),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        Text(
                          formatMoney(service.cost),
                          style: theme.textTheme.headlineSmall?.copyWith(
                            fontWeight: FontWeight.w700,
                            color: theme.colorScheme.primary,
                          ),
                        ),
                        const Spacer(),
                        if (!service.isActive)
                          Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 8,
                              vertical: 4,
                            ),
                            decoration: BoxDecoration(
                              color: const Color(0xFFECECF3),
                              borderRadius: BorderRadius.circular(999),
                            ),
                            child: Text(
                              context.statusLabel('Inactive'),
                              style: const TextStyle(
                                fontSize: 11,
                                fontWeight: FontWeight.w600,
                                color: Color(0xFF5A5A6E),
                              ),
                            ),
                          ),
                      ],
                    ),
                    const SizedBox(height: 16),
                    Wrap(
                      spacing: 8,
                      runSpacing: 8,
                      children: [
                        _InfoChip(
                          icon: Icons.category_outlined,
                          label: service.category,
                        ),
                        _InfoChip(
                          icon: Icons.location_on_outlined,
                          label: service.location,
                        ),
                        if (service.offers.isNotEmpty)
                          _InfoChip(
                            icon: Icons.local_offer_outlined,
                            label: service.offers,
                          ),
                      ],
                    ),
                    if (service.description.isNotEmpty) ...[
                      const SizedBox(height: 20),
                      Text(
                        l10n.serviceDetailAbout,
                        style: theme.textTheme.titleMedium
                            ?.copyWith(fontWeight: FontWeight.w700),
                      ),
                      const SizedBox(height: 6),
                      Text(
                        service.description,
                        style: theme.textTheme.bodyMedium?.copyWith(
                          color: theme.colorScheme.onSurfaceVariant,
                          height: 1.45,
                        ),
                      ),
                    ],
                    const SizedBox(height: 20),
                    Text(
                      l10n.serviceDetailContact,
                      style: theme.textTheme.titleMedium
                          ?.copyWith(fontWeight: FontWeight.w700),
                    ),
                    const SizedBox(height: 8),
                    Container(
                      width: double.infinity,
                      padding: const EdgeInsets.all(14),
                      decoration: BoxDecoration(
                        color: theme.colorScheme.surfaceContainerHighest,
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Row(
                        children: [
                          Icon(
                            Icons.contact_phone_outlined,
                            color: theme.colorScheme.primary,
                          ),
                          const SizedBox(width: 10),
                          Expanded(
                            child: Text(
                              service.contactInfo,
                              style: theme.textTheme.bodyMedium,
                            ),
                          ),
                        ],
                      ),
                    ),
                    if (mine) ...[
                      const SizedBox(height: 16),
                      Text(
                        l10n.serviceDetailYours,
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
                        context.go('/listings/service/${service.id}'),
                    icon: const Icon(Icons.edit_outlined),
                    label: Text(l10n.serviceDetailEditService),
                  )
                : FilledButton.icon(
                    onPressed:
                        service.isActive ? () => _reserve(context, ref) : null,
                    icon: const Icon(Icons.event_available_outlined),
                    label: Text(
                      service.isActive
                          ? l10n.serviceDetailReserve
                          : l10n.serviceDetailUnavailable,
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
          Flexible(child: Text(label, style: theme.textTheme.bodySmall)),
        ],
      ),
    );
  }
}
