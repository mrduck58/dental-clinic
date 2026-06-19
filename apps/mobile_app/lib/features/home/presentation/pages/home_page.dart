import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
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
      color: const Color(0xFFF8FAFC),
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

                // Lịch hẹn sắp tới
                HomeSectionHeader(
                  title: 'Lịch hẹn sắp tới',
                  onSeeAll: () => context.go(AppRoutes.appointments),
                ),
                const SizedBox(height: 12),
                const _NoAppointmentCard(),
                const SizedBox(height: 26),

                // Nha sĩ nổi bật
                const HomeSectionHeader(title: 'Nha sĩ nổi bật'),
                const SizedBox(height: 14),
                _isLoading
                    ? const _LoadingRow()
                    : _doctors.isEmpty
                        ? const _EmptySection(message: 'Chưa có thông tin nha sĩ.')
                        : SizedBox(
                            height: 132,
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
                const HomeSectionHeader(title: 'Dịch vụ nổi bật'),
                const SizedBox(height: 14),
                _isLoading
                    ? const _LoadingColumn()
                    : _services.isEmpty
                        ? const _EmptySection(message: 'Chưa có dịch vụ.')
                        : ServicesGrid(services: _services),
                const SizedBox(height: 26),

                // Tin tức nổi bật
                const HomeSectionHeader(title: 'Tin tức nổi bật'),
                const SizedBox(height: 14),
                if (_isLoading)
                  const _LoadingColumn()
                else if (_posts.isEmpty)
                  const _EmptySection(message: 'Chưa có tin tức.')
                else
                  ..._posts.map(
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
        color: Colors.white,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: AppColors.divider),
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
              color: AppColors.primaryLight,
              borderRadius: BorderRadius.circular(16),
            ),
            child: const Icon(Iconsax.calendar_tick, color: AppColors.primary, size: 28),
          ),
          const SizedBox(width: 16),
          const Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Chưa có lịch hẹn sắp tới',
                  style: TextStyle(
                    color: AppColors.textPrimary,
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                SizedBox(height: 4),
                Text(
                  'Đặt lịch ngay để gặp bác sĩ của bạn.',
                  style: TextStyle(color: AppColors.textSecondary, fontSize: 13),
                ),
              ],
            ),
          ),
          const SizedBox(width: 12),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
            decoration: BoxDecoration(
              color: AppColors.primary,
              borderRadius: BorderRadius.circular(999),
            ),
            child: const Text(
              'Đặt lịch',
              style: TextStyle(
                color: Colors.white,
                fontSize: 13,
                fontWeight: FontWeight.w700,
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
