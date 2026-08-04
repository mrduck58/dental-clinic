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

  List<ClinicFeedbackModel> _feedbacks = [];
  ClinicFeedbackModel? _myFeedback;
  bool _isLoading = true;
  bool _hasSubmitted = false;

  @override
  void initState() {
    super.initState();
    _checkSubmittedStatus();
    _loadFeedbacks();
  }

  Future<void> _checkSubmittedStatus() async {
    final prefs = await SharedPreferences.getInstance();
    final localSubmitted = prefs.getBool('has_submitted_clinic_feedback') ?? false;
    final eligibility = await _reviewService.checkClinicEligibility();
    if (!mounted) return;
    setState(() {
      _myFeedback = eligibility.myFeedback;
      _hasSubmitted = localSubmitted || _myFeedback != null || (!eligibility.canReview && eligibility.hasCompletedFirstVisit);
    });
  }

  Future<void> _loadFeedbacks() async {
    setState(() => _isLoading = true);
    try {
      final list = await _reviewService.getFeaturedClinicFeedbacks();
      if (mounted) {
        setState(() {
          _feedbacks = list;
          _isLoading = false;
        });
      }
    } catch (_) {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _onOpenFeedbackForm() async {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    if (_myFeedback != null) {
      _showMyFeedbackDetailsBottomSheet(_myFeedback!);
      return;
    }

    // Check eligibility
    final eligibility = await _reviewService.checkClinicEligibility();
    if (!mounted) return;

    if (eligibility.myFeedback != null) {
      setState(() {
        _myFeedback = eligibility.myFeedback;
        _hasSubmitted = true;
      });
      _showMyFeedbackDetailsBottomSheet(eligibility.myFeedback!);
      return;
    }

    if (!eligibility.canReview) {
      _showNoticeDialog(
        title: isVi ? 'Thông báo' : 'Notice',
        message: eligibility.reason.isNotEmpty
            ? eligibility.reason
            : (isVi
                ? 'Bạn cần hoàn tất ít nhất 1 lần khám hoặc điều trị đầu tiên tại phòng khám để có thể gửi đánh giá.'
                : 'You need to complete at least 1 first visit or treatment to submit clinic feedback.'),
      );
      return;
    }

    _showFeedbackFormBottomSheet();
  }

  void _showNoticeDialog({required String title, required String message}) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        backgroundColor: context.card,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        title: Row(
          children: [
            const Icon(Icons.info_outline_rounded, color: AppColors.primary, size: 24),
            const SizedBox(width: 10),
            Expanded(
              child: Text(
                title,
                style: TextStyle(color: context.textPrimary, fontWeight: FontWeight.bold, fontSize: 16),
              ),
            ),
          ],
        ),
        content: Text(
          message,
          style: TextStyle(color: context.textSecondary, fontSize: 14, height: 1.4),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(),
            child: Text(
              isVi ? 'Đã hiểu' : 'Understood',
              style: const TextStyle(color: AppColors.primary, fontWeight: FontWeight.bold),
            ),
          ),
        ],
      ),
    );
  }

  void _showMyFeedbackDetailsBottomSheet(ClinicFeedbackModel feedback) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final parsed = _ParsedFeedbackComment.parse(feedback.comment);

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) => Container(
        padding: const EdgeInsets.all(24),
        decoration: BoxDecoration(
          color: context.card,
          borderRadius: const BorderRadius.vertical(top: Radius.circular(28)),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Center(
              child: Container(
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: context.divider,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                const Icon(Icons.star_rounded, color: Colors.amber, size: 24),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    isVi ? 'Đánh giá của bạn' : 'Your Review',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: context.textPrimary),
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: AppColors.primaryLight,
                    borderRadius: BorderRadius.circular(999),
                  ),
                  child: Text(
                    isVi ? 'Đã gửi' : 'Submitted',
                    style: const TextStyle(fontSize: 11, fontWeight: FontWeight.bold, color: AppColors.primary),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Row(
              children: List.generate(
                5,
                (s) => Icon(
                  s < feedback.rating ? Icons.star_rounded : Icons.star_outline_rounded,
                  color: Colors.amber,
                  size: 20,
                ),
              ),
            ),
            if (parsed.text.isNotEmpty) ...[
              const SizedBox(height: 12),
              Text(
                parsed.text,
                style: TextStyle(fontSize: 14, color: context.textPrimary, height: 1.45),
              ),
            ],
            if (parsed.tags.isNotEmpty) ...[
              const SizedBox(height: 12),
              Wrap(
                spacing: 6,
                runSpacing: 6,
                children: parsed.tags.map((tag) {
                  return Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Text(
                      tag,
                      style: TextStyle(fontSize: 11, color: context.textSecondary, fontWeight: FontWeight.w600),
                    ),
                  );
                }).toList(),
              ),
            ],
            if (feedback.replyText != null && feedback.replyText!.isNotEmpty) ...[
              const SizedBox(height: 16),
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: AppColors.primaryLight.withValues(alpha: context.isDark ? 0.2 : 0.6),
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: AppColors.primary.withValues(alpha: 0.3)),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        const Icon(Iconsax.messages_2, color: AppColors.primary, size: 18),
                        const SizedBox(width: 8),
                        Text(
                          isVi ? 'Phản hồi từ phòng khám' : 'Clinic Response',
                          style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w800, color: AppColors.primary),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    Text(
                      feedback.replyText!,
                      style: TextStyle(fontSize: 13, color: context.textPrimary, height: 1.45),
                    ),
                  ],
                ),
              ),
            ],
            const SizedBox(height: 24),
            SizedBox(
              width: double.infinity,
              height: 48,
              child: OutlinedButton(
                onPressed: () => Navigator.of(ctx).pop(),
                style: OutlinedButton.styleFrom(
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
                ),
                child: Text(
                  isVi ? 'Đóng' : 'Close',
                  style: TextStyle(fontWeight: FontWeight.bold, color: context.textPrimary),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _showFeedbackFormBottomSheet() {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) => _ClinicFeedbackFormBottomSheet(
        onSubmitted: () async {
          final prefs = await SharedPreferences.getInstance();
          await prefs.setBool('has_submitted_clinic_feedback', true);
          if (!mounted) return;
          setState(() {
            _hasSubmitted = true;
          });
          _loadFeedbacks();
        },
      ),
    );
  }

  double get _avgRating {
    if (_feedbacks.isEmpty) return 5.0;
    final total = _feedbacks.fold<double>(0, (sum, item) => sum + item.rating);
    return double.parse((total / _feedbacks.length).toStringAsFixed(1));
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0,
        surfaceTintColor: Colors.transparent,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
          onPressed: () => context.pop(),
        ),
        title: Text(
          isVi ? 'Đánh giá phòng khám' : 'Clinic Reviews',
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
              child: RefreshIndicator(
                onRefresh: _loadFeedbacks,
                color: AppColors.primary,
                child: SingleChildScrollView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.all(20),
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
                              width: 56,
                              height: 56,
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
                                  Row(
                                    children: [
                                      const Icon(Icons.star_rounded, color: Colors.amber, size: 18),
                                      const SizedBox(width: 4),
                                      Text(
                                        '$_avgRating / 5.0 (${_feedbacks.length} ${isVi ? "đánh giá" : "reviews"})',
                                        style: const TextStyle(fontSize: 13, fontWeight: FontWeight.bold, color: Colors.white),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 24),

                      if (_myFeedback != null) ...[
                        Container(
                          padding: const EdgeInsets.all(16),
                          decoration: BoxDecoration(
                            color: AppColors.primaryLight.withValues(alpha: context.isDark ? 0.15 : 0.4),
                            borderRadius: BorderRadius.circular(16),
                            border: Border.all(color: AppColors.primary.withValues(alpha: 0.4)),
                          ),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                children: [
                                  Row(
                                    children: [
                                      const Icon(Icons.star_rounded, color: Colors.amber, size: 18),
                                      const SizedBox(width: 6),
                                      Text(
                                        isVi ? 'Đánh giá của bạn' : 'Your Review',
                                        style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w800, color: AppColors.primary),
                                      ),
                                    ],
                                  ),
                                  Container(
                                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                                    decoration: BoxDecoration(
                                      color: AppColors.primary,
                                      borderRadius: BorderRadius.circular(999),
                                    ),
                                    child: Text(
                                      isVi ? 'Đã gửi' : 'Submitted',
                                      style: const TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Colors.white),
                                    ),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 8),
                              Row(
                                children: List.generate(
                                  5,
                                  (s) => Icon(
                                    s < _myFeedback!.rating ? Icons.star_rounded : Icons.star_outline_rounded,
                                    color: Colors.amber,
                                    size: 14,
                                  ),
                                ),
                              ),
                              (() {
                                final parsed = _ParsedFeedbackComment.parse(_myFeedback!.comment);
                                return Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    if (parsed.text.isNotEmpty) ...[
                                      const SizedBox(height: 8),
                                      Text(
                                        parsed.text,
                                        style: TextStyle(fontSize: 13, color: context.textPrimary, height: 1.4),
                                      ),
                                    ],
                                    if (parsed.tags.isNotEmpty) ...[
                                      const SizedBox(height: 8),
                                      Wrap(
                                        spacing: 6,
                                        runSpacing: 6,
                                        children: parsed.tags.map((tag) {
                                          return Container(
                                            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                            decoration: BoxDecoration(
                                              color: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
                                              borderRadius: BorderRadius.circular(6),
                                            ),
                                            child: Text(
                                              tag,
                                              style: TextStyle(fontSize: 10, color: context.textSecondary, fontWeight: FontWeight.w600),
                                            ),
                                          );
                                        }).toList(),
                                      ),
                                    ],
                                  ],
                                );
                              })(),
                              if (_myFeedback!.replyText != null && _myFeedback!.replyText!.isNotEmpty) ...[
                                const SizedBox(height: 12),
                                Container(
                                  width: double.infinity,
                                  padding: const EdgeInsets.all(12),
                                  decoration: BoxDecoration(
                                    color: context.card,
                                    borderRadius: BorderRadius.circular(12),
                                    border: Border.all(color: AppColors.primary.withValues(alpha: 0.3)),
                                  ),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Row(
                                        children: [
                                          const Icon(Iconsax.messages_2, color: AppColors.primary, size: 16),
                                          const SizedBox(width: 6),
                                          Text(
                                            isVi ? 'Phản hồi từ phòng khám' : 'Clinic Response',
                                            style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w800, color: AppColors.primary),
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 6),
                                      Text(
                                        _myFeedback!.replyText!,
                                        style: TextStyle(fontSize: 13, color: context.textPrimary, height: 1.4),
                                      ),
                                    ],
                                  ),
                                ),
                              ],
                            ],
                          ),
                        ),
                        const SizedBox(height: 20),
                      ],

                      Text(
                        isVi ? 'Đánh giá từ bệnh nhân khác' : 'Other Patient Reviews',
                        style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800, color: context.textPrimary),
                      ),
                      const SizedBox(height: 14),

                      if (_isLoading)
                        const Center(child: Padding(padding: EdgeInsets.all(32), child: CircularProgressIndicator()))
                      else if (_feedbacks.isEmpty)
                        Center(
                          child: Padding(
                            padding: const EdgeInsets.symmetric(vertical: 40),
                            child: Text(
                              isVi ? 'Chưa có đánh giá nào cho phòng khám.' : 'No clinic reviews yet.',
                              style: TextStyle(color: context.textMuted, fontSize: 14),
                            ),
                          ),
                        )
                      else
                        ListView.separated(
                          shrinkWrap: true,
                          physics: const NeverScrollableScrollPhysics(),
                          itemCount: _feedbacks.length,
                          separatorBuilder: (_, _) => const SizedBox(height: 12),
                          itemBuilder: (context, index) {
                            final item = _feedbacks[index];
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
                                    children: [
                                      CircleAvatar(
                                        radius: 18,
                                        backgroundColor: AppColors.primaryLight,
                                        child: Text(
                                          item.customerName.isNotEmpty ? item.customerName[0].toUpperCase() : 'K',
                                          style: const TextStyle(color: AppColors.primary, fontWeight: FontWeight.bold, fontSize: 14),
                                        ),
                                      ),
                                      const SizedBox(width: 10),
                                      Expanded(
                                        child: Column(
                                          crossAxisAlignment: CrossAxisAlignment.start,
                                          children: [
                                            Text(
                                              item.customerName,
                                              style: TextStyle(fontSize: 14, fontWeight: FontWeight.w700, color: context.textPrimary),
                                            ),
                                            Row(
                                              children: List.generate(
                                                5,
                                                (s) => Icon(
                                                  s < item.rating ? Icons.star_rounded : Icons.star_outline_rounded,
                                                  color: Colors.amber,
                                                  size: 14,
                                                ),
                                              ),
                                            ),
                                          ],
                                        ),
                                      ),
                                    ],
                                  ),
                                   (() {
                                     final parsed = _ParsedFeedbackComment.parse(item.comment);
                                     return Column(
                                       crossAxisAlignment: CrossAxisAlignment.start,
                                       children: [
                                         if (parsed.text.isNotEmpty) ...[
                                           const SizedBox(height: 10),
                                           Text(
                                             parsed.text,
                                             style: TextStyle(fontSize: 13, color: context.textSecondary, height: 1.4),
                                           ),
                                         ],
                                         if (parsed.tags.isNotEmpty) ...[
                                           const SizedBox(height: 10),
                                           Wrap(
                                             spacing: 6,
                                             runSpacing: 6,
                                             children: parsed.tags.map((tag) {
                                               return Container(
                                                 padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                                                 decoration: BoxDecoration(
                                                   color: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9),
                                                   borderRadius: BorderRadius.circular(6),
                                                 ),
                                                 child: Text(
                                                   tag,
                                                   style: TextStyle(
                                                     fontSize: 11,
                                                     color: context.textSecondary,
                                                     fontWeight: FontWeight.w600,
                                                   ),
                                                 ),
                                               );
                                             }).toList(),
                                           ),
                                         ],
                                       ],
                                     );
                                   })(),
                                  if (item.replyText != null && item.replyText!.isNotEmpty) ...[
                                    const SizedBox(height: 12),
                                    Container(
                                      width: double.infinity,
                                      padding: const EdgeInsets.all(12),
                                      decoration: BoxDecoration(
                                        color: AppColors.primaryLight.withValues(alpha: context.isDark ? 0.2 : 0.6),
                                        borderRadius: BorderRadius.circular(12),
                                        border: Border.all(color: AppColors.primary.withValues(alpha: 0.3)),
                                      ),
                                      child: Column(
                                        crossAxisAlignment: CrossAxisAlignment.start,
                                        children: [
                                          Row(
                                            children: [
                                              const Icon(Iconsax.messages_2, color: AppColors.primary, size: 16),
                                              const SizedBox(width: 6),
                                              Text(
                                                isVi ? 'Phản hồi từ phòng khám' : 'Clinic Response',
                                                style: const TextStyle(
                                                  fontSize: 12,
                                                  fontWeight: FontWeight.w800,
                                                  color: AppColors.primary,
                                                ),
                                              ),
                                            ],
                                          ),
                                          const SizedBox(height: 6),
                                          Text(
                                            item.replyText!,
                                            style: TextStyle(
                                              fontSize: 13,
                                              color: context.textPrimary,
                                              height: 1.4,
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
                    ],
                  ),
                ),
              ),
            ),

            // Bottom Action Button
            Container(
              padding: const EdgeInsets.fromLTRB(24, 16, 24, 24),
              decoration: BoxDecoration(
                color: context.card,
                border: Border(top: BorderSide(color: context.divider)),
              ),
              child: SizedBox(
                width: double.infinity,
                height: 52,
                child: ElevatedButton.icon(
                  onPressed: _hasSubmitted ? null : _onOpenFeedbackForm,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    disabledBackgroundColor: context.isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
                    disabledForegroundColor: context.isDark ? Colors.white38 : Colors.black38,
                    elevation: 0,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
                  ),
                  icon: Icon(
                    _hasSubmitted ? Icons.check_circle_outline_rounded : Iconsax.edit,
                    size: 20,
                  ),
                  label: Text(
                    isVi
                        ? (_hasSubmitted ? 'Bạn đã đánh giá phòng khám' : 'Đánh giá phòng khám')
                        : (_hasSubmitted ? 'Already Reviewed Clinic' : 'Write Clinic Review'),
                    style: TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w700,
                      color: _hasSubmitted ? (context.isDark ? Colors.white38 : Colors.black38) : Colors.white,
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
}

class _ClinicFeedbackFormBottomSheet extends StatefulWidget {
  final VoidCallback onSubmitted;

  const _ClinicFeedbackFormBottomSheet({required this.onSubmitted});

  @override
  State<_ClinicFeedbackFormBottomSheet> createState() => _ClinicFeedbackFormBottomSheetState();
}

class _ClinicFeedbackFormBottomSheetState extends State<_ClinicFeedbackFormBottomSheet> {
  final _reviewService = ReviewService();
  final _commentCtrl = TextEditingController();

  double _rating = 5.0;
  final List<String> _selectedTags = [];
  bool _isSubmitting = false;

  @override
  void dispose() {
    _commentCtrl.dispose();
    super.dispose();
  }

  void _toggleTag(String tag) {
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
      final fullComment = _selectedTags.isNotEmpty ? '[${_selectedTags.join(", ")}] $comment' : comment;

      await _reviewService.submitClinicFeedback(
        rating: _rating,
        comment: fullComment,
      );
      if (!mounted) return;
      Navigator.of(context).pop();
      widget.onSubmitted();
      _showSuccessDialog();
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
      builder: (ctx) => Dialog(
        backgroundColor: context.card,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
        insetPadding: const EdgeInsets.symmetric(horizontal: 32),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(24, 32, 24, 24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 68,
                height: 68,
                decoration: const BoxDecoration(color: AppColors.successLight, shape: BoxShape.circle),
                child: const Icon(Iconsax.like_15, color: AppColors.success, size: 34),
              ),
              const SizedBox(height: 20),
              Text(
                isVi ? 'Đánh giá thành công!' : 'Feedback Sent!',
                style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: context.textPrimary),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              Text(
                isVi
                    ? 'Cảm ơn đóng góp của bạn. Ý kiến của bạn giúp phòng khám ngày càng nâng cao chất lượng phục vụ.'
                    : 'Thank you for your review. Your feedback helps us improve our service.',
                style: TextStyle(fontSize: 13, color: context.textSecondary, height: 1.4),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 24),
              SizedBox(
                width: double.infinity,
                height: 48,
                child: ElevatedButton(
                  onPressed: () => Navigator.of(ctx).pop(),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
                  ),
                  child: Text(
                    isVi ? 'Đóng' : 'Close',
                    style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
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

    return Container(
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: const BorderRadius.vertical(top: Radius.circular(28)),
      ),
      padding: EdgeInsets.only(
        left: 24,
        right: 24,
        top: 20,
        bottom: MediaQuery.of(context).viewInsets.bottom + 24,
      ),
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Center(
              child: Container(
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: context.divider,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),
            const SizedBox(height: 20),
            Row(
              children: [
                Expanded(
                  child: Text(
                    isVi ? 'Đánh giá phòng khám DentalCare' : 'Rate DentalCare Clinic',
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: context.textPrimary),
                  ),
                ),
                IconButton(
                  icon: Icon(Icons.close_rounded, color: context.textMuted),
                  onPressed: () => Navigator.of(context).pop(),
                ),
              ],
            ),
            const SizedBox(height: 16),

            // Star Rating Picker
            Center(
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: List.generate(5, (index) {
                  final score = index + 1.0;
                  final active = score <= _rating;
                  return GestureDetector(
                    onTap: () => setState(() => _rating = score),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 6),
                      child: Icon(
                        active ? Icons.star_rounded : Icons.star_outline_rounded,
                        color: active ? Colors.amber : (context.isDark ? const Color(0xFF475569) : const Color(0xFFCBD5E1)),
                        size: 40,
                      ),
                    ),
                  );
                }),
              ),
            ),
            const SizedBox(height: 8),
            Center(
              child: Text(
                _ratingLabel(_rating, isVi),
                style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: AppColors.primary),
              ),
            ),
            const SizedBox(height: 20),

            // Highlight Tags
            Text(
              isVi ? 'Điểm ấn tượng nhất' : 'What impressed you most?',
              style: TextStyle(fontSize: 14, fontWeight: FontWeight.w700, color: context.textPrimary),
            ),
            const SizedBox(height: 10),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: tags.map((tag) {
                final active = _selectedTags.contains(tag);
                return GestureDetector(
                  onTap: () => _toggleTag(tag),
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 7),
                    decoration: BoxDecoration(
                      color: active ? AppColors.primary : context.card,
                      borderRadius: BorderRadius.circular(999),
                      border: Border.all(color: active ? AppColors.primary : context.divider),
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
            const SizedBox(height: 20),

            // Comment Input
            Text(
              isVi ? 'Nội dung nhận xét & góp ý' : 'Comments & Feedback',
              style: TextStyle(fontSize: 14, fontWeight: FontWeight.w700, color: context.textPrimary),
            ),
            const SizedBox(height: 8),
            Container(
              decoration: BoxDecoration(
                color: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF8FAFC),
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: context.divider),
              ),
              child: TextField(
                controller: _commentCtrl,
                maxLines: 4,
                minLines: 3,
                style: TextStyle(fontSize: 14, color: context.textPrimary),
                decoration: InputDecoration(
                  hintText: isVi
                      ? 'Chia sẻ trải nghiệm về cơ sở vật chất, thái độ phục vụ...'
                      : 'Write your experience about facilities, service...',
                  hintStyle: TextStyle(color: context.textMuted, fontSize: 13),
                  contentPadding: const EdgeInsets.all(16),
                  border: InputBorder.none,
                ),
              ),
            ),
            const SizedBox(height: 24),

            SizedBox(
              width: double.infinity,
              height: 50,
              child: ElevatedButton(
                onPressed: _isSubmitting ? null : _submit,
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  elevation: 0,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
                ),
                child: _isSubmitting
                    ? const SizedBox(width: 22, height: 22, child: CircularProgressIndicator(strokeWidth: 2.4, color: Colors.white))
                    : Text(
                        isVi ? 'Gửi đánh giá' : 'Submit Review',
                        style: const TextStyle(color: Colors.white, fontSize: 15, fontWeight: FontWeight.w700),
                      ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ParsedFeedbackComment {
  final List<String> tags;
  final String text;

  const _ParsedFeedbackComment({required this.tags, required this.text});

  factory _ParsedFeedbackComment.parse(String raw) {
    if (raw.startsWith('[')) {
      final closingIndex = raw.indexOf(']');
      if (closingIndex > 0) {
        final tagsStr = raw.substring(1, closingIndex);
        final commentText = raw.substring(closingIndex + 1).trim();
        final tags = tagsStr.split(',').map((e) => e.trim()).where((e) => e.isNotEmpty).toList();
        return _ParsedFeedbackComment(tags: tags, text: commentText);
      }
    }
    return _ParsedFeedbackComment(tags: const [], text: raw);
  }
}
