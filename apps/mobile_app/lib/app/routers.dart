import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Cấu hình router chính của ứng dụng.
/// Sử dụng package go_router để điều hướng khai báo (declarative routing).
///
/// Cách thêm route mới:
///   1. Khai báo path constant ở dưới (ví dụ: static const home = '/')
///   2. Thêm GoRoute vào danh sách routes
///   3. Trỏ đến Page widget tương ứng ở thư mục features/<feature>/presentation/pages/
final GoRouter appRouter = GoRouter(
  initialLocation: AppRoutes.home,
  routes: [
    GoRoute(
      path: AppRoutes.home,
      // TODO: Thay bằng màn hình Home thực khi đã xây dựng UI
      builder: (context, state) => const _PlaceholderPage(title: 'Trang chủ'),
    ),
    GoRoute(
      path: AppRoutes.login,
      builder: (context, state) => const _PlaceholderPage(title: 'Đăng nhập'),
    ),
    GoRoute(
      path: AppRoutes.appointments,
      builder: (context, state) => const _PlaceholderPage(title: 'Lịch hẹn'),
    ),
    GoRoute(
      path: AppRoutes.medicalRecords,
      builder: (context, state) =>
          const _PlaceholderPage(title: 'Hồ sơ bệnh án'),
    ),
    GoRoute(
      path: AppRoutes.payment,
      builder: (context, state) =>
          const _PlaceholderPage(title: 'Thanh toán'),
    ),
    GoRoute(
      path: AppRoutes.chat,
      builder: (context, state) => const _PlaceholderPage(title: 'Hỏi đáp AI'),
    ),
  ],
);

/// Định nghĩa tất cả các đường dẫn (path) trong ứng dụng.
/// Luôn dùng các hằng số này thay vì viết chuỗi string trực tiếp.
abstract class AppRoutes {
  static const home = '/';
  static const login = '/login';
  static const appointments = '/appointments';
  static const medicalRecords = '/medical-records';
  static const payment = '/payment';
  static const chat = '/chat';
}

/// Widget tạm thời dùng trong quá trình phát triển để placeholder các màn hình chưa xây dựng.
/// XÓA class này khi toàn bộ màn hình thực đã được xây dựng.
class _PlaceholderPage extends StatelessWidget {
  final String title;
  const _PlaceholderPage({required this.title});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(title)),
      body: Center(
        child: Text(
          '🚧 Màn hình "$title" đang được xây dựng',
          style: Theme.of(context).textTheme.titleMedium,
        ),
      ),
    );
  }
}
