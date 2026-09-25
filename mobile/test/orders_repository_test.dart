import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:market_workplace/features/orders/models.dart';
import 'package:market_workplace/features/orders/orders_repository.dart';

import 'helpers.dart';

void main() {
  group('OrdersRepository.placeOrder', () {
    test('buys a product with the exact payload', () async {
      late RequestOptions captured;
      final container = containerWith((options, stream) async {
        captured = options;
        return jsonResponse({
          'id': 31,
          'customer': 'Cara Client',
          'product': 'Aurora Wireless Headset',
          'category': 'Audio',
          'total': 59.99,
          'status': 'Processing',
          'date': '2026-09-25T10:00:00.000Z',
          'kind': 'Product',
          'productId': 7,
          'serviceId': null,
          'buyerId': 3,
        });
      });

      final order = await container.read(ordersRepositoryProvider).placeOrder(
            kind: 'Product',
            productId: 7,
          );

      expect(captured.path, '/api/orders');
      expect(captured.data, {
        'kind': 'Product',
        'productId': 7,
        'serviceId': null,
      });
      expect(order.id, 31);
      expect(order.isProduct, isTrue);
      expect(order.total, 59.99);
      expect(order.isPlacedBy(3), isTrue);
      expect(order.isPlacedBy(6), isFalse);
    });

    test('reserves a service with the exact payload', () async {
      late RequestOptions captured;
      final container = containerWith((options, stream) async {
        captured = options;
        return jsonResponse({
          'id': 32,
          'customer': 'Cara Client',
          'product': 'Home Wi-Fi Setup',
          'category': 'Networking',
          'total': 35,
          'status': 'Reserved',
          'date': '2026-09-25T11:00:00.000Z',
          'kind': 'Service',
          'productId': null,
          'serviceId': 4,
          'buyerId': 3,
        });
      });

      final order = await container.read(ordersRepositoryProvider).placeOrder(
            kind: 'Service',
            serviceId: 4,
          );

      expect(captured.data, {
        'kind': 'Service',
        'productId': null,
        'serviceId': 4,
      });
      expect(order.isProduct, isFalse);
      expect(order.status, 'Reserved');
    });
  });

  group('OrdersRepository.getOrders', () {
    test('passes filters and parses rows', () async {
      late RequestOptions captured;
      final container = containerWith((options, stream) async {
        captured = options;
        return jsonResponse({
          'items': [
            {
              'id': 1,
              'customer': 'Sam Buyer',
              'product': 'Nimbus Keyboard',
              'category': 'Accessories',
              'total': 39,
              'status': 'Confirmed',
              'date': '2026-09-20T08:30:00.000Z',
              'kind': 'Product',
              'productId': 2,
              'serviceId': null,
              'buyerId': 12,
            },
          ],
          'total': 1,
          'page': 1,
          'pageSize': 20,
        });
      });

      final page = await container.read(ordersRepositoryProvider).getOrders(
            kind: 'Product',
            status: 'Confirmed',
          );

      expect(captured.path, '/api/orders');
      expect(captured.queryParameters, {
        'kind': 'Product',
        'status': 'Confirmed',
        'page': 1,
        'pageSize': 20,
      });
      expect(page.items.single.customer, 'Sam Buyer');
      expect(page.hasMore, isFalse);
    });
  });

  group('OrdersRepository.updateStatus', () {
    test('PUTs the status transition', () async {
      late RequestOptions captured;
      final container = containerWith((options, stream) async {
        captured = options;
        return jsonResponse({
          'id': 12,
          'customer': 'Sam',
          'product': 'Lamp',
          'category': 'Home',
          'total': 24.5,
          'status': 'Confirmed',
          'date': '2026-09-21T09:00:00.000Z',
          'kind': 'Product',
          'productId': 1,
          'serviceId': null,
          'buyerId': 5,
        });
      });

      final updated =
          await container.read(ordersRepositoryProvider).updateStatus(
                12,
                'Confirmed',
              );

      expect(captured.path, '/api/orders/12/status');
      expect(captured.method, 'PUT');
      expect(captured.data, {'status': 'Confirmed'});
      expect(updated.status, 'Confirmed');
    });
  });

  group('Order model', () {
    test('parses legacy rows without a buyer', () {
      final order = Order.fromJson({
        'id': 9,
        'customer': 'History',
        'product': 'Old thing',
        'category': 'Misc',
        'total': 10,
        'status': 'Completed',
        'date': '2025-01-01T00:00:00.000Z',
        'kind': 'Product',
      });

      expect(order.buyerId, isNull);
      expect(order.isPlacedBy(5), isFalse);
      expect(order.date.year, 2025);
    });

    test('exposes the accepted status list', () {
      expect(kOrderStatuses, contains('Processing'));
      expect(kOrderStatuses, contains('Reserved'));
      expect(kOrderStatuses, contains('Refunded'));
      expect(kOrderStatuses, hasLength(6));
    });
  });
}
