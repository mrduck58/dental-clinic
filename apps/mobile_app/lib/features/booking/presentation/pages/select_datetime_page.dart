import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/presentation/widgets/booking_widgets.dart';

import 'package:mobile_app/features/booking/data/booking_service.dart';

class SelectDatetimePage extends StatefulWidget {
  final BookingDraft draft;
  const SelectDatetimePage({super.key, required this.draft});

  @override
  State<SelectDatetimePage> createState() => _SelectDatetimePageState();
}

class _SelectDatetimePageState extends State<SelectDatetimePage> {
  late DateTime _month;
  DateTime? _selected;
  Set<String> _workingDates = {};
  bool _loadingWorkingDates = false;

  static const _headersVi = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
  static const _headersEn = ['SUN', 'MON', 'TUE', 'WED', 'THU', 'FRI', 'SAT'];

  static const _monthNamesVi = [
    '', 'Tháng 01', 'Tháng 02', 'Tháng 03', 'Tháng 04',
    'Tháng 05', 'Tháng 06', 'Tháng 07', 'Tháng 08',
    'Tháng 09', 'Tháng 10', 'Tháng 11', 'Tháng 12',
  ];
  static const _monthNamesEn = [
    '', 'January', 'February', 'March', 'April',
    'May', 'June', 'July', 'August',
    'September', 'October', 'November', 'December',
  ];

  @override
  void initState() {
    super.initState();
    final initialDate = widget.draft.date;
    final now = DateTime.now();
    _month = initialDate != null
        ? DateTime(initialDate.year, initialDate.month)
        : DateTime(now.year, now.month);
    _selected = initialDate;
    _loadWorkingDates();
  }

  Future<void> _loadWorkingDates() async {
    final docId = widget.draft.doctor?.id ?? widget.draft.preferredDentistId;
    if (docId == null) return;
    setState(() => _loadingWorkingDates = true);
    final dates = await BookingService().getWorkingDatesForDentist(docId, _month.year, _month.month);
    if (mounted) {
      setState(() {
        _workingDates = dates;
        _loadingWorkingDates = false;
      });
    }
  }

  void _changeMonth(int delta) {
    setState(() {
      _month = DateTime(_month.year, _month.month + delta);
    });
    _loadWorkingDates();
  }

  bool _isPast(DateTime d) {
    final today = DateTime.now();
    return d.isBefore(DateTime(today.year, today.month, today.day));
  }
  bool _isToday(DateTime d) {
    final now = DateTime.now();
    return d.year == now.year && d.month == now.month && d.day == now.day;
  }

  bool _isAvailable(DateTime d) {
    if (_isPast(d)) return false;
    final docId = widget.draft.doctor?.id ?? widget.draft.preferredDentistId;
    if (docId == null) return d.weekday != DateTime.sunday;
    if (_loadingWorkingDates) return false;
    if (_workingDates.isNotEmpty) {
      final dateStr = '${d.year.toString().padLeft(4, '0')}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';
      return _workingDates.contains(dateStr);
    }
    return d.weekday != DateTime.sunday;
  }
  bool _isSelected(DateTime d) =>
      _selected != null &&
      d.year == _selected!.year &&
      d.month == _selected!.month &&
      d.day == _selected!.day;

  int _colOf(DateTime d) => d.weekday % 7;

  bool get _canGoPrev {
    final now = DateTime.now();
    return _month.year > now.year ||
        (_month.year == now.year && _month.month > now.month);
  }

  List<DateTime?> _buildDays() {
    final first = DateTime(_month.year, _month.month, 1);
    final last = DateTime(_month.year, _month.month + 1, 0);
    final offset = _colOf(first);
    return [
      ...List.generate(offset, (_) => null),
      ...List.generate(last.day, (i) => DateTime(_month.year, _month.month, i + 1)),
    ];
  }

  void _onDateTap(DateTime date) {
    setState(() => _selected = date);
    final draft = widget.draft.copyWith(date: date);
    context.push(AppRoutes.bookingSelectDoctor, extra: draft);
  }

  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final days = _buildDays();
    final monthNames = isVi ? _monthNamesVi : _monthNamesEn;
    final headers = isVi ? _headersVi : _headersEn;

