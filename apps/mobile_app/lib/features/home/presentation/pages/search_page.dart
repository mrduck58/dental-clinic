import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/home/data/home_service.dart';
import 'package:mobile_app/features/home/data/models/doctor_model.dart';
import 'package:mobile_app/features/home/data/models/service_model.dart';
import 'package:mobile_app/features/home/data/review_service.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';

class SearchPage extends StatefulWidget {
  const SearchPage({super.key});

  @override
  State<SearchPage> createState() => _SearchPageState();
}

class _SearchPageState extends State<SearchPage> with SingleTickerProviderStateMixin {
  final _homeService = HomeService();
  final _reviewService = ReviewService();
  final _searchCtrl = TextEditingController();

  List<DoctorModel> _doctors = [];
  List<ServiceModel> _services = [];
  bool _isLoading = true;
  String _searchQuery = '';
  int _activeTab = 0; // 0 = Dentist, 1 = Service

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadData() async {
    setState(() => _isLoading = true);
    try {
      final results = await Future.wait<dynamic>([
        _homeService.getDentists(),
        _homeService.getServices(),
      ]);
      setState(() {
        _doctors = List<DoctorModel>.from(results[0] as List);
        _services = List<ServiceModel>.from(results[1] as List);
        _isLoading = false;
      });
    } catch (_) {
      setState(() => _isLoading = false);
    }
  }

  List<DoctorModel> get _filteredDoctors {
    if (_searchQuery.trim().isEmpty) return _doctors;
    final q = _searchQuery.toLowerCase();
    return _doctors.where((d) =>
        d.fullName.toLowerCase().contains(q) ||
        (d.specialty != null && d.specialty!.toLowerCase().contains(q))).toList();
  }

