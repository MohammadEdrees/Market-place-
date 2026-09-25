import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:market_workplace/core/api/api_exception.dart';
import 'package:market_workplace/features/catalog/catalog_repository.dart';
import 'package:market_workplace/features/catalog/models.dart';
import 'package:market_workplace/features/catalog/products_page.dart';

class FakeCatalogRepository implements CatalogRepository {
  FakeCatalogRepository({
    this.items,
    this.error,
    this.categories = const [],
    this.advertisements = const [],
  });

  /// Products to return; when null the call fails with [error].
  final List<Product>? items;
  final Object? error;
  final List<String> categories;

  /// Advertisement slides to return. Empty by default: the slider stays
  /// hidden and no `Timer.periodic` can hang `pumpAndSettle`.
  final List<Advertisement> advertisements;

  int productsCalls = 0;
  Map<String, Object?> lastQuery = const {};

  @override
  Future<PagedResponse<Product>> getProducts({
    String? search,
    String? category,
    int? sellerId,
    int page = 1,
    int pageSize = 20,
  }) async {
    productsCalls++;
    lastQuery = {
      'search': search,
      'category': category,
      'page': page,
    };
    final failure = error;
    if (failure != null) throw failure;
    final rows = items ?? const <Product>[];
    return PagedResponse<Product>(
      items: rows,
      total: rows.length,
      page: page,
      pageSize: pageSize,
    );
  }

  @override
  Future<List<String>> productCategories() async => categories;

  @override
  Future<List<Advertisement>> activeAdvertisements() async => advertisements;

  @override
  Future<Product> getProduct(int id) => throw UnimplementedError();

  @override
  Future<List<Product>> myProducts() => throw UnimplementedError();

  @override
  Future<Product> createProduct({
    required String name,
    required String sku,
    required String category,
    required double price,
    required int stock,
  }) =>
      throw UnimplementedError();

  @override
  Future<Product> updateProduct(
    int id, {
    required String name,
    required String sku,
    required String category,
    required double price,
    required int stock,
  }) =>
      throw UnimplementedError();

  @override
  Future<void> deleteProduct(int id) => throw UnimplementedError();

  @override
  Future<PagedResponse<Service>> getServices({
    String? search,
    String? category,
    int page = 1,
    int pageSize = 20,
  }) =>
      throw UnimplementedError();

  @override
  Future<Service> getService(int id) => throw UnimplementedError();

  @override
  Future<List<String>> serviceCategories() => throw UnimplementedError();

  @override
  Future<List<Service>> myServices() => throw UnimplementedError();

  @override
  Future<Service> createService({
    required String title,
    required String description,
    required String category,
    required double cost,
    required String contactInfo,
    required String location,
    String offers = '',
  }) =>
      throw UnimplementedError();

  @override
  Future<Service> updateService(
    int id, {
    required String title,
    required String description,
    required String category,
    required double cost,
    required String contactInfo,
    required String location,
    String offers = '',
  }) =>
      throw UnimplementedError();

  @override
  Future<void> deleteService(int id) => throw UnimplementedError();

  @override
  Future<void> addListingImage({
    required String kind,
    required int id,
    required String filePath,
    required String fileName,
  }) =>
      throw UnimplementedError();

  @override
  Future<void> deleteListingImage({
    required String kind,
    required int id,
    required int imageId,
  }) =>
      throw UnimplementedError();
}

void main() {
  Future<void> pumpProducts(
    WidgetTester tester,
    FakeCatalogRepository repo,
  ) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [catalogRepositoryProvider.overrideWithValue(repo)],
        child: const MaterialApp(home: ProductsPage()),
      ),
    );
    await tester.pumpAndSettle();
  }

  testWidgets('renders cards with en-US prices, status and category chips',
      (tester) async {
    final repo = FakeCatalogRepository(
      categories: const ['Audio', 'Gadgets'],
      items: const [
        Product(
          id: 1,
          name: 'Aurora Wireless Headset',
          sku: 'AH-100',
          category: 'Audio',
          price: 59.99,
          stock: 40,
          sold: 3,
        ),
        Product(
          id: 2,
          name: 'Nimbus Keyboard',
          sku: 'NK-9',
          category: 'Gadgets',
          price: 1234.5,
          stock: 10,
          sold: 12,
        ),
      ],
    );

    await pumpProducts(tester, repo);

    expect(find.text('Aurora Wireless Headset'), findsOneWidget);
    expect(find.text(r'$59.99'), findsOneWidget);
    expect(find.text(r'$1,234.50'), findsOneWidget);
    expect(find.text('Active'), findsOneWidget);
    expect(find.text('Low stock'), findsOneWidget);
    expect(find.widgetWithText(ChoiceChip, 'Audio'), findsWidgets);
    expect(find.widgetWithText(ChoiceChip, 'Gadgets'), findsOneWidget);
    expect(find.widgetWithText(ChoiceChip, 'All'), findsOneWidget);
    expect(repo.productsCalls, 1);
  });

  testWidgets('category chip refilters through the API', (tester) async {
    final repo = FakeCatalogRepository(
      categories: const ['Audio'],
      items: const [
        Product(
          id: 1,
          name: 'Aurora Wireless Headset',
          sku: 'AH-100',
          category: 'Audio',
          price: 59.99,
          stock: 40,
          sold: 3,
        ),
      ],
    );

    await pumpProducts(tester, repo);
    await tester.tap(find.widgetWithText(ChoiceChip, 'Audio'));
    await tester.pumpAndSettle();

    expect(repo.productsCalls, 2);
    expect(repo.lastQuery['category'], 'Audio');
    expect(repo.lastQuery['page'], 1);
  });

  testWidgets('shows an empty state when nothing matches', (tester) async {
    final repo = FakeCatalogRepository(items: const []);

    await pumpProducts(tester, repo);

    expect(find.text('No products match your search.'), findsOneWidget);
  });

  testWidgets('surfaces API errors with a retry action', (tester) async {
    final repo = FakeCatalogRepository(
      error: const ApiException(
        statusCode: 503,
        detail: 'Catalogue is down.',
      ),
    );

    await pumpProducts(tester, repo);

    expect(find.text('Catalogue is down.'), findsOneWidget);
    expect(find.widgetWithText(OutlinedButton, 'Try again'), findsOneWidget);
  });
}
