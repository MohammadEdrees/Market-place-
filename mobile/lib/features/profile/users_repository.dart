import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_client.dart';
import '../../core/auth/session.dart';

/// Self-service profile endpoints (`/api/users/me` and the avatar routes).
final usersRepositoryProvider = Provider<UsersRepository>(
  (ref) => UsersRepository(ref.watch(apiClientProvider)),
);

class UsersRepository {
  UsersRepository(this._dio);

  final Dio _dio;

  /// Updates the signed-in user's contact/profile fields.
  ///
  /// Backend semantics: `null` keeps a field, an empty string clears it.
  Future<UserProfile> updateMe({
    required String name,
    String? phone,
    String? location,
    String? bio,
  }) =>
      guardApi(() async {
        final response = await _dio.put<Map<String, dynamic>>(
          '/api/users/me',
          data: {
            'name': name,
            'phone': phone,
            'location': location,
            'bio': bio,
          },
        );
        return UserProfile.fromJson(response.data ?? const <String, dynamic>{});
      });

  /// Uploads a new avatar (`multipart/form-data`, field `file`).
  Future<void> setAvatar({
    required int userId,
    required String filePath,
    required String fileName,
  }) =>
      guardApi(() async {
        final form = FormData.fromMap({
          'file': await MultipartFile.fromFile(filePath, filename: fileName),
        });
        await _dio.post<void>('/api/users/$userId/image', data: form);
      });

  Future<void> removeAvatar(int userId) =>
      guardApi(() => _dio.delete<void>('/api/users/$userId/image'));
}