  List<ServiceModel> get _filteredServices {
    if (_searchQuery.trim().isEmpty) return _services;
    final q = _searchQuery.toLowerCase();
    return _services.where((s) =>
        s.name.toLowerCase().contains(q) ||
        s.description.toLowerCase().contains(q)).toList();
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0,
        scrolledUnderElevation: 0,
        surfaceTintColor: Colors.transparent,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
          onPressed: () => context.pop(),
        ),
        titleSpacing: 0,
        title: Padding(
          padding: const EdgeInsets.only(right: 16),
          child: Container(
            height: 44,
            decoration: BoxDecoration(
              color: context.bg,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: context.divider),
            ),
            child: TextField(
              controller: _searchCtrl,
              autofocus: true,
              style: TextStyle(color: context.textPrimary, fontSize: 14),
              decoration: InputDecoration(
                hintText: isVi ? 'Tìm kiếm nha sĩ, dịch vụ...' : 'Search dentists, services...',
                hintStyle: TextStyle(color: context.textMuted, fontSize: 13),
                prefixIcon: Icon(Iconsax.search_normal, color: context.textMuted, size: 16),
                suffixIcon: _searchCtrl.text.isNotEmpty
                    ? IconButton(
                        icon: Icon(Icons.clear, color: context.textMuted, size: 16),
                        onPressed: () {
                          _searchCtrl.clear();
                          setState(() {
                            _searchQuery = '';
                          });
                        },
                      )
                    : null,
                border: InputBorder.none,
                contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
              ),
              onChanged: (val) {
                setState(() {
                  _searchQuery = val;
                });
              },
            ),
          ),
        ),
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(60),
          child: Container(
            color: context.card,
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Row(
              children: [
                _buildTabItem(0, isVi ? 'Nha sĩ' : 'Dentists', Iconsax.user),
                const SizedBox(width: 12),
                _buildTabItem(1, isVi ? 'Dịch vụ' : 'Services', Iconsax.health),
              ],
            ),
          ),
        ),
      ),
      body: _isLoading
          ? Center(child: CircularProgressIndicator(color: AppColors.primary))
          : _activeTab == 0
              ? _buildDentistList(isVi)
              : _buildServiceList(isVi),
    );
  }

  Widget _buildTabItem(int index, String label, IconData icon) {
    final isActive = _activeTab == index;
    return GestureDetector(
      onTap: () => setState(() => _activeTab = index),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        decoration: BoxDecoration(
          color: isActive ? AppColors.primary : context.bg,
          borderRadius: BorderRadius.circular(30),
          border: Border.all(
            color: isActive ? AppColors.primary : context.divider,
            width: 1.5,
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              icon,
              color: isActive ? Colors.white : context.textSecondary,
              size: 16,
            ),
            const SizedBox(width: 6),
            Text(
              label,
              style: TextStyle(
                color: isActive ? Colors.white : context.textSecondary,
                fontWeight: FontWeight.w700,
                fontSize: 13,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildDentistList(bool isVi) {
    final list = _filteredDoctors;
    if (list.isEmpty) {
      return _buildEmptyState(isVi ? 'Không tìm thấy nha sĩ nào phù hợp.' : 'No dentists matching your search.');
    }

    return ListView.separated(
      padding: const EdgeInsets.all(20),
      itemCount: list.length,
      separatorBuilder: (_, __) => const SizedBox(height: 16),
      itemBuilder: (context, i) {
        final doc = list[i];
        final avgRating = _reviewService.getAverageRating(doc.id);
        final reviewsCount = _reviewService.getReviewsForDentist(doc.id).length;
        final specialty = doc.specialty ?? (isVi ? 'Nha sĩ tổng quát' : 'General Dentist');
        final expYears = doc.yearsOfExperience ?? 10;

        return Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: context.card,
            borderRadius: BorderRadius.circular(18),
            border: Border.all(color: context.divider),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: context.isDark ? 0.15 : 0.04),
                blurRadius: 10,
                offset: const Offset(0, 4),
              ),
            ],
          ),
          child: Row(
            children: [
              // Avatar
              ClipRRect(
                borderRadius: BorderRadius.circular(16),
                child: Container(
                  width: 72,
                  height: 72,
                  color: context.isDark ? AppColors.primary.withValues(alpha: 0.15) : AppColors.primaryLight,
                  child: doc.profilePictureUrl != null
                      ? Image.network(
                          doc.profilePictureUrl!,
                          fit: BoxFit.cover,
                          errorBuilder: (_, __, ___) => Icon(Iconsax.user, color: AppColors.primary, size: 30),
                        )
                      : Icon(Iconsax.user, color: AppColors.primary, size: 30),
                ),
              ),
              const SizedBox(width: 16),
              // Content
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      doc.fullName,
                      style: TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.w800,
                        color: context.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      specialty,
                      style: TextStyle(
                        fontSize: 12.5,
                        color: context.textSecondary,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '$expYears ${isVi ? 'năm kinh nghiệm' : 'years of experience'}',
                      style: TextStyle(
                        fontSize: 11,
                        color: context.textMuted,
                      ),
                    ),
                    const SizedBox(height: 6),
                    Row(
                      children: [
                        const Icon(Icons.star_rounded, color: Colors.amber, size: 16),
                        const SizedBox(width: 4),
                        Text(
                          '$avgRating ($reviewsCount ${isVi ? 'đánh giá' : 'reviews'})',
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.w700,
                            color: context.textSecondary,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              IconButton(
                icon: const Icon(Iconsax.arrow_right_3, color: AppColors.primary, size: 20),
                onPressed: () => context.push(AppRoutes.dentistProfile, extra: doc),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildServiceList(bool isVi) {
    final list = _filteredServices;
    if (list.isEmpty) {
      return _buildEmptyState(isVi ? 'Không tìm thấy dịch vụ nào phù hợp.' : 'No services matching your search.');
    }

    return ListView.separated(
      padding: const EdgeInsets.all(20),
      itemCount: list.length,
      separatorBuilder: (_, __) => const SizedBox(height: 16),
      itemBuilder: (context, i) {
        final service = list[i];
        final viewCountText = service.viewCount >= 1000
            ? '${(service.viewCount / 1000).toStringAsFixed(1)}k'
            : '${service.viewCount}';

        return Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: context.card,
            borderRadius: BorderRadius.circular(18),
            border: Border.all(color: context.divider),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: context.isDark ? 0.15 : 0.04),
                blurRadius: 10,
                offset: const Offset(0, 4),
              ),
            ],
          ),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      service.name,
                      style: TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.w800,
                        color: context.textPrimary,
                      ),
                    ),
                    const SizedBox(height: 6),
                    Text(
                      service.description,
                      style: TextStyle(
                        fontSize: 12.5,
                        color: context.textSecondary,
                        height: 1.4,
                      ),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 10),
                    Row(
                      children: [
                        Icon(Iconsax.eye, color: context.textMuted, size: 14),
                        const SizedBox(width: 4),
                        Text(
                          '$viewCountText ${isVi ? 'lượt sử dụng' : 'uses'}',
                          style: TextStyle(
                            fontSize: 11.5,
                            fontWeight: FontWeight.w700,
                            color: context.textSecondary,
                          ),
                        ),
                        const SizedBox(width: 16),
                        Text(
                          service.formattedPrice,
                          style: const TextStyle(
                            fontSize: 13.5,
                            fontWeight: FontWeight.w800,
                            color: AppColors.primary,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 12),
              Align(
                alignment: Alignment.center,
                child: IconButton(
                  icon: const Icon(Iconsax.arrow_right_3, color: AppColors.primary, size: 20),
                  onPressed: () {
                    context.push(
                      AppRoutes.bookingServiceDetail,
                      extra: ServiceInfo(
                        id: service.id,
                        name: service.name,
                        description: service.description,
                        price: service.formattedPrice,
                        note: '${service.durationText} • ${context.l10n('free_checkup')}',
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildEmptyState(String msg) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 40.0),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              width: 80,
              height: 80,
              decoration: BoxDecoration(
                color: context.isDark ? Colors.grey[800] : const Color(0xFFF1F5F9),
                shape: BoxShape.circle,
              ),
              child: Icon(Iconsax.search_status, size: 36, color: context.textMuted),
            ),
            const SizedBox(height: 20),
            Text(
              msg,
              style: TextStyle(
                color: context.textSecondary,
                fontSize: 14,
                fontWeight: FontWeight.w600,
              ),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }
}
