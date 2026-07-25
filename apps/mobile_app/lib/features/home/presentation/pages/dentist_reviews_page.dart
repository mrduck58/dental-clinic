import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
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
  List<ReviewModel> _reviews = [];
  double _avgRating = 0;
  String _sortBy = 'highest';
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
      _sortList(list);
      setState(() {
        _reviews = list;
        _avgRating = result.averageRating;
        _isLoading = false;
      });
    } catch (_) {
      setState(() {
        _error = 'load_failed';
        _isLoading = false;
      });
    }
  }

  void _sortList(List<ReviewModel> list) {
    if (_sortBy == 'highest') {
      list.sort((a, b) => b.rating.compareTo(a.rating));
    } else {
      list.sort((a, b) => b.createdAt.compareTo(a.createdAt));
    }
  }

  int get _recommendPercent {
    if (_reviews.isEmpty) return 0;
    final positive = _reviews.where((r) => r.rating >= 4).length;
    return ((positive / _reviews.length) * 100).round();
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
      body: SafeArea(
        child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? Center(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(
                          isVi ? 'Không thể tải đánh giá.' : 'Failed to load reviews.',
                          style: TextStyle(color: context.textMuted),
                        ),
                        const SizedBox(height: 12),
                        TextButton(onPressed: _loadReviews, child: Text(isVi ? 'Thử lại' : 'Retry')),
                      ],
                    ),
                  )
                : Column(
          children: [
            Expanded(
              child: CustomScrollView(
                slivers: [
                  // Rating Overview
                  SliverToBoxAdapter(
                    child: Padding(
                      padding: const EdgeInsets.fromLTRB(24, 24, 24, 16),
                      child: Container(
                        padding: const EdgeInsets.all(20),
                        decoration: BoxDecoration(
                          color: context.card,
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(color: context.divider),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withValues(alpha: context.isDark ? 0.15 : 0.03),
                              blurRadius: 12,
                              offset: const Offset(0, 4),
                            ),
                          ],
                        ),
                        child: Column(
                          children: [
                            Text(
                              doc.fullName,
                              style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: context.textPrimary),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              doc.specialty ?? (isVi ? 'Nha sĩ chuyên khoa' : 'Specialist Dentist'),
                              style: TextStyle(fontSize: 13, color: context.textSecondary, fontWeight: FontWeight.w500),
                            ),
                            const SizedBox(height: 16),
                            Divider(height: 1, color: context.divider),
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
                                      '${_reviews.length} ${isVi ? 'Đánh giá' : 'Reviews'}',
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

                  // Sort Dropdown
                  SliverToBoxAdapter(
                    child: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 8),
                      child: Row(
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
                                      _sortList(_reviews);
                                    });
                                  }
                                },
                              ),
                            ),
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
                              isVi ? 'Chưa có đánh giá nào cho nha sĩ này.' : 'No reviews yet for this dentist.',
                              style: TextStyle(color: context.textMuted),
                            ),
                          ),
                        )
                      : SliverPadding(
                          padding: const EdgeInsets.fromLTRB(24, 8, 24, 40),
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

            // Write Review Button
            Container(
              padding: const EdgeInsets.fromLTRB(24, 16, 24, 24),
              decoration: BoxDecoration(
                color: context.card,
                border: Border(top: BorderSide(color: context.divider)),
              ),
              child: SizedBox(
                width: double.infinity,
                height: 52,
                child: ElevatedButton(
                  onPressed: () async {
                    final added = await context.push(AppRoutes.writeReview, extra: doc);
                    if (added == true) _loadReviews();
                  },
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    elevation: 0,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
                  ),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Iconsax.edit, color: Colors.white, size: 20),
                      const SizedBox(width: 8),
                      Text(
                        isVi ? 'Viết đánh giá của bạn' : 'Write a Review',
                        style: const TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w700),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
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
