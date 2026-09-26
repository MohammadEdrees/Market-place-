import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_client.dart';
import '../auth/auth_controller.dart';

/// A plan bound to an account, as returned by `GET /api/subscriptions/user/{id}`.
///
/// The mobile app only ever reads its own rows: the dashboard's
/// `/subscriptions` page is where plans are created, edited and deleted.
class UserSubscription {
  const UserSubscription({
    required this.id,
    required this.userId,
    required this.plan,
    required this.price,
    required this.billingCycle,
    required this.status,
    required this.startsAt,
    required this.endsAt,
    required this.autoRenew,
  });

  final int id;
  final int userId;

  /// `Basic`, `Premium` or `Enterprise`.
  final String plan;

  /// Price in USD for one billing cycle.
  final double price;

  /// `Monthly` or `Yearly`.
  final String billingCycle;

  /// `Active`, `Inactive` or `Expired`.
  final String status;

  final DateTime startsAt;

  /// `null` for an open-ended plan.
  final DateTime? endsAt;

  final bool autoRenew;

  factory UserSubscription.fromJson(Map<String, dynamic> json) =>
      UserSubscription(
        id: (json['id'] as num?)?.toInt() ?? 0,
        userId: (json['userId'] as num?)?.toInt() ?? 0,
        plan: json['plan'] as String? ?? '',
        price: (json['price'] as num?)?.toDouble() ?? 0,
        billingCycle: json['billingCycle'] as String? ?? '',
        status: json['status'] as String? ?? '',
        startsAt: DateTime.tryParse(json['startsAt'] as String? ?? '') ??
            DateTime.now(),
        endsAt: DateTime.tryParse(json['endsAt'] as String? ?? ''),
        autoRenew: json['autoRenew'] as bool? ?? false,
      );
}

/// The signed-in account's plans — an empty list when it has none.
///
/// Re-fetches whenever the session changes so a different user never sees the
/// previous account's plan.
final mySubscriptionsProvider =
    FutureProvider.autoDispose<List<UserSubscription>>((ref) async {
  final session = ref.watch(authControllerProvider).value;
  if (session == null) return const <UserSubscription>[];
  final dio = ref.watch(apiClientProvider);
  final response = await guardApi(
    () => dio.get<List<dynamic>>('/api/subscriptions/user/${session.user.id}'),
  );
  return [
    for (final row in response.data ?? const <dynamic>[])
      UserSubscription.fromJson(row as Map<String, dynamic>),
  ];
});
