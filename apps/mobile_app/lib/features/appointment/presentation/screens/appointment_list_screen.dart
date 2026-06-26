import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/core/constants/app_colors.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/data/booking_service.dart';

class AppointmentListScreen extends StatefulWidget {
  const AppointmentListScreen({super.key});

  @override
  State<AppointmentListScreen> createState() => _AppointmentListScreenState();
}

class _AppointmentListScreenState extends State<AppointmentListScreen> {
  final _service = BookingService();
  List<MyAppointmentItem> _items = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final list = await _service.getMyAppointments();
      if (mounted) setState(() { _items = list; _loading = false; });
    } catch (e) {
      if (mounted) setState(() { _error = 'KhÃ´ng thá»ƒ táº£i lá»‹ch háº¹n.'; _loading = false; });
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
          context.l10n('my_appointments'),
          style: TextStyle(
            fontSize: 17,
            fontWeight: FontWeight.w800,
            color: context.textPrimary,
          ),
        ),
        centerTitle: true,
        actions: [
          IconButton(
            icon: Icon(Iconsax.refresh, color: context.textMuted, size: 20),
            onPressed: _load,
          ),
        ],
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(1),
          child: Container(color: context.divider, height: 1),
        ),
      ),
      body: _loading
          ? Center(child: CircularProgressIndicator(color: AppColors.primary))
          : _error != null
              ? _ErrorView(message: _error!, onRetry: _load, isVi: isVi)
              : RefreshIndicator(
                  onRefresh: _load,
                  color: AppColors.primary,
                  child: _items.isEmpty
                      ? _EmptyView(
                          onBook: () => context.push(AppRoutes.bookingSelectPatient),
                          isVi: isVi,
                        )
                      : ListView.builder(
                          padding: EdgeInsets.fromLTRB(16, 12, 16, 100),
                          itemCount: _items.length,
                          itemBuilder: (_, i) => _AppointmentCard(item: _items[i], isVi: isVi),
                        ),
                ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push(AppRoutes.bookingSelectPatient),
        backgroundColor: AppColors.primary,
        foregroundColor: Colors.white,
        elevation: 2,
        icon: Icon(Iconsax.calendar_add, size: 20),
        label: Text(
          context.l10n('book_appointment'),
          style: TextStyle(fontWeight: FontWeight.w700, fontSize: 14),
        ),
      ),
    );
  }
}

// â”€â”€ Appointment Card â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

class _AppointmentCard extends StatelessWidget {
  final MyAppointmentItem item;
  final bool isVi;
  const _AppointmentCard({required this.item, required this.isVi});

  static const _weekdaysVi = ['', 'Thá»© Hai', 'Thá»© Ba', 'Thá»© TÆ°', 'Thá»© NÄƒm', 'Thá»© SÃ¡u', 'Thá»© Báº£y', 'Chá»§ Nháº­t'];
  static const _weekdaysEn = ['', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

  @override
  Widget build(BuildContext context) {
    final date = item.parsedDate;
    final dateStr =
        '${date.day.toString().padLeft(2, '0')}/${date.month.toString().padLeft(2, '0')}/${date.year}';
    final timeStr =
        '${date.hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')}';
    final dayLabel = isVi ? _weekdaysVi[date.weekday] : _weekdaysEn[date.weekday];
    final (statusLabel, statusColor, statusBg) = _statusStyle(item.status, isVi, context);

    return Container(
      margin: EdgeInsets.only(bottom: 12),
      decoration: BoxDecoration(
        color: context.card,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: context.divider),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 8,
            offset: Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        children: [
          // Header
          Padding(
            padding: EdgeInsets.fromLTRB(16, 14, 16, 12),
            child: Row(
              children: [
                _DoctorAvatar(avatarUrl: item.dentistAvatarUrl),
                SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        item.dentistName,
                        style: TextStyle(
                          fontSize: 14,
                          fontWeight: FontWeight.w800,
                          color: AppColors.primary,
                        ),
                      ),
                      SizedBox(height: 2),
                      Text(
                        item.specialization,
                        style: TextStyle(
                          fontSize: 12,
                          color: context.textSecondary,
                        ),
                      ),
                    ],
                  ),
                ),
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: statusBg,
                    borderRadius: BorderRadius.circular(999),
                  ),
                  child: Text(
                    statusLabel,
                    style: TextStyle(
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      color: statusColor,
                    ),
                  ),
                ),
              ],
            ),
          ),

          Divider(color: context.divider, height: 1),

          // Detail rows
          Padding(
            padding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Column(
              children: [
                _DetailRow(icon: Iconsax.tag, text: '#${item.appointmentCode}', bold: true),
                SizedBox(height: 8),
                _DetailRow(icon: Iconsax.calendar_1, text: '$dateStr Â· $dayLabel'),
                SizedBox(height: 8),
                _DetailRow(icon: Iconsax.clock, text: timeStr),
                if (item.serviceName != null) ...[
                  SizedBox(height: 8),
                  _DetailRow(icon: Iconsax.health, text: item.serviceName!),
                ],
                if (item.symptoms != null && item.symptoms!.isNotEmpty) ...[
                  SizedBox(height: 8),
                  _DetailRow(icon: Iconsax.note_text, text: item.symptoms!, muted: true),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }

  static (String, Color, Color) _statusStyle(String status, bool isVi, BuildContext context) {
    switch (status.toLowerCase()) {
      case 'confirmed':
        return (isVi ? 'ÄÃ£ xÃ¡c nháº­n' : 'Confirmed', Color(0xFF16A34A), Color(0xFFDCFCE7));
      case 'completed':
        return (isVi ? 'HoÃ n thÃ nh' : 'Completed', Color(0xFF0284C7), Color(0xFFE0F2FE));
      case 'cancelled':
        return (isVi ? 'ÄÃ£ huá»·' : 'Cancelled', context.textMuted, context.isDark ? const Color(0xFF1E293B) : const Color(0xFFF1F5F9));
      default:
        return (isVi ? 'Chá» xÃ¡c nháº­n' : 'Pending', Color(0xFFD97706), Color(0xFFFEF3C7));
    }
  }
}

// â”€â”€ Detail Row â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

class _DetailRow extends StatelessWidget {
  final IconData icon;
  final String text;
  final bool bold;
  final bool muted;
  const _DetailRow({required this.icon, required this.text, this.bold = false, this.muted = false});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Icon(icon, size: 14, color: context.textMuted),
        SizedBox(width: 8),
        Expanded(
          child: Text(
            text,
            style: TextStyle(
              fontSize: 13,
              fontWeight: bold ? FontWeight.w700 : FontWeight.w500,
              color: muted ? context.textMuted : context.textPrimary,
            ),
          ),
        ),
      ],
    );
  }
}

