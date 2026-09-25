/// Connection settings for the Market Workplace API.
class ApiConfig {
  ApiConfig._();

  /// Base URL of the API. Override at build/run time:
  ///
  /// ```
  /// flutter run --dart-define=API_BASE=http://192.168.1.50:5240
  /// ```
  ///
  /// A physical device must use the host machine's LAN IP; the Android
  /// emulator reaches it via `http://10.0.2.2:5240`.
  static const String baseUrl = String.fromEnvironment(
    'API_BASE',
    defaultValue: 'http://localhost:5240',
  );
}
