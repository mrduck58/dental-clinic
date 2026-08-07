import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';
import 'package:mobile_app/features/profile/data/family_member.dart';

class FamilyMembersPage extends StatefulWidget {
  const FamilyMembersPage({super.key});

  @override
  State<FamilyMembersPage> createState() => _FamilyMembersPageState();
}

class _FamilyMembersPageState extends State<FamilyMembersPage> {
  final _familyService = FamilyService();
  bool _isLoading = true;
  bool _hasError = false;
  final Map<String, DateTime> _lastVisitByPatientId = {};

  static const _visitedStatuses = {'Completed', 'PendingPayment'};

  String? _resolveAvatarUrl(String? url) => ApiConstants.resolveAssetUrl(url);

  @override
  void initState() {
    super.initState();
    _loadMembers();
  }

  Future<void> _loadMembers() async {
    setState(() {
      _isLoading = true;
      _hasError = false;
    });
    try {
      await _familyService.loadFromServer();
      final appointments = await BookingService().getMyAppointments();
      _lastVisitByPatientId.clear();
      for (final a in appointments) {
        if (a.patientId == null || !_visitedStatuses.contains(a.status)) continue;
        final date = a.parsedDate;
        final current = _lastVisitByPatientId[a.patientId];
        if (current == null || date.isAfter(current)) {
          _lastVisitByPatientId[a.patientId!] = date;
        }
      }
    } catch (_) {
      if (mounted) setState(() => _hasError = true);
    }
    if (mounted) {
      setState(() => _isLoading = false);
    }
  }

  String _getEnglishRelationship(String rel) {
    switch (rel) {
      case 'Bố':
        return 'Father';
      case 'Mẹ':
        return 'Mother';
      case 'Vợ/Chồng':
        return 'Spouse';
      case 'Con':
        return 'Child';
      case 'Anh/Chị/Em':
        return 'Sibling';
      default:
        return 'Other';
    }
  }

