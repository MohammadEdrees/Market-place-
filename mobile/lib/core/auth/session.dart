import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Profile of the signed-in user (`UserProfileDto` from the API).
class UserProfile {
  const UserProfile({
    required this.id,
    required this.email,
    required this.name,
    required this.role,
    this.type = 'Mobile',
    this.phone,
    this.location,
    this.bio,
    this.imagePath,
  });

  final int id;
  final String email;
  final String name;
  final String role;
  final String type;
  final String? phone;
  final String? location;
  final String? bio;
  final String? imagePath;

  /// Providers manage listings and order statuses; everyone else browses and buys.
  bool get isProvider => role == 'Provider';

  /// The mobile app only accepts the two mobile account types.
  bool get canUseMobileApp => role == 'Client' || role == 'Provider';

  factory UserProfile.fromJson(Map<String, dynamic> json) => UserProfile(
        id: (json['id'] as num?)?.toInt() ?? 0,
        email: json['email'] as String? ?? '',
        name: json['name'] as String? ?? '',
        role: json['role'] as String? ?? '',
        type: json['type'] as String? ?? 'Dashboard',
        phone: json['phone'] as String?,
        location: json['location'] as String?,
        bio: json['bio'] as String?,
        imagePath: json['imagePath'] as String?,
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'email': email,
        'name': name,
        'role': role,
        'type': type,
        'phone': phone,
        'location': location,
        'bio': bio,
        'imagePath': imagePath,
      };

  UserProfile copyWith({
    String? name,
    String? phone,
    String? location,
    String? bio,
    String? imagePath,
  }) =>
      UserProfile(
        id: id,
        email: email,
        name: name ?? this.name,
        role: role,
        type: type,
        phone: phone ?? this.phone,
        location: location ?? this.location,
        bio: bio ?? this.bio,
        imagePath: imagePath ?? this.imagePath,
      );
}

/// The authenticated session: bearer token plus the profile it belongs to.
class Session {
  const Session({
    required this.token,
    required this.expiresAt,
    required this.user,
  });

  final String token;
  final DateTime expiresAt;
  final UserProfile user;

  bool get isExpired => DateTime.now().toUtc().isAfter(expiresAt);
}

/// Persists the session in [SharedPreferences] and keeps an in-memory mirror
/// of the token for the HTTP auth interceptor.
///
/// Listens for API 401s through [expire] so the auth controller can drop the
/// session and bounce the user to the sign-in screen.
final sessionStoreProvider = Provider<SessionStore>((ref) => SessionStore());

class SessionStore {
  static const _kToken = 'session.token';
  static const _kExpiresAt = 'session.expiresAt';
  static const _kUser = 'session.user';

  /// Current bearer token, or `null` when signed out.
  String? token;

  /// The persisted session, when signed in.
  Session? get current => _session;

  /// Invoked after the API rejects the token (HTTP 401).
  void Function()? onUnauthorized;

  Session? _session;

  /// Restores a persisted, unexpired session — or clears stale data.
  Future<Session?> restore() async {
    final prefs = await SharedPreferences.getInstance();
    final storedToken = prefs.getString(_kToken);
    final storedExpiresAt = prefs.getString(_kExpiresAt);
    final storedUser = prefs.getString(_kUser);
    if (storedToken == null || storedExpiresAt == null || storedUser == null) {
      return null;
    }
    final expiresAt = DateTime.tryParse(storedExpiresAt);
    final userJson = jsonDecode(storedUser);
    if (expiresAt == null || userJson is! Map<String, dynamic>) {
      await clear();
      return null;
    }
    final session = Session(
      token: storedToken,
      expiresAt: expiresAt,
      user: UserProfile.fromJson(userJson),
    );
    if (session.isExpired) {
      await clear();
      return null;
    }
    _session = session;
    token = storedToken;
    return session;
  }

  Future<void> save(Session session) async {
    _session = session;
    token = session.token;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_kToken, session.token);
    await prefs.setString(_kExpiresAt, session.expiresAt.toIso8601String());
    await prefs.setString(_kUser, jsonEncode(session.user.toJson()));
  }

  Future<void> clear() async {
    _session = null;
    token = null;
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_kToken);
    await prefs.remove(_kExpiresAt);
    await prefs.remove(_kUser);
  }

  /// Called on API 401: drops the persisted session and notifies the app.
  Future<void> expire() async {
    final wasSignedIn = _session != null || token != null;
    await clear();
    if (wasSignedIn) onUnauthorized?.call();
  }
}
