import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/presentation/widgets/booking_widgets.dart';

class SelectDatetimePage extends StatefulWidget {
  final BookingDraft draft;
  const SelectDatetimePage({super.key, required this.draft});

  @override
  State<SelectDatetimePage> createState() => _SelectDatetimePageState();
}

class _SelectDatetimePageState extends State<SelectDatetimePage> {
  late DateTime _month;
  DateTime? _selected;

  static const _headers = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
  static const _monthNames = [
    '', 'Tháng 01', 'Tháng 02', 'Tháng 03', 'Tháng 04',
    'Tháng 05', 'Tháng 06', 'Tháng 07', 'Tháng 08',
    'Tháng 09', 'Tháng 10', 'Tháng 11', 'Tháng 12',
  ];

  @override
  void initState() {
    super.initState();
    final now = DateTime.now();
    _month = DateTime(now.year, now.month);
  }

  bool _isPast(DateTime d) {
    final today = DateTime.now();
    return d.isBefore(DateTime(today.year, today.month, today.day));
  }
  bool _isToday(DateTime d) {
    final now = DateTime.now();
    return d.year == now.year && d.month == now.month && d.day == now.day;
  }
  bool _isAvailable(DateTime d) => !_isPast(d);
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
    final days = _buildDays();
    return Scaffold(
      backgroundColor: AppColors.surface,
      appBar: BookingAppBar(title: 'Chọn ngày khám'),
      body: SingleChildScrollView(
        padding: const EdgeInsets.symmetric(horizontal: 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 16),

            // ── Month navigation ─────────────────────────────────────────────
            Row(
              children: [
                _NavBtn(
                  icon: Iconsax.arrow_left_2,
                  enabled: _canGoPrev,
                  onTap: () => setState(() =>
                      _month = DateTime(_month.year, _month.month - 1)),
                ),
                Expanded(
                  child: Text(
                    '${_monthNames[_month.month]} - ${_month.year}',
                    textAlign: TextAlign.center,
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                      color: AppColors.primary,
                    ),
                  ),
                ),
                _NavBtn(
                  icon: Iconsax.arrow_right_2,
                  onTap: () => setState(() =>
                      _month = DateTime(_month.year, _month.month + 1)),
                ),
              ],
            ),
            const SizedBox(height: 16),

            // ── Day headers ──────────────────────────────────────────────────
            Row(
              children: _headers.map((h) {
                final isSun = h == 'CN';
                return Expanded(
                  child: Center(
                    child: Text(
                      h,
                      style: TextStyle(
                        fontSize: 13,
                        fontWeight: FontWeight.w800,
                        color: isSun ? AppColors.primary : AppColors.textSecondary,
                      ),
                    ),
                  ),
                );
              }).toList(),
            ),
            const SizedBox(height: 8),

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
                  cellColor = AppColors.primaryDark;
                } else if (today && available) {
                  cellColor = AppColors.primaryLight;
                } else if (available) {
                  cellColor = AppColors.primary;
                } else {
                  cellColor = AppColors.background;
                }

                Color textColor;
                if (selected) {
                  textColor = Colors.white;
                } else if (today && available) {
                  textColor = AppColors.primary;
                } else if (available) {
                  textColor = Colors.white;
                } else {
                  textColor = AppColors.textMuted.withValues(alpha: 0.55);
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
                                'Hôm nay',
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
            const SizedBox(height: 16),

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
                  style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                  children: const [
                    TextSpan(text: 'Chọn ngày '),
                    TextSpan(
                      text: 'có màu xanh',
                      style: TextStyle(
                        color: AppColors.primary,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                    TextSpan(text: ' để đặt khám.'),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 16),

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
    return GestureDetector(
      onTap: enabled ? onTap : null,
      child: Container(
        width: 36,
        height: 36,
        decoration: BoxDecoration(
          color: enabled ? AppColors.primary : AppColors.background,
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: AppColors.divider),
        ),
        child: Icon(
          icon,
          size: 20,
          color: enabled ? Colors.white : AppColors.textMuted,
        ),
      ),
    );
  }
}

// ─── Legend ───────────────────────────────────────────────────────────────────

class _Legend extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        _LegendRow(
          color: AppColors.primary,
          label: 'Ngày có thể đặt khám',
          isRect: true,
        ),
        const SizedBox(height: 6),
        _LegendRow(
          color: AppColors.primaryLight,
          label: 'Hôm nay',
          border: AppColors.primary,
          isCircle: true,
        ),
        const SizedBox(height: 6),
        _LegendRow(
          color: AppColors.background,
          label: 'Ngày đã qua',
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
          style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
        ),
      ],
    );
  }
}