  String _getLastVisitText(String memberId, bool isVi) {
    final date = _lastVisitByPatientId[memberId];
    if (date == null) {
      return isVi ? 'Chưa có lịch khám gần đây' : 'No recent visits';
    }
    final d = date.day.toString().padLeft(2, '0');
    final m = date.month.toString().padLeft(2, '0');
    return isVi ? 'Khám lần cuối: $d/$m/${date.year}' : 'Last Visit: ${date.year}-$m-$d';
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final members = _familyService.getMembers();

    return Scaffold(
      backgroundColor: context.bg,
      appBar: AppBar(
        backgroundColor: context.card,
        elevation: 0.5,
        leading: IconButton(
          icon: Icon(Icons.arrow_back_ios_new_rounded, color: context.textPrimary, size: 20),
          onPressed: () => context.pop(),
        ),
        title: Text(
          isVi ? 'Thành viên gia đình' : 'Family Members',
          style: TextStyle(
            color: context.textPrimary,
            fontWeight: FontWeight.bold,
            fontSize: 18,
          ),
        ),
        centerTitle: true,
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator(color: AppColors.primary))
          : _hasError && members.isEmpty
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        isVi ? 'Không thể tải danh sách thành viên.' : 'Failed to load family members.',
                        style: TextStyle(color: context.textMuted),
                      ),
                      const SizedBox(height: 12),
                      TextButton(onPressed: _loadMembers, child: Text(isVi ? 'Thử lại' : 'Retry')),
                    ],
                  ),
                )
              : SingleChildScrollView(
              padding: const EdgeInsets.all(18.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [


            // Members list
            ...members.map((member) {
              final relText = isVi ? member.relationship : _getEnglishRelationship(member.relationship);
              final lastVisitText = _getLastVisitText(member.id, isVi);
              final badgeColor = context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9);
              final badgeTextColor = context.isDark ? context.textSecondary : const Color(0xFF475569);

              return Container(
                margin: const EdgeInsets.only(bottom: 16),
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: context.card,
                  borderRadius: BorderRadius.circular(20),
                  border: Border.all(color: context.divider),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.03),
                      blurRadius: 10,
                      offset: const Offset(0, 4),
                    ),
                  ],
                ),
                child: Column(
                  children: [
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.center,
                      children: [
                        // Avatar
                        Container(
                          width: 54,
                          height: 54,
                          decoration: BoxDecoration(
                            shape: BoxShape.circle,
                            color: AppColors.primaryLight,
                            border: Border.all(color: AppColors.primary, width: 2),
                          ),
                          child: ClipOval(
                            child: _resolveAvatarUrl(member.profilePictureUrl) != null
                                ? Image.network(
                                    _resolveAvatarUrl(member.profilePictureUrl)!,
                                    fit: BoxFit.cover,
                                    errorBuilder: (_, __, ___) => const Icon(Iconsax.user, color: AppColors.primary, size: 24),
                                  )
                                : const Icon(Iconsax.user, color: AppColors.primary, size: 24),
                          ),
                        ),
                        const SizedBox(width: 14),
                        // Details
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  Text(
                                    member.fullName,
                                    style: TextStyle(
                                      fontSize: 16,
                                      fontWeight: FontWeight.w800,
                                      color: context.textPrimary,
                                    ),
                                  ),
                                  const SizedBox(width: 8),
                                  Container(
                                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                                    decoration: BoxDecoration(
                                      color: badgeColor,
                                      borderRadius: BorderRadius.circular(6),
                                    ),
                                    child: Text(
                                      relText.toUpperCase(),
                                      style: TextStyle(
                                        fontSize: 9,
                                        fontWeight: FontWeight.w900,
                                        color: badgeTextColor,
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                              const SizedBox(height: 4),
                              Text(
                                lastVisitText,
                                style: TextStyle(
                                  fontSize: 13,
                                  color: context.textSecondary,
                                ),
                              ),
                            ],
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    Divider(color: context.divider, height: 1),
                    const SizedBox(height: 10),
                    // Action Button
                    Row(
                      children: [
                        // View Profile Button
                        Expanded(
                          child: GestureDetector(
                            onTap: () async {
                              final updated = await context.push(
                                '${AppRoutes.editFamilyMember}?id=${member.id}',
                                extra: member,
                              );
                              if (updated == true && mounted) {
                                _loadMembers();
                              }
                            },
                            child: Padding(
                              padding: const EdgeInsets.symmetric(vertical: 8.0),
                              child: Row(
                                mainAxisAlignment: MainAxisAlignment.center,
                                children: [
                                  Text(
                                    isVi ? 'HỒ SƠ' : 'PROFILE',
                                    style: const TextStyle(
                                      color: AppColors.primary,
                                      fontSize: 13,
                                      fontWeight: FontWeight.w900,
                                      letterSpacing: 0.5,
                                    ),
                                  ),
                                  const SizedBox(width: 4),
                                  const Icon(
                                    Icons.arrow_forward_ios_rounded,
                                    color: AppColors.primary,
                                    size: 11,
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                        Container(
                          width: 1,
                          height: 16,
                          color: context.divider,
                        ),
                        // View Appointments Button
                        Expanded(
                          child: GestureDetector(
                            onTap: () {
                              context.go(
                                AppRoutes.appointments,
                                extra: {
                                  'patientId': member.id,
                                  'patientName': member.fullName,
                                },
                              );
                            },
                            child: Padding(
                              padding: const EdgeInsets.symmetric(vertical: 8.0),
                              child: Row(
                                mainAxisAlignment: MainAxisAlignment.center,
                                children: [
                                  Text(
                                    isVi ? 'LỊCH HẸN' : 'APPOINTMENTS',
                                    style: const TextStyle(
                                      color: AppColors.primary,
                                      fontSize: 13,
                                      fontWeight: FontWeight.w900,
                                      letterSpacing: 0.5,
                                    ),
                                  ),
                                  const SizedBox(width: 4),
                                  const Icon(
                                    Icons.calendar_month,
                                    color: AppColors.primary,
                                    size: 13,
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              );
            }),

            const SizedBox(height: 8),
            // Proactive Care card
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFEFF6FF),
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: context.isDark ? Colors.transparent : const Color(0xFFDBEAFE)),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    padding: const EdgeInsets.all(8),
                    decoration: const BoxDecoration(
                      color: Color(0xFF3B82F6),
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(
                      Iconsax.heart5,
                      color: Colors.white,
                      size: 18,
                    ),
                  ),
                  const SizedBox(width: 14),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          isVi ? 'Chăm sóc Chủ động' : 'Proactive Care',
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.w800,
                            color: context.isDark ? Colors.white : const Color(0xFF1E3A8A),
                          ),
                        ),
                        const SizedBox(height: 6),
                        Text(
                          isVi
                              ? 'Bạn có biết? Trẻ em nên đi khám nha sĩ 6 tháng một lần để duy trì nụ cười khỏe mạnh. Đặt lịch khám định kỳ ngay.'
                              : 'Did you know? Children should visit the dentist every 6 months to maintain healthy smiles. Schedule a checkup now.',
                          style: TextStyle(
                            fontSize: 13,
                            height: 1.5,
                            color: context.isDark ? const Color(0xFF94A3B8) : const Color(0xFF1E40AF),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 80), // spacer for FAB
          ],
        ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final added = await context.push(AppRoutes.addFamilyMember);
          if (added == true && mounted) {
            _loadMembers();
          }
        },
        backgroundColor: AppColors.primary,
        foregroundColor: Colors.white,
        elevation: 3,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        child: const Icon(Icons.add, size: 28),
      ),
    );
  }
}
