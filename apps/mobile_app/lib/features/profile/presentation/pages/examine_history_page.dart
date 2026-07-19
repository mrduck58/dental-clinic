import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';
import 'package:mobile_app/features/profile/data/family_member.dart';
import 'package:mobile_app/features/profile/data/medical_record_service.dart';

class _SelectablePatient {
  final String id;
  final String name;
  final String relation;
  const _SelectablePatient({required this.id, required this.name, required this.relation});
}

class ExamineHistoryPage extends StatefulWidget {
  const ExamineHistoryPage({super.key});

  @override
  State<ExamineHistoryPage> createState() => _ExamineHistoryPageState();
}

class _ExamineHistoryPageState extends State<ExamineHistoryPage> {
  final _searchCtrl = TextEditingController();
  final _service = MedicalRecordService();

  List<MedicalHistoryEvent> _allEvents = [];
  List<_SelectablePatient> _patients = [];
  _SelectablePatient? _selectedPatient;
  List<MedicalHistoryEvent> _filteredEvents = [];
  String _searchQuery = '';
  bool _isDropdownOpen = false;
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
    _searchCtrl.addListener(() {
      setState(() {
        _searchQuery = _searchCtrl.text;
        _filterEvents();
      });
    });
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final events = await _service.getMyMedicalHistory();
      final selfEvents = events.where((e) => e.patientRelationship == 'Tôi');
      final selfEvent = selfEvents.isEmpty ? null : selfEvents.first;
      final profile = await AuthService().getMyProfile();
      await FamilyService().loadFromServer();
      final family = FamilyService().getMembers();

      final patients = <_SelectablePatient>[
        _SelectablePatient(
          id: selfEvent?.patientId ?? 'self',
          name: profile.fullName,
          relation: 'Tôi',
        ),
        ...family.map((m) => _SelectablePatient(id: m.id, name: m.fullName, relation: m.relationship)),
      ];

