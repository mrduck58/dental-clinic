import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:iconsax/iconsax.dart';
import 'package:mobile_app/app/routers.dart';
import 'package:mobile_app/core/constants/app_colors.dart';

class HomePage extends StatelessWidget {
  const HomePage({super.key});

  // ── Mock data ─────────────────────────────────────────────────────────
  static const _appointment = _Appointment(
    doctorName: 'BS. Nguyễn Minh Khoa',
    specialty: 'Nha sĩ tổng quát',
    date: 'Thứ Sáu, 20 Tháng 6, 2026',
    time: '09:00 SA',
    imagePath: 'assets/images/bac_si_1.png',
  );

  static const _doctors = [
    _Doctor(name: 'BS. Nguyễn Minh Khoa', specialty: 'Tổng quát', rating: 4.9, experience: '8 năm', imagePath: 'assets/images/bac_si_1.png'),
    _Doctor(name: 'BS. Trần Thị Lan', specialty: 'Chỉnh nha', rating: 4.8, experience: '6 năm', imagePath: 'assets/images/bac_si_2.png'),
    _Doctor(name: 'BS. Lê Văn Đức', specialty: 'Implant', rating: 5.0, experience: '12 năm', imagePath: 'assets/images/bac_si_3.png'),
    _Doctor(name: 'BS. Phạm Hồng Nhung', specialty: 'Tẩy trắng', rating: 4.7, experience: '5 năm', imagePath: 'assets/images/bac_si_4.png'),
  ];

  static const _services = [
    _Service(icon: Iconsax.health, label: 'Khám tổng quát', quickInfo: '30–60 phút • Tại phòng khám', price: 'Từ 200.000đ', gradientColors: [Color(0xFFDC2626), Color(0xFFF87171)], bgColor: AppColors.primaryLight),
    _Service(icon: Iconsax.flash_1, label: 'Tẩy trắng răng', quickInfo: '60–90 phút • Không đau', price: 'Từ 500.000đ', gradientColors: [Color(0xFFF59E0B), Color(0xFFFCD34D)], bgColor: AppColors.accentLight),
    _Service(icon: Iconsax.element_4, label: 'Niềng răng', quickInfo: '18–24 tháng • Chỉnh nha', price: 'Từ 8.000.000đ', gradientColors: [Color(0xFF0284C7), Color(0xFF38BDF8)], bgColor: AppColors.secondaryLight),
    _Service(icon: Iconsax.shield_tick, label: 'Trám răng', quickInfo: '20–40 phút • Không đau', price: 'Từ 300.000đ', gradientColors: [Color(0xFF16A34A), Color(0xFF4ADE80)], bgColor: AppColors.successLight),
    _Service(icon: Iconsax.scissor, label: 'Nhổ răng', quickInfo: '20–30 phút • Gây tê cục bộ', price: 'Từ 400.000đ', gradientColors: [Color(0xFF7C3AED), Color(0xFFA78BFA)], bgColor: Color(0xFFF5F3FF)),
    _Service(icon: Iconsax.medal_star, label: 'Cấy ghép Implant', quickInfo: '2–3 giờ • Phục hồi vĩnh viễn', price: 'Từ 20.000.000đ', gradientColors: [Color(0xFFEA580C), Color(0xFFFB923C)], bgColor: Color(0xFFFFF7ED)),
  ];

  static const _newsItems = [
    _NewsItem(
      tag: 'Nha khoa',
      tagColor: AppColors.primary,
      tagBg: AppColors.primaryLight,
      imagePath: 'assets/images/banner_1.png',
      title: 'Bí quyết chăm sóc răng miệng mỗi ngày để có nụ cười rạng rỡ',
      excerpt: 'Hướng dẫn chi tiết các bước vệ sinh răng miệng đúng cách cho mọi lứa tuổi.',
      date: '15 Th6 2026',
      readTime: '3 phút',
    ),
    _NewsItem(
      tag: 'Khuyến mãi',
      tagColor: AppColors.accent,
      tagBg: AppColors.accentLight,
      imagePath: 'assets/images/banner_1.png',
      title: 'Ưu đãi đặc biệt tháng 6 — Làm trắng răng giảm 30%',
      excerpt: 'Chương trình ưu đãi hè dành riêng cho khách hàng đặt lịch qua ứng dụng.',
      date: '10 Th6 2026',
      readTime: '2 phút',
    ),
    _NewsItem(
      tag: 'Sức khoẻ',
      tagColor: AppColors.secondary,
      tagBg: AppColors.secondaryLight,
      imagePath: 'assets/images/banner_1.png',
      title: 'Mối liên hệ giữa sức khoẻ răng miệng và bệnh tim mạch',
      excerpt: 'Các nghiên cứu mới nhất chỉ ra tầm quan trọng của việc chăm sóc nướu răng.',
      date: '05 Th6 2026',
      readTime: '5 phút',
    ),
  ];

