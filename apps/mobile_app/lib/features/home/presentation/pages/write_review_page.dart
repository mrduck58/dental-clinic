import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/home/data/models/doctor_model.dart';
import 'package:mobile_app/features/home/data/review_service.dart';

class WriteReviewPage extends StatefulWidget {
  final DoctorModel doctor;
  const WriteReviewPage({super.key, required this.doctor});

  @override
  State<WriteReviewPage> createState() => _WriteReviewPageState();
}

class _WriteReviewPageState extends State<WriteReviewPage> {
  final _reviewService = ReviewService();
  final _commentCtrl = TextEditingController();

  double _rating = 0.0;
  final List<String> _selectedTags = [];
  bool _isSubmitting = false;
  bool _isCheckingEligibility = true;
  bool _canReview = true;
  String _ineligibilityReason = '';

  @override
  void initState() {
    super.initState();
    _checkEligibility();
  }

  Future<void> _checkEligibility() async {
    final result = await _reviewService.checkEligibility(widget.doctor.id);
    if (!mounted) return;
    setState(() {
      _canReview = result.canReview;
      _ineligibilityReason = result.reason;
      _isCheckingEligibility = false;
      if (result.myReview != null) {
        _rating = result.myReview!.rating;
        _commentCtrl.text = result.myReview!.comment;
        _selectedTags.clear();
        _selectedTags.addAll(result.myReview!.tags);
      }
    });
  }

  @override
  void dispose() {
    _commentCtrl.dispose();
    super.dispose();
  }

  void _toggleTag(String tag) {
    if (!_canReview) return;
    setState(() {
      if (_selectedTags.contains(tag)) {
        _selectedTags.remove(tag);
      } else {
        _selectedTags.add(tag);
      }
    });
  }

  Future<void> _submit() async {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final comment = _commentCtrl.text.trim();

    if (!_canReview) {
      _showSnackbar(_ineligibilityReason.isNotEmpty
          ? _ineligibilityReason
          : (isVi
              ? 'Bạn chỉ có thể đánh giá nha sĩ sau khi hoàn tất cuộc khám.'
              : 'You can only review after completing a visit.'));
      return;
    }
    if (_rating == 0.0) {
      _showSnackbar(isVi ? 'Vui lòng chọn số sao đánh giá (1 - 5 sao).' : 'Please select a star rating (1-5 stars).');
      return;
    }
    if (comment.isEmpty) {
      _showSnackbar(isVi ? 'Vui lòng nhập nội dung đánh giá.' : 'Please enter your review content.');
      return;
    }

    setState(() => _isSubmitting = true);
    try {
      await _reviewService.submitReview(
        dentistId: widget.doctor.id,
        rating: _rating,
        comment: comment,
        tags: _selectedTags,
      );
      if (!mounted) return;
      _showSuccessDialog();
    } on DioException catch (e) {
      if (!mounted) return;
      _showSnackbar(ApiClient.errorMessage(e));
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }

  void _showSnackbar(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        backgroundColor: AppColors.primary,
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
    );
  }

