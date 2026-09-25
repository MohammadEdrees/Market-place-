/// Order models parsed from the API's camelCase JSON.
library;

/// Statuses accepted by `PUT /api/orders/{id}/status`.
const List<String> kOrderStatuses = [
  'Processing',
  'Confirmed',
  'Completed',
  'Cancelled',
  'Reserved',
  'Refunded',
];

/// An order: a product purchase or a service reservation.
class Order {
  const Order({
    required this.id,
    required this.customer,
    required this.product,
    required this.category,
    required this.total,
    required this.status,
    required this.date,
    this.kind = 'Product',
    this.productId,
    this.serviceId,
    this.buyerId,
  });

  final int id;
  final String customer;

  /// Denormalised listing name.
  final String product;
  final String category;
  final double total;
  final String status;
  final DateTime date;

  /// `Product` (buy) or `Service` (reserve).
  final String kind;
  final int? productId;
  final int? serviceId;

  /// The buyer; `null` for legacy/demo history rows.
  final int? buyerId;

  bool get isProduct => kind == 'Product';

  /// True when the order was placed by [userId] (rather than being an
  /// incoming order on their listing).
  bool isPlacedBy(int? userId) =>
      buyerId != null && userId != null && buyerId == userId;

  factory Order.fromJson(Map<String, dynamic> json) => Order(
        id: (json['id'] as num?)?.toInt() ?? 0,
        customer: json['customer'] as String? ?? '',
        product: json['product'] as String? ?? '',
        category: json['category'] as String? ?? '',
        total: (json['total'] as num?)?.toDouble() ?? 0,
        status: json['status'] as String? ?? '',
        date: DateTime.tryParse(json['date'] as String? ?? '') ??
            DateTime.now(),
        kind: json['kind'] as String? ?? 'Product',
        productId: (json['productId'] as num?)?.toInt(),
        serviceId: (json['serviceId'] as num?)?.toInt(),
        buyerId: (json['buyerId'] as num?)?.toInt(),
      );
}
