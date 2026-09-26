import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

import '../../core/auth/session.dart';
import '../../core/format/formatters.dart';
import '../../core/i18n/language_switcher.dart';
import '../../core/i18n/l10n_ext.dart';
import '../../core/i18n/locale_provider.dart';
import '../../core/widgets/state_views.dart';
import '../../core/widgets/status_badge.dart';
import '../auth/auth_controller.dart';
import '../auth/auth_repository.dart';
import 'subscription.dart';
import 'users_repository.dart';

/// View/edit the signed-in profile, manage the avatar and sign out.
class ProfilePage extends ConsumerStatefulWidget {
  const ProfilePage({super.key});

  @override
  ConsumerState<ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends ConsumerState<ProfilePage> {
  final _formKey = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _phone = TextEditingController();
  final _location = TextEditingController();
  final _bio = TextEditingController();

  bool _editing = false;
  bool _saving = false;

  @override
  void dispose() {
    _name.dispose();
    _phone.dispose();
    _location.dispose();
    _bio.dispose();
    super.dispose();
  }

  void _startEditing(UserProfile user) {
    _name.text = user.name;
    _phone.text = user.phone ?? '';
    _location.text = user.location ?? '';
    _bio.text = user.bio ?? '';
    setState(() => _editing = true);
  }

  Future<void> _save() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    setState(() => _saving = true);
    try {
      final updated = await ref.read(usersRepositoryProvider).updateMe(
            name: _name.text.trim(),
            phone: _phone.text.trim(),
            location: _location.text.trim(),
            bio: _bio.text.trim(),
          );
      await ref.read(authControllerProvider.notifier).applyUser(updated);
      if (mounted) {
        setState(() => _editing = false);
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(context.l10n.profileUpdated)),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(errorMessage(context, e)),
            backgroundColor: Theme.of(context).colorScheme.error,
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Future<void> _refreshUser() async {
    final me = await ref.read(authRepositoryProvider).me();
    await ref.read(authControllerProvider.notifier).applyUser(me);
  }

  Future<void> _pickAvatar() async {
    final picked = await ImagePicker().pickImage(
      source: ImageSource.gallery,
      maxWidth: 800,
      imageQuality: 85,
    );
    if (picked == null || !mounted) return;

    final user = ref.read(authControllerProvider).value?.user;
    if (user == null) return;
    try {
      await ref.read(usersRepositoryProvider).setAvatar(
            userId: user.id,
            filePath: picked.path,
            fileName: picked.name,
          );
      await _refreshUser();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(context.l10n.profilePhotoUpdated)),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(errorMessage(context, e)),
            backgroundColor: Theme.of(context).colorScheme.error,
          ),
        );
      }
    }
  }

  Future<void> _removeAvatar() async {
    final user = ref.read(authControllerProvider).value?.user;
    if (user == null) return;
    try {
      await ref.read(usersRepositoryProvider).removeAvatar(user.id);
      await _refreshUser();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(context.l10n.profilePhotoRemoved)),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(errorMessage(context, e)),
            backgroundColor: Theme.of(context).colorScheme.error,
          ),
        );
      }
    }
  }

  void _showAvatarSheet(UserProfile user) {
    final hasAvatar = user.imagePath != null && user.imagePath!.isNotEmpty;
    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: Text(sheetContext.l10n.profileChoosePhoto),
              onTap: () {
                Navigator.of(sheetContext).pop();
                _pickAvatar();
              },
            ),
            if (hasAvatar)
              ListTile(
                leading: Icon(
                  Icons.delete_outline,
                  color: Theme.of(context).colorScheme.error,
                ),
                title: Text(sheetContext.l10n.profileRemovePhoto),
                onTap: () {
                  Navigator.of(sheetContext).pop();
                  _removeAvatar();
                },
              ),
          ],
        ),
      ),
    );
  }

  Future<void> _signOut() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: Text(dialogContext.l10n.profileSignOutTitle),
        content: Text(dialogContext.l10n.profileSignOutMessage),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: Text(dialogContext.l10n.commonCancel),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: Text(dialogContext.l10n.profileSignOut),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;
    await ref.read(authControllerProvider.notifier).signOut();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final session = ref.watch(authControllerProvider).value;

    if (session == null) return const LoadingView();
    final user = session.user;

    return Scaffold(
      appBar: AppBar(
        title: Text(context.l10n.profileTitle),
        actions: [
          if (!_editing)
            IconButton(
              icon: const Icon(Icons.edit_outlined),
              tooltip: context.l10n.profileEditProfile,
              onPressed: () => _startEditing(user),
            ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Center(
            child: Stack(
              children: [
                _Avatar(user: user),
                Positioned(
                  right: 0,
                  bottom: 0,
                  child: Material(
                    color: theme.colorScheme.primary,
                    shape: const CircleBorder(),
                    elevation: 2,
                    child: InkWell(
                      customBorder: const CircleBorder(),
                      onTap: () => _showAvatarSheet(user),
                      child: const Padding(
                        padding: EdgeInsets.all(8),
                        child: Icon(
                          Icons.camera_alt_outlined,
                          size: 18,
                          color: Colors.white,
                        ),
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
          Center(
            child: Text(
              user.name,
              style: theme.textTheme.headlineSmall
                  ?.copyWith(fontWeight: FontWeight.w700),
            ),
          ),
          Center(
            child: Text(
              user.email,
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
          ),
          const SizedBox(height: 10),
          Center(
            child: Wrap(
              spacing: 8,
              runSpacing: 8,
              alignment: WrapAlignment.center,
              children: [
                _RolePill(label: context.statusLabel(user.role)),
                _RolePill(label: context.statusLabel(user.type), muted: true),
              ],
            ),
          ),
          const SizedBox(height: 24),
          if (_editing) _buildEditForm() else _buildReadView(user),
        ],
      ),
    );
  }

  Widget _buildReadView(UserProfile user) {
    final theme = Theme.of(context);
    final l10n = context.l10n;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _infoRow(theme, Icons.phone_outlined, l10n.profilePhone, user.phone),
        _infoRow(
            theme, Icons.location_on_outlined, l10n.profileLocation, user.location),
        _infoRow(theme, Icons.notes_outlined, l10n.profileBio, user.bio),
        const SizedBox(height: 14),
        const _SubscriptionCard(),
        const SizedBox(height: 20),
        FilledButton.icon(
          onPressed: () => _startEditing(user),
          icon: const Icon(Icons.edit_outlined),
          label: Text(l10n.profileEditProfile),
        ),
        const SizedBox(height: 10),
        OutlinedButton.icon(
          onPressed: _signOut,
          style: OutlinedButton.styleFrom(
            foregroundColor: theme.colorScheme.error,
          ),
          icon: const Icon(Icons.logout),
          label: Text(l10n.profileSignOut),
        ),
        const SizedBox(height: 20),
        Card(
          margin: EdgeInsets.zero,
          child: ListTile(
            leading: const Icon(Icons.language_outlined),
            title: Text(l10n.commonLanguage),
            subtitle: Text(
              currentLanguageLabel(context, ref.watch(localeProvider)),
            ),
            trailing: const Icon(Icons.chevron_right),
            onTap: () => showLanguagePicker(context, ref),
          ),
        ),
      ],
    );
  }

  Widget _infoRow(ThemeData theme, IconData icon, String label, String? value) {
    final hasValue = value != null && value.trim().isNotEmpty;
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 20, color: theme.colorScheme.onSurfaceVariant),
          const SizedBox(width: 10),
          SizedBox(
            width: 76,
            child: Text(
              label,
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
          ),
          Expanded(
            child: Text(
              hasValue ? value : '—',
              style: theme.textTheme.bodyMedium,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildEditForm() {
    final l10n = context.l10n;
    return Form(
      key: _formKey,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          TextFormField(
            controller: _name,
            maxLength: 120,
            decoration: InputDecoration(
              labelText: l10n.profileFullName,
              border: const OutlineInputBorder(),
              counterText: '',
            ),
            validator: (value) {
              final v = value?.trim() ?? '';
              if (v.isEmpty) return l10n.commonNameRequired;
              if (v.length > 120) return l10n.commonMaxCharacters('120');
              return null;
            },
          ),
          const SizedBox(height: 16),
          TextFormField(
            controller: _phone,
            keyboardType: TextInputType.phone,
            maxLength: 40,
            decoration: InputDecoration(
              labelText: l10n.profilePhone,
              helperText: l10n.profileClearToRemove,
              border: const OutlineInputBorder(),
              counterText: '',
            ),
            validator: (value) =>
                (value?.trim().length ?? 0) > 40
                    ? l10n.commonMaxCharacters('40')
                    : null,
          ),
          const SizedBox(height: 16),
          TextFormField(
            controller: _location,
            maxLength: 120,
            decoration: InputDecoration(
              labelText: l10n.profileLocation,
              helperText: l10n.profileClearToRemove,
              border: const OutlineInputBorder(),
              counterText: '',
            ),
            validator: (value) =>
                (value?.trim().length ?? 0) > 120
                    ? l10n.commonMaxCharacters('120')
                    : null,
          ),
          const SizedBox(height: 16),
          TextFormField(
            controller: _bio,
            maxLength: 500,
            maxLines: 4,
            decoration: InputDecoration(
              labelText: l10n.profileBio,
              helperText: l10n.profileClearToRemove,
              alignLabelWithHint: true,
              border: const OutlineInputBorder(),
              counterText: '',
            ),
            validator: (value) =>
                (value?.trim().length ?? 0) > 500
                    ? l10n.commonMaxCharacters('500')
                    : null,
          ),
          const SizedBox(height: 24),
          FilledButton(
            onPressed: _saving ? null : _save,
            style: FilledButton.styleFrom(
              padding: const EdgeInsets.symmetric(vertical: 16),
            ),
            child: _saving
                ? const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : Text(l10n.commonSaveChanges),
          ),
          const SizedBox(height: 10),
          OutlinedButton(
            onPressed: _saving ? null : () => setState(() => _editing = false),
            child: Text(l10n.commonCancel),
          ),
        ],
      ),
    );
  }
}

class _Avatar extends StatelessWidget {
  const _Avatar({required this.user});

  final UserProfile user;

  String get _initials {
    final words = user.name.trim().split(RegExp(r'\s+')).where((w) => w.isNotEmpty);
    if (words.isEmpty) {
      return user.email.isEmpty
          ? '?'
          : user.email.characters.first.toUpperCase();
    }
    final first = words.first.characters.first.toUpperCase();
    if (words.length == 1) return first;
    return '$first${words.elementAt(1).characters.first.toUpperCase()}';
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final path = user.imagePath;
    const radius = 48.0;

    if (path == null || path.isEmpty) {
      return CircleAvatar(
        radius: radius,
        backgroundColor: theme.colorScheme.primaryContainer,
        child: Text(
          _initials,
          style: theme.textTheme.headlineSmall?.copyWith(
            fontWeight: FontWeight.w700,
            color: theme.colorScheme.onPrimaryContainer,
          ),
        ),
      );
    }

    return ClipOval(
      child: SizedBox(
        width: radius * 2,
        height: radius * 2,
        child: Image.network(
          path,
          fit: BoxFit.cover,
          errorBuilder: (context, error, stackTrace) => Container(
            color: theme.colorScheme.primaryContainer,
            alignment: Alignment.center,
            child: Text(
              _initials,
              style: theme.textTheme.headlineSmall?.copyWith(
                fontWeight: FontWeight.w700,
                color: theme.colorScheme.onPrimaryContainer,
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _RolePill extends StatelessWidget {
  const _RolePill({required this.label, this.muted = false});

  final String label;
  final bool muted;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 5),
      decoration: BoxDecoration(
        color: muted
            ? theme.colorScheme.surfaceContainerHighest
            : theme.colorScheme.primaryContainer,
        borderRadius: BorderRadius.circular(999),
      ),
      child: Text(
        label,
        style: TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.w700,
          color: muted
              ? theme.colorScheme.onSurfaceVariant
              : theme.colorScheme.onPrimaryContainer,
        ),
      ),
    );
  }
}

/// The plan attached to this account, read from
/// `GET /api/subscriptions/user/{id}`.
///
/// Plans are managed from the dashboard's `/subscriptions` page; the app only
/// reports what it has been assigned.
class _SubscriptionCard extends ConsumerWidget {
  const _SubscriptionCard();

  String _planLabel(AppLocalizations l10n, String plan) => switch (plan) {
        'Basic' => l10n.planBasic,
        'Premium' => l10n.planPremium,
        'Enterprise' => l10n.planEnterprise,
        _ => plan,
      };

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final l10n = context.l10n;
    final plans = ref.watch(mySubscriptionsProvider);
    final muted = theme.colorScheme.onSurfaceVariant;

    return Card(
      margin: EdgeInsets.zero,
      child: plans.when(
        loading: () => const Padding(
          padding: EdgeInsets.symmetric(horizontal: 16, vertical: 22),
          child: Center(
            child: SizedBox(
              width: 20,
              height: 20,
              child: CircularProgressIndicator(strokeWidth: 2),
            ),
          ),
        ),
        // The API needs a signed-in token; a failure here is not worth
        // interrupting the profile screen with an error card.
        error: (_, _) => const SizedBox.shrink(),
        data: (rows) {
          if (rows.isEmpty) {
            return ListTile(
              leading: const Icon(Icons.workspace_premium_outlined),
              title: Text(l10n.profileSubscription),
              subtitle: Text(l10n.profileNoPlan),
            );
          }

          // Prefer the active plan when an account holds more than one row.
          final plan = rows.firstWhere(
            (row) => row.status == 'Active',
            orElse: () => rows.first,
          );
          final period = plan.endsAt == null
              ? '${formatDate(plan.startsAt)} · ${l10n.subscriptionOpenEnded}'
              : '${formatDate(plan.startsAt)} · ${formatDate(plan.endsAt!)}';

          return Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    Icon(
                      Icons.workspace_premium_outlined,
                      size: 20,
                      color: theme.colorScheme.primary,
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        l10n.profileSubscription,
                        style: theme.textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                    StatusBadge(status: plan.status),
                  ],
                ),
                const SizedBox(height: 12),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Expanded(
                      child: Text(
                        _planLabel(l10n, plan.plan),
                        style: theme.textTheme.headlineSmall?.copyWith(
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          formatMoney(plan.price),
                          style: theme.textTheme.titleLarge?.copyWith(
                            fontWeight: FontWeight.w700,
                            color: theme.colorScheme.primary,
                          ),
                        ),
                        Text(
                          plan.billingCycle == 'Yearly'
                              ? l10n.subscriptionPerYear
                              : l10n.subscriptionPerMonth,
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: muted,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                _detail(
                  theme,
                  Icons.schedule_outlined,
                  period,
                ),
                const SizedBox(height: 6),
                _detail(
                  theme,
                  plan.autoRenew ? Icons.autorenew : Icons.block_outlined,
                  plan.autoRenew
                      ? l10n.subscriptionAutoRenewOn
                      : l10n.subscriptionAutoRenewOff,
                ),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _detail(ThemeData theme, IconData icon, String text) => Row(
        children: [
          Icon(icon, size: 16, color: theme.colorScheme.onSurfaceVariant),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              text,
              style: theme.textTheme.bodyMedium?.copyWith(
                color: theme.colorScheme.onSurfaceVariant,
              ),
            ),
          ),
        ],
      );
}
