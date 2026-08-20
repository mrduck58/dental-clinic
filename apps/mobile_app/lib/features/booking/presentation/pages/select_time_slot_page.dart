import 'dart:async';
import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/core/utils/app_toast.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';
import 'package:mobile_app/features/booking/presentation/widgets/booking_widgets.dart';

class SelectTimeSlotPage extends StatefulWidget {
  final BookingDraft draft;
  const SelectTimeSlotPage({super.key, required this.draft});

  @override
  State<SelectTimeSlotPage> createState() => _SelectTimeSlotPageState();
}

class _SelectTimeSlotPageState extends State<SelectTimeSlotPage> {
  final _service = BookingService();
  late DateTime _currentDate;
  ApiDoctorWithSlots? _doctorWithSlots;
  ApiTimeSlot? _selectedSlot;
  bool _loading = true;
  bool _isHoldingSlot = false;
  String? _error;
  bool _isShowingExpiredDialog = false;
  Timer? _autoRefreshTimer;
  DateTime? _holdExpiresAt;

  static const _weekdaysVi = [
    '', 'Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy', 'Chủ Nhật'
  ];
  static const _weekdaysShortVi = [
    '', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'
  ];
  static const _weekdaysEn = [
    '', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'
  ];
  static const _weekdaysShortEn = [
    '', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'
  ];

  @override
  void initState() {
    super.initState();
    _currentDate = widget.draft.date ?? DateTime.now();
    _load();
    _checkActiveHold();
    // Tự động làm mới dữ liệu slot mỗi 8 giây để đồng bộ real-time
    _autoRefreshTimer = Timer.periodic(const Duration(seconds: 8), (_) {
      if (mounted && !_loading && !_isHoldingSlot) {
        _load(silent: true);
      }
    });
  }

  @override
  void dispose() {
    _autoRefreshTimer?.cancel();
    super.dispose();
  }

  Future<void> _checkActiveHold() async {
    final patientId = widget.draft.patient?.id;
    if (patientId == null || patientId.isEmpty || patientId == 'self') return;
    try {
      final activeHold = await _service.getActiveHold(patientId: patientId);
      if (activeHold != null && activeHold.remainingSeconds > 0 && mounted) {
        final targetDocId = widget.draft.doctor?.id ?? widget.draft.preferredDentistId;
        if (targetDocId != null && activeHold.holdId.isNotEmpty) {
          _startHoldCountdown(activeHold.remainingSeconds, activeHold.expiresAt);
        }
      }
    } catch (_) {}
  }

  void _startHoldCountdown(int seconds, DateTime? expiresAt) {
    setState(() {
      _holdExpiresAt = expiresAt ?? DateTime.now().add(Duration(seconds: seconds));
    });
  }

  Future<void> _load({bool silent = false}) async {
    if (!silent) {
      setState(() {
        _loading = true;
        _error = null;
      });
    }
    try {
      final list = await _service.getDoctorsWithSlots(_currentDate);
      if (!mounted) return;

      final targetDocId = widget.draft.doctor?.id ?? widget.draft.preferredDentistId;
      ApiDoctorWithSlots? matched;

      if (targetDocId != null && targetDocId.isNotEmpty) {
        matched = list.where((d) => d.dentistId == targetDocId).firstOrNull;
      }

      matched ??= list.firstOrNull;

      // Pre-select slot from draft if not already set and is on the same date
      if (_selectedSlot == null && widget.draft.timeSlot != null && matched != null) {
        final sameDate = widget.draft.date != null &&
            widget.draft.date!.year == _currentDate.year &&
            widget.draft.date!.month == _currentDate.month &&
            widget.draft.date!.day == _currentDate.day;
        if (sameDate) {
          _selectedSlot = matched.slots.where((s) => s.range == widget.draft.timeSlot!.range).firstOrNull;
        }
      }

      setState(() {
        _doctorWithSlots = matched;
        _loading = false;
        // Nếu slot đã chọn bị người khác đặt hoặc không còn khả dụng (ngoại trừ slot lịch hẹn đang dời của chính mình)
        if (_selectedSlot != null && matched != null && (_holdExpiresAt == null || _holdExpiresAt!.isBefore(DateTime.now()))) {
          final isCurrentReschedulingSlot = widget.draft.isRescheduling &&
              widget.draft.timeSlot != null &&
              _selectedSlot!.range == widget.draft.timeSlot!.range;

          final stillAvailable = isCurrentReschedulingSlot || matched.slots.any(
            (s) => s.range == _selectedSlot!.range && !s.isBooked,
          );
          if (!stillAvailable) _selectedSlot = null;
        }
      });
    } catch (e) {
      if (mounted && !silent) {
        setState(() {
          _error = 'Không thể tải thông tin ca khám.';
          _loading = false;
        });
      }
    }
  }

