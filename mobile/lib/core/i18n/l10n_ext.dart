import 'package:flutter/widgets.dart';

import '../../l10n/generated/app_localizations.dart';

export '../../l10n/generated/app_localizations.dart';

/// The single access style for localized strings: `context.l10n.loginSignIn`.
extension L10nX on BuildContext {
  AppLocalizations get l10n => AppLocalizations.of(this)!;

  /// Translated label for a known status, role or account-type value.
  ///
  /// Server values that are not in the list (a status added later, for
  /// example) fall back to the raw string so nothing ever renders empty.
  String statusLabel(String raw) => switch (raw) {
        'Active' => l10n.statusActive,
        'Low stock' => l10n.statusLowStock,
        'Out of stock' => l10n.statusOutOfStock,
        'Inactive' => l10n.statusInactive,
        'Expired' => l10n.statusExpired,
        'Processing' => l10n.statusProcessing,
        'Confirmed' => l10n.statusConfirmed,
        'Completed' => l10n.statusCompleted,
        'Cancelled' => l10n.statusCancelled,
        'Reserved' => l10n.statusReserved,
        'Refunded' => l10n.statusRefunded,
        'Client' => l10n.statusClient,
        'Provider' => l10n.statusProvider,
        'Mobile' => l10n.statusMobile,
        'Dashboard' => l10n.statusDashboard,
        _ => raw,
      };

  /// Renders an API error message for display.
  ///
  /// The [apiMessageMap] allowlist holds exactly the 10 English problem-detail
  /// messages the backend can return on the auth screens; those are shown in
  /// the current language. Unknown messages pass through unchanged.
  String localizeApiMessage(String message) {
    final trimmed = message.trim();
    final resolve = apiMessageMap[trimmed];
    if (resolve != null) return resolve(l10n);
    // Client-generated messages (never produced by the API itself).
    final client = clientMessageMap[trimmed];
    if (client != null) return client(l10n);
    // `Request failed (422)` is the status-based fallback with an HTTP code.
    if (trimmed.startsWith('Request failed (') && trimmed.endsWith(')')) {
      return l10n.networkRequestFailed;
    }
    return message;
  }
}

/// English strings the client generates itself — Dio/network fallbacks and the
/// two generic catch-alls — mapped to their translations. Like the API
/// allowlist, anything not keyed here is displayed exactly as produced.
final Map<String, String Function(AppLocalizations l10n)> clientMessageMap = {
  'Something went wrong. Please try again.':
      (l10n) => l10n.commonGenericError,
  'Something went wrong.': (l10n) => l10n.commonSomethingWentWrong,
  'The server took too long to respond.': (l10n) => l10n.networkTimeout,
  'Could not reach the server. Check your connection.':
      (l10n) => l10n.networkUnreachable,
  'Request cancelled.': (l10n) => l10n.networkCancelled,
  'Unexpected network error.': (l10n) => l10n.networkUnexpected,
  // Status-based problem titles used when the server sent no body.
  'Connection failed': (l10n) => l10n.networkConnectionFailed,
  'Invalid request': (l10n) => l10n.networkInvalidRequest,
  'Sign-in required': (l10n) => l10n.networkSignInRequired,
  'Not allowed': (l10n) => l10n.networkNotAllowed,
  'Not found': (l10n) => l10n.networkNotFound,
  'Conflict': (l10n) => l10n.networkConflict,
  'File too large': (l10n) => l10n.networkFileTooLarge,
  'Server error': (l10n) => l10n.networkServerError,
  'Request failed': (l10n) => l10n.networkRequestFailed,
};

/// Exact English API problem detail → its translation. Anything not keyed
/// here is displayed exactly as the server sent it.
final Map<String, String Function(AppLocalizations l10n)> apiMessageMap = {
  'Invalid email or password.':
      (l10n) => l10n.errorsInvalidCredentials,
  'Check the credentials and try again.':
      (l10n) => l10n.errorsCheckCredentials,
  'Sign in instead or choose another email address.':
      (l10n) => l10n.errorsEmailAlreadyRegistered,
  'An account with this email already exists.':
      (l10n) => l10n.errorsAccountExists,
  'Role must be Client or Provider.':
      (l10n) => l10n.errorsInvalidRoleClientProvider,
  'This account belongs to the web dashboard. Sign in with a Client or Provider account.':
      (l10n) => l10n.errorsDashboardAccount,
  'Password must be at least 6 characters.':
      (l10n) => l10n.errorsPasswordTooShort,
  'The Email field is required.': (l10n) => l10n.errorsEmailFieldRequired,
  'The Password field is required.':
      (l10n) => l10n.errorsPasswordFieldRequired,
  'The Email field is not a valid e-mail address.':
      (l10n) => l10n.errorsEmailNotValid,
};
