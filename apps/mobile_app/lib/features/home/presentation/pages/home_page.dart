import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/home/data/home_service.dart';
import 'package:mobile_app/features/home/data/models/doctor_model.dart';
import 'package:mobile_app/features/home/data/models/post_model.dart';
import 'package:mobile_app/features/home/data/models/service_model.dart';
import 'package:mobile_app/features/home/presentation/widgets/doctor_avatar_card.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/home/presentation/widgets/home_banner.dart';
import 'package:mobile_app/features/home/presentation/widgets/home_header.dart';
import 'package:mobile_app/features/home/presentation/widgets/home_search_bar.dart';
import 'package:mobile_app/features/home/presentation/widgets/home_section_header.dart';
import 'package:mobile_app/features/home/presentation/widgets/news_card.dart';
import 'package:mobile_app/features/home/presentation/widgets/service_card.dart';

class HomePage extends StatefulWidget {
  const HomePage({super.key});

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  final _homeService = HomeService();
  final _auth = AuthService();

  List<DoctorModel> _doctors = [];
  List<ServiceModel> _services = [];
  List<PostModel> _posts = [];
  String _userName = '';
  String? _avatarUrl;
  MyAppointmentItem? _upcomingAppointment;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadAll();
  }

  Future<void> _loadAll() async {
    setState(() => _isLoading = true);
    final token = await _auth.getToken();
    final results = await Future.wait<dynamic>([
      _homeService.getDentists().catchError((_) => <DoctorModel>[]),
      _homeService.getServices().catchError((_) => <ServiceModel>[]),
      _homeService.getPosts().catchError((_) => <PostModel>[]),
      _auth.getUserName(),
      _auth.getUserAvatar(),
      token != null
          ? BookingService().getMyAppointments().catchError((_) => <MyAppointmentItem>[])
          : Future.value(<MyAppointmentItem>[]),
    ]);
    if (!mounted) return;

    final appointments = List<MyAppointmentItem>.from(results[5] as List);
    MyAppointmentItem? nearestUpcoming;
    if (appointments.isNotEmpty) {
      final upcoming = appointments.where((a) {
        final statusLower = a.status.toLowerCase();
        return statusLower == 'confirmed' ||
            statusLower == 'pending' ||
            statusLower == 'checkedin' ||
            statusLower == 'inprogress';
      }).toList();

      if (upcoming.isNotEmpty) {
        upcoming.sort((a, b) => a.parsedDate.compareTo(b.parsedDate));
        nearestUpcoming = upcoming.first;
      }
    }

    setState(() {
      _doctors = List<DoctorModel>.from(results[0] as List);
      _services = List<ServiceModel>.from(results[1] as List);
      _posts = List<PostModel>.from(results[2] as List);
      _userName = (results[3] as String?) ?? '';
      _avatarUrl = results[4] as String?;
      _upcomingAppointment = nearestUpcoming;
      _isLoading = false;
    });

    try {
      final p = await _auth.getMyProfile();
      if (mounted) {
        setState(() {
          _userName = p.fullName;
          _avatarUrl = p.profilePictureUrl;
        });
        _auth.saveUserName(p.fullName);
        if (p.profilePictureUrl != null) {
          _auth.saveUserAvatar(p.profilePictureUrl!);
        }
      }
    } catch (_) {}
  }

  @override
  Widget build(BuildContext context) {
    final bottomPad = MediaQuery.of(context).padding.bottom + 16;
    return ColoredBox(
      color: context.bg,
      child: CustomScrollView(
        slivers: [
          SliverToBoxAdapter(child: HomeHeader(userName: _userName, avatarUrl: _avatarUrl)),
          const SliverToBoxAdapter(child: HomeSearchBar()),
          SliverPadding(
            padding: EdgeInsets.fromLTRB(18, 0, 18, bottomPad),
            sliver: SliverList(
              delegate: SliverChildListDelegate([
                const HomeBanner(),
                const SizedBox(height: 26),
                const _QuickAccessPanel(),
                const SizedBox(height: 26),

                // Lịch hẹn sắp tới
                HomeSectionHeader(
                  title: context.l10n('upcoming_appointment'),
                  onSeeAll: () => context.go(AppRoutes.appointments),
                ),
                const SizedBox(height: 12),
                _isLoading
                    ? const _LoadingRow()
                    : (_upcomingAppointment == null
                        ? const _NoAppointmentCard()
                        : _UpcomingAppointmentCard(
                            item: _upcomingAppointment!,
                            onRefresh: _loadAll,
                          )),
                const SizedBox(height: 26),

                // Nha sĩ nổi bật
                HomeSectionHeader(
                  title: context.l10n('featured_dentists'),
                  onSeeAll: () => context.push(AppRoutes.dentistsList),
                ),
                const SizedBox(height: 14),
                _isLoading
                    ? const _LoadingRow()
                    : _doctors.isEmpty
                        ? _EmptySection(message: context.l10n('load_doctors_failed'))
                        : SizedBox(
                            height: 150,
                            child: ListView.separated(
                              scrollDirection: Axis.horizontal,
                              itemCount: _doctors.length > 4 ? 4 : _doctors.length,
                              separatorBuilder: (_, _) => const SizedBox(width: 16),
                              itemBuilder: (_, i) =>
                                  DoctorAvatarCard(doctor: _doctors[i]),
                            ),
                          ),
                const SizedBox(height: 26),

                 // Dịch vụ nổi bật
                HomeSectionHeader(
                  title: context.l10n('our_services'),
                  onSeeAll: () => context.push(AppRoutes.servicesList),
                ),
                const SizedBox(height: 14),
                _isLoading
                    ? const _LoadingColumn()
                    : _services.isEmpty
                        ? _EmptySection(message: context.l10n('load_services_failed'))
                        : Column(
                            children: List.generate(
                              _services.take(5).length,
                              (i) => Padding(
                                padding: const EdgeInsets.only(bottom: 12),
                                child: ServiceCard(service: _services[i], index: i),
                              ),
                            ),
                          ),
                const SizedBox(height: 26),

                // Tin tức nổi bật
                HomeSectionHeader(
                  title: context.l10n('news'),
                  onSeeAll: () => context.push(AppRoutes.postsList),
                ),
                const SizedBox(height: 14),
                if (_isLoading)
                  const _LoadingColumn()
                else if (_posts.isEmpty)
                  const _EmptySection(message: 'No news.')
                else
                  ..._posts.take(5).map(
                    (post) => Padding(
                      padding: const EdgeInsets.only(bottom: 14),
                      child: NewsCard(post: post),
                    ),
                  ),
              ]),
            ),
          ),
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────
// No Appointment Placeholder
// ─────────────────────────────────────────────────
class _NoAppointmentCard extends StatelessWidget {
  const _NoAppointmentCard();

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 24),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: context.divider),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Row(
        children: [
          Container(
            width: 56,
            height: 56,
            decoration: BoxDecoration(
              color: context.primaryLight,
              borderRadius: BorderRadius.circular(16),
            ),
            child: const Icon(Iconsax.calendar_tick, color: AppColors.primary, size: 28),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  context.l10n('no_appointments'),
                  style: TextStyle(
                    color: context.textPrimary,
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  context.l10n('book_now'),
                  style: TextStyle(color: context.textSecondary, fontSize: 13),
                ),
              ],
            ),
          ),
          const SizedBox(width: 12),
          GestureDetector(
            onTap: () => context.push(AppRoutes.bookingSelectPatient),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
              decoration: BoxDecoration(
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(999),
              ),
              child: Text(
                context.l10n('book_button'),
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────
// Loading & Empty states
// ─────────────────────────────────────────────────
class _LoadingRow extends StatelessWidget {
  const _LoadingRow();

  @override
  Widget build(BuildContext context) {
    return const SizedBox(
      height: 80,
      child: Center(
        child: CircularProgressIndicator(color: AppColors.primary, strokeWidth: 2.5),
      ),
    );
  }
}

class _LoadingColumn extends StatelessWidget {
  const _LoadingColumn();

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.symmetric(vertical: 24),
      child: Center(
        child: CircularProgressIndicator(color: AppColors.primary, strokeWidth: 2.5),
      ),
    );
  }
}

class _EmptySection extends StatelessWidget {
  final String message;
  const _EmptySection({required this.message});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 16),
      child: Center(
        child: Text(
          message,
          style: const TextStyle(color: AppColors.textMuted, fontSize: 14),
        ),
      ),
    );
  }
}

