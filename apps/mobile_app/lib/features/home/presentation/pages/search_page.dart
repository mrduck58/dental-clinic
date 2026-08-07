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
  final _searchCtrl = TextEditingController();

  List<DoctorModel> _doctors = [];
  List<ServiceModel> _services = [];
  final Map<String, DentistReviewsResult> _reviewsByDentist = {};
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
      final doctors = List<DoctorModel>.from(results[0] as List);
      setState(() {
        _doctors = doctors;
        _services = List<ServiceModel>.from(results[1] as List);
        _isLoading = false;
      });

      try {
        final reviewResults = await Future.wait(
          doctors.map((d) async {
            try {
              return await ReviewService().getReviewsForDentist(d.id);
            } catch (_) {
              return null;
            }
          }),
        );
        if (mounted) {
          setState(() {
            for (var i = 0; i < doctors.length; i++) {
              if (reviewResults[i] != null) {
                _reviewsByDentist[doctors[i].id] = reviewResults[i]!;
              }
            }
          });
        }
      } catch (_) {}
    } catch (_) {
      setState(() => _isLoading = false);
    }
  }

  String _selectedSpecialty = 'Tất cả';

  String _removeDiacritics(String str) {
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

  List<String> get _specialties {
    final set = <String>{};
    for (final d in _doctors) {
      if (d.specialty != null && d.specialty!.isNotEmpty) {
        set.add(d.specialty!);
      }
    }
    return ['Tất cả', ...set];
  }

  List<DoctorModel> get _filteredDoctors {
    var list = _doctors;
    if (_selectedSpecialty != 'Tất cả') {
      list = list.where((d) => d.specialty == _selectedSpecialty).toList();
    }

    final query = _searchQuery.trim();
    if (query.isEmpty) return list;

    final qNorm = _removeDiacritics(query);
    return list.where((d) {
      final nameNorm = _removeDiacritics(d.fullName);
      final specNorm = _removeDiacritics(d.specialty ?? '');
      return nameNorm.contains(qNorm) || specNorm.contains(qNorm);
    }).toList();
  }

  List<ServiceModel> get _filteredServices {
    final query = _searchQuery.trim();
    if (query.isEmpty) return _services;

    final qNorm = _removeDiacritics(query);
    return _services.where((s) {
      final nameNorm = _removeDiacritics(s.name);
      final descNorm = _removeDiacritics(s.description);
      return nameNorm.contains(qNorm) || descNorm.contains(qNorm);
    }).toList();
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
    final specialties = _specialties;

    return Column(
      children: [
        if (specialties.length > 2)
          Container(
            height: 48,
            padding: const EdgeInsets.symmetric(vertical: 6),
            child: ListView.separated(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              scrollDirection: Axis.horizontal,
              itemCount: specialties.length,
              separatorBuilder: (_, _) => const SizedBox(width: 8),
              itemBuilder: (context, idx) {
                final spec = specialties[idx];
                final isSelected = _selectedSpecialty == spec;
                final label = (spec == 'Tất cả' && !isVi) ? 'All' : spec;
                return ChoiceChip(
                  label: Text(
                    label,
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: isSelected ? FontWeight.bold : FontWeight.w500,
                      color: isSelected ? Colors.white : context.textPrimary,
                    ),
                  ),
                  selected: isSelected,
                  selectedColor: AppColors.primary,
                  backgroundColor: context.card,
                  side: BorderSide(
                    color: isSelected ? AppColors.primary : context.divider,
                  ),
                  onSelected: (val) {
                    if (val) {
                      setState(() {
                        _selectedSpecialty = spec;
                      });
                    }
                  },
                );
              },
            ),
          ),
        Expanded(
          child: list.isEmpty
              ? _buildEmptyState(isVi ? 'Không tìm thấy nha sĩ nào phù hợp.' : 'No dentists matching your search.')
              : ListView.separated(
                  padding: const EdgeInsets.all(16),
                  itemCount: list.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 12),
                  itemBuilder: (context, i) {
                    final doc = list[i];
                    final reviewsResult = _reviewsByDentist[doc.id];
                    final avgRating = reviewsResult?.averageRating ?? 0;
                    final reviewsCount = reviewsResult?.reviewCount ?? 0;
                    final specialty = doc.specialty ?? (isVi ? 'Nha sĩ tổng quát' : 'General Dentist');
                    final expYears = doc.yearsOfExperience ?? 10;

                    return Material(
                      color: context.card,
                      borderRadius: BorderRadius.circular(18),
                      elevation: 1,
                      shadowColor: Colors.black.withValues(alpha: 0.05),
                      child: InkWell(
                        onTap: () => context.push(AppRoutes.dentistProfile, extra: doc),
                        borderRadius: BorderRadius.circular(18),
                        child: Padding(
                          padding: const EdgeInsets.all(14),
                          child: Row(
                            children: [
                              ClipRRect(
                                borderRadius: BorderRadius.circular(16),
                                child: Container(
                                  width: 72,
                                  height: 72,
                                  color: context.isDark
                                      ? AppColors.primary.withValues(alpha: 0.15)
                                      : AppColors.primaryLight,
                                  child: doc.profilePictureUrl != null && doc.profilePictureUrl!.isNotEmpty
                                      ? Image.network(
                                          ApiConstants.resolveAssetUrl(doc.profilePictureUrl)!,
                                          fit: BoxFit.cover,
                                          errorBuilder: (_, __, ___) => Icon(Iconsax.user, color: AppColors.primary, size: 30),
                                        )
                                      : Icon(Iconsax.user, color: AppColors.primary, size: 30),
                                ),
                              ),
                              const SizedBox(width: 14),
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
                                    Container(
                                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                                      decoration: BoxDecoration(
                                        color: AppColors.primary.withValues(alpha: 0.08),
                                        borderRadius: BorderRadius.circular(6),
                                      ),
                                      child: Text(
                                        specialty,
                                        style: const TextStyle(
                                          fontSize: 11.5,
                                          color: AppColors.primary,
                                          fontWeight: FontWeight.w700,
                                        ),
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
                                        const SizedBox(width: 12),
                                        Text(
                                          '$expYears ${isVi ? 'năm KN' : 'yrs exp'}',
                                          style: TextStyle(
                                            fontSize: 11.5,
                                            color: context.textMuted,
                                          ),
                                        ),
                                      ],
                                    ),
                                  ],
                                ),
                              ),
                              const SizedBox(width: 8),
                              Icon(
                                Iconsax.arrow_right_3,
                                color: context.textMuted,
                                size: 18,
                              ),
                            ],
                          ),
                        ),
                      ),
                    );
                  },
                ),
        ),
      ],
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