  @override
  Widget build(BuildContext context) {
    final bottomPad = MediaQuery.of(context).padding.bottom + 16;
    return ColoredBox(
      color: const Color(0xFFF8FAFC),
      child: CustomScrollView(
        slivers: [
          SliverToBoxAdapter(child: _buildHeader()),
          SliverToBoxAdapter(child: _buildSearchBar()),
          SliverPadding(
            padding: EdgeInsets.fromLTRB(18, 0, 18, bottomPad),
            sliver: SliverList(
              delegate: SliverChildListDelegate([
                // Banner
                _Banner(),
                const SizedBox(height: 26),

                // Lịch hẹn sắp tới
                _SectionHeader(title: 'Lịch hẹn sắp tới', onSeeAll: () => context.go(AppRoutes.appointments)),
                const SizedBox(height: 12),
                _AppointmentCard(appointment: _appointment),
                const SizedBox(height: 26),

                // Nha sĩ nổi bật — scroll ngang, chỉ avatar + tên
                _SectionHeader(title: 'Nha sĩ nổi bật', onSeeAll: () {}),
                const SizedBox(height: 14),
                _DoctorList(doctors: _doctors),
                const SizedBox(height: 26),

                // Dịch vụ nổi bật — danh sách dọc full-width
                _SectionHeader(title: 'Dịch vụ nổi bật', onSeeAll: () {}),
                const SizedBox(height: 14),
                _ServicesList(services: _services),
                const SizedBox(height: 26),

                // Tin tức nổi bật
                _SectionHeader(title: 'Tin tức nổi bật', onSeeAll: () {}),
                const SizedBox(height: 14),
                ..._newsItems.map(
                  (item) => Padding(
                    padding: const EdgeInsets.only(bottom: 14),
                    child: _NewsCard(item: item),
                  ),
                ),
              ]),
            ),
          ),
        ],
      ),
    );
  }

  // ── Header ─────────────────────────────────────────────────────────────
  Widget _buildHeader() {
    return SafeArea(
      bottom: false,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 14, 20, 8),
        child: Row(
          children: [
            Container(
              width: 48,
              height: 48,
              decoration: const BoxDecoration(
                gradient: LinearGradient(
                  colors: [Color(0xFFDC2626), Color(0xFFB91C1C)],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
                shape: BoxShape.circle,
              ),
              child: const Center(
                child: Text('NA', style: TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.w900)),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: const [
                  Text('Xin chào', style: TextStyle(color: AppColors.textSecondary, fontSize: 15)),
                  Text(
                    'Nguyễn Văn An',
                    style: TextStyle(color: AppColors.textPrimary, fontSize: 20, fontWeight: FontWeight.w900),
                  ),
                ],
              ),
            ),
            const _NotificationBell(count: 3),
          ],
        ),
      ),
    );
  }

  // ── Search Bar ─────────────────────────────────────────────────────────
  Widget _buildSearchBar() {
    return Padding(
      padding: const EdgeInsets.fromLTRB(18, 4, 18, 14),
      child: Container(
        height: 54,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(999),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.07),
              blurRadius: 12,
              offset: const Offset(0, 3),
            ),
          ],
        ),
        child: Row(
          children: [
            const SizedBox(width: 20),
            const Expanded(
              child: Text(
                'Tìm kiếm dịch vụ, bác sĩ...',
                style: TextStyle(color: AppColors.textMuted, fontSize: 16),
              ),
            ),
            Container(
              width: 40,
              height: 40,
              margin: const EdgeInsets.only(right: 7),
              decoration: BoxDecoration(
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(999),
              ),
              child: const Icon(Iconsax.search_normal, size: 20, color: Colors.white),
            ),
          ],
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────
// Notification Bell
// ─────────────────────────────────────────────────
class _NotificationBell extends StatelessWidget {
  final int count;
  const _NotificationBell({required this.count});

  @override
  Widget build(BuildContext context) {
    return Stack(
      clipBehavior: Clip.none,
      children: [
        Container(
          width: 48,
          height: 48,
          decoration: BoxDecoration(
            color: Colors.white,
            shape: BoxShape.circle,
            border: Border.all(color: AppColors.divider, width: 1.5),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withValues(alpha: 0.05),
                blurRadius: 8,
                offset: const Offset(0, 2),
              ),
            ],
          ),
          child: const Icon(Iconsax.notification, size: 24, color: AppColors.textPrimary),
        ),
        if (count > 0)
          Positioned(
            top: -2,
            right: -2,
            child: Container(
              width: 20,
              height: 20,
              decoration: BoxDecoration(
                color: AppColors.primary,
                shape: BoxShape.circle,
                border: Border.all(color: Colors.white, width: 1.5),
              ),
              child: Center(
                child: Text('$count', style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.w900)),
              ),
            ),
          ),
      ],
    );
  }
}

