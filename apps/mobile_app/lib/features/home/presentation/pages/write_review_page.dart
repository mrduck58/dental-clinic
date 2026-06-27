import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/home/data/models/doctor_model.dart';
import 'package:mobile_app/features/home/data/review_service.dart';

class WriteReviewPage extends StatefulWidget {
  final DoctorModel doctor;
  const WriteReviewPage({super.key, required this.doctor});

  @override
  State<WriteReviewPage> createState() => _WriteReviewPageState();
}

class _WriteReviewPageState extends State<WriteReviewPage> {
  final _auth = AuthService();
  final _reviewService = ReviewService();
  final _commentCtrl = TextEditingController();

  double _rating = 0.0;
  final List<String> _selectedTags = [];
  String _patientName = '';

  @override
  void initState() {
    super.initState();
    _loadPatientName();
  }

  @override
  void dispose() {
    _commentCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadPatientName() async {
    final name = await _auth.getUserName();
    if (mounted) {
      setState(() {
        _patientName = name ?? 'Patient';
      });
    }
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

  void _submit() {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final comment = _commentCtrl.text.trim();

    if (_rating == 0.0) {
      _showSnackbar(isVi ? 'Vui lòng chọn số sao đánh giá (1 - 5 sao).' : 'Please select a star rating (1-5 stars).');
      return;
    }
    if (comment.isEmpty) {
      _showSnackbar(isVi ? 'Vui lòng nhập nội dung đánh giá.' : 'Please enter your review content.');
      return;
    }

    _reviewService.addReview(
      dentistId: widget.doctor.id,
      rating: _rating,
      comment: comment,
      tags: _selectedTags,
      patientName: _patientName,
    );

    _showSuccessDialog();
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
        ? ['Không đau', 'Bác sĩ thân thiện', 'Cơ sở sạch sẽ', 'Chi phí hợp lý', 'Chuyên nghiệp', 'Nhiệt tình', 'Nhẹ nhàng']
        : ['Painless', 'Friendly Doctor', 'Clean Facility', 'Affordable', 'Professional', 'Attentive', 'Gentle'];

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
          isVi ? 'Viết đánh giá' : 'Write a Review',
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

                    // Star Rating Picker
                    Center(
                      child: Text(
                        isVi ? 'Bạn đánh giá thế nào về trải nghiệm của mình?' : 'How would you rate your experience?',
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
                            onTap: () => setState(() => _rating = score),
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
                      isVi ? 'Chia sẻ chi tiết trải nghiệm khám' : 'Share Your Experience',
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
                        maxLines: 6,
                        minLines: 4,
                        style: TextStyle(fontSize: 14, color: context.textPrimary),
                        decoration: InputDecoration(
                          hintText: isVi
                              ? 'Hãy chia sẻ về thái độ phục vụ, mức độ hài lòng, thời gian chờ khám...'
                              : 'Share about service quality, satisfaction level, waiting time...',
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
                  onPressed: _submit,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    elevation: 0,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
                  ),
                  child: Text(
                    isVi ? 'Gửi đánh giá' : 'Submit Review',
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

  Widget _placeholderAvatar() {
    return Container(
      color: AppColors.primaryLight,
      child: const Icon(Iconsax.user, color: AppColors.primary, size: 34),
    );
  }
}