  void _onDateSelected(DateTime newDate) {
    if (_currentDate.year == newDate.year &&
        _currentDate.month == newDate.month &&
        _currentDate.day == newDate.day) {
      return;
    }
    setState(() {
      _currentDate = newDate;
      _selectedSlot = null;
      _holdExpiresAt = null;
    });
    _load();
  }

  Future<void> _pickCustomDate() async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _currentDate.isBefore(now) ? now : _currentDate,
      firstDate: DateTime(now.year, now.month, now.day),
      lastDate: DateTime(now.year + 1, now.month, now.day),
    );
    if (picked != null) {
      _onDateSelected(picked);
    }
  }

  String _calculateDisplaySlotRange(String rawRange) {
    final duration = widget.draft.service?.durationMinutes ?? 0;
    if (duration <= 30) return rawRange;
    try {
      final startPart = rawRange.split(' - ').first.trim();
      final parts = startPart.split(':');
      final startH = int.parse(parts[0]);
      final startM = int.parse(parts[1]);

      final totalEndMinutes = startH * 60 + startM + duration;
      final endH = (totalEndMinutes ~/ 60) % 24;
      final endM = totalEndMinutes % 60;

      final startStr = '${startH.toString().padLeft(2, '0')}:${startM.toString().padLeft(2, '0')}';
      final endStr = '${endH.toString().padLeft(2, '0')}:${endM.toString().padLeft(2, '0')}';
      return '$startStr - $endStr';
    } catch (_) {
      return rawRange;
    }
  }

  bool _isSlotInSelectedRange(String slotRange) {
    if (_selectedSlot == null) return false;
    final duration = widget.draft.service?.durationMinutes ?? 30;
    if (duration <= 30) return _selectedSlot!.range == slotRange;

    try {
      final selStartPart = _selectedSlot!.range.split(' - ').first.trim();
      final selParts = selStartPart.split(':');
      final selStartMin = int.parse(selParts[0]) * 60 + int.parse(selParts[1]);
      final selEndMin = selStartMin + duration;

      final curStartPart = slotRange.split(' - ').first.trim();
      final curParts = curStartPart.split(':');
      final curStartMin = int.parse(curParts[0]) * 60 + int.parse(curParts[1]);
      final curEndMin = curStartMin + 30;

      return curStartMin >= selStartMin && curEndMin <= selEndMin;
    } catch (_) {
      return _selectedSlot!.range == slotRange;
    }
  }

  void _onSlotTapped(ApiTimeSlot slot) {
    if ((slot.isBooked && !slot.isHeldByMe) || _isHoldingSlot) return;
    setState(() {
      _selectedSlot = slot;
    });
  }

  String _extractDioError(DioException e) {
    try {
      final data = e.response?.data;
      if (data is Map<String, dynamic>) {
        if (data.containsKey('message')) return data['message'].toString();
        if (data.containsKey('detail')) return data['detail'].toString();
        if (data.containsKey('title')) return data['title'].toString();
      }
    } catch (_) {}
    return 'Ca khám này hiện không khả dụng. Vui lòng chọn ca khám khác.';
  }

  void _showHoldErrorDialog(String message) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: Row(
          children: [
            const Icon(Iconsax.info_circle, color: Color(0xFFDC2626), size: 24),
            const SizedBox(width: 8),
            Text(
              isVi ? 'Không thể giữ chỗ' : 'Unable to Hold Slot',
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
          ],
        ),
        content: Text(
          message,
          style: const TextStyle(fontSize: 14, height: 1.4),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(ctx).pop(),
            child: Text(
              isVi ? 'Đã hiểu' : 'Understood',
              style: const TextStyle(fontWeight: FontWeight.bold, color: AppColors.primary),
            ),
          ),
        ],
      ),
    );
  }

  void _showHoldExpiredDialog() {
    if (!mounted || _isShowingExpiredDialog) return;
    _isShowingExpiredDialog = true;
    setState(() {
      _holdExpiresAt = null;
      _selectedSlot = null;
      _isHoldingSlot = false;
    });
    _service.clearActiveDraft();
    _load(silent: true);

    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        title: Row(
          children: [
            const Icon(Iconsax.timer_pause, color: Color(0xFFD97706), size: 24),
            const SizedBox(width: 8),
            Text(
              isVi ? 'Hết giờ giữ chỗ' : 'Hold Expired',
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
          ],
        ),
        content: Text(
          isVi
              ? 'Thời gian giữ chỗ tạm thời (5 phút) cho ca khám đã kết thúc. Ca khám đã được giải phóng để bệnh nhân khác có thể đặt.'
              : 'The 5-minute temporary hold period has ended. The slot has been released.',
          style: const TextStyle(fontSize: 14, height: 1.4),
        ),
        actions: [
          ElevatedButton(
            onPressed: () {
              Navigator.of(ctx).pop();
              _load();
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: AppColors.primary,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
            ),
            child: Text(isVi ? 'Chọn lại ca khám' : 'Select Slot Again'),
          ),
        ],
      ),
    ).then((_) {
      _isShowingExpiredDialog = false;
    });
  }

  String _fmtDate(DateTime d) =>
      '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';

  String _dayLabel(DateTime d, bool isVi) {
    final weekdays = isVi ? _weekdaysVi : _weekdaysEn;
    return '${weekdays[d.weekday]}, ${_fmtDate(d)}';
  }

  static String _weekdayShort(DateTime d, bool isVi) {
    final list = isVi ? _weekdaysShortVi : _weekdaysShortEn;
    return list[d.weekday];
  }

  static String _periodLabel(String period, bool isVi) {
    if (isVi) return period.isEmpty ? 'Ca khám' : period;
    return switch (period) {
      'Buổi sáng' => 'Morning',
      'Buổi chiều' => 'Afternoon',
      'Buổi tối' => 'Evening',
      _ => 'Shift',
    };
  }

  Future<void> _onContinue() async {
    if (_selectedSlot == null || _doctorWithSlots == null || _isHoldingSlot) return;
    final doc = _doctorWithSlots!;
    final patientId = widget.draft.patient?.id ?? 'self';

    setState(() {
      _isHoldingSlot = true;
    });

    try {
      final result = await _service.holdSlot(
        patientId: patientId == 'self' ? '' : patientId,
        dentistId: doc.dentistId,
        date: _currentDate,
        timeSlot: _selectedSlot!.range,
        serviceId: widget.draft.service?.id,
        reschedulingAppointmentId: widget.draft.reschedulingAppointmentId,
      );

      if (!mounted) return;

      setState(() {
        _isHoldingSlot = false;
      });

      if (result.isSuccess) {
        _startHoldCountdown(result.remainingSeconds, result.expiresAt);
        final doctorInfo = doc.toDoctorInfo();
        final updatedDraft = widget.draft.copyWith(
          date: _currentDate,
          doctor: doctorInfo,
          timeSlot: _selectedSlot!.toTimeSlot(),
          holdExpiresAt: result.expiresAt ?? _holdExpiresAt,
        );
        _service.setActiveDraft(updatedDraft);
        context.push(AppRoutes.bookingReview, extra: updatedDraft);
      } else {
        _showHoldErrorDialog(result.message);
        _load(silent: true);
      }
    } on DioException catch (e) {
      if (!mounted) return;
      setState(() {
        _isHoldingSlot = false;
      });
      final msg = _extractDioError(e);
      final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
      if (msg.contains('3 lần giữ chỗ không thành công') || msg.contains('giới hạn 3 lần')) {
        final doctorInfo = doc.toDoctorInfo();
        final updatedDraft = widget.draft.copyWith(
          date: _currentDate,
          doctor: doctorInfo,
          timeSlot: _selectedSlot!.toTimeSlot(),
          holdExpiresAt: null,
        );
        AppToast.showWarning(
          context,
          isVi
              ? 'Bạn đã hết lượt giữ chỗ tạm thời hôm nay. Bạn có thể tiếp tục xác nhận đặt lịch trực tiếp.'
              : 'Daily hold limit reached. You can proceed to confirm your booking directly without holding.',
        );
        context.push(AppRoutes.bookingReview, extra: updatedDraft);
        return;
      }
      _showHoldErrorDialog(msg);
      _load(silent: true);
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _isHoldingSlot = false;
      });
      _showHoldErrorDialog('Không thể giữ ca khám. Vui lòng thử lại.');
      _load(silent: true);
    }
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final doc = _doctorWithSlots;

    return Scaffold(
      backgroundColor: context.bg,
      appBar: BookingAppBar(title: isVi ? 'Chọn ca khám' : 'Select Time Slot'),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Text(
                        isVi ? 'Không thể tải thông tin ca khám.' : 'Unable to load slots.',
                        style: TextStyle(color: context.textSecondary),
                      ),
                      const SizedBox(height: 12),
                      TextButton(
                        onPressed: () => _load(),
                        child: Text(isVi ? 'Thử lại' : 'Retry'),
                      ),
                    ],
                  ),
                )
              : doc == null
                  ? Center(
                      child: Padding(
                        padding: const EdgeInsets.all(24.0),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            const Icon(Iconsax.calendar_remove, size: 48, color: AppColors.primary),
                            const SizedBox(height: 16),
                            Text(
                              isVi ? 'Bác sĩ không có lịch trực vào ngày này' : 'No available slots on this date',
                              textAlign: TextAlign.center,
                              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: context.textPrimary),
                            ),
                            const SizedBox(height: 8),
                            Text(
                              isVi ? 'Vui lòng chọn ngày khác bên dưới để tiếp tục.' : 'Please select another date below.',
                              textAlign: TextAlign.center,
                              style: TextStyle(fontSize: 13, color: context.textSecondary),
                            ),
                            const SizedBox(height: 16),
                            ElevatedButton.icon(
                              onPressed: _pickCustomDate,
                              icon: const Icon(Iconsax.calendar_1, size: 18),
                              label: Text(isVi ? 'Chọn ngày khác' : 'Change date'),
                              style: ElevatedButton.styleFrom(
                                backgroundColor: AppColors.primary,
                                foregroundColor: Colors.white,
                              ),
                            ),
                          ],
                        ),
                      ),
                    )
                  : CustomScrollView(
                      slivers: [
                        if (_holdExpiresAt != null || widget.draft.holdExpiresAt != null)
                          SliverToBoxAdapter(
                            child: HoldCountdownBanner(
                              holdExpiresAt: _holdExpiresAt ?? widget.draft.holdExpiresAt,
                              onExpired: _showHoldExpiredDialog,
                            ),
                          ),
                        // ── Doctor Info Header Card ───────────────────────────
                        SliverToBoxAdapter(
                          child: Container(
                            margin: const EdgeInsets.fromLTRB(16, 14, 16, 8),
                            padding: const EdgeInsets.all(16),
                            decoration: BoxDecoration(
                              color: context.card,
                              borderRadius: BorderRadius.circular(16),
                              border: Border.all(color: context.divider),
                              boxShadow: [
                                BoxShadow(
                                  color: Colors.black.withValues(alpha: 0.03),
                                  blurRadius: 10,
                                  offset: const Offset(0, 3),
                                ),
                              ],
                            ),
                            child: Row(
                              children: [
                                _DoctorAvatar(avatarUrl: doc.avatarUrl, size: 52),
                                const SizedBox(width: 14),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        doc.fullName,
                                        style: TextStyle(
                                          fontSize: 16,
                                          fontWeight: FontWeight.w800,
                                          color: context.isDark ? Colors.white : AppColors.primary,
                                        ),
                                      ),
                                      const SizedBox(height: 3),
                                      Text(
                                        doc.specialization,
                                        style: TextStyle(
                                          fontSize: 13,
                                          color: context.textSecondary,
                                        ),
                                      ),
                                      if (doc.experienceYears > 0) ...[
                                        const SizedBox(height: 4),
                                        Row(
                                          children: [
                                            const Icon(Iconsax.award, size: 14, color: Color(0xFFD97706)),
                                            const SizedBox(width: 4),
                                            Text(
                                              isVi ? '${doc.experienceYears} năm kinh nghiệm' : '${doc.experienceYears} yrs experience',
                                              style: const TextStyle(
                                                fontSize: 12,
                                                fontWeight: FontWeight.w600,
                                                color: Color(0xFFD97706),
                                              ),
                                            ),
                                          ],
                                        ),
                                      ],
                                    ],
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),

                        // ── Service & Estimate Time Badge ────────────────────
                        if (widget.draft.service != null)
                          SliverToBoxAdapter(
                            child: Container(
                              margin: const EdgeInsets.fromLTRB(16, 0, 16, 8),
                              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                              decoration: BoxDecoration(
                                color: context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF0FDF4),
                                borderRadius: BorderRadius.circular(12),
                                border: Border.all(
                                  color: context.isDark ? context.divider : const Color(0xFFBBF7D0),
                                ),
                              ),
                              child: Row(
                                children: [
                                  Container(
                                    padding: const EdgeInsets.all(6),
                                    decoration: BoxDecoration(
                                      color: context.isDark ? const Color(0xFF064E3B) : const Color(0xFFDCFCE7),
                                      shape: BoxShape.circle,
                                    ),
                                    child: const Icon(Iconsax.clock, size: 16, color: Color(0xFF16A34A)),
                                  ),
                                  const SizedBox(width: 10),
                                  Expanded(
                                    child: Column(
                                      crossAxisAlignment: CrossAxisAlignment.start,
                                      children: [
                                        Text(
                                          widget.draft.service!.name,
                                          style: TextStyle(
                                            fontSize: 13,
                                            fontWeight: FontWeight.w700,
                                            color: context.textPrimary,
                                          ),
                                          maxLines: 1,
                                          overflow: TextOverflow.ellipsis,
                                        ),
                                        const SizedBox(height: 2),
                                        Row(
                                          children: [
                                            Text(
                                              isVi ? 'Thời lượng dự kiến: ' : 'Est. Duration: ',
                                              style: TextStyle(fontSize: 11.5, color: context.textSecondary),
                                            ),
                                            Text(
                                              widget.draft.service!.durationText.isNotEmpty
                                                  ? widget.draft.service!.durationText
                                                  : (isVi ? '~30 phút' : '~30 mins'),
                                              style: const TextStyle(
                                                fontSize: 11.5,
                                                fontWeight: FontWeight.w700,
                                                color: Color(0xFF16A34A),
                                              ),
                                            ),
                                            if (widget.draft.service!.durationMinutes > 30) ...[
                                              const SizedBox(width: 4),
                                              Text(
                                                isVi
                                                    ? '(${((widget.draft.service!.durationMinutes + 29) ~/ 30)} ca)'
                                                    : '(${((widget.draft.service!.durationMinutes + 29) ~/ 30)} slots)',
                                                style: TextStyle(fontSize: 11, color: context.textSecondary),
                                              ),
                                            ],
                                          ],
                                        ),
                                      ],
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),


                        // ── Date Selector Strip ───────────────────────────────
                        SliverToBoxAdapter(
                          child: Container(
                            margin: const EdgeInsets.fromLTRB(16, 6, 16, 12),
                            decoration: BoxDecoration(
                              color: context.card,
                              borderRadius: BorderRadius.circular(14),
                              border: Border.all(color: context.divider),
                            ),
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Padding(
                                  padding: const EdgeInsets.fromLTRB(14, 12, 14, 6),
                                  child: Row(
                                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                    children: [
                                      Row(
                                        children: [
                                          const Icon(Iconsax.calendar_1, size: 16, color: AppColors.primary),
                                          const SizedBox(width: 6),
                                          Text(
                                            _dayLabel(_currentDate, isVi),
                                            style: TextStyle(
                                              fontSize: 14,
                                              fontWeight: FontWeight.w700,
                                              color: context.textPrimary,
                                            ),
                                          ),
                                        ],
                                      ),
                                      GestureDetector(
                                        onTap: _pickCustomDate,
                                        child: Text(
                                          isVi ? 'Đổi ngày' : 'Change',
                                          style: const TextStyle(
                                            fontSize: 12,
                                            fontWeight: FontWeight.w700,
                                            color: AppColors.primary,
                                          ),
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                                Divider(color: context.divider, height: 1),
                                SizedBox(
                                  height: 66,
                                  child: ListView.builder(
                                    scrollDirection: Axis.horizontal,
                                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
                                    itemCount: 14,
                                    itemBuilder: (context, index) {
                                      final d = DateTime.now().add(Duration(days: index));
                                      final isSelected = d.year == _currentDate.year &&
                                          d.month == _currentDate.month &&
                                          d.day == _currentDate.day;
                                      final isToday = index == 0;
                                      final shortWeekday = _weekdayShort(d, isVi);

                                      return GestureDetector(
                                        onTap: () => _onDateSelected(d),
                                        child: Container(
                                          width: 60,
                                          margin: const EdgeInsets.symmetric(horizontal: 4),
                                          decoration: BoxDecoration(
                                            color: isSelected
                                                ? AppColors.primary
                                                : (context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF8FAFC)),
                                            borderRadius: BorderRadius.circular(10),
                                            border: Border.all(
                                              color: isSelected ? AppColors.primary : context.divider,
                                            ),
                                          ),
                                          child: Column(
                                            mainAxisAlignment: MainAxisAlignment.center,
                                            children: [
                                              Text(
                                                isToday ? (isVi ? 'Hôm nay' : 'Today') : shortWeekday,
                                                style: TextStyle(
                                                  fontSize: 10,
                                                  fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
                                                  color: isSelected
                                                      ? Colors.white.withValues(alpha: 0.9)
                                                      : context.textSecondary,
                                                ),
                                              ),
                                              const SizedBox(height: 2),
                                              Text(
                                                '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}',
                                                style: TextStyle(
                                                  fontSize: 12,
                                                  fontWeight: FontWeight.w800,
                                                  color: isSelected ? Colors.white : context.textPrimary,
                                                ),
                                              ),
                                            ],
                                          ),
                                        ),
                                      );
                                    },
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),

                        // ── Available Slots List ──────────────────────────────
                        if (doc.slots.isEmpty)
                          SliverFillRemaining(
                            hasScrollBody: false,
                            child: Center(
                              child: Padding(
                                padding: const EdgeInsets.all(24.0),
                                child: Text(
                                  isVi ? 'Bác sĩ không có ca khám nào trong ngày này.' : 'No slots available on this date.',
                                  style: TextStyle(color: context.textSecondary),
                                ),
                              ),
                            ),
                          )
                        else
                          ..._buildPeriodSections(doc.slots, isVi),

                        const SliverToBoxAdapter(child: SizedBox(height: 100)),
                      ],
                    ),
      bottomNavigationBar: doc != null && doc.slots.isNotEmpty
          ? BookingBottomBar(
              label: _isHoldingSlot
                  ? (isVi ? 'Đang giữ ca khám...' : 'Holding slot...')
                  : _selectedSlot != null
                      ? (isVi
                          ? ((widget.draft.service?.durationMinutes ?? 0) > 30
                              ? 'Tiếp tục • ${_calculateDisplaySlotRange(_selectedSlot!.range)} (~${widget.draft.service!.durationMinutes}p)'
                              : 'Tiếp tục • ${_selectedSlot!.range}')
                          : ((widget.draft.service?.durationMinutes ?? 0) > 30
                              ? 'Continue • ${_calculateDisplaySlotRange(_selectedSlot!.range)} (~${widget.draft.service!.durationMinutes}m)'
                              : 'Continue • ${_selectedSlot!.range}'))
                      : (isVi ? 'Vui lòng chọn ca khám' : 'Please select a slot'),
              onTap: (_selectedSlot != null && !_isHoldingSlot) ? _onContinue : null,
            )
          : null,
    );
  }

  List<Widget> _buildPeriodSections(List<ApiTimeSlot> slots, bool isVi) {
    final periods = ['Buổi sáng', 'Buổi chiều', 'Buổi tối'];
    final groups = <String, List<ApiTimeSlot>>{};

    for (final s in slots) {
      final key = s.period.isNotEmpty ? s.period : 'Buổi sáng';
      groups.putIfAbsent(key, () => []).add(s);
    }

    final orderedKeys = [
      ...periods.where((p) => groups.containsKey(p)),
      ...groups.keys.where((k) => !periods.contains(k)),
    ];

    final now = DateTime.now();
    final isToday = _currentDate.year == now.year &&
        _currentDate.month == now.month &&
        _currentDate.day == now.day;

    final widgets = <Widget>[];

    for (final period in orderedKeys) {
      final periodSlots = groups[period]!;
      final icon = period.contains('chiều')
          ? Icons.wb_sunny_rounded
          : period.contains('tối')
              ? Icons.nights_stay_rounded
              : Icons.wb_twilight_rounded;

      widgets.add(
        SliverToBoxAdapter(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
            child: Row(
              children: [
                Icon(icon, size: 18, color: AppColors.primary),
                const SizedBox(width: 8),
                Text(
                  _periodLabel(period, isVi),
                  style: TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.w800,
                    color: context.textPrimary,
                  ),
                ),
                const SizedBox(width: 8),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                  decoration: BoxDecoration(
                    color: AppColors.primaryLight.withValues(alpha: 0.5),
                    borderRadius: BorderRadius.circular(99),
                  ),
                  child: Text(
                    '${periodSlots.where((s) => (!s.isBooked || s.isHeldByMe) && (!s.isHeld || s.isHeldByMe)).length} ${isVi ? 'trống' : 'open'}',
                    style: const TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: AppColors.primary,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      );

      widgets.add(
        SliverPadding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
          sliver: SliverGrid(
            gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: 3,
              mainAxisSpacing: 10,
              crossAxisSpacing: 10,
              childAspectRatio: 2.2,
            ),
            delegate: SliverChildBuilderDelegate(
              (context, index) {
                final slot = periodSlots[index];
                final isBooked = slot.isBooked;
                final isHeld = slot.isHeld;

                // Kiểm tra xem slot trong quá khứ nếu là hôm nay
                bool isPast = false;
                if (isToday) {
                  try {
                    final startPart = slot.range.split(' - ').first.trim();
                    final parts = startPart.split(':');
                    final slotHour = int.parse(parts[0]);
                    final slotMin = int.parse(parts[1]);
                    if (now.hour > slotHour || (now.hour == slotHour && now.minute >= slotMin)) {
                      isPast = true;
                    }
                  } catch (_) {}
                }

                final isCurrentRescheduleSlot = widget.draft.isRescheduling &&
                    widget.draft.timeSlot != null &&
                    _isSlotInSelectedRange(slot.range);

                final isAvailable = !isPast && (!isBooked || slot.isHeldByMe || isCurrentRescheduleSlot) && (!isHeld || slot.isHeldByMe);
                final isSelected = _isSlotInSelectedRange(slot.range);

                return GestureDetector(
                  onTap: isAvailable ? () => _onSlotTapped(slot) : null,
                  child: AnimatedContainer(
                    duration: const Duration(milliseconds: 150),
                    decoration: BoxDecoration(
                      color: isSelected
                          ? AppColors.primary
                          : isAvailable
                              ? (context.isDark ? const Color(0xFF1E293B) : Colors.white)
                              : (context.isDark ? const Color(0xFF0F172A) : const Color(0xFFF1F5F9)),
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(
                        color: isSelected
                            ? AppColors.primary
                            : isAvailable
                                ? AppColors.primary.withValues(alpha: 0.3)
                                : (context.isDark ? const Color(0xFF334155) : const Color(0xFFE2E8F0)),
                        width: isSelected ? 1.5 : 1,
                      ),
                      boxShadow: isSelected
                          ? [
                              BoxShadow(
                                color: AppColors.primary.withValues(alpha: 0.3),
                                blurRadius: 6,
                                offset: const Offset(0, 2),
                              ),
                            ]
                          : null,
                    ),
                    alignment: Alignment.center,
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Text(
                          slot.range,
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: isSelected ? FontWeight.w800 : (isAvailable ? FontWeight.w600 : FontWeight.w400),
                            color: isSelected
                                ? Colors.white
                                : isAvailable
                                    ? (context.isDark ? Colors.white : AppColors.primary)
                                    : context.textMuted,
                          ),
                        ),
                        if (isSelected && isCurrentRescheduleSlot && (_holdExpiresAt == null || _holdExpiresAt!.isBefore(DateTime.now())))
                          Text(
                            isVi ? 'Lịch hiện tại' : 'Current slot',
                            style: const TextStyle(
                              fontSize: 9,
                              fontWeight: FontWeight.w600,
                              color: Colors.white70,
                            ),
                          )
                        else if (isHeld)
                          Text(
                            slot.isHeldByMe
                                ? (isVi ? 'Đang giữ' : 'Your hold')
                                : (isVi ? 'Đang giữ' : 'Held'),
                            style: TextStyle(
                              fontSize: 9,
                              fontWeight: FontWeight.w600,
                              color: slot.isHeldByMe ? (isSelected ? Colors.white70 : AppColors.primary) : const Color(0xFFD97706),
                            ),
                          )
                        else if (isBooked && !isCurrentRescheduleSlot)
                          Text(
                            isVi ? 'Đã kín' : 'Booked',
                            style: const TextStyle(
                              fontSize: 9,
                              fontWeight: FontWeight.w600,
                              color: Color(0xFFEF4444),
                            ),
                          )
                        else if (isPast)
                          Text(
                            isVi ? 'Đã qua' : 'Passed',
                            style: TextStyle(
                              fontSize: 9,
                              fontWeight: FontWeight.w500,
                              color: context.textMuted,
                            ),
                          ),
                      ],
                    ),
                  ),
                );
              },
              childCount: periodSlots.length,
            ),
          ),
        ),
      );
    }

    return widgets;
  }
}

class _DoctorAvatar extends StatelessWidget {
  final String? avatarUrl;
  final double size;
  const _DoctorAvatar({this.avatarUrl, this.size = 46});

  @override
  Widget build(BuildContext context) {
    if (avatarUrl != null && avatarUrl!.isNotEmpty) {
      if (avatarUrl!.startsWith('assets/')) {
        return ClipOval(
          child: Image.asset(avatarUrl!, width: size, height: size, fit: BoxFit.cover),
        );
      }
      final resolved = ApiConstants.resolveAssetUrl(avatarUrl);
      if (resolved != null) {
        return ClipOval(
          child: Image.network(
            resolved,
            width: size,
            height: size,
            fit: BoxFit.cover,
            errorBuilder: (context, error, stackTrace) => _fallback(),
          ),
        );
      }
    }
    return _fallback();
  }

  Widget _fallback() => Container(
        width: size,
        height: size,
        decoration: const BoxDecoration(
          color: AppColors.primaryLight,
          shape: BoxShape.circle,
        ),
        child: Icon(Iconsax.user, color: AppColors.primary, size: size * 0.55),
      );
}
