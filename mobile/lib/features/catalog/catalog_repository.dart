import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_client.dart';
import 'models.dart';

/// Catalogue endpoints: products, services, categories and galleries.
final catalogRepositoryProvider = Provider<CatalogRepository>(
  (ref) => CatalogRepository(ref.watch(apiClientProvider)),
);

class CatalogRepository {
  CatalogRepository(this._dio);

  final Dio _dio;

  // ---------------------------------------------------------------- products

  Future<PagedResponse<Product>> getProducts({
    String? search,
    String? category,
    int? sellerId,
    int page = 1,
    int pageSize = 20,
  }) =>
      guardApi(() async {
        final query = <String, dynamic>{'page': page, 'pageSize': pageSize};
        if (search != null && search.trim().isNotEmpty) {
          query['search'] = search.trim();
        }
        if (category != null && category.isNotEmpty) {
          query['category'] = category;
        }
        if (sellerId != null) query['sellerId'] = sellerId;
        final response = await _dio.get<Map<String, dynamic>>(
          '/api/products',
          queryParameters: query,
        );
        return PagedResponse.fromJson(
          response.data ?? const <String, dynamic>{},
          Product.fromJson,
        );
      });

  Future<Product> getProduct(int id) => guardApi(() async {
        final response =
            await _dio.get<Map<String, dynamic>>('/api/products/$id');
        return Product.fromJson(response.data ?? const <String, dynamic>{});
      });

  Future<List<String>> productCategories() => guardApi(() async {
        final response =
            await _dio.get<List<dynamic>>('/api/products/categories');
        return (response.data ?? const []).whereType<String>().toList();
      });

  Future<List<Product>> myProducts() => guardApi(() async {
        final response = await _dio.get<List<dynamic>>('/api/products/mine');
        return (response.data ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(Product.fromJson)
            .toList();
      });

  Future<Product> createProduct({
    required String name,
    required String sku,
    required String category,
    required double price,
    required int stock,
  }) =>
      guardApi(() async {
        final response = await _dio.post<Map<String, dynamic>>(
          '/api/products',
          data: {
            'name': name,
            'sku': sku,
            'category': category,
            'price': price,
            'stock': stock,
          },
        );
        return Product.fromJson(response.data ?? const <String, dynamic>{});
      });

  Future<Product> updateProduct(
    int id, {
    required String name,
    required String sku,
    required String category,
    required double price,
    required int stock,
  }) =>
      guardApi(() async {
        final response = await _dio.put<Map<String, dynamic>>(
          '/api/products/$id',
          data: {
            'name': name,
            'sku': sku,
            'category': category,
            'price': price,
            'stock': stock,
          },
        );
        return Product.fromJson(response.data ?? const <String, dynamic>{});
      });

  Future<void> deleteProduct(int id) =>
      guardApi(() => _dio.delete<void>('/api/products/$id'));

  // ---------------------------------------------------------------- services

  Future<PagedResponse<Service>> getServices({
    String? search,
    String? category,
    int page = 1,
    int pageSize = 20,
  }) =>
      guardApi(() async {
        final query = <String, dynamic>{'page': page, 'pageSize': pageSize};
        if (search != null && search.trim().isNotEmpty) {
          query['search'] = search.trim();
        }
        if (category != null && category.isNotEmpty) {
          query['category'] = category;
        }
        final response = await _dio.get<Map<String, dynamic>>(
          '/api/services',
          queryParameters: query,
        );
        return PagedResponse.fromJson(
          response.data ?? const <String, dynamic>{},
          Service.fromJson,
        );
      });

  Future<Service> getService(int id) => guardApi(() async {
        final response =
            await _dio.get<Map<String, dynamic>>('/api/services/$id');
        return Service.fromJson(response.data ?? const <String, dynamic>{});
      });

  Future<List<String>> serviceCategories() => guardApi(() async {
        final response =
            await _dio.get<List<dynamic>>('/api/services/categories');
        return (response.data ?? const []).whereType<String>().toList();
      });

  Future<List<Service>> myServices() => guardApi(() async {
        final response = await _dio.get<List<dynamic>>('/api/services/mine');
        return (response.data ?? const [])
            .whereType<Map<String, dynamic>>()
            .map(Service.fromJson)
            .toList();
      });

  Future<Service> createService({
    required String title,
    required String description,
    required String category,
    required double cost,
    required String contactInfo,
    required String location,
    String offers = '',
  }) =>
      guardApi(() async {
        final response = await _dio.post<Map<String, dynamic>>(
          '/api/services',
          data: {
            'title': title,
            'description': description,
            'category': category,
            'cost': cost,
            'contactInfo': contactInfo,
            'location': location,
            'offers': offers,
          },
        );
        return Service.fromJson(response.data ?? const <String, dynamic>{});
      });

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
      guardApi(() async {
        final response = await _dio.put<Map<String, dynamic>>(
          '/api/services/$id',
          data: {
            'title': title,
            'description': description,
            'category': category,
            'cost': cost,
            'contactInfo': contactInfo,
            'location': location,
            'offers': offers,
          },
        );
        return Service.fromJson(response.data ?? const <String, dynamic>{});
      });

  Future<void> deleteService(int id) =>
      guardApi(() => _dio.delete<void>('/api/services/$id'));

  // ----------------------------------------------------------------- galleries

  /// Uploads one gallery image (`multipart/form-data`, field `file`).
  /// [kind] is `products` or `services`.
  Future<void> addListingImage({
    required String kind,
    required int id,
    required String filePath,
    required String fileName,
  }) =>
      guardApi(() async {
        final form = FormData.fromMap({
          'file': await MultipartFile.fromFile(filePath, filename: fileName),
        });
        await _dio.post<void>('/api/$kind/$id/images', data: form);
      });

  Future<void> deleteListingImage({
    required String kind,
    required int id,
    required int imageId,
  }) =>
      guardApi(() => _dio.delete<void>('/api/$kind/$id/images/$imageId'));
}