// â”€â”€ Doctor Avatar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

class _DoctorAvatar extends StatelessWidget {
  final String? avatarUrl;
  const _DoctorAvatar({this.avatarUrl});

  @override
  Widget build(BuildContext context) {
    final placeholder = Container(
      width: 48,
      height: 48,
      decoration: BoxDecoration(
        color: context.primaryLight,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Icon(Iconsax.profile_circle, color: AppColors.primary, size: 26),
    );
    if (avatarUrl == null || avatarUrl!.isEmpty) return placeholder;
    return ClipRRect(
      borderRadius: BorderRadius.circular(12),
      child: Image.network(
        avatarUrl!,
        width: 48,
        height: 48,
        fit: BoxFit.cover,
        errorBuilder: (_, _, _) => placeholder,
        loadingBuilder: (_, child, prog) => prog == null
            ? child
            : Container(
                width: 48,
                height: 48,
                decoration: BoxDecoration(
                  color: context.bg,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Center(
                  child: SizedBox(
                    width: 18,
                    height: 18,
                    child: CircularProgressIndicator(strokeWidth: 2, color: AppColors.primary),
                  ),
                ),
              ),
      ),
    );
  }
}

// â”€â”€ Empty View â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

class _EmptyView extends StatelessWidget {
  final VoidCallback onBook;
  final bool isVi;
  const _EmptyView({required this.onBook, required this.isVi});

  @override
  Widget build(BuildContext context) {
    return ListView(
      children: [
        SizedBox(height: 80),
        Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 80,
                height: 80,
                decoration: BoxDecoration(
                  color: context.primaryLight,
                  shape: BoxShape.circle,
                ),
                child: Icon(Iconsax.calendar_1, color: AppColors.primary, size: 36),
              ),
              SizedBox(height: 20),
              Text(
                isVi ? 'ChÆ°a cÃ³ lá»‹ch háº¹n nÃ o' : 'No appointments yet',
                style: TextStyle(
                  fontSize: 17,
                  fontWeight: FontWeight.w700,
                  color: context.textPrimary,
                ),
              ),
              SizedBox(height: 8),
              Text(
                isVi
                    ? 'Äáº·t lá»‹ch khÃ¡m Ä‘á»ƒ báº¯t Ä‘áº§u theo dÃµi\nlá»‹ch sá»­ khÃ¡m chá»¯a bá»‡nh cá»§a báº¡n.'
                    : 'Book an appointment to start tracking\nyour dental health history.',
                textAlign: TextAlign.center,
                style: TextStyle(
                  fontSize: 14,
                  color: context.textSecondary,
                  height: 1.6,
                ),
              ),
              SizedBox(height: 28),
              ElevatedButton.icon(
                onPressed: onBook,
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  foregroundColor: Colors.white,
                  padding: EdgeInsets.symmetric(horizontal: 24, vertical: 14),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                ),
                icon: Icon(Iconsax.calendar_add, size: 18),
                label: Text(
                  isVi ? 'Äáº·t lá»‹ch ngay' : 'Book Now',
                  style: TextStyle(fontWeight: FontWeight.w700, fontSize: 15),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

// â”€â”€ Error View â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

class _ErrorView extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;
  final bool isVi;
  const _ErrorView({required this.message, required this.onRetry, required this.isVi});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(message, style: TextStyle(color: context.textMuted)),
          SizedBox(height: 12),
          TextButton(onPressed: onRetry, child: Text(isVi ? 'Thá»­ láº¡i' : 'Retry')),
        ],
      ),
    );
  }
}

