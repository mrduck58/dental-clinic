import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';
import 'package:mobile_app/features/home/data/medication_reminder_service.dart';
import 'package:mobile_app/features/home/data/models/medication_reminder_model.dart';

class RemindersPage extends StatefulWidget {
  const RemindersPage({super.key});

  @override
  State<RemindersPage> createState() => _RemindersPageState();
}

class _RemindersPageState extends State<RemindersPage> {
  DateTime _selectedDate = DateTime.now();
  List<MedicationReminder> _reminders = [];
  List<MyAppointmentItem> _appointmentReminders = [];
  final Map<String, bool> _taken = {};
  bool _isLoading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  String _takenKey(MedicationReminder r) => '${r.prescriptionItemId}|${r.time}';

  Future<void> _load() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });
    try {
      final medicationFuture = MedicationReminderService().getReminders(_selectedDate);
      final appointmentsFuture = BookingService().getMyAppointments().catchError((_) => <MyAppointmentItem>[]);

      final reminders = await medicationFuture;
      final allAppointments = await appointmentsFuture;

      final dayAppointments = allAppointments.where((a) {
        return _isSameDay(a.parsedDate, _selectedDate) && a.status != 'Cancelled';
      }).toList();

      final takenEntries = await Future.wait(reminders.map((r) async {
        final taken = await ReminderTakenStore.isTaken(r.prescriptionItemId, _selectedDate, r.time);
        return MapEntry(_takenKey(r), taken);
      }));

      if (!mounted) return;
      setState(() {
        _reminders = reminders;
        _appointmentReminders = dayAppointments;
        _taken
          ..clear()
          ..addEntries(takenEntries);
        _isLoading = false;
      });
    } catch (_) {
      setState(() {
        _error = 'load_failed';
        _isLoading = false;
      });
    }
  }

  Future<void> _markTaken(MedicationReminder r) async {
    await ReminderTakenStore.setTaken(r.prescriptionItemId, _selectedDate, r.time, true);
    if (!mounted) return;
    setState(() => _taken[_takenKey(r)] = true);
  }

  bool _isSameDay(DateTime a, DateTime b) => a.year == b.year && a.month == b.month && a.day == b.day;

  /// 'completed' | 'overdue' | 'scheduled' — chỉ 'overdue' khi mốc giờ đã qua và chưa đánh dấu.
  String _statusFor(MedicationReminder r) {
    if (_taken[_takenKey(r)] == true) return 'completed';
    final now = DateTime.now();
    if (_selectedDate.isBefore(DateTime(now.year, now.month, now.day))) return 'overdue';
    if (_isSameDay(_selectedDate, now)) {
      final reminderMinutes = r.hour * 60 + (int.tryParse(r.time.split(':').last) ?? 0);
      final nowMinutes = now.hour * 60 + now.minute;
      return nowMinutes > reminderMinutes ? 'overdue' : 'scheduled';
    }
    return 'scheduled';
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';

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
          isVi ? 'Nhắc nhở y tế & lịch khám' : 'Medical & Booking Reminders',
          style: TextStyle(
            color: context.textPrimary,
            fontWeight: FontWeight.bold,
            fontSize: 18,
          ),
        ),
        centerTitle: true,
      ),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        isVi ? 'Không thể tải lịch nhắc nhở.' : 'Failed to load reminders.',
                        style: TextStyle(color: context.textMuted),
                      ),
                      const SizedBox(height: 12),
                      TextButton(onPressed: _load, child: Text(isVi ? 'Thử lại' : 'Retry')),
                    ],
                  ),
                )
              : SingleChildScrollView(
        padding: const EdgeInsets.all(18.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Month Header
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  _monthLabel(_selectedDate, isVi),
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w900,
                    color: context.textPrimary,
                  ),
                ),
                Icon(Iconsax.calendar, color: context.textSecondary, size: 20),
              ],
            ),
            const SizedBox(height: 16),

            _buildDaySelector(isVi),
            const SizedBox(height: 28),

            _buildAppointmentRemindersSection(isVi),
            _buildReminderSection('MORNING', isVi),
            const SizedBox(height: 20),
            _buildReminderSection('AFTERNOON', isVi),
            const SizedBox(height: 20),
            _buildReminderSection('EVENING', isVi),

            if (_reminders.isEmpty && _appointmentReminders.isEmpty)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 40),
                child: Center(
                  child: Text(
                    isVi ? 'Không có thuốc cần uống hoặc lịch khám nào vào ngày này.' : 'No medications or appointments scheduled for this date.',
                    style: TextStyle(color: context.textMuted),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }

  String _monthLabel(DateTime date, bool isVi) {
    if (isVi) return 'Tháng ${date.month}, ${date.year}';
    const months = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
    return '${months[date.month - 1]} ${date.year}';
  }

  Widget _buildDaySelector(bool isVi) {
    final monday = _selectedDate.subtract(Duration(days: _selectedDate.weekday - 1));
    final days = List.generate(7, (i) => monday.add(Duration(days: i)));
    const weekViLabels = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];
    const weekEnLabels = ['MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT', 'SUN'];

    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: List.generate(7, (i) {
        final day = days[i];
        final isSelected = _isSameDay(_selectedDate, day);
        final weekText = isVi ? weekViLabels[i] : weekEnLabels[i];

        return GestureDetector(
          onTap: () {
            setState(() => _selectedDate = day);
            _load();
          },
          child: Container(
            width: 46,
            padding: const EdgeInsets.symmetric(vertical: 12),
            decoration: BoxDecoration(
              color: isSelected ? AppColors.primary : context.card,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: isSelected ? Colors.transparent : context.divider),
              boxShadow: isSelected
                  ? [
                      BoxShadow(
                        color: AppColors.primary.withValues(alpha: 0.25),
                        blurRadius: 10,
                        offset: const Offset(0, 4),
                      ),
                    ]
                  : null,
            ),
            child: Column(
              children: [
                Text(
                  weekText,
                  style: TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.w900,
                    color: isSelected ? Colors.white.withValues(alpha: 0.9) : context.textSecondary,
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  '${day.day}',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.w900,
                    color: isSelected ? Colors.white : context.textPrimary,
                  ),
                ),
              ],
            ),
          ),
        );
      }),
    );
  }

  Widget _buildReminderSection(String type, bool isVi) {
    final list = _reminders.where((r) {
      if (type == 'MORNING') return r.hour < 12;
      if (type == 'AFTERNOON') return r.hour >= 12 && r.hour < 18;
      return r.hour >= 18;
    }).toList()
      ..sort((a, b) => a.time.compareTo(b.time));
    if (list.isEmpty) return const SizedBox.shrink();

    final sectionTitle = switch (type) {
      'MORNING' => isVi ? 'BUỔI SÁNG' : 'MORNING',
      'AFTERNOON' => isVi ? 'BUỔI CHIỀU' : 'AFTERNOON',
      _ => isVi ? 'BUỔI TỐI' : 'EVENING',
    };
    // Icon + màu riêng cho từng buổi — dễ quét mắt hơn là chỉ đọc chữ "SÁNG/CHIỀU/TỐI".
    final (sectionIcon, sectionColor) = switch (type) {
      'MORNING' => (Icons.wb_twilight_rounded, AppColors.accent),
      'AFTERNOON' => (Icons.wb_sunny_rounded, AppColors.accent),
      _ => (Icons.nights_stay_rounded, const Color(0xFF6366F1)),
    };

    return Padding(
      padding: const EdgeInsets.only(bottom: 20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 24,
                height: 24,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: sectionColor.withValues(alpha: context.isDark ? 0.22 : 0.12),
                  shape: BoxShape.circle,
                ),
                child: Icon(sectionIcon, size: 14, color: sectionColor),
              ),
              const SizedBox(width: 8),
              Text(
                sectionTitle,
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w900,
                  color: context.textSecondary,
                  letterSpacing: 0.5,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          ...list.map((item) => _buildReminderCard(item, isVi)),
        ],
      ),
    );
  }

  Widget _buildReminderCard(MedicationReminder item, bool isVi) {
    final status = _statusFor(item);
    final subtitle = item.usage.isNotEmpty ? '${item.dosage} · ${item.usage}' : item.dosage;

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: context.divider),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  color: context.isDark ? AppColors.primary.withValues(alpha: 0.15) : AppColors.primaryLight,
                  borderRadius: BorderRadius.circular(14),
                ),
                child: const Icon(Icons.local_pharmacy_outlined, color: AppColors.primary, size: 22),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Expanded(
                          child: Text(
                            item.medicineName,
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w800,
                              color: context.textPrimary,
                            ),
                          ),
                        ),
                        _buildStatusBadge(status, isVi),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      subtitle,
                      style: TextStyle(fontSize: 13, color: context.textSecondary),
                    ),
                    if (item.patientRelationship != 'Tôi') ...[
                      const SizedBox(height: 4),
                      Text(
                        '${isVi ? 'Cho' : 'For'}: ${item.patientName}',
                        style: TextStyle(fontSize: 11.5, color: context.textMuted, fontWeight: FontWeight.w600),
                      ),
                    ],
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          Divider(color: context.divider, height: 1),
          const SizedBox(height: 12),
          _buildActionButtons(item, status, isVi),
        ],
      ),
    );
  }

  Widget _buildStatusBadge(String status, bool isVi) {
    Color bg;
    Color text;
    String label;

    if (status == 'completed') {
      bg = const Color(0xFFDCFCE7);
      text = const Color(0xFF16A34A);
      label = isVi ? 'ĐÃ UỐNG' : 'TAKEN';
    } else if (status == 'overdue') {
      bg = const Color(0xFFFEF3C7);
      text = const Color(0xFFD97706);
      label = isVi ? 'QUÁ HẠN' : 'OVERDUE';
    } else {
      bg = context.isDark ? const Color(0xFF334155) : const Color(0xFFF1F5F9);
      text = context.textSecondary;
      label = isVi ? 'ĐÃ LÊN LỊCH' : 'SCHEDULED';
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(6)),
      child: Text(label, style: TextStyle(fontSize: 9, fontWeight: FontWeight.w900, color: text)),
    );
  }

  Widget _buildActionButtons(MedicationReminder item, String status, bool isVi) {
    if (status == 'completed') {
      return Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.check_circle_rounded, color: Color(0xFF10B981), size: 16),
          const SizedBox(width: 6),
          Text(
            isVi ? 'Đã uống thuốc lúc ${item.time}' : 'Taken at ${item.time}',
            style: const TextStyle(fontSize: 13, fontWeight: FontWeight.bold, color: Color(0xFF10B981)),
          ),
        ],
      );
    }
    return SizedBox(
      width: double.infinity,
      height: 38,
      child: ElevatedButton(
        onPressed: () => _markTaken(item),
        style: ElevatedButton.styleFrom(
          backgroundColor: status == 'overdue' ? const Color(0xFFD97706) : AppColors.primary,
          foregroundColor: Colors.white,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
        ),
        child: Text(
          isVi ? 'ĐÁNH DẤU ĐÃ UỐNG' : 'MARK AS TAKEN',
          style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 12),
        ),
      ),
    );
  }

  Widget _buildAppointmentRemindersSection(bool isVi) {
    if (_appointmentReminders.isEmpty) return const SizedBox.shrink();

    return Padding(
      padding: const EdgeInsets.only(bottom: 24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 26,
                height: 26,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: AppColors.primary.withValues(alpha: context.isDark ? 0.25 : 0.12),
                  shape: BoxShape.circle,
                ),
                child: const Icon(Iconsax.calendar_tick, size: 14, color: AppColors.primary),
              ),
              const SizedBox(width: 8),
              Text(
                isVi ? 'LỊCH HẸN KHÁM TRONG NGÀY' : 'BOOKING APPOINTMENTS',
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.w900,
                  color: context.textSecondary,
                  letterSpacing: 0.5,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          ..._appointmentReminders.map((app) => _buildAppointmentCard(app, isVi)),
        ],
      ),
    );
  }

  Widget _buildAppointmentCard(MyAppointmentItem app, bool isVi) {
    final timeStr = app.displayTimeRange;
    final doctorName = app.dentistName.toLowerCase().startsWith('bs') ||
            app.dentistName.toLowerCase().startsWith('bác sĩ') ||
            app.dentistName.toLowerCase().startsWith('dr')
        ? app.dentistName
        : 'BS. ${app.dentistName}';

    final (statusBg, statusText, statusLabel) = switch (app.status) {
      'Confirmed' => (const Color(0xFFDCFCE7), const Color(0xFF16A34A), isVi ? 'ĐÃ XÁC NHẬN' : 'CONFIRMED'),
      'CheckedIn' || 'InProgress' => (const Color(0xFFDBEAFE), const Color(0xFF2563EB), isVi ? 'ĐANG KHÁM' : 'IN PROGRESS'),
      'Completed' => (const Color(0xFFF3E8FF), const Color(0xFF9333EA), isVi ? 'ĐÃ KHÁM' : 'COMPLETED'),
      _ => (const Color(0xFFFEF3C7), const Color(0xFFD97706), isVi ? 'CHỜ XÁC NHẬN' : 'PENDING'),
    };

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: AppColors.primary.withValues(alpha: 0.3)),
        boxShadow: [
          BoxShadow(
            color: AppColors.primary.withValues(alpha: 0.05),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  color: AppColors.primaryLight,
                  borderRadius: BorderRadius.circular(14),
                ),
                child: const Icon(Iconsax.calendar_1, color: AppColors.primary, size: 22),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Expanded(
                          child: Text(
                            doctorName,
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w800,
                              color: context.textPrimary,
                            ),
                          ),
                        ),
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                          decoration: BoxDecoration(color: statusBg, borderRadius: BorderRadius.circular(6)),
                          child: Text(statusLabel, style: TextStyle(fontSize: 9, fontWeight: FontWeight.w900, color: statusText)),
                        ),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      app.serviceName ?? (isVi ? 'Khám nha khoa' : 'Dental Checkup'),
                      style: TextStyle(fontSize: 13, color: context.textSecondary, fontWeight: FontWeight.w600),
                    ),
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        Icon(Iconsax.clock, size: 13, color: context.textMuted),
                        const SizedBox(width: 4),
                        Text(
                          '$timeStr - ${isVi ? "Mã" : "Code"}: #${app.appointmentCode}',
                          style: TextStyle(fontSize: 12, color: context.textMuted),
                        ),
                      ],
                    ),
                    if (app.patientName != null && app.patientName!.isNotEmpty) ...[
                      const SizedBox(height: 4),
                      Text(
                        '${isVi ? 'Bệnh nhân' : 'Patient'}: ${app.patientName}',
                        style: TextStyle(fontSize: 11.5, color: context.textMuted, fontWeight: FontWeight.w600),
                      ),
                    ],
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          Divider(color: context.divider, height: 1),
          const SizedBox(height: 12),
          SizedBox(
            width: double.infinity,
            height: 38,
            child: ElevatedButton.icon(
              onPressed: () => context.push(AppRoutes.appointmentDetails, extra: app),
              icon: const Icon(Iconsax.eye, size: 16),
              label: Text(
                isVi ? 'XEM CHI TIẾT LỊCH HẸN' : 'VIEW APPOINTMENT DETAILS',
                style: const TextStyle(fontWeight: FontWeight.w900, fontSize: 12),
              ),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primary,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