class _QuickAccessPanel extends StatelessWidget {
  const _QuickAccessPanel();

  /// Chatbot AI yêu cầu đăng nhập — kiểm tra token trước khi vào, vì router
  /// hiện không có redirect guard chung cho các route cần xác thực.
  Future<void> _openChatbot(BuildContext context) async {
    final token = await AuthService().getToken();
    if (!context.mounted) return;
    context.push(token == null ? AppRoutes.login : AppRoutes.chat);
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          isVi ? 'Truy cập nhanh' : 'Quick Access',
          style: TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.w800,
            color: context.textPrimary,
          ),
        ),
        const SizedBox(height: 12),
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Left - Book Appointment
            Expanded(
              flex: 4,
              child: GestureDetector(
                onTap: () => context.push(AppRoutes.bookingSelectPatient),
                child: Container(
                  height: 140,
                  decoration: BoxDecoration(
                    color: AppColors.primary,
                    borderRadius: BorderRadius.circular(20),
                    boxShadow: [
                      BoxShadow(
                        color: AppColors.primary.withValues(alpha: 0.2),
                        blurRadius: 10,
                        offset: const Offset(0, 4),
                      ),
                    ],
                  ),
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Container(
                        padding: const EdgeInsets.all(8),
                        decoration: BoxDecoration(
                          color: Colors.white.withValues(alpha: 0.2),
                          shape: BoxShape.circle,
                        ),
                        child: const Icon(
                          Iconsax.calendar_add,
                          color: Colors.white,
                          size: 24,
                        ),
                      ),
                      Text(
                        isVi ? 'Đặt khám' : 'Book Appointment',
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 14,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            const SizedBox(width: 12),
            // Right - Grid of 2x2
            Expanded(
              flex: 6,
              child: SizedBox(
                height: 140,
                child: Column(
                  children: [
                    Expanded(
                      child: Row(
                        children: [
                          _buildSmallCard(
                            context,
                            icon: Icons.smart_toy_rounded,
                            label: 'DENTAL AI',
                            onTap: () => _openChatbot(context),
                          ),
                          const SizedBox(width: 12),
                          _buildSmallCard(
                            context,
                            icon: Iconsax.notification,
                            label: isVi ? 'NHẮC NHỞ' : 'REMINDER',
                            onTap: () => context.push(AppRoutes.reminders),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 12),
                    Expanded(
                      child: Row(
                        children: [
                          _buildSmallCard(
                            context,
                            icon: Iconsax.folder,
                            label: isVi ? 'HỒ SƠ' : 'RECORDS',
                            onTap: () => context.push(AppRoutes.medicalHistory),
                          ),
                          const SizedBox(width: 12),
                          _buildSmallCard(
                            context,
                            icon: Iconsax.timer,
                            label: isVi ? 'HÀNG CHỜ' : 'QUEUE',
                            onTap: () => context.push(AppRoutes.queue),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildSmallCard(
    BuildContext context, {
    required IconData icon,
    required String label,
    required VoidCallback onTap,
  }) {
    final bgColor = context.card;
    final iconColor = context.isDark ? Colors.white : AppColors.primary;
    final textColor = context.textPrimary;

    return Expanded(
      child: GestureDetector(
        onTap: onTap,
        child: Container(
          decoration: BoxDecoration(
            color: bgColor,
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: context.divider),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.03),
                blurRadius: 6,
                offset: const Offset(0, 2),
              ),
            ],
          ),
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon, color: iconColor, size: 20),
              const SizedBox(height: 6),
              FittedBox(
                fit: BoxFit.scaleDown,
                child: Text(
                  label,
                  style: TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.w900,
                    color: textColor,
                    letterSpacing: 0.5,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _UpcomingAppointmentCard extends StatelessWidget {
  final MyAppointmentItem item;
  final VoidCallback onRefresh;

  const _UpcomingAppointmentCard({
    required this.item,
    required this.onRefresh,
  });

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final weekdays = isVi
        ? ['', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy', 'Chủ Nhật']
        : ['', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

    final date = item.parsedDate;
    final dateStr =
        '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}/${date.year}';
    final timeStr =
        '${date.hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')}';
    final dayLabel = weekdays[date.weekday];
    final (statusLabel, statusColor, statusBg) = _statusStyle(item.status, isVi);

    return Container(
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: context.divider),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 44,
                height: 44,
                decoration: BoxDecoration(
                  color: context.divider,
                  shape: BoxShape.circle,
                ),
                child: ClipOval(
                  child: item.dentistAvatarUrl != null && item.dentistAvatarUrl!.isNotEmpty
                      ? (item.dentistAvatarUrl!.startsWith('assets/')
                          ? Image.asset(item.dentistAvatarUrl!, fit: BoxFit.cover)
                          : Image.network(
                              item.dentistAvatarUrl!,
                              fit: BoxFit.cover,
                              errorBuilder: (_, __, ___) => const Icon(Iconsax.user, size: 20),
                            ))
                      : const Icon(Iconsax.user, size: 20),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.dentistName,
                      style: TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                        color: context.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      item.specialization,
                      style: TextStyle(
                        fontSize: 12,
                        color: context.textSecondary,
                      ),
                    ),
                  ],
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: statusBg,
                  borderRadius: BorderRadius.circular(999),
                ),
                child: Text(
                  statusLabel,
                  style: TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.bold,
                    color: statusColor,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          Divider(color: context.divider, height: 1),
          const SizedBox(height: 16),
          Row(
            children: [
              Icon(Iconsax.calendar_1, size: 16, color: AppColors.primary),
              const SizedBox(width: 8),
              Text(
                '$dateStr · $dayLabel',
                style: TextStyle(fontSize: 13, color: context.textPrimary, fontWeight: FontWeight.w600),
              ),
              const Spacer(),
              Icon(Iconsax.clock, size: 16, color: AppColors.primary),
              const SizedBox(width: 8),
              Text(
                timeStr,
                style: TextStyle(fontSize: 13, color: context.textPrimary, fontWeight: FontWeight.w600),
              ),
            ],
          ),
          if (item.patientName != null && item.patientName!.isNotEmpty) ...[
            const SizedBox(height: 10),
            Row(
              children: [
                Icon(Iconsax.user, size: 16, color: context.textSecondary),
                const SizedBox(width: 8),
                Text(
                  item.patientRelationship == null || item.patientRelationship!.isEmpty || item.patientRelationship == 'Tôi' || item.patientRelationship == 'Self'
                      ? '${isVi ? 'Bệnh nhân' : 'Patient'}: ${item.patientName} (${isVi ? 'Tôi' : 'Self'})'
                      : '${isVi ? 'Bệnh nhân' : 'Patient'}: ${item.patientName} (${item.patientRelationship})',
                  style: TextStyle(fontSize: 12, color: context.textSecondary),
                ),
              ],
            ),
          ],
          if (item.serviceName != null && item.serviceName!.isNotEmpty) ...[
            const SizedBox(height: 10),
            Row(
              children: [
                Icon(Iconsax.health, size: 16, color: context.textSecondary),
                const SizedBox(width: 8),
                Text(
                  item.serviceName!,
                  style: TextStyle(fontSize: 12, color: context.textSecondary),
                ),
              ],
            ),
          ],
          const SizedBox(height: 16),
          SizedBox(
            width: double.infinity,
            height: 44,
            child: OutlinedButton(
              onPressed: () async {
                await context.push(AppRoutes.appointmentDetails, extra: item);
                onRefresh();
              },
              style: OutlinedButton.styleFrom(
                foregroundColor: AppColors.primary,
                side: const BorderSide(color: AppColors.primary, width: 1.5),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              ),
              child: Text(
                isVi ? 'Xem chi tiết' : 'View Details',
                style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
              ),
            ),
          ),
        ],
      ),
    );
  }

  (String, Color, Color) _statusStyle(String status, bool isVi) {
    switch (status.toLowerCase()) {
      case 'pending':
        return (
          isVi ? 'Chờ xác nhận' : 'Pending',
          const Color(0xFFF59E0B),
          const Color(0xFFFEF3C7),
        );
      case 'confirmed':
        return (
          isVi ? 'Đã xác nhận' : 'Confirmed',
          AppColors.primary,
          const Color(0xFFFEE2E2),
        );
      case 'checkedin':
        return (
          isVi ? 'Đã check-in' : 'Checked In',
          const Color(0xFF10B981),
          const Color(0xFFD1FAE5),
        );
      case 'inprogress':
        return (
          isVi ? 'Đang khám' : 'In Progress',
          const Color(0xFF3B82F6),
          const Color(0xFFDBEAFE),
        );
      default:
        return (
          status,
          const Color(0xFF64748B),
          const Color(0xFFF1F5F9),
        );
    }
  }
}