// ─────────────────────────────────────────────────
// Banner
// ─────────────────────────────────────────────────
class _Banner extends StatelessWidget {
  const _Banner();

  @override
  Widget build(BuildContext context) {
    return ClipRRect(
      borderRadius: BorderRadius.circular(22),
      child: Image.asset(
        'assets/images/banner_1.png',
        width: double.infinity,
        height: 170,
        fit: BoxFit.cover,
      ),
    );
  }
}

// ─────────────────────────────────────────────────
// Appointment Card
// ─────────────────────────────────────────────────
class _AppointmentCard extends StatelessWidget {
  final _Appointment appointment;
  const _AppointmentCard({required this.appointment});

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        gradient: const LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF991B1B), Color(0xFFDC2626), Color(0xFFEF4444)],
        ),
        borderRadius: BorderRadius.circular(24),
        boxShadow: [
          BoxShadow(
            color: AppColors.primary.withValues(alpha: 0.32),
            blurRadius: 20,
            offset: const Offset(0, 8),
          ),
        ],
      ),
      child: Stack(
        children: [
          Positioned(
            right: -24,
            top: -24,
            child: Container(
              width: 130,
              height: 130,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                color: Colors.white.withValues(alpha: 0.06),
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    ClipOval(
                      child: Image.asset(
                        appointment.imagePath,
                        width: 56,
                        height: 56,
                        fit: BoxFit.cover,
                        errorBuilder: (_, _, _) => Container(
                          width: 56,
                          height: 56,
                          color: Colors.white.withValues(alpha: 0.2),
                          child: const Icon(Iconsax.user, color: Colors.white, size: 28),
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(appointment.doctorName, style: const TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.w800)),
                          const SizedBox(height: 3),
                          Text(appointment.specialty, style: TextStyle(color: Colors.white.withValues(alpha: 0.75), fontSize: 14)),
                        ],
                      ),
                    ),
                    Container(
                      width: 42,
                      height: 42,
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.18),
                        borderRadius: BorderRadius.circular(999),
                      ),
                      child: const Icon(Iconsax.notification_bing, color: Colors.white, size: 20),
                    ),
                  ],
                ),
                const SizedBox(height: 18),
                Divider(color: Colors.white.withValues(alpha: 0.2), height: 1),
                const SizedBox(height: 16),
                Row(
                  children: [
                    Expanded(
                      child: Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(8),
                            decoration: BoxDecoration(
                              color: Colors.white.withValues(alpha: 0.15),
                              borderRadius: BorderRadius.circular(999),
                            ),
                            child: const Icon(Iconsax.calendar_2, color: Colors.white, size: 18),
                          ),
                          const SizedBox(width: 10),
                          Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text('Ngày khám', style: TextStyle(color: Colors.white.withValues(alpha: 0.65), fontSize: 13, fontWeight: FontWeight.w600)),
                              const SizedBox(height: 1),
                              Text(appointment.date, style: const TextStyle(color: Colors.white, fontSize: 13, fontWeight: FontWeight.w700)),
                            ],
                          ),
                        ],
                      ),
                    ),
                    Container(width: 1, height: 34, color: Colors.white.withValues(alpha: 0.2)),
                    Expanded(
                      child: Padding(
                        padding: const EdgeInsets.only(left: 14),
                        child: Row(
                          children: [
                            Container(
                              padding: const EdgeInsets.all(8),
                              decoration: BoxDecoration(
                                color: Colors.white.withValues(alpha: 0.15),
                                borderRadius: BorderRadius.circular(999),
                              ),
                              child: const Icon(Iconsax.clock, color: Colors.white, size: 18),
                            ),
                            const SizedBox(width: 10),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('Giờ khám', style: TextStyle(color: Colors.white.withValues(alpha: 0.65), fontSize: 13, fontWeight: FontWeight.w600)),
                                const SizedBox(height: 1),
                                Text(appointment.time, style: const TextStyle(color: Colors.white, fontSize: 13, fontWeight: FontWeight.w700)),
                              ],
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────
// Doctor List — scroll ngang, chỉ avatar + tên
// ─────────────────────────────────────────────────
class _DoctorList extends StatelessWidget {
  final List<_Doctor> doctors;
  const _DoctorList({required this.doctors});

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 132,
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        itemCount: doctors.length,
        separatorBuilder: (_, _) => const SizedBox(width: 16),
        itemBuilder: (_, i) => _DoctorAvatar(doctor: doctors[i]),
      ),
    );
  }
}

