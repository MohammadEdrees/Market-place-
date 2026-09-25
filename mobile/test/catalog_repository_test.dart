import 'dart:convert';
import 'dart:io';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:market_workplace/features/catalog/catalog_repository.dart';

import 'helpers.dart';

void main() {
  group('CatalogRepository.getProducts', () {
    test('passes filters as query parameters and parses the page', () async {
      late RequestOptions captured;
      final container = containerWith((options, stream) async {
        captured = options;
        return jsonResponse({
          'items': [
            {
              'id': 1,
              'name': 'Aurora Wireless Headset',
              'sku': 'AH-100',
              'category': 'Audio',
              'price': 59.99,
              'stock': 40,
              'sold': 3,
              'createdAt': '2026-09-01T00:00:00.000Z',
              'sellerId': null,
              'images': [
                {
                  'id': 9,
                  'path': 'http://localhost:5240/images/products/a.png',
                  'sortOrder': 0,
                },
              ],
            },
            {
              'id': 2,
              'name': 'Nimbus Keyboard',
              'sku': 'NK-9',
              'category': 'Accessories',
              'price': 39,
              'stock': 8,
              'sold': 12,
              'createdAt': '2026-09-02T00:00:00.000Z',
              'sellerId': 6,
              'images': <Map<String, dynamic>>[],
            },
          ],
          'total': 45,
          'page': 2,
          'pageSize': 20,
        });
      });

      final page = await container.read(catalogRepositoryProvider).getProducts(
            search: '  headset  ',
            category: 'Audio',
            page: 2,
          );

      expect(captured.path, '/api/products');
      expect(captured.queryParameters, {
        'search': 'headset',
        'category': 'Audio',
        'page': 2,
        'pageSize': 20,
      });
      expect(page.items, hasLength(2));
      expect(page.page, 2);
      expect(page.total, 45);
      expect(page.hasMore, isTrue);

      final first = page.items.first;
      expect(first.price, 59.99);
      expect(first.stock, 40);
      expect(first.status, 'Active');
      expect(first.sellerId, isNull);
      expect(first.images.single.path,
          'http://localhost:5240/images/products/a.png');
      expect(page.items[1].status, 'Low stock');
    });

    test('reports hasMore=false on the final page', () async {
      final container = containerWith(
        (options, stream) async => jsonResponse({
          'items': <Map<String, dynamic>>[],
          'total': 20,
          'page': 1,
          'pageSize': 20,
        }),
      );

      final page = await container.read(catalogRepositoryProvider).getProducts();
      expect(page.hasMore, isFalse);
    });
  });

  group('CatalogRepository mutations', () {
    test('createProduct posts the exact payload', () async {
      late RequestOptions captured;
      final container = containerWith((options, stream) async {
        captured = options;
        return jsonResponse({
          'id': 21,
          'name': 'Pixel Lamp',
          'sku': 'PL-1',
          'category': 'Home',
          'price': 24.5,
          'stock': 5,
          'sold': 0,
          'images': <Map<String, dynamic>>[],
        });
      });

      final created =
          await container.read(catalogRepositoryProvider).createProduct(
                name: 'Pixel Lamp',
                sku: 'PL-1',
                category: 'Home',
                price: 24.5,
                stock: 5,
              );

      expect(captured.path, '/api/products');
      expect(captured.data, {
        'name': 'Pixel Lamp',
        'sku': 'PL-1',
        'category': 'Home',
        'price': 24.5,
        'stock': 5,
      });
      expect(created.id, 21);
    });

    test('createService posts the exact payload', () async {
      late RequestOptions captured;
      final container = containerWith((options, stream) async {
        captured = options;
        return jsonResponse({
          'id': 4,
          'providerId': 6,
          'title': 'Home Wi-Fi Setup',
          'description': 'Full setup',
          'category': 'Networking',
          'cost': 35,
          'contactInfo': '555-0100',
          'location': 'Amman',
          'offers': '10% off',
          'isActive': true,
          'images': <Map<String, dynamic>>[],
        });
      });

      final created =
          await container.read(catalogRepositoryProvider).createService(
                title: 'Home Wi-Fi Setup',
                description: 'Full setup',
                category: 'Networking',
                cost: 35,
                contactInfo: '555-0100',
                location: 'Amman',
                offers: '10% off',
              );

      expect(captured.path, '/api/services');
      expect(captured.data, {
        'title': 'Home Wi-Fi Setup',
        'description': 'Full setup',
        'category': 'Networking',
        'cost': 35,
        'contactInfo': '555-0100',
        'location': 'Amman',
        'offers': '10% off',
      });
      expect(created.id, 4);
      expect(created.isActive, isTrue);
    });

    test('deleteProduct hits the right route', () async {
      late RequestOptions captured;
      final container = containerWith((options, stream) async {
        captured = options;
        return jsonResponse(null);
      });

      await container.read(catalogRepositoryProvider).deleteProduct(15);
      expect(captured.path, '/api/products/15');
      expect(captured.method, 'DELETE');
    });
  });

  group('CatalogRepository galleries', () {
    test('addListingImage uploads multipart to /api/products/{id}/images',
        () async {
      late RequestOptions captured;
      late Stream<Uint8List>? sentStream;
      final container = containerWith((options, stream) async {
        captured = options;
        sentStream = stream;
        return jsonResponse(null);
      });

      final tempDir = Directory.systemTemp.createTempSync('mw_image_test');
      final file = File('${tempDir.path}/photo.png')
        ..writeAsBytesSync(const [137, 80, 78, 71]);

      try {
        await container.read(catalogRepositoryProvider).addListingImage(
              kind: 'products',
              id: 7,
              filePath: file.path,
              fileName: 'photo.png',
            );
      } finally {
        // Windows may still hold the uploaded file briefly — temp gets
        // cleaned by the OS either way.
        try {
          tempDir.deleteSync(recursive: true);
        } catch (_) {}
      }

      expect(captured.path, '/api/products/7/images');
      expect(captured.method, 'POST');
      final contentType =
          captured.headers[Headers.contentTypeHeader]?.toString() ?? '';
      expect(contentType, contains('multipart/form-data'));

      // The multipart body carries the file field when a stream was sent.
      final bytes = await collectStream(sentStream);
      if (bytes.isNotEmpty) {
        // allowMalformed: the body embeds raw PNG bytes after the headers.
        final body = utf8.decode(bytes, allowMalformed: true);
        expect(body, contains('name="file"'));
        expect(body, contains('photo.png'));
      } else {
        expect(captured.data, isA<FormData>());
      }
    });

    test('deleteListingImage hits services image route', () async {
      late RequestOptions captured;
      final container = containerWith((options, stream) async {
        captured = options;
        return jsonResponse(null);
      });

      await container.read(catalogRepositoryProvider).deleteListingImage(
            kind: 'services',
            id: 3,
            imageId: 11,
          );
      expect(captured.path, '/api/services/3/images/11');
      expect(captured.method, 'DELETE');
    });
  });
}
