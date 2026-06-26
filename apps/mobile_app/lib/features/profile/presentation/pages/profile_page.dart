import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';

class ProfilePage extends StatefulWidget {
  const ProfilePage({super.key});

  @override
  State<ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends State<ProfilePage> {
  final _auth = AuthService();
  String _userName = '';
  String _userEmail = '';
  bool _isLoggingOut = false;
  late bool _darkModeVal;

  @override
  void initState() {
    super.initState();
    _loadUserInfo();
    _darkModeVal = SettingsManager.instance.isDarkMode.value;
  }

  Future<void> _loadUserInfo() async {
    final name = await _auth.getUserName();
    final email = await _auth.getUserEmail();
    if (mounted) {
      setState(() {
        _userName = name ?? '';
        _userEmail = email ?? '';
      });
    }
  }

  String _initials() {
    final words = _userName.trim().split(' ').where((w) => w.isNotEmpty).toList();
    if (words.isEmpty) return 'U';
    if (words.length == 1) return words[0][0].toUpperCase();
    return '${words.first[0]}${words.last[0]}'.toUpperCase();
  }

  Future<void> _showLogoutDialog() async {
    final confirmed = await showDialog<bool>(
      context: context,
      barrierColor: Colors.black.withValues(alpha: 0.45),
      builder: (_) => const _LogoutDialog(),
    );
    if (confirmed == true && mounted) _doLogout();
  }

  Future<void> _doLogout() async {
    setState(() => _isLoggingOut = true);
    try {
      final token = await _auth.getToken();
      if (token != null) {
        try {
          await _auth.logout(token: token);
        } catch (_) {}
      }
    } finally {
      await _auth.clearAll();
      if (mounted) context.go(AppRoutes.login);
    }
  }

  void _showComingSoon(String feature) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text('$feature đang được phát triển'),
        duration: Duration(seconds: 2),
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
    );
  }

  void _showLanguageDialog() {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: context.card,
        surfaceTintColor: Colors.transparent,
        title: Text(
          context.l10n('select_language'),
          style: TextStyle(color: context.textPrimary, fontWeight: FontWeight.bold),
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              title: Text('Tiếng Việt', style: TextStyle(color: context.textPrimary)),
              trailing: SettingsManager.instance.locale.value.languageCode == 'vi'
                  ? Icon(Icons.check, color: AppColors.primary)
                  : null,
              onTap: () {
                SettingsManager.instance.setLocale('vi');
                Navigator.of(ctx).pop();
                setState(() {});
              },
            ),
            ListTile(
              title: Text('English', style: TextStyle(color: context.textPrimary)),
              trailing: SettingsManager.instance.locale.value.languageCode == 'en'
                  ? Icon(Icons.check, color: AppColors.primary)
                  : null,
              onTap: () {
                SettingsManager.instance.setLocale('en');
                Navigator.of(ctx).pop();
                setState(() {});
              },
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0.5,
        leading: IconButton(
          icon: Icon(Iconsax.arrow_left, color: context.textPrimary),
          onPressed: () => context.go(AppRoutes.home),
        ),
        title: Text(
          context.l10n('settings'),
          style: TextStyle(
            fontSize: 20,
            fontWeight: FontWeight.w900,
            color: context.textPrimary,
          ),
        ),
      ),
      body: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        child: Padding(
          padding: EdgeInsets.symmetric(horizontal: 20, vertical: 16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // ── Profile Header Card ──────────────────────────────────────
              _buildProfileCard(),
              SizedBox(height: 20),

              // ── Section 1: PROFILE SETTINGS ──────────────────────────────
              _buildSectionHeader(context.l10n('profile_settings')),
              _SettingsTile(
                icon: Iconsax.document_text,
                label: context.l10n('medical_info'),
                onTap: () => context.push(AppRoutes.medicalHistory),
              ),
              _SettingsTile(
                icon: Iconsax.profile_2user,
                label: context.l10n('family_members'),
                onTap: () => context.push(AppRoutes.familyMembers),
              ),
              _SettingsTile(
                icon: Iconsax.card,
                label: context.l10n('payment_method'),
                onTap: () => context.go(AppRoutes.payment),
              ),
              _SettingsTile(
                icon: Iconsax.receipt_item,
                label: context.l10n('payment_history'),
                onTap: () => context.push(AppRoutes.paymentHistory),
              ),

              // ── Section 2: APP PREFERENCES ───────────────────────────────
              _buildSectionHeader(context.l10n('app_preferences')),
              _SettingsTile(
                icon: Iconsax.moon,
                label: context.l10n('dark_mode'),
                trailing: SizedBox(
                  height: 30,
                  child: Switch(
                    value: _darkModeVal,
                    activeColor: AppColors.primary,
                    onChanged: (val) {
                      setState(() {
                        _darkModeVal = val;
                      });
                      SettingsManager.instance.setDarkMode(val);
                    },
                  ),
                ),
                onTap: () {},
              ),
              _SettingsTile(
                icon: Iconsax.notification,
                label: context.l10n('notifications'),
                onTap: () => context.push(AppRoutes.notifications),
              ),
              _SettingsTile(
                icon: Iconsax.global,
                label: context.l10n('language'),
                trailing: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      SettingsManager.instance.locale.value.languageCode == 'vi' ? 'Tiếng Việt' : 'English',
                      style: TextStyle(color: context.textMuted, fontSize: 13),
                    ),
                    SizedBox(width: 4),
                    Icon(
                      Icons.arrow_forward_ios_rounded,
                      size: 14,
                      color: context.textMuted.withValues(alpha: 0.7),
                    ),
                  ],
                ),
                onTap: _showLanguageDialog,
              ),

              // ── Section 3: SECURITY ──────────────────────────────────────
              _buildSectionHeader(context.l10n('security')),
              _SettingsTile(
                icon: Iconsax.key,
                label: context.l10n('change_password'),
                onTap: () => context.push(AppRoutes.changePassword),
              ),
              _SettingsTile(
                icon: Iconsax.shield_tick,
                label: context.l10n('active_sessions'),
                onTap: () => context.push(AppRoutes.security),
              ),

              // ── Section 4: SUPPORT & LEGAL ────────────────────────────────
              _buildSectionHeader(context.l10n('support_legal')),
              _SettingsTile(
                icon: Iconsax.info_circle,
                label: context.l10n('help_faq'),
                onTap: () => _showComingSoon('Trung tâm trợ giúp'),
              ),
              _SettingsTile(
                icon: Iconsax.note_2,
                label: context.l10n('tos'),
                onTap: () => _showComingSoon('Điều khoản dịch vụ'),
              ),
              _SettingsTile(
                icon: Iconsax.security_card,
                label: context.l10n('privacy_policy'),
                onTap: () => _showComingSoon('Chính sách bảo mật'),
              ),

              const SizedBox(height: 32),

              // ── Log Out Button ───────────────────────────────────────────
              SizedBox(
                width: double.infinity,
                height: 52,
                child: ElevatedButton(
                  onPressed: _isLoggingOut ? null : _showLogoutDialog,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(16),
                    ),
                    elevation: 0,
                  ),
                  child: _isLoggingOut
                      ? const SizedBox(
                          width: 24,
                          height: 24,
                          child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2.5),
                        )
                      : Text(
                          context.l10n('logout'),
                          style: const TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                ),
              ),
              const SizedBox(height: 12),
              Center(
                child: Text(
                  'Version 2.4.0',
                  style: TextStyle(
                    fontSize: 12,
                    color: AppColors.textMuted,
                  ),
                ),
              ),
              const SizedBox(height: 120),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildProfileCard() {
    return Container(
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: context.isDark ? context.divider : AppColors.primaryLight, width: 1.5),
      ),
      child: InkWell(
        onTap: () async {
          final updated = await context.push(AppRoutes.editProfile);
          if (updated == true && mounted) {
            _loadUserInfo();
          }
        },
        borderRadius: BorderRadius.circular(20),
        child: Padding(
          padding: EdgeInsets.all(16.0),
          child: Row(
            children: [
              // Circle initials avatar
              CircleAvatar(
                radius: 26,
                backgroundColor: AppColors.primary,
                child: Text(
                  _initials(),
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
              SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      _userName.isNotEmpty ? _userName : 'Bệnh nhân',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: context.textPrimary,
                      ),
                    ),
                    SizedBox(height: 4),
                    Text(
                      _userEmail.isNotEmpty ? _userEmail : 'chưa cấu hình email',
                      style: TextStyle(
                        fontSize: 13,
                        color: context.textSecondary,
                      ),
                    ),
                  ],
                ),
              ),
              Icon(
                Icons.chevron_right_rounded,
                color: AppColors.primary,
                size: 24,
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildSectionHeader(String title) {
    return Padding(
      padding: EdgeInsets.only(top: 24, bottom: 8),
      child: Text(
        title,
        style: TextStyle(
          fontSize: 11,
          fontWeight: FontWeight.w900,
          color: context.textSecondary,
          letterSpacing: 0.8,
        ),
      ),
    );
  }
}

class _SettingsTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final Widget? trailing;
  final VoidCallback onTap;

  const _SettingsTile({
    required this.icon,
    required this.label,
    this.trailing,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        InkWell(
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.symmetric(vertical: 14),
            child: Row(
              children: [
                Icon(icon, color: context.textSecondary, size: 22),
                const SizedBox(width: 14),
                Expanded(
                  child: Text(
                    label,
                    style: TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w600,
                      color: context.textPrimary,
                    ),
                  ),
                ),
                trailing ??
                    Icon(
                      Icons.arrow_forward_ios_rounded,
                      size: 14,
                      color: context.textMuted.withValues(alpha: 0.7),
                    ),
              ],
            ),
          ),
        ),
        Divider(color: context.divider, height: 1),
      ],
    );
  }
}