class _DoctorAvatar extends StatelessWidget {
  final _Doctor doctor;
  const _DoctorAvatar({required this.doctor});

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 84,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          ClipOval(
            child: Image.asset(
              doctor.imagePath,
              width: 76,
              height: 76,
              fit: BoxFit.cover,
              errorBuilder: (_, _, _) => Container(
                width: 76,
                height: 76,
                color: AppColors.primaryLight,
                child: const Icon(Iconsax.user, color: AppColors.primary, size: 34),
              ),
            ),
          ),
          const SizedBox(height: 8),
          Text(
            doctor.name,
            style: const TextStyle(color: AppColors.textPrimary, fontSize: 13, fontWeight: FontWeight.w700, height: 1.3),
            textAlign: TextAlign.center,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
          ),
        ],
      ),
    );
  }
}

// ─────────────────────────────────────────────────
// Services Grid — 2 cột
// ─────────────────────────────────────────────────
class _ServicesList extends StatelessWidget {
  final List<_Service> services;
  const _ServicesList({required this.services});

  @override
  Widget build(BuildContext context) {
    final rows = <Widget>[];
    for (int i = 0; i < services.length; i += 2) {
      if (i > 0) rows.add(const SizedBox(height: 12));
      rows.add(
        IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Expanded(child: _ServiceCard(service: services[i])),
              const SizedBox(width: 12),
              Expanded(
                child: i + 1 < services.length
                    ? _ServiceCard(service: services[i + 1])
                    : const SizedBox(),
              ),
            ],
          ),
        ),
      );
    }
    return Column(children: rows);
  }
}

