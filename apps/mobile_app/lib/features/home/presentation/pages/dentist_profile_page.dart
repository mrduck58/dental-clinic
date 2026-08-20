import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';
import 'package:mobile_app/features/home/data/home_service.dart';
import 'package:mobile_app/features/home/data/models/dentist_detail_model.dart';
import 'package:mobile_app/features/home/data/models/doctor_model.dart';
import 'package:mobile_app/features/home/data/models/review_model.dart';
import 'package:mobile_app/features/home/data/review_service.dart';

class DentistProfilePage extends StatefulWidget {
  final DoctorModel doctor;
  const DentistProfilePage({super.key, required this.doctor});

  @override
  State<DentistProfilePage> createState() => _DentistProfilePageState();
}

class _DentistProfilePageState extends State<DentistProfilePage> {
  DentistDetailModel? _detail;
  double _avgRating = 0;
  int _reviewsCount = 0;
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _isLoading = true);
    try {
      final results = await Future.wait([
        HomeService().getDentistDetail(widget.doctor.id),
        ReviewService().getReviewsForDentist(widget.doctor.id),
      ]);
      if (!mounted) return;
      final detail = results[0] as DentistDetailModel;
      final reviews = results[1] as DentistReviewsResult;
      setState(() {
        _detail = detail;
        _avgRating = reviews.averageRating;
        _reviewsCount = reviews.reviewCount;
        _isLoading = false;
      });
    } catch (_) {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final doc = widget.doctor;
    final detail = _detail;
    final expYears = detail?.yearsOfExperience ?? doc.yearsOfExperience;
    final specialtyStr = (detail?.specialty ?? doc.specialty ?? (isVi ? 'Nha sĩ tổng quát' : 'General Dentist')).toUpperCase();

    final bioText = detail?.bio ??
        doc.bio ??
        (isVi ? 'Chưa có thông tin giới thiệu.' : 'No biography available yet.');

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
          isVi ? 'Hồ sơ nha sĩ' : 'Dentist Profile',
          style: TextStyle(color: context.textPrimary, fontWeight: FontWeight.w800, fontSize: 20),
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
            : Column(
          children: [
            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Doctor Image & Interactive Rating Badge Button
                    Center(
                      child: Stack(
                        alignment: Alignment.bottomCenter,
                        children: [
                          Container(
                            width: 140,
                            height: 140,
                            margin: const EdgeInsets.only(bottom: 16),
                            decoration: BoxDecoration(
                              shape: BoxShape.circle,
                              border: Border.all(color: context.card, width: 4),
                              boxShadow: [
                                BoxShadow(
                                  color: Colors.black.withValues(alpha: 0.1),
                                  blurRadius: 16,
                                  offset: const Offset(0, 6),
                                ),
                              ],
                            ),
                            child: ClipOval(
                              child: doc.profilePictureUrl != null
                                  ? Image.network(
                                      ApiConstants.resolveAssetUrl(doc.profilePictureUrl)!,
                                      fit: BoxFit.cover,
                                      errorBuilder: (_, _, _) => _placeholderAvatar(),
                                    )
                                  : _placeholderAvatar(),
                            ),
                          ),
                          Positioned(
                            bottom: 0,
                            child: GestureDetector(
                              onTap: () async {
                                await context.push(AppRoutes.dentistReviews, extra: doc);
                                _load();
                              },
                              child: Container(
                                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                                decoration: BoxDecoration(
                                  color: AppColors.primary,
                                  borderRadius: BorderRadius.circular(999),
                                  border: Border.all(color: context.card, width: 2),
                                  boxShadow: [
                                    BoxShadow(
                                      color: AppColors.primary.withValues(alpha: 0.35),
                                      blurRadius: 8,
                                      offset: const Offset(0, 3),
                                    ),
                                  ],
                                ),
                                child: Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    const Icon(Icons.star_rounded, color: Colors.amber, size: 14),
                                    const SizedBox(width: 4),
                                    Text(
                                      _reviewsCount > 0
                                          ? '$_avgRating ($_reviewsCount)'
                                          : (isVi ? 'Đánh giá' : 'Rate'),
                                      style: const TextStyle(color: Colors.white, fontSize: 12, fontWeight: FontWeight.w800),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),

                    // Name, Specialty, Experience
                    Center(
                      child: Column(
                        children: [
                          Text(
                            doc.fullName,
                            style: TextStyle(fontSize: 24, fontWeight: FontWeight.w800, color: context.textPrimary),
                          ),
                          const SizedBox(height: 8),
                          Container(
                            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 5),
                            decoration: BoxDecoration(
                              color: AppColors.secondary.withValues(alpha: context.isDark ? 0.2 : 0.1),
                              borderRadius: BorderRadius.circular(999),
                            ),
                            child: Text(
                              expYears == null
                                  ? specialtyStr
                                  : '$specialtyStr • $expYears ${isVi ? 'NĂM KINH NGHIỆM' : 'YRS EXPERIENCE'}',
                              style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w800, color: AppColors.secondary, letterSpacing: 0.4),
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 18),

                    // Biography
                    Text(
                      bioText,
                      style: TextStyle(fontSize: 14, color: context.textSecondary, height: 1.5),
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 28),

                    // Numeric Stats (dữ liệu thật)
                    Row(
                      children: [
                        Expanded(
                          child: _buildStatCard(
                            context: context,
                            icon: Iconsax.people,
                            color: AppColors.secondary,
                            value: '${detail?.patientCount ?? 0}',
                            label: isVi ? 'Bệnh nhân đã khám' : 'Patients Treated',
                          ),
                        ),
                        const SizedBox(width: 16),
                        Expanded(
                          child: _buildStatCard(
                            context: context,
                            icon: Iconsax.star,
                            color: AppColors.accent,
                            value: '$_reviewsCount',
                            label: isVi ? 'Đánh giá' : 'Reviews',
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 20),

                    // Verified Badge
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                      decoration: BoxDecoration(
                        color: context.isDark ? const Color(0xFF1A2A1A) : const Color(0xFFF0FDF4),
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: context.isDark ? Colors.green.withValues(alpha: 0.3) : const Color(0xFFBBF7D0)),
                      ),
                      child: Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(6),
                            decoration: const BoxDecoration(color: AppColors.success, shape: BoxShape.circle),
                            child: const Icon(Icons.check_rounded, color: Colors.white, size: 14),
                          ),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  isVi ? 'NHA SĨ CHÍNH THỨC' : 'OFFICIAL CLINIC DENTIST',
                                  style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w800, color: AppColors.success, letterSpacing: 0.5),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  isVi ? 'Thuộc đội ngũ nha sĩ đang làm việc tại phòng khám.' : 'Part of the clinic\'s active dentist team.',
                                  style: TextStyle(fontSize: 12, color: context.textSecondary),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),

                    if (detail?.education != null && detail!.education!.isNotEmpty) ...[
                      const SizedBox(height: 28),
                      Text(
                        isVi ? 'Học vấn' : 'Education',
                        style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: context.textPrimary),
                      ),
                      const SizedBox(height: 12),
                      _buildInfoCard(context: context, icon: Iconsax.book_1, text: detail.education!),
                    ],

                    if (detail?.certificateIssuedBy != null && detail!.certificateIssuedBy!.isNotEmpty) ...[
                      const SizedBox(height: 20),
                      Text(
                        isVi ? 'Chứng chỉ hành nghề' : 'Certification',
                        style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: context.textPrimary),
                      ),
                      const SizedBox(height: 12),
                      _buildInfoCard(context: context, icon: Iconsax.medal_star, text: detail.certificateIssuedBy!),
                    ],
                    const SizedBox(height: 28),

                    const SizedBox(height: 12),
                  ],
                ),
              ),
            ),

            // Book Appointment Button
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
                          date: null,
                          timeSlot: null,
                          holdExpiresAt: null,
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
                    } else {
                      context.push(AppRoutes.bookingSelectPatient, extra: draft);
                    }
                  },
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    elevation: 0,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(999)),
                  ),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Iconsax.calendar_1, color: Colors.white, size: 20),
                      const SizedBox(width: 8),
                      Text(
                        isVi ? 'Đặt lịch khám ngay' : 'Book Appointment',
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

  Widget _placeholderAvatar() {
    return Container(
      color: AppColors.primaryLight,
      child: const Icon(Iconsax.user, color: AppColors.primary, size: 64),
    );
  }

  Widget _buildStatCard({
    required BuildContext context,
    required IconData icon,
    required Color color,
    required String value,
    required String label,
  }) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 16),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: context.divider),
        boxShadow: [
          BoxShadow(color: Colors.black.withValues(alpha: 0.03), blurRadius: 10, offset: const Offset(0, 4)),
        ],
      ),
      child: Column(
        children: [
          Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(
              color: color.withValues(alpha: context.isDark ? 0.2 : 0.12),
              shape: BoxShape.circle,
            ),
            child: Icon(icon, color: color, size: 20),
          ),
          const SizedBox(height: 10),
          Text(value, style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: context.textPrimary)),
          const SizedBox(height: 2),
          Text(label, style: TextStyle(fontSize: 12, color: context.textSecondary), textAlign: TextAlign.center),
        ],
      ),
    );
  }

  Widget _buildInfoCard({required BuildContext context, required IconData icon, required String text}) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: context.divider),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: context.isDark ? AppColors.primary.withValues(alpha: 0.15) : AppColors.primaryLight,
              borderRadius: BorderRadius.circular(10),
            ),
            child: Icon(icon, color: AppColors.primary, size: 20),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Text(text, style: TextStyle(fontSize: 13, color: context.textSecondary, height: 1.4)),
          ),
        ],
      ),
    );
  }
}