class _LogoutDialog extends StatelessWidget {
  const _LogoutDialog();

  @override
  Widget build(BuildContext context) {
    return Dialog(
      backgroundColor: context.card,
      surfaceTintColor: Colors.transparent,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(28)),
      insetPadding: const EdgeInsets.symmetric(horizontal: 32),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(28, 32, 28, 28),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Stack(
              alignment: Alignment.center,
              children: [
                Container(
                  width: 90,
                  height: 90,
                  decoration: BoxDecoration(
                    color: context.primaryLight.withValues(alpha: 0.4),
                    shape: BoxShape.circle,
                  ),
                ),
                Container(
                  width: 70,
                  height: 70,
                  decoration: BoxDecoration(
                    color: context.primaryLight,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(
                    Iconsax.logout,
                    color: AppColors.primary,
                    size: 30,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 20),
            Text(
              context.l10n('logout_confirm'),
              style: TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.w800,
                color: context.textPrimary,
              ),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 8),
            Text(
              context.l10n('logout_desc'),
              style: TextStyle(
                fontSize: 13,
                color: context.textSecondary,
                height: 1.4,
              ),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 28),
            Row(
              children: [
                Expanded(
                  child: GestureDetector(
                    onTap: () => Navigator.of(context).pop(false),
                    child: Container(
                      height: 50,
                      decoration: BoxDecoration(
                        color: context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9),
                        borderRadius: BorderRadius.circular(999),
                      ),
                      child: Center(
                        child: Text(
                          context.l10n('cancel'),
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.w700,
                            color: context.textSecondary,
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: GestureDetector(
                    onTap: () => Navigator.of(context).pop(true),
                    child: Container(
                      height: 50,
                      decoration: BoxDecoration(
                        color: AppColors.primary,
                        borderRadius: BorderRadius.circular(999),
                        boxShadow: [
                          BoxShadow(
                            color: AppColors.primary.withValues(alpha: 0.35),
                            blurRadius: 14,
                            offset: const Offset(0, 5),
                          ),
                        ],
                      ),
                      child: Center(
                        child: Text(
                          context.l10n('logout'),
                          style: const TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.w700,
                            color: Colors.white,
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