class _ServiceCard extends StatelessWidget {
  final _Service service;
  const _ServiceCard({required this.service});

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(20),
      shadowColor: Colors.black.withValues(alpha: 0.07),
      elevation: 3,
      child: InkWell(
        onTap: () {},
        borderRadius: BorderRadius.circular(20),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(14, 14, 14, 12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Hàng trên: icon + tên + mô tả
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    width: 40,
                    height: 40,
                    decoration: BoxDecoration(
                      gradient: LinearGradient(
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                        colors: service.gradientColors,
                      ),
                      borderRadius: BorderRadius.circular(999),
                    ),
                    child: Icon(service.icon, color: Colors.white, size: 20),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          service.label,
                          style: const TextStyle(color: AppColors.textPrimary, fontSize: 13, fontWeight: FontWeight.w800, height: 1.25),
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                        const SizedBox(height: 3),
                        Text(
                          service.quickInfo,
                          style: const TextStyle(color: AppColors.textMuted, fontSize: 11, height: 1.3),
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              // Hàng dưới: giá + mũi tên
              Row(
                children: [
                  Expanded(
                    child: Text(
                      service.price,
                      style: const TextStyle(color: AppColors.primary, fontSize: 12, fontWeight: FontWeight.w700),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  Container(
                    width: 28,
                    height: 28,
                    decoration: BoxDecoration(
                      color: AppColors.primaryLight,
                      borderRadius: BorderRadius.circular(999),
                    ),
                    child: const Icon(Iconsax.arrow_right_3, color: AppColors.primary, size: 15),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────
// News Card — dùng ảnh thật
// ─────────────────────────────────────────────────
class _NewsCard extends StatelessWidget {
  final _NewsItem item;
  const _NewsCard({required this.item});

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(20),
      shadowColor: Colors.black.withValues(alpha: 0.07),
      elevation: 4,
      child: InkWell(
        onTap: () {},
        borderRadius: BorderRadius.circular(20),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              ClipRRect(
                borderRadius: BorderRadius.circular(14),
                child: Image.asset(
                  item.imagePath,
                  width: 90,
                  height: 90,
                  fit: BoxFit.cover,
                  errorBuilder: (_, _, _) => Container(
                    width: 90,
                    height: 90,
                    color: AppColors.primaryLight,
                    child: const Icon(Iconsax.image, color: AppColors.primary, size: 32),
                  ),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
                      decoration: BoxDecoration(color: item.tagBg, borderRadius: BorderRadius.circular(999)),
                      child: Text(item.tag, style: TextStyle(color: item.tagColor, fontSize: 13, fontWeight: FontWeight.w800)),
                    ),
                    const SizedBox(height: 7),
                    Text(
                      item.title,
                      style: const TextStyle(color: AppColors.textPrimary, fontSize: 15, fontWeight: FontWeight.w700, height: 1.35),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 4),
                    Text(
                      item.excerpt,
                      style: const TextStyle(color: AppColors.textSecondary, fontSize: 13, height: 1.4),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        const Icon(Iconsax.clock, size: 13, color: AppColors.textMuted),
                        const SizedBox(width: 4),
                        Text('${item.date} · ${item.readTime} đọc', style: const TextStyle(color: AppColors.textMuted, fontSize: 13)),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ─────────────────────────────────────────────────
// Section Header
// ─────────────────────────────────────────────────
class _SectionHeader extends StatelessWidget {
  final String title;
  final VoidCallback onSeeAll;

  const _SectionHeader({required this.title, required this.onSeeAll});

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        Row(
          children: [
            Container(width: 4, height: 22, decoration: BoxDecoration(color: AppColors.primary, borderRadius: BorderRadius.circular(999))),
            const SizedBox(width: 10),
            Text(title, style: const TextStyle(color: AppColors.textPrimary, fontSize: 20, fontWeight: FontWeight.w900)),
          ],
        ),
        GestureDetector(
          onTap: onSeeAll,
          child: const Row(
            children: [
              Text('Xem tất cả', style: TextStyle(color: AppColors.primary, fontSize: 15, fontWeight: FontWeight.w700)),
              SizedBox(width: 2),
              Icon(Iconsax.arrow_right_3, size: 16, color: AppColors.primary),
            ],
          ),
        ),
      ],
    );
  }
}

// ─────────────────────────────────────────────────
// Data classes
// ─────────────────────────────────────────────────
class _Appointment {
  final String doctorName, specialty, date, time, imagePath;
  const _Appointment({required this.doctorName, required this.specialty, required this.date, required this.time, required this.imagePath});
}

class _Doctor {
  final String name, specialty, experience, imagePath;
  final double rating;
  const _Doctor({required this.name, required this.specialty, required this.rating, required this.experience, required this.imagePath});
}

class _Service {
  final IconData icon;
  final String label, quickInfo, price;
  final List<Color> gradientColors;
  final Color bgColor;
  const _Service({required this.icon, required this.label, required this.quickInfo, required this.price, required this.gradientColors, required this.bgColor});
}

class _NewsItem {
  final String tag, imagePath, title, excerpt, date, readTime;
  final Color tagColor, tagBg;
  const _NewsItem({required this.tag, required this.tagColor, required this.tagBg, required this.imagePath, required this.title, required this.excerpt, required this.date, required this.readTime});
}