  void _showSuccessDialog() {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    showDialog(
      context: context,
      barrierDismissible: false,
      barrierColor: Colors.black.withValues(alpha: 0.5),
      builder: (ctx) {
        return Dialog(
          backgroundColor: context.card,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
          insetPadding: const EdgeInsets.symmetric(horizontal: 32),
          child: Padding(
            padding: const EdgeInsets.fromLTRB(24, 32, 24, 24),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Container(
                  width: 72,
                  height: 72,
                  decoration: BoxDecoration(
                    color: AppColors.successLight,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(Iconsax.like_15, color: AppColors.success, size: 36),
                ),
                const SizedBox(height: 20),
                Text(
                  isVi ? 'Gửi đánh giá thành công!' : 'Review Submitted!',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: context.textPrimary),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 8),
                Text(
                  isVi
                      ? 'Cảm ơn đóng góp của bạn. Ý kiến của bạn giúp chúng tôi cải thiện chất lượng phục vụ tốt hơn.'
                      : 'Thank you for your feedback. Your review helps us improve our service quality.',
                  style: TextStyle(fontSize: 13, color: context.textSecondary, height: 1.4),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 24),
                SizedBox(
                  width: double.infinity,
                  height: 48,
                  child: ElevatedButton(
                    onPressed: () {
                      Navigator.of(ctx).pop();
                      context.pop(true);
                    },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.primary,
                      elevation: 0,
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
                    ),
                    child: Text(
                      isVi ? 'Đóng' : 'Close',
                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 14),
                    ),
                  ),
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final doc = widget.doctor;

    final tags = isVi
        ? [
            'Bác sĩ tận tâm',
            'Thao tác nhẹ nhàng',
            'Không đau',
            'Tư vấn kỹ lưỡng',
            'Chuyên môn cao',
            'Thái độ ân cần',
            'Lắng nghe bệnh nhân',
            'Giải thích dễ hiểu'
          ]
        : [
            'Attentive Doctor',
            'Gentle Touch',
            'Painless',
            'Thorough Consultation',
            'Highly Skilled',
            'Caring Attitude',
            'Good Listener',
            'Clear Explanation'
          ];

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
          isVi ? 'Đánh giá nha sĩ' : 'Rate Dentist',
          style: TextStyle(color: context.textPrimary, fontWeight: FontWeight.w800, fontSize: 18),
        ),
        centerTitle: true,
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(color: context.divider, height: 1),
        ),
      ),
      body: SafeArea(
        child: Column(
          children: [
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Doctor Details Row
                    Row(
                      children: [
                        Container(
                          width: 64,
                          height: 64,
                          decoration: BoxDecoration(
                            shape: BoxShape.circle,
                            border: Border.all(color: context.divider, width: 2),
                          ),
                          child: ClipOval(
                            child: doc.profilePictureUrl != null
                                ? Image.network(
                                    doc.profilePictureUrl!,
                                    fit: BoxFit.cover,
                                    errorBuilder: (_, _, _) => _placeholderAvatar(),
                                  )
                                : _placeholderAvatar(),
                          ),
                        ),
                        const SizedBox(width: 16),
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
                                doc.specialty ?? (isVi ? 'Nha sĩ chuyên khoa' : 'Specialist Dentist'),
                                style: TextStyle(fontSize: 12, color: context.textSecondary, fontWeight: FontWeight.w500),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 28),
                    Divider(height: 1, color: context.divider),
                    const SizedBox(height: 24),

                    if (!_canReview) ...[
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: context.isDark ? const Color(0xFF332A15) : const Color(0xFFFEF3C7),
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(
                            color: context.isDark ? const Color(0xFF78350F) : const Color(0xFFFDE68A),
                          ),
                        ),
                        child: Row(
                          children: [
                            const Icon(Icons.info_outline_rounded, color: Color(0xFFD97706), size: 22),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Text(
                                _ineligibilityReason.isNotEmpty
                                    ? _ineligibilityReason
                                    : (isVi
                                        ? 'Bạn chỉ có thể đánh giá nha sĩ sau khi hoàn tất cuộc khám với nha sĩ này.'
                                        : 'You can only review a dentist after completing a visit with them.'),
                                style: TextStyle(
                                  fontSize: 13,
                                  fontWeight: FontWeight.w600,
                                  color: context.isDark ? const Color(0xFFFDE68A) : const Color(0xFF92400E),
                                  height: 1.4,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 24),
                    ],

                    // Star Rating Picker
                    Center(
                      child: Text(
                        isVi ? 'Bạn đánh giá thế nào về tay nghề & thái độ của nha sĩ?' : 'How would you rate the dentist\'s service & skill?',
                        style: TextStyle(fontSize: 14, fontWeight: FontWeight.w700, color: context.textPrimary),
                        textAlign: TextAlign.center,
                      ),
                    ),
                    const SizedBox(height: 14),
                    Center(
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: List.generate(5, (index) {
                          final score = index + 1.0;
                          final active = score <= _rating;
                          return GestureDetector(
                            onTap: _canReview ? () => setState(() => _rating = score) : null,
                            child: Padding(
                              padding: const EdgeInsets.symmetric(horizontal: 6),
                              child: Icon(
                                active ? Icons.star_rounded : Icons.star_outline_rounded,
                                color: active ? Colors.amber : (context.isDark ? const Color(0xFF475569) : const Color(0xFFCBD5E1)),
                                size: 44,
                              ),
                            ),
                          );
                        }),
                      ),
                    ),
                    const SizedBox(height: 28),

                    // Comment Input
                    Text(
                      isVi ? 'Chia sẻ chi tiết về nha sĩ' : 'Share Details About the Dentist',
                      style: TextStyle(fontSize: 14, fontWeight: FontWeight.w700, color: context.textPrimary),
                    ),
                    const SizedBox(height: 10),
                    Container(
                      decoration: BoxDecoration(
                        color: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF8FAFC),
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: context.divider),
                      ),
                      child: TextField(
                        controller: _commentCtrl,
                        enabled: _canReview,
                        maxLines: 6,
                        minLines: 4,
                        style: TextStyle(fontSize: 14, color: context.textPrimary),
                        decoration: InputDecoration(
                          hintText: isVi
                              ? 'Nhận xét về tay nghề, thái độ giao tiếp, mức độ nhẹ nhàng khi thao tác...'
                              : 'Share about doctor skill, attitude, gentleness during treatment...',
                          hintStyle: TextStyle(color: context.textMuted, fontSize: 13, height: 1.4),
                          contentPadding: const EdgeInsets.all(16),
                          border: InputBorder.none,
                        ),
                      ),
                    ),
                    const SizedBox(height: 28),

                    // Tags
                    Text(
                      isVi ? 'Chọn nhãn đánh giá nổi bật' : 'Select Highlight Tags',
                      style: TextStyle(fontSize: 14, fontWeight: FontWeight.w700, color: context.textPrimary),
                    ),
                    const SizedBox(height: 12),
                    Wrap(
                      spacing: 8,
                      runSpacing: 10,
                      children: tags.map((tag) {
                        final active = _selectedTags.contains(tag);
                        return GestureDetector(
                          onTap: () => _toggleTag(tag),
                          child: AnimatedContainer(
                            duration: const Duration(milliseconds: 180),
                            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
                            decoration: BoxDecoration(
                              color: active ? AppColors.primary : context.card,
                              borderRadius: BorderRadius.circular(999),
                              border: Border.all(
                                color: active ? AppColors.primary : context.divider,
                              ),
                            ),
                            child: Text(
                              tag,
                              style: TextStyle(
                                fontSize: 12,
                                fontWeight: FontWeight.w700,
                                color: active ? Colors.white : context.textSecondary,
                              ),
                            ),
                          ),
                        );
                      }).toList(),
                    ),
                    const SizedBox(height: 24),
                  ],
                ),
              ),
            ),

            // Submit Button
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
                  onPressed: (_isSubmitting || !_canReview) ? null : _submit,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    disabledBackgroundColor: context.isDark ? const Color(0xFF334155) : const Color(0xFFCBD5E1),
                    elevation: 0,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
                  ),
                  child: _isSubmitting
                      ? const SizedBox(
                          width: 22,
                          height: 22,
                          child: CircularProgressIndicator(strokeWidth: 2.4, color: Colors.white),
                        )
                      : Text(
                          isVi ? 'Gửi đánh giá' : 'Submit Review',
                          style: TextStyle(
                            color: _canReview ? Colors.white : (context.isDark ? Colors.white38 : Colors.black38),
                            fontSize: 15,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _placeholderAvatar() {
    return Container(
      color: AppColors.primaryLight,
      child: const Icon(Iconsax.user, color: AppColors.primary, size: 34),
    );
  }
}
