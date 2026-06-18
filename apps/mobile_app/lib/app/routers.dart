import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_app/app/main_shell.dart';
import 'package:mobile_app/features/home/presentation/pages/home_page.dart';

final GoRouter appRouter = GoRouter(
  initialLocation: AppRoutes.home,
  routes: [
    // Shell chứa Bottom Navigation Bar cho các tab chính
    ShellRoute(
      builder: (context, state, child) => MainShell(
        location: state.uri.path,
        child: child,
      ),
      routes: [
        GoRoute(
          path: AppRoutes.home,
          builder: (context, state) => const HomePage(),
        ),
        GoRoute(
          path: AppRoutes.appointments,
          builder: (context, state) => const _PlaceholderPage(title: 'Lịch hẹn'),
        ),
        GoRoute(
          path: AppRoutes.medicalRecords,
          builder: (context, state) => const _PlaceholderPage(title: 'Hồ sơ bệnh án'),
        ),
        GoRoute(
          path: AppRoutes.profile,
          builder: (context, state) => const _PlaceholderPage(title: 'Cá nhân'),
        ),
      ],
    ),
    // Màn hình ngoài shell (full-screen, không có Bottom Nav)
    GoRoute(
      path: AppRoutes.login,
      builder: (context, state) => const _PlaceholderPage(title: 'Đăng nhập'),
    ),
    GoRoute(
      path: AppRoutes.payment,
      builder: (context, state) => const _PlaceholderPage(title: 'Thanh toán'),
    ),
    GoRoute(
      path: AppRoutes.chat,
      builder: (context, state) => const _PlaceholderPage(title: 'Hỏi đáp AI'),
    ),
  ],
);

abstract class AppRoutes {
  static const home = '/';
  static const login = '/login';
  static const appointments = '/appointments';
  static const medicalRecords = '/medical-records';
  static const payment = '/payment';
  static const chat = '/chat';
  static const profile = '/profile';
}

/// Widget tạm thời trong quá trình phát triển.
/// Xóa khi toàn bộ màn hình thực đã được xây dựng.
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