    return Scaffold(
      backgroundColor: context.bg,
      appBar: BookingAppBar(title: isVi ? 'Chọn ngày khám' : 'Select Date'),
      body: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 16),

            if (widget.draft.doctor != null) ...[
              Container(
                margin: const EdgeInsets.only(bottom: 16),
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: AppColors.primary.withValues(alpha: context.isDark ? 0.2 : 0.08),
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: AppColors.primary.withValues(alpha: 0.3)),
                ),
                child: Row(
                  children: [
                    ClipRRect(
                      borderRadius: BorderRadius.circular(10),
                      child: Container(
                        width: 44,
                        height: 44,
                        color: AppColors.primaryLight,
                        child: widget.draft.doctor!.avatarUrl != null && widget.draft.doctor!.avatarUrl!.isNotEmpty
                            ? Image.network(
                                widget.draft.doctor!.avatarUrl!,
                                fit: BoxFit.cover,
                                errorBuilder: (_, __, ___) => const Icon(Iconsax.user, color: AppColors.primary, size: 20),
                              )
                            : const Icon(Iconsax.user, color: AppColors.primary, size: 20),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            isVi ? 'Đang chọn lịch cho Nha sĩ:' : 'Selected Dentist:',
                            style: TextStyle(fontSize: 11, color: context.textMuted),
                          ),
                          const SizedBox(height: 2),
                          Text(
                            widget.draft.doctor!.fullName,
                            style: TextStyle(fontSize: 14, fontWeight: FontWeight.w800, color: context.textPrimary),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ],

            // ── Month navigation ─────────────────────────────────────────────
            Row(
              children: [
                _NavBtn(
                  icon: Iconsax.arrow_left_2,
                  enabled: _canGoPrev,
                  onTap: () => _changeMonth(-1),
                ),
                Expanded(
                  child: Text(
                    '${monthNames[_month.month]} - ${_month.year}',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w800,
                      color: context.isDark ? Colors.white : AppColors.primary,
                    ),
                  ),
                ),
                _NavBtn(
                  icon: Iconsax.arrow_right_2,
                  onTap: () => _changeMonth(1),
                ),
              ],
            ),
            const SizedBox(height: 20),

            // ── Day headers ──────────────────────────────────────────────────
            Row(
              children: headers.map((h) {
                final isSun = h == 'CN' || h == 'SUN';
                return Expanded(
                  child: Center(
                    child: Text(
                      h,
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w800,
                        color: isSun ? AppColors.primary : context.textSecondary,
                      ),
                    ),
                  ),
                );
              }).toList(),
            ),
            const SizedBox(height: 10),

            // ── Calendar grid ────────────────────────────────────────────────
            GridView.count(
              crossAxisCount: 7,
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              childAspectRatio: 1.0,
              mainAxisSpacing: 5,
              crossAxisSpacing: 5,
              children: days.map((date) {
                if (date == null) return const SizedBox();
                final available = _isAvailable(date);
                final today = _isToday(date);
                final selected = _isSelected(date);

                Color cellColor;
                if (selected) {
                  cellColor = context.isDark ? const Color(0xFFDC2626) : AppColors.primaryDark;
                } else if (today && available) {
                  cellColor = context.isDark ? const Color(0xFF451A1A) : AppColors.primaryLight;
                } else if (available) {
                  cellColor = AppColors.primary;
                } else {
                  cellColor = context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9);
                }

                Color textColor;
                if (selected) {
                  textColor = Colors.white;
                } else if (today && available) {
                  textColor = context.isDark ? Colors.white : AppColors.primary;
                } else if (available) {
                  textColor = Colors.white;
                } else {
                  textColor = context.textSecondary.withValues(alpha: 0.4);
                }

                return GestureDetector(
                  onTap: available ? () => _onDateTap(date) : null,
                  child: AnimatedContainer(
                    duration: const Duration(milliseconds: 180),
                    decoration: BoxDecoration(
                      color: cellColor,
                      borderRadius: BorderRadius.circular(today ? 999 : 6),
                      border: today && !selected
                          ? Border.all(color: AppColors.primary, width: 2)
                          : null,
                    ),
                    child: Stack(
                      alignment: Alignment.center,
                      children: [
                        Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Text(
                              '${date.day}',
                              style: TextStyle(
                                fontSize: 13,
                                fontWeight: FontWeight.w600,
                                color: textColor,
                              ),
                            ),
                            if (today)
                              Text(
                                isVi ? 'Hôm nay' : 'Today',
                                style: TextStyle(
                                  fontSize: 6,
                                  fontWeight: FontWeight.w700,
                                  color: selected ? Colors.white : AppColors.primary,
                                  height: 1.2,
                                ),
                              ),
                          ],
                        ),
                        if (selected)
                          const Positioned(
                            top: 2,
                            right: 2,
                            child: Icon(Iconsax.tick_circle, size: 9, color: Colors.white),
                          ),
                      ],
                    ),
                  ),
                );
              }).toList(),
            ),
            const SizedBox(height: 24),

            // ── Instruction ──────────────────────────────────────────────────
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
              decoration: const BoxDecoration(
                border: Border(
                  left: BorderSide(color: AppColors.primary, width: 3),
                ),
              ),
              child: RichText(
                text: TextSpan(
                  style: TextStyle(fontSize: 13, color: context.textSecondary),
                  children: [
                    TextSpan(text: isVi ? 'Chọn ngày ' : 'Select '),
                    TextSpan(
                      text: isVi ? 'có màu đỏ' : 'highlighted date',
                      style: const TextStyle(
                        color: AppColors.primary,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    TextSpan(text: isVi ? ' để đặt khám.' : ' to schedule appointment.'),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),

            // ── Legend ───────────────────────────────────────────────────────
            _Legend(),
            const SizedBox(height: 24),
          ],
        ),
      ),
    );
  }
}

