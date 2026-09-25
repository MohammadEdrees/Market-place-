/// Data models for the catalogue, parsed from the API's camelCase JSON.
library;

/// One image in a product or service gallery (absolute URL from the API).
class GalleryImage {
  const GalleryImage({required this.id, required this.path, this.sortOrder = 0});

  final int id;
  final String path;
  final int sortOrder;

  factory GalleryImage.fromJson(Map<String, dynamic> json) => GalleryImage(
        id: (json['id'] as num?)?.toInt() ?? 0,
        path: json['path'] as String? ?? '',
        sortOrder: (json['sortOrder'] as num?)?.toInt() ?? 0,
      );
}

List<GalleryImage> parseImages(Object? raw) => raw is List
    ? raw
        .whereType<Map<String, dynamic>>()
        .map(GalleryImage.fromJson)
        .toList()
    : const [];

/// A catalogue product.
class Product {
  const Product({
    required this.id,
    required this.name,
    required this.sku,
    required this.category,
    required this.price,
    required this.stock,
    required this.sold,
    this.createdAt,
    this.sellerId,
    this.images = const [],
  });

  final int id;
  final String name;
  final String sku;
  final String category;
  final double price;
  final int stock;
  final int sold;
  final DateTime? createdAt;
  final int? sellerId;
  final List<GalleryImage> images;

  /// Derived status shown as a badge: `Active`, `Low stock` or `Out of stock`.
  String get status =>
      stock == 0 ? 'Out of stock' : stock <= 15 ? 'Low stock' : 'Active';

  bool get inStock => stock > 0;

  factory Product.fromJson(Map<String, dynamic> json) => Product(
        id: (json['id'] as num?)?.toInt() ?? 0,
        name: json['name'] as String? ?? '',
        sku: json['sku'] as String? ?? '',
        category: json['category'] as String? ?? '',
        price: (json['price'] as num?)?.toDouble() ?? 0,
        stock: (json['stock'] as num?)?.toInt() ?? 0,
        sold: (json['sold'] as num?)?.toInt() ?? 0,
        createdAt: DateTime.tryParse(json['createdAt'] as String? ?? ''),
        sellerId: (json['sellerId'] as num?)?.toInt(),
        images: parseImages(json['images']),
      );
}

/// A service offered by a provider.
class Service {
  const Service({
    required this.id,
    required this.providerId,
    required this.title,
    required this.description,
    required this.category,
    required this.cost,
    required this.contactInfo,
    required this.location,
    this.offers = '',
    this.isActive = true,
    this.createdAt,
    this.images = const [],
  });

  final int id;
  final int providerId;
  final String title;
  final String description;
  final String category;
  final double cost;
  final String contactInfo;
  final String location;
  final String offers;
  final bool isActive;
  final DateTime? createdAt;
  final List<GalleryImage> images;

  factory Service.fromJson(Map<String, dynamic> json) => Service(
        id: (json['id'] as num?)?.toInt() ?? 0,
        providerId: (json['providerId'] as num?)?.toInt() ?? 0,
        title: json['title'] as String? ?? '',
        description: json['description'] as String? ?? '',
        category: json['category'] as String? ?? '',
        cost: (json['cost'] as num?)?.toDouble() ?? 0,
        contactInfo: json['contactInfo'] as String? ?? '',
        location: json['location'] as String? ?? '',
        offers: json['offers'] as String? ?? '',
        isActive: json['isActive'] as bool? ?? true,
        createdAt: DateTime.tryParse(json['createdAt'] as String? ?? ''),
        images: parseImages(json['images']),
      );
}

/// Envelope returned by every paged list endpoint.
class PagedResponse<T> {
  const PagedResponse({
    required this.items,
    required this.total,
    required this.page,
    required this.pageSize,
  });

  final List<T> items;
  final int total;
  final int page;
  final int pageSize;

  bool get hasMore => page * pageSize < total;

  factory PagedResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) fromItem,
  ) {
    final raw = json['items'];
    return PagedResponse<T>(
      items: raw is List
          ? raw.whereType<Map<String, dynamic>>().map(fromItem).toList()
          : const [],
      total: (json['total'] as num?)?.toInt() ?? 0,
      page: (json['page'] as num?)?.toInt() ?? 1,
      pageSize: (json['pageSize'] as num?)?.toInt() ?? 0,
    );
  }
}
