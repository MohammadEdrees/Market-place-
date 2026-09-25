import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/format/formatters.dart';
import '../../core/widgets/state_views.dart';
import '../auth/auth_controller.dart';
import 'models.dart';
import 'orders_repository.dart';

/// Order history for clients and incoming-order management for providers.
class OrdersPage extends ConsumerStatefulWidget {
  const OrdersPage({super.key});

  @override
  ConsumerState<OrdersPage> createState() => _OrdersPageState();
}

class _OrdersPageState extends ConsumerState<OrdersPage> {
  final _searchController = TextEditingController();
  final _scrollController = ScrollController();

  List<Order> _orders = [];
  String _search = '';
  String? _kind;
  String? _status;
  int _page = 0;
  bool _hasMore = true;
  bool _loading = false;
  Object? _error;

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_loadMoreWhenNearEnd);
    _load(reset: true);
  }

  @override
  void dispose() {
    _scrollController.removeListener(_loadMoreWhenNearEnd);
    _scrollController.dispose();
    _searchController.dispose();
    super.dispose();
  }

  void _loadMoreWhenNearEnd() {
    if (!_hasMore || _loading) return;
    if (_scrollController.position.pixels >=
        _scrollController.position.maxScrollExtent - 400) {
      _load();
    }
  }

  Future<void> _load({bool reset = false}) async {
    if (_loading) return;
    setState(() {
      _loading = true;
      if (reset) _error = null;
    });
    try {
      final result = await ref.read(ordersRepositoryProvider).getOrders(
            search: _search,
            kind: _kind,
            status: _status,
            page: reset ? 1 : _page + 1,
          );
      if (!mounted) return;
      setState(() {
        _orders = reset ? result.items : [..._orders, ...result.items];
        _page = result.page;
        _hasMore = result.hasMore;
        _loading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e;
        _loading = false;
      });
    }
  }

  void _applySearch(String value) {
    setState(() => _search = value.trim());
    _load(reset: true);
  }

  void _setFilter({String? kind, String? status}) {
    setState(() {
      _kind = kind;
      _status = status;
    });
    _load(reset: true);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Orders')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 4),
            child: TextField(
              controller: _searchController,
              textInputAction: TextInputAction.search,
              onSubmitted: _applySearch,
              decoration: InputDecoration(
                hintText: 'Search orders',
                prefixIcon: const Icon(Icons.search),
                suffixIcon: _searchController.text.isEmpty
                    ? null
                    : IconButton(
                        icon: const Icon(Icons.clear),
                        onPressed: () {
                          _searchController.clear();
                          _applySearch('');
                        },
                      ),
                isDense: true,
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
            ),
          ),
          SizedBox(
            height: 56,
            child: Row(
              children: [
                Expanded(
                  child: ListView(
                    scrollDirection: Axis.horizontal,
                    padding: const EdgeInsets.only(left: 16),
                    children: [
                      Padding(
                        padding: const EdgeInsets.only(right: 8),
                        child: ChoiceChip(
                          label: const Text('All'),
                          selected: _kind == null,
                          onSelected: (_) => _setFilter(kind: null),
                        ),
                      ),
                      Padding(
                        padding: const EdgeInsets.only(right: 8),
                        child: ChoiceChip(
                          label: const Text('Products'),
                          selected: _kind == 'Product',
                          onSelected: (_) => _setFilter(kind: 'Product'),
                        ),
                      ),
                      Padding(
                        padding: const EdgeInsets.only(right: 8),
                        child: ChoiceChip(
                          label: const Text('Services'),
                          selected: _kind == 'Service',
                          onSelected: (_) => _setFilter(kind: 'Service'),
                        ),
                      ),
                    ],
                  ),
                ),
                PopupMenuButton<String?>(
                  tooltip: 'Filter by status',
                  icon: Icon(
                    Icons.filter_list,
                    color: _status == null
                        ? null
                        : Theme.of(context).colorScheme.primary,
                  ),
                  onSelected: (value) => _setFilter(status: value),
                  itemBuilder: (context) => [
                    const PopupMenuItem(value: null, child: Text('Any status')),
                    for (final status in kOrderStatuses)
                      PopupMenuItem(value: status, child: Text(status)),
                  ],
                ),
                const SizedBox(width: 4),
              ],
            ),
          ),
          Expanded(child: _buildBody()),
        ],
      ),
    );
  }

  Widget _buildBody() {
    if (_error != null && _orders.isEmpty) {
      return ErrorView(
        message: errorMessage(_error!),
        onRetry: () => _load(reset: true),
      );
    }
    if (_loading && _orders.isEmpty) return const LoadingView();
    if (_orders.isEmpty) {
      return const EmptyView(
        icon: Icons.receipt_long_outlined,
        message: 'No orders yet.',
      );
    }

    return RefreshIndicator(
      onRefresh: () => _load(reset: true),
      child: ListView.builder(
        controller: _scrollController,
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.fromLTRB(16, 4, 16, 16),
        itemCount: _orders.length + (_hasMore ? 1 : 0),
        itemBuilder: (context, index) {
          if (index == _orders.length) {
            return const Padding(
              padding: EdgeInsets.all(12),
              child: Center(
                child: SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(strokeWidth: 2),
                ),
              ),
            );
          }
          return _OrderCard(
            order: _orders[index],
            onOpen: () => _showOrderSheet(_orders[index]),
          );
        },
      ),
    );
  }

  /// Details sheet; providers also get the status actions for incoming orders.
  Future<void> _showOrderSheet(Order order) async {
    final session = ref.read(authControllerProvider).value;
    final myId = session?.user.id;
    final canManage =
        order.buyerId != null && !order.isPlacedBy(myId);

    await showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (sheetContext) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                order.product,
                style: Theme.of(sheetContext)
                    .textTheme
                    .titleLarge
                    ?.copyWith(fontWeight: FontWeight.w700),
              ),
              const SizedBox(height: 4),
              Text(
                '${order.isProduct ? 'Product purchase' : 'Service reservation'} · Order #${order.id}',
                style: Theme.of(sheetContext).textTheme.bodySmall?.copyWith(
                      color: Theme.of(sheetContext).colorScheme.onSurfaceVariant,
                    ),
              ),
              const SizedBox(height: 16),
              _detailRow(sheetContext, 'Status', order.status),
              _detailRow(sheetContext, 'Total', formatMoney(order.total)),
              _detailRow(sheetContext, 'Date', formatDate(order.date)),
              _detailRow(sheetContext, 'Customer', order.customer),
              if (order.category.isNotEmpty)
                _detailRow(sheetContext, 'Category', order.category),
              if (canManage) ...[
                const SizedBox(height: 16),
                const Divider(height: 1),
                const SizedBox(height: 12),
                Text(
                  'Update status',
                  style: Theme.of(sheetContext)
                      .textTheme
                      .titleMedium
                      ?.copyWith(fontWeight: FontWeight.w700),
                ),
                const SizedBox(height: 10),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    for (final status in kOrderStatuses)
                      if (status != order.status)
                        ActionChip(
                          label: Text(status),
                          onPressed: () async {
                            Navigator.of(sheetContext).pop();
                            await _updateStatus(order, status);
                          },
                        ),
                  ],
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _detailRow(BuildContext context, String label, String value) {
    final theme = Theme.of(context);
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 96,
            child: Text(
              label,
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
          ),
          Expanded(child: Text(value, style: theme.textTheme.bodyMedium)),
        ],
      ),
    );
  }

  Future<void> _updateStatus(Order order, String status) async {
    try {
      await ref.read(ordersRepositoryProvider).updateStatus(order.id, status);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Order #$order.id → $status')),
        );
      }
      await _load(reset: true);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(errorMessage(e)),
            backgroundColor: Theme.of(context).colorScheme.error,
          ),
        );
      }
    }
  }
}