// ─── Nav button ───────────────────────────────────────────────────────────────

class _NavBtn extends StatelessWidget {
  final IconData icon;
  final VoidCallback onTap;
  final bool enabled;
  const _NavBtn({required this.icon, required this.onTap, this.enabled = true});

  @override
  Widget build(BuildContext context) {
    final inactiveBg = context.isDark ? const Color(0xFF1E293B) : AppColors.background;
    return GestureDetector(
      onTap: enabled ? onTap : null,
      child: Container(
        width: 36,
        height: 36,
        decoration: BoxDecoration(
          color: enabled ? AppColors.primary : inactiveBg,
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: context.divider),
        ),
        child: Icon(
          icon,
          size: 20,
          color: enabled ? Colors.white : context.textSecondary,
        ),
      ),
    );
  }
}

// ─── Legend ───────────────────────────────────────────────────────────────────

class _Legend extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    final isVi = SettingsManager.instance.locale.value.languageCode == 'vi';
    final inactiveBg = context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9);

    return Column(
      children: [
        _LegendRow(
          color: AppColors.primary,
          label: isVi ? 'Ngày có thể đặt khám' : 'Available dates',
          isRect: true,
        ),
        const SizedBox(height: 6),
        _LegendRow(
          color: context.isDark ? const Color(0xFF451A1A) : AppColors.primaryLight,
          label: isVi ? 'Hôm nay' : 'Today',
          border: AppColors.primary,
          isCircle: true,
        ),
        const SizedBox(height: 6),
        _LegendRow(
          color: inactiveBg,
          label: isVi ? 'Ngày đã qua' : 'Past dates',
          isRect: true,
        ),
      ],
    );
  }
}

class _LegendRow extends StatelessWidget {
  final Color color;
  final String label;
  final Color? border;
  final bool isRect;
  final bool isCircle;

  const _LegendRow({
    required this.color,
    required this.label,
    this.border,
    this.isRect = true,
    this.isCircle = false,
  });

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Container(
          width: 20,
          height: 20,
          decoration: BoxDecoration(
            color: color,
            borderRadius: isCircle
                ? BorderRadius.circular(999)
                : isRect
                    ? BorderRadius.circular(4)
                    : null,
            shape: (!isRect && !isCircle) ? BoxShape.circle : BoxShape.rectangle,
            border: border != null ? Border.all(color: border!, width: 1.5) : null,
          ),
        ),
        const SizedBox(width: 10),
        Text(
          label,
          style: TextStyle(fontSize: 13, color: context.textSecondary),
        ),
      ],
    );
  }
}
