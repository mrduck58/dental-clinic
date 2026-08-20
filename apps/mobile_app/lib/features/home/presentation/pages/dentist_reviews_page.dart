import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';
import 'package:mobile_app/features/home/data/models/doctor_model.dart';
import 'package:mobile_app/features/home/data/models/review_model.dart';
import 'package:mobile_app/features/home/data/review_service.dart';

class DentistReviewsPage extends StatefulWidget {
  final DoctorModel doctor;
  const DentistReviewsPage({super.key, required this.doctor});

  @override
  State<DentistReviewsPage> createState() => _DentistReviewsPageState();
}

class _DentistReviewsPageState extends State<DentistReviewsPage> {
  List<ReviewModel> _allReviews = [];
  List<ReviewModel> _reviews = [];
  double _avgRating = 0;
  String _sortBy = 'highest';
  int _selectedStarFilter = 0; // 0 = Tất cả, 5, 4, 3, 2, 1
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadReviews();
  }

  Future<void> _loadReviews() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final result = await ReviewService().getReviewsForDentist(widget.doctor.id);
      final list = List<ReviewModel>.from(result.reviews);
      setState(() {
        _allReviews = list;
        _avgRating = result.averageRating;
        _isLoading = false;
      });
      _applyFilterAndSort();
    } catch (_) {
      setState(() {
        _error = 'load_failed';
        _isLoading = false;
      });
    }
  }

  void _applyFilterAndSort() {
    List<ReviewModel> filtered = _selectedStarFilter == 0
        ? List.from(_allReviews)
        : _allReviews.where((r) => r.rating.round() == _selectedStarFilter).toList();
    _sortList(filtered);
    setState(() {
      _reviews = filtered;
    });
  }

  void _sortList(List<ReviewModel> list) {
    if (_sortBy == 'highest') {
      list.sort((a, b) => b.rating.compareTo(a.rating));
    } else {
      list.sort((a, b) => b.createdAt.compareTo(a.createdAt));
    }
  }

  int get _recommendPercent {
    if (_allReviews.isEmpty) return 0;
    final positive = _allReviews.where((r) => r.rating >= 4).length;
    return ((positive / _allReviews.length) * 100).round();
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final doc = widget.doctor;

    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
          onPressed: () => context.pop(),
        ),
        title: Text(
          isVi ? 'Đánh giá từ bệnh nhân' : 'Patient Reviews',
          style: TextStyle(color: context.textPrimary, fontWeight: FontWeight.w800, fontSize: 18),
        ),
        centerTitle: true,
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(color: context.divider, height: 1),
        ),
      ),
      bottomNavigationBar: Container(
        padding: const EdgeInsets.fromLTRB(24, 12, 24, 24),
        decoration: BoxDecoration(
          color: context.card,
          border: Border(top: BorderSide(color: context.divider)),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: context.isDark ? 0.2 : 0.05),
              blurRadius: 10,
              offset: const Offset(0, -4),
            ),
          ],
        ),
        child: SizedBox(
          width: double.infinity,
          height: 50,
          child: ElevatedButton.icon(
            onPressed: () {
              final doctorInfo = doc.toDoctorInfo();
              final activeDraft = BookingService().activeDraft;
              BookingDraft draft;
              if (activeDraft != null) {
                if (activeDraft.doctor?.id == doc.id) {
                  // Cùng bác sĩ: giữ nguyên toàn bộ (bệnh nhân, dịch vụ, ngày, ca khám, hold 5p)
                  draft = activeDraft.copyWith(doctor: doctorInfo, preferredDentistId: doc.id);
                } else {
                  // Khác bác sĩ: giữ nguyên bệnh nhân và dịch vụ đã chọn, chọn lại ngày và slot khám của bác sĩ mới
                  draft = activeDraft.copyWith(
                    doctor: doctorInfo,
                    preferredDentistId: doc.id,
                    clearDate: true,
                    clearTimeSlot: true,
                    clearHold: true,
                  );
                }
              } else {
                draft = BookingDraft(
                  doctor: doctorInfo,
                  preferredDentistId: doc.id,
                );
              }

              if (draft.isHoldActive && draft.isComplete) {
                context.push(AppRoutes.bookingReview, extra: draft);
              } else if (draft.patient != null && draft.service != null) {
                context.push(AppRoutes.bookingSelectDatetime, extra: draft);
              } else if (draft.patient != null) {
                context.push(AppRoutes.bookingSelectService, extra: draft);
              } else {
                context.push(AppRoutes.bookingSelectPatient, extra: draft);
              }
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.primary,
              elevation: 0,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
            ),
            icon: const Icon(Iconsax.calendar_1, color: Colors.white, size: 18),
            label: Text(
              isVi ? 'Đặt lịch khám với ${doc.fullName}' : 'Book with ${doc.fullName}',
              style: const TextStyle(color: Colors.white, fontSize: 14, fontWeight: FontWeight.w700),
              overflow: TextOverflow.ellipsis,
            ),
          ),
        ),
      ),
      body: SafeArea(
        child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? Center(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          isVi ? 'Không thể tải danh sách đánh giá.' : 'Unable to load reviews.',
                          style: TextStyle(color: context.textSecondary),
                        ),
                        const SizedBox(height: 12),
                        ElevatedButton(
                          onPressed: _loadReviews,
                          child: Text(isVi ? 'Thử lại' : 'Retry'),
                        ),
                      ],
                    ),
                  )
                : CustomScrollView(
                    slivers: [
                      // Header Summary Card
                      SliverToBoxAdapter(
                        child: Padding(
                          padding: const EdgeInsets.all(24),
                          child: Container(
                            padding: const EdgeInsets.all(20),
                            decoration: BoxDecoration(
                              color: context.card,
                              borderRadius: BorderRadius.circular(20),
                              border: Border.all(color: context.divider),
                              boxShadow: [
                                BoxShadow(
                                  color: Colors.black.withValues(alpha: context.isDark ? 0.2 : 0.03),
                                  blurRadius: 10,
                                  offset: const Offset(0, 4),
                                ),
                              ],
                            ),
                            child: Column(
                              children: [
                                Row(
                                  children: [
                                    ClipRRect(
                                      borderRadius: BorderRadius.circular(12),
                                      child: Container(
                                        width: 54,
                                        height: 54,
                                        color: AppColors.primaryLight,
                                        child: doc.profilePictureUrl != null && doc.profilePictureUrl!.isNotEmpty
                                            ? Image.network(
                                                ApiConstants.resolveAssetUrl(doc.profilePictureUrl)!,
                                                fit: BoxFit.cover,
                                                errorBuilder: (_, __, ___) => const Icon(Iconsax.user, color: AppColors.primary, size: 26),
                                              )
                                            : const Icon(Iconsax.user, color: AppColors.primary, size: 26),
                                      ),
                                    ),
                                    const SizedBox(width: 14),
                                    Expanded(
                                      child: Column(
                                        crossAxisAlignment: CrossAxisAlignment.start,
                                        children: [
                                          Text(
                                            doc.fullName,
                                            style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800, color: context.textPrimary),
                                          ),
                                          const SizedBox(height: 2),
                                          Text(
                                            doc.specialty ?? (isVi ? 'Nha sĩ tổng quát' : 'General Dentist'),
                                            style: TextStyle(fontSize: 13, color: context.textSecondary),
                                          ),
                                        ],
                                      ),
                                    ),
                                  ],
                                ),
                                const SizedBox(height: 16),
                                Container(color: context.divider, height: 1),
                                const SizedBox(height: 16),
                                Row(
                                  mainAxisAlignment: MainAxisAlignment.spaceAround,
                                  children: [
                                    Column(
                                      children: [
                                        Text(
                                          _avgRating.toString(),
                                          style: TextStyle(fontSize: 34, fontWeight: FontWeight.w900, color: context.textPrimary),
                                        ),
                                        const SizedBox(height: 4),
                                        Row(
                                          children: List.generate(5, (index) {
                                            final filled = index < _avgRating.round();
                                            return Icon(
                                              Icons.star_rounded,
                                              color: filled ? Colors.amber : (context.isDark ? const Color(0xFF475569) : const Color(0xFFE2E8F0)),
                                              size: 16,
                                            );
                                          }),
                                        ),
                                        const SizedBox(height: 4),
                                        Text(
                                          '${_allReviews.length} ${isVi ? 'Đánh giá' : 'Reviews'}',
                                          style: TextStyle(fontSize: 11, color: context.textMuted, fontWeight: FontWeight.w600),
                                        ),
                                      ],
                                    ),
                                    Container(width: 1, height: 60, color: context.divider),
                                    Column(
                                      children: [
                                        Text(
                                          '$_recommendPercent%',
                                          style: TextStyle(fontSize: 34, fontWeight: FontWeight.w900, color: context.textPrimary),
                                        ),
                                        const SizedBox(height: 4),
                                        Text(
                                          isVi ? 'Bệnh nhân khuyên dùng' : 'Recommend',
                                          style: TextStyle(fontSize: 12, color: context.textSecondary, fontWeight: FontWeight.w600),
                                        ),
                                        const SizedBox(height: 4),
                                        Text(
                                          isVi ? 'Từ 4 sao trở lên' : '4 stars and above',
                                          style: TextStyle(fontSize: 11, color: context.textMuted),
                                        ),
                                      ],
                                    ),
                                  ],
                                ),
                              ],
                            ),
                          ),
                        ),
                      ),

                      // Star Filter Bar & Sort Dropdown
                      SliverToBoxAdapter(
                        child: Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 4),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                isVi ? 'LỌC THEO ĐÁNH GIÁ SAO' : 'FILTER BY STARS',
                                style: TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.w800,
                                  color: context.textSecondary,
                                  letterSpacing: 0.5,
                                ),
                              ),
                              const SizedBox(height: 8),
                              SingleChildScrollView(
                                scrollDirection: Axis.horizontal,
                                child: Row(
                                  children: [
                                    _buildStarFilterChip(0, isVi ? 'Tất cả (${_allReviews.length})' : 'All (${_allReviews.length})'),
                                    const SizedBox(width: 6),
                                    _buildStarFilterChip(5, '5 ⭐'),
                                    const SizedBox(width: 6),
                                    _buildStarFilterChip(4, '4 ⭐'),
                                    const SizedBox(width: 6),
                                    _buildStarFilterChip(3, '3 ⭐'),
                                    const SizedBox(width: 6),
                                    _buildStarFilterChip(2, '2 ⭐'),
                                    const SizedBox(width: 6),
                                    _buildStarFilterChip(1, '1 ⭐'),
                                  ],
                                ),
                              ),
                              const SizedBox(height: 16),
                              Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Text(
                                    isVi ? 'SẮP XẾP THEO' : 'SORT BY',
                                    style: TextStyle(
                                      fontSize: 12,
                                      fontWeight: FontWeight.w800,
                                      color: context.textSecondary,
                                      letterSpacing: 0.5,
                                    ),
                                  ),
                                  Container(
                                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                                    decoration: BoxDecoration(
                                      color: context.card,
                                      borderRadius: BorderRadius.circular(10),
                                      border: Border.all(color: context.divider),
                                    ),
                                    child: DropdownButtonHideUnderline(
                                      child: DropdownButton<String>(
                                        value: _sortBy,
                                        isDense: true,
                                        dropdownColor: context.card,
                                        icon: Icon(Icons.keyboard_arrow_down_rounded, color: context.textSecondary, size: 18),
                                        style: TextStyle(color: context.textPrimary, fontSize: 13, fontWeight: FontWeight.w700),
                                        items: [
                                          DropdownMenuItem(
                                            value: 'highest',
                                            child: Text(isVi ? 'Đánh giá cao nhất' : 'Highest Rated'),
                                          ),
                                          DropdownMenuItem(
                                            value: 'latest',
                                            child: Text(isVi ? 'Gần đây nhất' : 'Most Recent'),
                                          ),
                                        ],
                                        onChanged: (val) {
                                          if (val != null) {
                                            setState(() {
                                              _sortBy = val;
                                              _applyFilterAndSort();
                                            });
                                          }
                                        },
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                            ],
                          ),
                        ),
                      ),

                      // Reviews List
                      _reviews.isEmpty
                          ? SliverFillRemaining(
                              hasScrollBody: false,
                              child: Center(
                                child: Text(
                                  isVi ? 'Không có đánh giá nào phù hợp.' : 'No matching reviews.',
                                  style: TextStyle(color: context.textMuted),
                                ),
                              ),
                            )
                          : SliverPadding(
                              padding: const EdgeInsets.fromLTRB(24, 12, 24, 40),
                              sliver: SliverList(
                                delegate: SliverChildBuilderDelegate(
                                  (context, index) => Padding(
                                    padding: const EdgeInsets.only(bottom: 16),
                                    child: _buildReviewCard(context, _reviews[index], isVi),
                                  ),
                                  childCount: _reviews.length,
                                ),
                              ),
                            ),
                    ],
                  ),
      ),
    );
  }

  Widget _buildStarFilterChip(int stars, String label) {
    final selected = _selectedStarFilter == stars;
    return ChoiceChip(
      label: Text(label),
      selected: selected,
      onSelected: (_) {
        setState(() {
          _selectedStarFilter = stars;
          _applyFilterAndSort();
        });
      },
      selectedColor: AppColors.primary,
      backgroundColor: context.card,
      labelStyle: TextStyle(
        color: selected ? Colors.white : context.textPrimary,
        fontWeight: selected ? FontWeight.w800 : FontWeight.w600,
        fontSize: 12,
      ),
      side: BorderSide(color: selected ? AppColors.primary : context.divider),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
    );
  }

  Widget _buildReviewCard(BuildContext context, ReviewModel review, bool isVi) {
    final initials = review.patientName.trim().split(' ').where((w) => w.isNotEmpty).toList();
    final avatarChar = initials.isEmpty ? 'U' : initials.last[0].toUpperCase();
    final dateText = '${review.createdAt.day.toString().padLeft(2, '0')}/${review.createdAt.month.toString().padLeft(2, '0')}/${review.createdAt.year}';

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: context.divider),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: context.isDark ? 0.12 : 0.02),
            blurRadius: 8,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 20,
                backgroundColor: AppColors.primaryLight,
                child: Text(
                  avatarChar,
                  style: const TextStyle(color: AppColors.primary, fontWeight: FontWeight.w800, fontSize: 14),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      review.patientName,
                      style: TextStyle(fontSize: 14, fontWeight: FontWeight.w700, color: context.textPrimary),
                    ),
                    const SizedBox(height: 2),
                    Row(
                      children: [
                        Row(
                          children: List.generate(5, (index) {
                            final filled = index < review.rating.round();
                            return Icon(
                              Icons.star_rounded,
                              color: filled ? Colors.amber : (context.isDark ? const Color(0xFF475569) : const Color(0xFFE2E8F0)),
                              size: 14,
                            );
                          }),
                        ),
                        const SizedBox(width: 8),
                        Text(dateText, style: TextStyle(fontSize: 11, color: context.textMuted)),
                      ],
                    ),
                    if (review.serviceName != null && review.serviceName!.isNotEmpty) ...[
                      const SizedBox(height: 4),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                        decoration: BoxDecoration(
                          color: AppColors.primaryLight,
                          borderRadius: BorderRadius.circular(6),
                          border: Border.all(color: AppColors.primary.withValues(alpha: 0.2)),
                        ),
                        child: Text(
                          'Dịch vụ: ${review.serviceName}',
                          style: const TextStyle(fontSize: 10, fontWeight: FontWeight.w700, color: AppColors.primary),
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Text(
            review.comment,
            style: TextStyle(fontSize: 13, color: context.textSecondary, height: 1.45),
          ),
          if (review.tags.isNotEmpty) ...[
            const SizedBox(height: 12),
            Wrap(
              spacing: 6,
              runSpacing: 6,
              children: review.tags.map((tag) {
                return Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(tag, style: TextStyle(fontSize: 11, color: context.textSecondary, fontWeight: FontWeight.w600)),
                );
              }).toList(),
            ),
          ],
        ],
      ),
    );
  }
}
