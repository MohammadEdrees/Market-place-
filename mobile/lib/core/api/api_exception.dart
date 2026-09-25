/// A failed API call, carrying the RFC 7807 (problem details) fields the
/// backend returns: `title`, `detail` and optional per-field `errors`.
class ApiException implements Exception {
  const ApiException({
    required this.statusCode,
    this.title = 'Request failed',
    this.detail,
    this.errors = const {},
  });

  /// HTTP status code; `0` when the server could not be reached at all.
  final int statusCode;

  /// Short problem title, e.g. `Role in use`.
  final String title;

  /// Human-readable explanation shown to the user when present.
  final String? detail;

  /// Field-level validation errors keyed by property name.
  final Map<String, List<String>> errors;

  bool get isUnauthorized => statusCode == 401;
  bool get isForbidden => statusCode == 403;
  bool get isNotFound => statusCode == 404;
  bool get isConflict => statusCode == 409;
  bool get isOffline => statusCode == 0;
  bool get isValidation => statusCode == 400 && errors.isNotEmpty;

  /// Best message for direct display in a SnackBar or inline error.
  String get message {
    if (detail != null && detail!.trim().isNotEmpty) return detail!.trim();
    if (errors.isNotEmpty) return errors.values.expand((v) => v).join('\n');
    return title;
  }

  /// First validation error for [field], if any.
  String? fieldError(String field) {
    final list = errors[field] ?? errors[field.toLowerCase()];
    if (list == null || list.isEmpty) return null;
    return list.first;
  }

  /// Builds an exception from an error response body.
  ///
  /// The backend returns `application/problem+json` for errors; anything that
  /// is not a problem-details object falls back to a status-based title.
  factory ApiException.fromResponse(int statusCode, Object? body) {
    if (body is Map<String, dynamic>) {
      final rawTitle = body['title'] as String?;
      final detail = body['detail'] as String?;
      final fieldErrors = <String, List<String>>{};
      final rawErrors = body['errors'];
      if (rawErrors is Map<String, dynamic>) {
        rawErrors.forEach((key, value) {
          if (value is List) {
            fieldErrors[key] = value.map((e) => e.toString()).toList();
          }
        });
      }
      final resolvedTitle = rawTitle ?? '';
      return ApiException(
        statusCode: statusCode,
        title: resolvedTitle.isEmpty ? _defaultTitle(statusCode) : resolvedTitle,
        detail: detail,
        errors: fieldErrors,
      );
    }
    return ApiException(
      statusCode: statusCode,
      title: _defaultTitle(statusCode),
      detail: body?.toString(),
    );
  }

  static String _defaultTitle(int status) => switch (status) {
        0 => 'Connection failed',
        400 => 'Invalid request',
        401 => 'Sign-in required',
        403 => 'Not allowed',
        404 => 'Not found',
        409 => 'Conflict',
        413 => 'File too large',
        >= 500 => 'Server error',
        _ => 'Request failed ($status)',
      };

  @override
  String toString() => 'ApiException($statusCode, $title)';
}
