import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/home/data/models/review_model.dart';
import 'package:mobile_app/features/home/data/review_service.dart';

import 'package:shared_preferences/shared_preferences.dart';

class ClinicFeedbackPage extends StatefulWidget {
  const ClinicFeedbackPage({super.key});

  @override
  State<ClinicFeedbackPage> createState() => _ClinicFeedbackPageState();
}

class _ClinicFeedbackPageState extends State<ClinicFeedbackPage> {
  final _reviewService = ReviewService();
  final _commentCtrl = TextEditingController();

  double _rating = 5.0;
  final List<String> _selectedTags = [];
  bool _isSubmitting = false;
  bool _hasSubmitted = false;

  List<ClinicFeedbackModel> _featuredFeedbacks = [];
  bool _isLoadingFeatured = true;

  @override
  void initState() {
    super.initState();
    _checkSubmittedStatus();
    _loadFeaturedFeedbacks();
  }

  Future<void> _checkSubmittedStatus() async {
    final prefs = await SharedPreferences.getInstance();
    final submitted = prefs.getBool('has_submitted_clinic_feedback') ?? false;
    if (!mounted) return;
    setState(() {
      _hasSubmitted = submitted;
    });
  }

  @override
  void dispose() {
    _commentCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadFeaturedFeedbacks() async {
    try {
      final list = await _reviewService.getFeaturedClinicFeedbacks();
      if (mounted) {
        setState(() {
          _featuredFeedbacks = list;
          _isLoadingFeatured = false;
        });
      }
    } catch (_) {
      if (mounted) setState(() => _isLoadingFeatured = false);
    }
  }

  void _toggleTag(String tag) {
    if (_hasSubmitted) return;
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

    if (_hasSubmitted) {
      _showSnackbar(isVi ? 'Bạn đã gửi đánh giá trước đó.' : 'You have already submitted feedback.');
      return;
    }
    if (_rating == 0.0) {
      _showSnackbar(isVi ? 'Vui lòng chọn số sao đánh giá (1 - 5 sao).' : 'Please select a rating (1-5 stars).');
      return;
    }
    if (comment.isEmpty) {
      _showSnackbar(isVi ? 'Vui lòng nhập nội dung đánh giá / góp ý.' : 'Please enter your feedback comment.');
      return;
    }

    setState(() => _isSubmitting = true);
    try {
      final fullComment = _selectedTags.isNotEmpty
          ? '[${_selectedTags.join(", ")}] $comment'
          : comment;

      await _reviewService.submitClinicFeedback(
        rating: _rating,
        comment: fullComment,
      );
      final prefs = await SharedPreferences.getInstance();
      await prefs.setBool('has_submitted_clinic_feedback', true);
      if (!mounted) return;
      setState(() => _hasSubmitted = true);
      _showSuccessDialog();
      _loadFeaturedFeedbacks();
    } on DioException catch (e) {
      if (!mounted) return;
      _showSnackbar(ApiClient.errorMessage(e));
    } catch (e) {
      if (!mounted) return;
      _showSnackbar(isVi ? 'Gửi đánh giá thất bại.' : 'Failed to submit feedback.');
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
                  decoration: const BoxDecoration(
                    color: AppColors.successLight,
                    shape: BoxShape.circle,
                  ),
                  child: const Icon(Iconsax.like_15, color: AppColors.success, size: 36),
                ),
                const SizedBox(height: 20),
                Text(
                  isVi ? 'Đóng góp ý kiến thành công!' : 'Feedback Sent Successfully!',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: context.textPrimary),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 8),
                Text(
                  isVi
                      ? 'Cảm ơn bạn đã dành thời gian đánh giá phòng khám DentalCare. Ý kiến của bạn giúp chúng tôi không ngừng hoàn thiện dịch vụ.'
                      : 'Thank you for rating DentalCare Clinic. Your feedback helps us continuously improve our service.',
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

  String _ratingLabel(double rating, bool isVi) {
    switch (rating.round()) {
      case 1:
        return isVi ? 'Rất không hài lòng 😞' : 'Very Dissatisfied 😞';
      case 2:
        return isVi ? 'Chưa hài lòng 🙁' : 'Dissatisfied 🙁';
      case 3:
        return isVi ? 'Bình thường 😐' : 'Average 😐';
      case 4:
        return isVi ? 'Hài lòng 😊' : 'Satisfied 😊';
      case 5:
      default:
        return isVi ? 'Rất tuyệt vời! 😍' : 'Excellent! 😍';
    }
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    final tags = isVi
        ? [
            'Cơ sở hiện đại',
            'Vệ sinh sạch sẽ',
            'Lễ tân chu đáo',
            'Nhân viên thân thiện',
            'Không phải chờ lâu',
            'Chi phí minh bạch',
            'Trang thiết bị mới',
            'Bãi xe tiện lợi'
          ]
        : [
            'Modern Facility',
            'Clean & Hygienic',
            'Welcoming Reception',
            'Friendly Staff',
            'Short Wait Time',
            'Transparent Pricing',
            'New Equipment',
            'Easy Parking'
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
          isVi ? 'Đánh giá phòng khám' : 'Clinic Review',
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
                    // Header Banner Card
                    Container(
                      padding: const EdgeInsets.all(20),
                      decoration: BoxDecoration(
                        gradient: LinearGradient(
                          colors: context.isDark
                              ? [const Color(0xFF1E293B), const Color(0xFF0F172A)]
                              : [AppColors.primary, const Color(0xFF1D4ED8)],
                          begin: Alignment.topLeft,
                          end: Alignment.bottomRight,
                        ),
                        borderRadius: BorderRadius.circular(20),
                        boxShadow: [
                          BoxShadow(
                            color: AppColors.primary.withValues(alpha: 0.3),
                            blurRadius: 16,
                            offset: const Offset(0, 6),
                          ),
                        ],
                      ),
                      child: Row(
                        children: [
                          Container(
                            width: 52,
                            height: 52,
                            decoration: BoxDecoration(
                              color: Colors.white.withValues(alpha: 0.2),
                              shape: BoxShape.circle,
                            ),
                            child: const Icon(Iconsax.hospital, color: Colors.white, size: 28),
                          ),
                          const SizedBox(width: 16),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  isVi ? 'Nha khoa DentalCare' : 'DentalCare Clinic',
                                  style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: Colors.white),
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  isVi
                                      ? 'Trải nghiệm của bạn là kim chỉ nam giúp chúng tôi nâng cao chất lượng.'
                                      : 'Your rating guides us to constantly improve service quality.',
                                  style: TextStyle(fontSize: 12, color: Colors.white.withValues(alpha: 0.85), height: 1.35),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 28),

                    if (_hasSubmitted) ...[
                      Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: AppColors.successLight,
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: AppColors.success.withValues(alpha: 0.3)),
                        ),
                        child: Row(
                          children: [
                            const Icon(Icons.check_circle_rounded, color: AppColors.success, size: 22),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Text(
                                isVi
                                    ? 'Bạn đã gửi đánh giá cho phòng khám. Cảm ơn sự đóng góp quý báu của bạn!'
                                    : 'You have submitted your clinic feedback. Thank you for your review!',
                                style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: AppColors.success, height: 1.4),
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
                        isVi ? 'Đánh giá mức độ hài lòng tổng thể' : 'Rate Overall Satisfaction',
                        style: TextStyle(fontSize: 15, fontWeight: FontWeight.w800, color: context.textPrimary),
                      ),
                    ),
                    const SizedBox(height: 12),
                    Center(
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: List.generate(5, (index) {
                          final score = index + 1.0;
                          final active = score <= _rating;
                          return GestureDetector(
                            onTap: _hasSubmitted ? null : () => setState(() => _rating = score),
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
                    const SizedBox(height: 8),
                    Center(
                      child: AnimatedSwitcher(
                        duration: const Duration(milliseconds: 200),
                        child: Text(
                          _ratingLabel(_rating, isVi),
                          key: ValueKey(_rating),
                          style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: AppColors.primary),
                        ),
                      ),
                    ),
                    const SizedBox(height: 28),

                    // Highlight Tags
                    Text(
                      isVi ? 'Điểm nổi bật bạn ấn tượng nhất' : 'What impressed you the most?',
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
                    const SizedBox(height: 28),

                    // Comment Input
                    Text(
                      isVi ? 'Nội dung phản hồi & đóng góp ý kiến' : 'Feedback & Suggestions',
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
                        enabled: !_hasSubmitted,
                        maxLines: 5,
                        minLines: 4,
                        style: TextStyle(fontSize: 14, color: context.textPrimary),
                        decoration: InputDecoration(
                          hintText: isVi
                              ? 'Nhập nhận xét chi tiết về cơ sở vật chất, thái độ phục vụ, quy trình khám hoặc điều cần cải thiện...'
                              : 'Write detailed feedback on facilities, staff attitude, procedures...',
                          hintStyle: TextStyle(color: context.textMuted, fontSize: 13, height: 1.4),
                          contentPadding: const EdgeInsets.all(16),
                          border: InputBorder.none,
                        ),
                      ),
                    ),
                    const SizedBox(height: 32),

                    // Featured Feedback List
                    if (!_isLoadingFeatured && _featuredFeedbacks.isNotEmpty) ...[
                      Row(
                        children: [
                          const Icon(Iconsax.star_1, color: Colors.amber, size: 20),
                          const SizedBox(width: 8),
                          Text(
                            isVi ? 'Đánh giá nổi bật từ bệnh nhân' : 'Featured Patient Reviews',
                            style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800, color: context.textPrimary),
                          ),
                        ],
                      ),
                      const SizedBox(height: 14),
                      ListView.separated(
                        shrinkWrap: true,
                        physics: const NeverScrollableScrollPhysics(),
                        itemCount: _featuredFeedbacks.length > 5 ? 5 : _featuredFeedbacks.length,
                        separatorBuilder: (_, _) => const SizedBox(height: 12),
                        itemBuilder: (context, index) {
                          final item = _featuredFeedbacks[index];
                          return Container(
                            padding: const EdgeInsets.all(16),
                            decoration: BoxDecoration(
                              color: context.card,
                              borderRadius: BorderRadius.circular(16),
                              border: Border.all(color: context.divider),
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Row(
                                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                  children: [
                                    Text(
                                      item.customerName,
                                      style: TextStyle(fontSize: 14, fontWeight: FontWeight.w700, color: context.textPrimary),
                                    ),
                                    Row(
                                      children: List.generate(5, (starIdx) {
                                        final active = starIdx < item.rating.round();
                                        return Icon(
                                          Icons.star_rounded,
                                          color: active ? Colors.amber : (context.isDark ? const Color(0xFF475569) : const Color(0xFFE2E8F0)),
                                          size: 14,
                                        );
                                      }),
                                    ),
                                  ],
                                ),
                                const SizedBox(height: 8),
                                Text(
                                  item.comment,
                                  style: TextStyle(fontSize: 13, color: context.textSecondary, height: 1.4),
                                ),
                                if (item.replyText != null && item.replyText!.isNotEmpty) ...[
                                  const SizedBox(height: 10),
                                  Container(
                                    padding: const EdgeInsets.all(12),
                                    decoration: BoxDecoration(
                                      color: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
                                      borderRadius: BorderRadius.circular(12),
                                      border: Border.all(color: context.divider),
                                    ),
                                    child: Row(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        const Icon(Iconsax.message_text, color: AppColors.primary, size: 16),
                                        const SizedBox(width: 8),
                                        Expanded(
                                          child: Text(
                                            'Phòng khám: ${item.replyText}',
                                            style: TextStyle(fontSize: 12, color: context.textPrimary, fontWeight: FontWeight.w500),
                                          ),
                                        ),
                                      ],
                                    ),
                                  ),
                                ],
                              ],
                            ),
                          );
                        },
                      ),
                      const SizedBox(height: 24),
                    ],
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
                  onPressed: _isSubmitting ? null : _submit,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
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
                          isVi ? 'Gửi đánh giá phòng khám' : 'Submit Clinic Review',
                          style: const TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w700),
                        ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
