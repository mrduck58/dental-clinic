import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/home/data/models/doctor_model.dart';
import 'package:mobile_app/features/home/data/review_service.dart';

class DentistProfilePage extends StatefulWidget {
  final DoctorModel doctor;
  const DentistProfilePage({super.key, required this.doctor});

  @override
  State<DentistProfilePage> createState() => _DentistProfilePageState();
}

class _DentistProfilePageState extends State<DentistProfilePage> {
  final _reviewService = ReviewService();
  late double _avgRating;
  late int _reviewsCount;

  @override
  void initState() {
    super.initState();
    _loadReviewsInfo();
  }

  void _loadReviewsInfo() {
    setState(() {
      _avgRating = _reviewService.getAverageRating(widget.doctor.id);
      _reviewsCount = _reviewService.getReviewsForDentist(widget.doctor.id).length;
    });
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final doc = widget.doctor;
    final expYears = doc.yearsOfExperience ?? 12;
    final specialtyStr = (doc.specialty ?? (isVi ? 'Nha sĩ tổng quát' : 'General Dentist')).toUpperCase();

    final bioText = doc.bio ??
        (isVi
            ? 'Chuyên gia về chỉnh nha nâng cao và nha khoa thẩm mỹ. Tận tâm mang lại nụ cười hoàn hảo bằng kỹ thuật ít xâm lấn và công nghệ nha khoa tiên tiến.'
            : 'Specialist in advanced orthodontics and cosmetic dentistry. Committed to delivering perfect smiles using minimally invasive techniques and cutting-edge dental technology.');

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
        actions: [
          IconButton(
            icon: Icon(Iconsax.notification, color: context.textPrimary),
            onPressed: () {},
          ),
        ],
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
                    // Doctor Image & Rating badge
                    Center(
                      child: Stack(
                        alignment: Alignment.bottomCenter,
                        children: [
                          Container(
                            width: 140,
                            height: 140,
                            margin: const EdgeInsets.only(bottom: 14),
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
                                      doc.profilePictureUrl!,
                                      fit: BoxFit.cover,
                                      errorBuilder: (_, _, _) => _placeholderAvatar(),
                                    )
                                  : _placeholderAvatar(),
                            ),
                          ),
                          Positioned(
                            bottom: 6,
                            child: Container(
                              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                              decoration: BoxDecoration(
                                color: AppColors.primary,
                                borderRadius: BorderRadius.circular(999),
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
                                  const Icon(Icons.star_rounded, color: Colors.white, size: 14),
                                  const SizedBox(width: 4),
                                  Text(
                                    _avgRating.toString(),
                                    style: const TextStyle(color: Colors.white, fontSize: 12, fontWeight: FontWeight.w800),
                                  ),
                                ],
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
                          const SizedBox(height: 4),
                          Text(
                            '$specialtyStr • $expYears ${isVi ? 'NĂM KINH NGHIỆM' : 'YRS EXPERIENCE'}',
                            style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: AppColors.primary, letterSpacing: 0.5),
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
                    const SizedBox(height: 20),

                    // Badges row
                    Wrap(
                      spacing: 8,
                      runSpacing: 8,
                      alignment: WrapAlignment.center,
                      children: [
                        _BadgeWidget(label: isVi ? 'Chứng nhận Invisalign®' : 'Invisalign® Certified'),
                        _BadgeWidget(label: isVi ? 'Được đánh giá cao 2025' : 'Top Rated 2025'),
                        _BadgeWidget(label: isVi ? 'Hội đồng y khoa chứng nhận' : 'Board Certified'),
                      ],
                    ),
                    const SizedBox(height: 28),

                    // Numeric Stats
                    Row(
                      children: [
                        Expanded(
                          child: _buildStatCard(
                            context: context,
                            icon: Iconsax.people,
                            value: '2.4k+',
                            label: isVi ? 'Bệnh nhân' : 'Patients',
                          ),
                        ),
                        const SizedBox(width: 16),
                        Expanded(
                          child: _buildStatCard(
                            context: context,
                            icon: Iconsax.award,
                            value: '15+',
                            label: isVi ? 'Giải thưởng' : 'Awards',
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
                                  isVi ? 'ĐÃ XÁC MINH DANH TÍNH' : 'IDENTITY VERIFIED',
                                  style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w800, color: AppColors.success, letterSpacing: 0.5),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  isVi ? 'Chuyên gia y tế được cấp phép hành nghề đầy đủ.' : 'Fully licensed medical professional.',
                                  style: TextStyle(fontSize: 12, color: context.textSecondary),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 28),

                    // Specializations
                    Text(
                      isVi ? 'Chuyên môn chuyên sâu' : 'Areas of Expertise',
                      style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: context.textPrimary),
                    ),
                    const SizedBox(height: 12),
                    _buildSpecialtyItem(
                      context: context,
                      icon: Iconsax.magicpen,
                      title: isVi ? 'Chỉnh nha vô hình (Invisalign)' : 'Invisible Orthodontics (Invisalign)',
                      desc: isVi
                          ? 'Chuyên gia về phương pháp niềng khay trong suốt và lập kế hoạch điều trị kỹ thuật số.'
                          : 'Expert in clear aligner therapy and digital treatment planning.',
                    ),
                    const SizedBox(height: 12),
                    _buildSpecialtyItem(
                      context: context,
                      icon: Iconsax.health,
                      title: isVi ? 'Nha khoa trẻ em' : 'Pediatric Dentistry',
                      desc: isVi
                          ? 'Can thiệp sớm và hướng dẫn phát triển răng xương cho bệnh nhân nhỏ tuổi.'
                          : 'Early intervention and dental development guidance for young patients.',
                    ),
                    const SizedBox(height: 28),

                    // Work Experience
                    Text(
                      isVi ? 'Kinh nghiệm làm việc' : 'Work Experience',
                      style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: context.textPrimary),
                    ),
                    const SizedBox(height: 16),
                    _buildExperienceTimeline(
                      context: context,
                      yearRange: isVi ? '2020 - HIỆN TẠI' : '2020 - PRESENT',
                      role: isVi ? 'Bác sĩ Chỉnh nha Cấp cao' : 'Senior Orthodontist',
                      hospital: isVi ? 'Bệnh viện Răng Hàm Mặt Quốc tế Sài Gòn' : 'Saigon International Dental Hospital',
                      isFirst: true,
                    ),
                    _buildExperienceTimeline(
                      context: context,
                      yearRange: '2014 - 2020',
                      role: isVi ? 'Bác sĩ Nha khoa Liên kết' : 'Associate Dentist',
                      hospital: isVi ? 'Tập đoàn Y tế Metropolitan' : 'Metropolitan Healthcare Group',
                      isLast: true,
                    ),
                    const SizedBox(height: 28),

                    // Reviews Preview Card
                    InkWell(
                      onTap: () async {
                        await context.push(AppRoutes.dentistReviews, extra: doc);
                        _loadReviewsInfo();
                      },
                      borderRadius: BorderRadius.circular(18),
                      child: Container(
                        padding: const EdgeInsets.all(18),
                        decoration: BoxDecoration(
                          color: context.card,
                          borderRadius: BorderRadius.circular(18),
                          border: Border.all(color: context.divider),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withValues(alpha: context.isDark ? 0.15 : 0.04),
                              blurRadius: 12,
                              offset: const Offset(0, 4),
                            ),
                          ],
                        ),
                        child: Row(
                          children: [
                            Container(
                              width: 48,
                              height: 48,
                              decoration: BoxDecoration(
                                color: context.isDark ? Colors.red[900]?.withValues(alpha: 0.3) : const Color(0xFFFEF2F2),
                                shape: BoxShape.circle,
                              ),
                              child: const Icon(Iconsax.messages_1, color: AppColors.primary, size: 24),
                            ),
                            const SizedBox(width: 14),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    isVi ? 'Đánh giá từ bệnh nhân' : 'Patient Reviews',
                                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700, color: context.textPrimary),
                                  ),
                                  const SizedBox(height: 2),
                                  Text(
                                    '⭐ $_avgRating/5.0 ($_reviewsCount ${isVi ? 'đánh giá' : 'reviews'})',
                                    style: TextStyle(fontSize: 13, color: context.textSecondary, fontWeight: FontWeight.w600),
                                  ),
                                ],
                              ),
                            ),
                            Icon(Icons.arrow_forward_ios_rounded, size: 16, color: context.textMuted),
                          ],
                        ),
                      ),
                    ),
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
                  onPressed: () => context.push(AppRoutes.bookingSelectPatient),
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
          Icon(icon, color: AppColors.primary, size: 24),
          const SizedBox(height: 8),
          Text(value, style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: context.textPrimary)),
          const SizedBox(height: 2),
          Text(label, style: TextStyle(fontSize: 12, color: context.textSecondary)),
        ],
      ),
    );
  }

  Widget _buildSpecialtyItem({
    required BuildContext context,
    required IconData icon,
    required String title,
    required String desc,
  }) {
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
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(title, style: TextStyle(fontSize: 14, fontWeight: FontWeight.w700, color: context.textPrimary)),
                const SizedBox(height: 4),
                Text(desc, style: TextStyle(fontSize: 12, color: context.textSecondary, height: 1.4)),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildExperienceTimeline({
    required BuildContext context,
    required String yearRange,
    required String role,
    required String hospital,
    bool isFirst = false,
    bool isLast = false,
  }) {
    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Column(
            children: [
              Container(
                width: 12,
                height: 12,
                decoration: BoxDecoration(
                  color: isFirst ? AppColors.primary : const Color(0xFF94A3B8),
                  shape: BoxShape.circle,
                  border: Border.all(color: context.card, width: 2),
                ),
              ),
              if (!isLast)
                Expanded(
                  child: Container(
                    width: 2,
                    color: context.isDark ? const Color(0xFF334155) : const Color(0xFFCBD5E1),
                  ),
                ),
            ],
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Container(
              margin: const EdgeInsets.only(bottom: 16),
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: context.card,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: context.divider),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    yearRange,
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w800,
                      color: isFirst ? AppColors.primary : context.textMuted,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(role, style: TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: context.textPrimary)),
                  const SizedBox(height: 2),
                  Text(hospital, style: TextStyle(fontSize: 12, color: context.textSecondary)),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _BadgeWidget extends StatelessWidget {
  final String label;
  const _BadgeWidget({required this.label});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: context.isDark ? const Color(0xFF1E1B4B) : const Color(0xFFEEF2FF),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: context.isDark ? const Color(0xFF312E81) : const Color(0xFFE0E7FF)),
      ),
      child: Text(
        label,
        style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: Color(0xFF6366F1)),
      ),
    );
  }
}
