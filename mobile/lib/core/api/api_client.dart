import 'dart:async';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../auth/session.dart';
import '../config/api_config.dart';
import 'api_exception.dart';

/// Shared Dio instance: base URL, bearer token and error mapping.
final apiClientProvider = Provider<Dio>((ref) {
  final dio = Dio(
    BaseOptions(
      baseUrl: ApiConfig.baseUrl,
      connectTimeout: const Duration(seconds: 15),
      receiveTimeout: const Duration(seconds: 30),
      headers: const {'Accept': 'application/json'},
    ),
  );

  dio.interceptors.add(
    InterceptorsWrapper(
      onRequest: (options, handler) {
        final token = ref.read(sessionStoreProvider).token;
        if (token != null && token.isNotEmpty) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        handler.next(options);
      },
      onError: (err, handler) {
        final status = err.response?.statusCode;
        final api = status != null
            ? ApiException.fromResponse(status, err.response?.data)
            : ApiException(
                statusCode: 0,
                title: 'Connection failed',
                detail: _networkDetail(err),
              );
        if (status == 401) {
          unawaited(ref.read(sessionStoreProvider).expire());
        }
        handler.next(err.copyWith(error: api));
      },
    ),
  );

  return dio;
});

/// Runs [call] and rethrows any [DioException] as an [ApiException].
///
/// Every repository method goes through this so the UI only ever catches
/// [ApiException].
Future<T> guardApi<T>(Future<T> Function() call) async {
  try {
    return await call();
  } on DioException catch (e) {
    final error = e.error;
    if (error is ApiException) throw error;
    throw ApiException(
      statusCode: e.response?.statusCode ?? 0,
      title: 'Request failed',
      detail: e.message,
    );
  }
}

String _networkDetail(DioException err) => switch (err.type) {
      DioExceptionType.connectionTimeout ||
      DioExceptionType.sendTimeout ||
      DioExceptionType.receiveTimeout =>
        'The server took too long to respond.',
      DioExceptionType.connectionError =>
        'Could not reach the server. Check your connection.',
      DioExceptionType.cancel => 'Request cancelled.',
      _ => 'Unexpected network error.',
    };
