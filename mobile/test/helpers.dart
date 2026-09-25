import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:market_workplace/core/api/api_client.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Handler that answers a mocked HTTP call.
typedef MockHandler = Future<ResponseBody> Function(
  RequestOptions options,
  Stream<Uint8List>? requestStream,
);

/// A [HttpClientAdapter] that routes every call to [handler].
class MockAdapter implements HttpClientAdapter {
  MockAdapter(this.handler);

  final MockHandler handler;

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) =>
      handler(options, requestStream);

  @override
  void close({bool force = false}) {}
}

/// JSON response body with the API's content type.
ResponseBody jsonResponse(Object? body, {int status = 200}) =>
    ResponseBody.fromString(
      jsonEncode(body),
      status,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );

/// A provider container whose Dio instance answers with [handler].
///
/// SharedPreferences is mocked so the session store works headless.
ProviderContainer containerWith(MockHandler handler) {
  SharedPreferences.setMockInitialValues({});
  final container = ProviderContainer();
  container.read(apiClientProvider).httpClientAdapter = MockAdapter(handler);
  addTearDown(container.dispose);
  return container;
}

/// Reads a whole request stream (used for multipart bodies).
Future<List<int>> collectStream(Stream<Uint8List>? stream) async {
  if (stream == null) return const [];
  final builder = BytesBuilder();
  await for (final chunk in stream) {
    builder.add(chunk);
  }
  return builder.takeBytes();
}
