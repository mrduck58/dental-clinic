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
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadAll();
  }

  Future<void> _loadAll() async {
    setState(() => _isLoading = true);
    final results = await Future.wait<dynamic>([
      _homeService.getDentists().catchError((_) => <DoctorModel>[]),
      _homeService.getServices().catchError((_) => <ServiceModel>[]),
      _homeService.getPosts().catchError((_) => <PostModel>[]),
      _auth.getUserName(),
    ]);
    if (!mounted) return;
    setState(() {
      _doctors = List<DoctorModel>.from(results[0] as List);
      _services = List<ServiceModel>.from(results[1] as List);
      _posts = List<PostModel>.from(results[2] as List);
      _userName = (results[3] as String?) ?? '';
      _isLoading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    final bottomPad = MediaQuery.of(context).padding.bottom + 16;
    return ColoredBox(
      color: context.bg,
      child: CustomScrollView(
        slivers: [
          SliverToBoxAdapter(child: HomeHeader(userName: _userName)),
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
                const _NoAppointmentCard(),
                const SizedBox(height: 26),

                // Nha sĩ nổi bật
                HomeSectionHeader(title: context.l10n('featured_dentists')),
                const SizedBox(height: 14),
                _isLoading
                    ? const _LoadingRow()
                    : _doctors.isEmpty
                        ? _EmptySection(message: context.l10n('load_doctors_failed'))
                        : SizedBox(
                            height: 150,
                            child: ListView.separated(
                              scrollDirection: Axis.horizontal,
                              itemCount: _doctors.length,
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
                            onTap: () => context.push(AppRoutes.chat),
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
