import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_client.dart';
import '../catalog/models.dart';
import 'models.dart';

/// Order endpoints: browsing, placing buy/reserve orders and status changes.
final ordersRepositoryProvider = Provider<OrdersRepository>(
  (ref) => OrdersRepository(ref.watch(apiClientProvider)),
);

class OrdersRepository {
  OrdersRepository(this._dio);

  final Dio _dio;

  /// Orders visible to the caller: their own purchases plus orders on their
  /// own listings (the API applies that visibility server-side).
  Future<PagedResponse<Order>> getOrders({
    String? search,
    String? kind,
    String? status,
    int page = 1,
    int pageSize = 20,
  }) =>
      guardApi(() async {
        final response = await _dio.get<Map<String, dynamic>>(
          '/api/orders',
          queryParameters: {
            if (search != null && search.trim().isNotEmpty)
              'search': search.trim(),
            if (kind != null && kind.isNotEmpty) 'kind': kind,
            if (status != null && status.isNotEmpty) 'status': status,
            'page': page,
            'pageSize': pageSize,
          },
        );
        return PagedResponse.fromJson(
          response.data ?? const <String, dynamic>{},
          Order.fromJson,
        );
      });

  /// Places an order: buy a product or reserve a service.
  Future<Order> placeOrder({
    required String kind,
    int? productId,
    int? serviceId,
  }) =>
      guardApi(() async {
        final response = await _dio.post<Map<String, dynamic>>(
          '/api/orders',
          data: {
            'kind': kind,
            'productId': productId,
            'serviceId': serviceId,
          },
        );
        return Order.fromJson(response.data ?? const <String, dynamic>{});
      });

  /// Status transition — allowed for admins and the listing's owner only.
  Future<Order> updateStatus(int id, String status) => guardApi(() async {
        final response = await _dio.put<Map<String, dynamic>>(
          '/api/orders/$id/status',
          data: {'status': status},
        );
        return Order.fromJson(response.data ?? const <String, dynamic>{});
      });
}