      setState(() {
        _allEvents = events;
        _patients = patients;
        _selectedPatient = patients.first;
        _isLoading = false;
      });
      _filterEvents();
    } catch (_) {
      setState(() {
        _error = 'load_failed';
        _isLoading = false;
      });
    }
  }

  void _filterEvents() {
    final selected = _selectedPatient;
    var events = selected == null
        ? _allEvents
        : _allEvents.where((e) {
            if (selected.relation == 'Tôi') return e.patientRelationship == 'Tôi';
            return e.patientId == selected.id;
          }).toList();

    if (_searchQuery.trim().isNotEmpty) {
      final q = _searchQuery.toLowerCase();
      events = events.where((e) {
        return e.serviceName.toLowerCase().contains(q) ||
            e.dentistName.toLowerCase().contains(q) ||
            e.appointmentDate.year.toString().contains(q);
      }).toList();
    }
    setState(() => _filteredEvents = events);
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

    final years = <int>{};
    for (var e in _filteredEvents) {
      years.add(e.appointmentDate.year);
    }
    final sortedYears = years.toList()..sort((a, b) => b.compareTo(a));

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
          isVi ? 'Lịch sử khám bệnh' : 'Examine history',
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
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        isVi ? 'Không thể tải lịch sử khám bệnh.' : 'Failed to load examine history.',
                        style: TextStyle(color: context.textMuted),
                      ),
                      const SizedBox(height: 12),
                      TextButton(onPressed: _load, child: Text(isVi ? 'Thử lại' : 'Retry')),
                    ],
                  ),
                )
              : Column(
        children: [
          // Dropdown Member Card & Inline dropdown
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 20, 20, 10),
            child: Column(
              children: [
                GestureDetector(
                  onTap: () {
                    setState(() {
                      _isDropdownOpen = !_isDropdownOpen;
                    });
                  },
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                    decoration: BoxDecoration(
                      color: context.card,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: context.divider),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: context.isDark ? 0.15 : 0.04),
                          blurRadius: 8,
                          offset: const Offset(0, 3),
                        ),
                      ],
                    ),
                    child: Row(
                      children: [
                        Container(
                          width: 44,
                          height: 44,
                          decoration: BoxDecoration(
                            color: AppColors.primaryLight,
                            shape: BoxShape.circle,
                          ),
                          child: const Icon(Icons.person, color: AppColors.primary, size: 22),
                        ),
                        const SizedBox(width: 14),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                _selectedPatient?.name ?? '',
                                style: TextStyle(
                                  fontSize: 15,
                                  fontWeight: FontWeight.w800,
                                  color: context.textPrimary,
                                ),
                              ),
                              const SizedBox(height: 2),
                              Text(
                                _selectedPatient?.relation ?? '',
                                style: TextStyle(
                                  fontSize: 12,
                                  color: context.textSecondary,
                                ),
                              ),
                            ],
                          ),
                        ),
                        AnimatedRotation(
                          turns: _isDropdownOpen ? 0.5 : 0.0,
                          duration: const Duration(milliseconds: 200),
                          child: Icon(Icons.keyboard_arrow_down_rounded, color: context.textSecondary, size: 24),
                        ),
                      ],
                    ),
                  ),
                ),
                if (_isDropdownOpen) ...[
                  const SizedBox(height: 8),
                  Container(
                    decoration: BoxDecoration(
                      color: context.card,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: context.divider),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withValues(alpha: context.isDark ? 0.12 : 0.03),
                          blurRadius: 8,
                          offset: const Offset(0, 4),
                        ),
                      ],
                    ),
                    child: Column(
                      children: _patients.map((p) {
                        final isSelected = p.id == _selectedPatient?.id;
                        return Column(
                          children: [
                            ListTile(
                              leading: Container(
                                width: 34,
                                height: 34,
                                decoration: BoxDecoration(
                                  color: isSelected ? AppColors.primary : context.divider,
                                  shape: BoxShape.circle,
                                ),
                                child: Icon(
                                  Icons.person,
                                  color: isSelected ? Colors.white : context.textSecondary,
                                  size: 18,
                                ),
                              ),
                              title: Text(
                                p.name,
                                style: TextStyle(
                                  fontSize: 14.5,
                                  fontWeight: isSelected ? FontWeight.w800 : FontWeight.w600,
                                  color: context.textPrimary,
                                ),
                              ),
                              subtitle: Text(
                                p.relation,
                                style: TextStyle(
                                  fontSize: 12,
                                  color: context.textSecondary,
                                ),
                              ),
                              trailing: isSelected
                                  ? const Icon(Icons.check_circle_rounded, color: AppColors.primary, size: 20)
                                  : null,
                              onTap: () {
                                setState(() {
                                  _selectedPatient = p;
                                  _isDropdownOpen = false;
                                });
                                _filterEvents();
                              },
                            ),
                            if (p != _patients.last) Divider(color: context.divider, height: 1),
                          ],
                        );
                      }).toList(),
                    ),
                  ),
                ],
              ],
            ),
          ),

          // Search Bar
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
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
                  hintText: isVi ? 'Tìm kiếm đợt khám, bác sĩ hoặc năm...' : 'Search treatments, doctors, or years...',
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

          // Timeline List
          Expanded(
            child: _filteredEvents.isEmpty
                ? Center(
                    child: Text(
                      isVi ? 'Không tìm thấy kết quả phù hợp.' : 'No records found.',
                      style: TextStyle(color: context.textMuted),
                    ),
                  )
                : ListView.builder(
                    padding: const EdgeInsets.fromLTRB(20, 10, 20, 80),
                    itemCount: sortedYears.length,
                    itemBuilder: (context, yearIndex) {
                      final year = sortedYears[yearIndex];
                      final yearEvents = _filteredEvents.where((e) => e.appointmentDate.year == year).toList();

                      return Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          // Year header with icon
                          Padding(
                            padding: const EdgeInsets.symmetric(vertical: 14),
                            child: Row(
                              children: [
                                Container(
                                  width: 32,
                                  height: 32,
                                  decoration: const BoxDecoration(
                                    color: AppColors.primary,
                                    shape: BoxShape.circle,
                                  ),
                                  alignment: Alignment.center,
                                  child: Text(
                                    year.toString().substring(2),
                                    style: const TextStyle(
                                      color: Colors.white,
                                      fontWeight: FontWeight.w900,
                                      fontSize: 14,
                                    ),
                                  ),
                                ),
                                const SizedBox(width: 12),
                                Text(
                                  year.toString(),
                                  style: TextStyle(
                                    fontSize: 22,
                                    fontWeight: FontWeight.w900,
                                    color: context.textPrimary,
                                  ),
                                ),
                              ],
                            ),
                          ),

                          // List of events under this year — mốc thời gian có màu theo đúng
                          // trạng thái buổi khám (xanh lá = đang điều trị, sky = đã hoàn thành),
                          // khớp với badge trên chính thẻ sự kiện thay vì chỉ là 1 đường kẻ vô nghĩa.
                          Column(
                            children: List.generate(yearEvents.length, (eventIndex) {
                              final event = yearEvents[eventIndex];
                              final isLastOverall = year == sortedYears.last && eventIndex == yearEvents.length - 1;
                              final dotColor = event.isJourney ? AppColors.success : AppColors.secondary;
                              return IntrinsicHeight(
                                child: Row(
                                  crossAxisAlignment: CrossAxisAlignment.stretch,
                                  children: [
                                    SizedBox(
                                      width: 32,
                                      child: Column(
                                        children: [
                                          const SizedBox(height: 6),
                                          Container(
                                            width: 10,
                                            height: 10,
                                            decoration: BoxDecoration(
                                              color: dotColor,
                                              shape: BoxShape.circle,
                                              border: Border.all(color: context.bg, width: 2),
                                            ),
                                          ),
                                          if (!isLastOverall)
                                            Expanded(
                                              child: Container(
                                                width: 2,
                                                color: context.isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0),
                                              ),
                                            ),
                                        ],
                                      ),
                                    ),
                                    const SizedBox(width: 8),
                                    // Event Card Content
                                    Expanded(
                                      child: _buildEventCard(event, isVi),
                                    ),
                                  ],
                                ),
                              );
                            }),
                          ),
                        ],
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }

  String _formatDate(DateTime d) => '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';

  Widget _buildEventCard(MedicalHistoryEvent event, bool isVi) {
    final isJourney = event.isJourney;
    final statusColor = isJourney ? const Color(0xFF10B981) : const Color(0xFF0284C7);
    final statusBg = isJourney
        ? const Color(0xFF10B981).withValues(alpha: 0.15)
        : const Color(0xFF0284C7).withValues(alpha: 0.1);

    return Container(
      margin: const EdgeInsets.only(bottom: 16),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: context.divider),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: context.isDark ? 0.15 : 0.04),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(16),
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: () => context.push(AppRoutes.examinationDetail, extra: event),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(
                        child: Text(
                          event.serviceName,
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.w800,
                            color: context.textPrimary,
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                        decoration: BoxDecoration(
                          color: statusBg,
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          isJourney
                              ? (isVi ? 'Đang điều trị' : 'In progress')
                              : (isVi ? 'Hoàn thành' : 'Completed'),
                          style: TextStyle(
                            fontSize: 10,
                            fontWeight: FontWeight.w900,
                            color: statusColor,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      Icon(Iconsax.briefcase, size: 14, color: context.textMuted),
                      const SizedBox(width: 8),
                      Text(
                        event.dentistName,
                        style: TextStyle(
                          fontSize: 13,
                          fontWeight: FontWeight.w600,
                          color: context.textSecondary,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Row(
                    children: [
                      Icon(Iconsax.calendar_1, size: 14, color: context.textMuted),
                      const SizedBox(width: 8),
                      Text(
                        _formatDate(event.appointmentDate),
                        style: TextStyle(
                          fontSize: 13,
                          color: context.textSecondary,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