class _OrderCard extends StatelessWidget {
  const _OrderCard({required this.order, required this.onOpen});

  final Order order;
  final VoidCallback onOpen;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onOpen,
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            children: [
              CircleAvatar(
                backgroundColor: order.isProduct
                    ? const Color(0xFFE3EDFF)
                    : const Color(0xFFEDE7FF),
                foregroundColor: order.isProduct
                    ? const Color(0xFF2B5BC7)
                    : const Color(0xFF6A4BC7),
                child: Icon(
                  order.isProduct
                      ? Icons.shopping_bag_outlined
                      : Icons.home_repair_service_outlined,
                  size: 20,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      order.product,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: theme.textTheme.titleSmall
                          ?.copyWith(fontWeight: FontWeight.w600),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      '${formatDate(order.date)} · ${order.customer}',
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant,
                      ),
                    ),
                    const SizedBox(height: 6),
                    Row(
                      children: [
                        _StatusPill(status: order.status),
                        const Spacer(),
                        Text(
                          formatMoney(order.total),
                          style: theme.textTheme.titleSmall?.copyWith(
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _StatusPill extends StatelessWidget {
  const _StatusPill({required this.status});

  final String status;

  ({Color background, Color foreground}) get _palette => switch (status) {
        'Processing' => (
            background: const Color(0xFFE3EDFF),
            foreground: const Color(0xFF2B5BC7),
          ),
        'Confirmed' => (
            background: const Color(0xFFE3EDFF),
            foreground: const Color(0xFF1F4FA8),
          ),
        'Completed' => (
            background: const Color(0xFFDCF5E4),
            foreground: const Color(0xFF1B7A43),
          ),
        'Reserved' => (
            background: const Color(0xFFEDE7FF),
            foreground: const Color(0xFF6A4BC7),
          ),
        'Cancelled' => (
            background: const Color(0xFFFDE3E3),
            foreground: const Color(0xFFB3261E),
          ),
        'Refunded' => (
            background: const Color(0xFFFFF0D6),
            foreground: const Color(0xFF9A6400),
          ),
        _ => (
            background: const Color(0xFFECECF3),
            foreground: const Color(0xFF5A5A6E),
          ),
      };

  @override
  Widget build(BuildContext context) {
    final palette = _palette;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: palette.background,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        status,
        style: TextStyle(
          fontSize: 11,
          fontWeight: FontWeight.w600,
          color: palette.foreground,
        ),
      ),
    );
  }
}
