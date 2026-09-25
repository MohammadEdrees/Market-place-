import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_client.dart';
import '../../core/auth/session.dart';

/// Authentication endpoints: sign-in, registration and the current profile.
final authRepositoryProvider = Provider<AuthRepository>(
  (ref) => AuthRepository(ref.watch(apiClientProvider)),
);

class AuthRepository {
  AuthRepository(this._dio);

  final Dio _dio;

  Future<Session> login({
    required String email,
    required String password,
  }) =>
      guardApi(() async {
        final response = await _dio.post<Map<String, dynamic>>(
          '/api/auth/login',
          data: {'email': email, 'password': password},
        );
        return _toSession(response.data ?? const <String, dynamic>{});
      });

  Future<Session> register({
    required String name,
    required String email,
    required String password,
    String? role,
    String? phone,
    String? location,
  }) =>
      guardApi(() async {
        final data = <String, dynamic>{
          'name': name,
          'email': email,
          'password': password,
        };
        if (role != null) data['role'] = role;
        if (phone != null && phone.isNotEmpty) data['phone'] = phone;
        if (location != null && location.isNotEmpty) data['location'] = location;
        final response = await _dio.post<Map<String, dynamic>>(
          '/api/auth/register',
          data: data,
        );
        return _toSession(response.data ?? const <String, dynamic>{});
      });

  Future<UserProfile> me() => guardApi(() async {
        final response = await _dio.get<Map<String, dynamic>>('/api/auth/me');
        return UserProfile.fromJson(response.data ?? const <String, dynamic>{});
      });

  Session _toSession(Map<String, dynamic> json) {
    final expiresAt = DateTime.tryParse(json['expiresAt'] as String? ?? '') ??
        DateTime.now().toUtc().add(const Duration(hours: 12));
    return Session(
      token: json['token'] as String? ?? '',
      expiresAt: expiresAt,
      user: UserProfile.fromJson(
        (json['user'] as Map<String, dynamic>?) ?? const <String, dynamic>{},
      ),
    );
  }
}
