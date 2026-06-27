import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';

class DentistsListPage extends StatefulWidget {
  const DentistsListPage({super.key});

  @override
  State<DentistsListPage> createState() => _DentistsListPageState();
}

class _DentistsListPageState extends State<DentistsListPage> {
  final _searchCtrl = TextEditingController();
  List<DoctorInfo> _filteredDentists = [];
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    _filterDentists();
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

  void _filterDentists() {
    final list = BookingMockData.doctors;
    if (_searchQuery.trim().isEmpty) {
      _filteredDentists = list;
    } else {
      final q = _searchQuery.toLowerCase();
      _filteredDentists = list.where((d) {
        return d.name.toLowerCase().contains(q) ||
            d.specialty.toLowerCase().contains(q);
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
            child: _filteredDentists.isEmpty
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
                      return Container(
                        margin: const EdgeInsets.only(bottom: 16),
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
                          children: [
                            // Avatar
                            ClipRRect(
                              borderRadius: BorderRadius.circular(16),
                              child: Container(
                                width: 72,
                                height: 72,
                                color: context.isDark ? AppColors.primary.withValues(alpha: 0.15) : AppColors.primaryLight,
                                child: Icon(Iconsax.user, color: AppColors.primary, size: 30),
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
                                  const SizedBox(height: 4),
                                  Text(
                                    doc.specialty,
                                    style: TextStyle(
                                      fontSize: 12.5,
                                      color: context.textSecondary,
                                      fontWeight: FontWeight.w600,
                                    ),
                                  ),
                                  const SizedBox(height: 8),
                                  Row(
                                    children: [
                                      const Icon(Icons.star_rounded, color: Colors.amber, size: 16),
                                      const SizedBox(width: 4),
                                      Text(
                                        '${doc.rating} (${doc.reviewCount}+ reviews)',
                                        style: TextStyle(
                                          fontSize: 12,
                                          fontWeight: FontWeight.w700,
                                          color: context.textSecondary,
                                        ),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                            ),

                            // Navigation arrow
                            IconButton(
                              icon: const Icon(Iconsax.arrow_right_3, color: AppColors.primary, size: 20),
                              onPressed: () {
                                // Navigate to booking using this doctor
                                context.push(
                                  AppRoutes.bookingSelectPatient,
                                  extra: doc,
                                );
                              },
                            ),
                          ],
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
