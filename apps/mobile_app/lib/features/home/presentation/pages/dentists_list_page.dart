import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/home/data/home_service.dart';
import 'package:mobile_app/features/home/data/models/doctor_model.dart';
import 'package:mobile_app/features/home/data/models/review_model.dart';
import 'package:mobile_app/features/home/data/review_service.dart';

class DentistsListPage extends StatefulWidget {
  const DentistsListPage({super.key});

  @override
  State<DentistsListPage> createState() => _DentistsListPageState();
}

class _DentistsListPageState extends State<DentistsListPage> {
  final _searchCtrl = TextEditingController();
  List<DoctorModel> _dentists = [];
  List<DoctorModel> _filteredDentists = [];
  final Map<String, DentistReviewsResult> _reviewsByDentist = {};
  String _searchQuery = '';
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadDentists();
    _searchCtrl.addListener(() {
      setState(() {
        _searchQuery = _searchCtrl.text;
        _filterDentists();
      });
    });
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadDentists() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final dentists = await HomeService().getDentists();
      setState(() {
        _dentists = dentists;
        _isLoading = false;
        _filterDentists();
      });

      final reviewResults = await Future.wait(
        dentists.map((d) => ReviewService().getReviewsForDentist(d.id)),
      );
      if (!mounted) return;
      setState(() {
        for (var i = 0; i < dentists.length; i++) {
          _reviewsByDentist[dentists[i].id] = reviewResults[i];
        }
      });
    } catch (_) {
      setState(() {
        _error = 'load_failed';
        _isLoading = false;
      });
    }
  }

  String _removeVietnameseDiacritics(String str) {
    const vietnamese = [
      'aàáạảãâầấậẩẫăằắặẳẵ', 'AÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴ',
      'eèéẹẻẽêềếệểễ', 'EÈÉẸẺẼÊỀẾỆỂỄ',
      'iìíịỉĩ', 'IÌÍỊỈĨ',
      'oòóọỏõôồốộổỗơờớợởỡ', 'OÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠ',
      'uùúụủũưừứựửữ', 'UÙÚỤỦŨƯỪỨỰỬỮ',
      'yỳýỵỷỹ', 'YỲÝỴỶỸ',
      'dđ', 'DĐ'
    ];
    var result = str;
    for (var element in vietnamese) {
      for (var i = 1; i < element.length; i++) {
        result = result.replaceAll(element[i], element[0]);
      }
    }
    return result.toLowerCase();
  }

  void _filterDentists() {
    if (_searchQuery.trim().isEmpty) {
      _filteredDentists = _dentists;
    } else {
      final qNorm = _removeVietnameseDiacritics(_searchQuery.trim());
      _filteredDentists = _dentists.where((d) {
        final nameNorm = _removeVietnameseDiacritics(d.fullName);
        final specNorm = _removeVietnameseDiacritics(d.specialty ?? '');
        return nameNorm.contains(qNorm) || specNorm.contains(qNorm);
      }).toList();
    }
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
          onPressed: () => context.pop(),
        ),
        title: Text(
          isVi ? 'Đội ngũ nha sĩ' : 'Our Dentist Directory',
          style: TextStyle(
            color: context.textPrimary,
            fontWeight: FontWeight.w800,
            fontSize: 18,
          ),
        ),
        centerTitle: true,
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(color: context.divider, height: 1),
        ),
      ),
      body: Column(
        children: [
          // Search Input
          Padding(
            padding: const EdgeInsets.all(20),
            child: Container(
              decoration: BoxDecoration(
                color: context.card,
                borderRadius: BorderRadius.circular(14),
                border: Border.all(color: context.divider),
              ),
              child: TextField(
                controller: _searchCtrl,
                style: TextStyle(fontSize: 14, color: context.textPrimary),
                decoration: InputDecoration(
                  hintText: isVi ? 'Tìm kiếm nha sĩ hoặc chuyên khoa...' : 'Search dentist or specialty...',
                  hintStyle: TextStyle(color: context.textMuted, fontSize: 13),
                  prefixIcon: Icon(Iconsax.search_normal, color: context.textMuted, size: 18),
                  suffixIcon: _searchQuery.isNotEmpty
                      ? IconButton(
                          icon: Icon(Icons.clear, color: context.textMuted, size: 18),
                          onPressed: () => _searchCtrl.clear(),
                        )
                      : null,
                  contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                  border: InputBorder.none,
                ),
              ),
            ),
          ),

          // Dentists directory list
          Expanded(
            child: _isLoading
                ? const Center(child: CircularProgressIndicator())
                : _error != null
                    ? Center(
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Text(
                              isVi ? 'Không thể tải danh sách nha sĩ.' : 'Failed to load dentists.',
                              style: TextStyle(color: context.textMuted),
                            ),
                            const SizedBox(height: 12),
                            TextButton(onPressed: _loadDentists, child: Text(isVi ? 'Thử lại' : 'Retry')),
                          ],
                        ),
                      )
                    : _filteredDentists.isEmpty
                ? Center(
                    child: Text(
                      isVi ? 'Không tìm thấy nha sĩ phù hợp.' : 'No dentist matching search.',
                      style: TextStyle(color: context.textMuted),
                    ),
                  )
                : ListView.builder(
                    padding: const EdgeInsets.symmetric(horizontal: 20),
                    itemCount: _filteredDentists.length,
                    itemBuilder: (context, i) {
                      final doc = _filteredDentists[i];
                      final reviewsResult = _reviewsByDentist[doc.id];
                      final avgRating = reviewsResult?.averageRating ?? 0;
                      final reviewsCount = reviewsResult?.reviewCount ?? 0;
                      final specialty = doc.specialty ?? (isVi ? 'Nha sĩ tổng quát' : 'General Dentist');
                      return Container(
                        margin: const EdgeInsets.only(bottom: 16),
                        child: Material(
                          color: context.card,
                          borderRadius: BorderRadius.circular(18),
                          elevation: 1,
                          shadowColor: Colors.black.withValues(alpha: 0.05),
                          child: InkWell(
                            onTap: () => context.push(AppRoutes.dentistProfile, extra: doc),
                            borderRadius: BorderRadius.circular(18),
                            child: Padding(
                              padding: const EdgeInsets.all(16),
                              child: Row(
                                children: [
                                  // Avatar
                                  Container(
                                    padding: const EdgeInsets.all(2),
                                    decoration: BoxDecoration(
                                      borderRadius: BorderRadius.circular(18),
                                      border: Border.all(color: AppColors.secondary.withValues(alpha: context.isDark ? 0.35 : 0.25), width: 1.5),
                                    ),
                                    child: ClipRRect(
                                      borderRadius: BorderRadius.circular(15),
                                      child: Container(
                                        width: 68,
                                        height: 68,
                                        color: context.isDark ? AppColors.primary.withValues(alpha: 0.15) : AppColors.primaryLight,
                                        child: doc.profilePictureUrl != null
                                            ? Image.network(
                                                ApiConstants.resolveAssetUrl(doc.profilePictureUrl)!,
                                                fit: BoxFit.cover,
                                                errorBuilder: (_, __, ___) => Icon(Iconsax.user, color: AppColors.primary, size: 28),
                                              )
                                            : Icon(Iconsax.user, color: AppColors.primary, size: 28),
                                      ),
                                    ),
                                  ),
                                  const SizedBox(width: 16),

                                  // Dentist Details
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
                                        const SizedBox(height: 6),
                                        Wrap(
                                          crossAxisAlignment: WrapCrossAlignment.center,
                                          spacing: 6,
                                          runSpacing: 4,
                                          children: [
                                            Container(
                                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                              decoration: BoxDecoration(
                                                color: AppColors.secondary.withValues(alpha: context.isDark ? 0.2 : 0.1),
                                                borderRadius: BorderRadius.circular(6),
                                              ),
                                              child: Text(
                                                specialty,
                                                style: const TextStyle(
                                                  fontSize: 11,
                                                  color: AppColors.secondary,
                                                  fontWeight: FontWeight.w700,
                                                ),
                                              ),
                                            ),
                                            Container(
                                              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                              decoration: BoxDecoration(
                                                color: AppColors.accent.withValues(alpha: context.isDark ? 0.2 : 0.12),
                                                borderRadius: BorderRadius.circular(6),
                                              ),
                                              child: Row(
                                                mainAxisSize: MainAxisSize.min,
                                                children: [
                                                  const Icon(Icons.star_rounded, color: AppColors.accent, size: 13),
                                                  const SizedBox(width: 2),
                                                  Text(
                                                    '$avgRating ($reviewsCount)',
                                                    style: const TextStyle(
                                                      fontSize: 11,
                                                      fontWeight: FontWeight.w800,
                                                      color: AppColors.accent,
                                                    ),
                                                  ),
                                                ],
                                              ),
                                            ),
                                          ],
                                        ),
                                      ],
                                    ),
                                  ),

                                  // Navigation arrow
                                  Icon(Iconsax.arrow_right_3, color: AppColors.primary, size: 20),
                                ],
                              ),
                            ),
                          ),
                        ),
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }
}
