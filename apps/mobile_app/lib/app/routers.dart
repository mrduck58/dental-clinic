import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_app/app/main_shell.dart';
import 'package:mobile_app/features/auth/presentation/pages/fill_profile_page.dart';
import 'package:mobile_app/features/auth/presentation/pages/login_page.dart';
import 'package:mobile_app/features/auth/presentation/pages/otp_page.dart';
import 'package:mobile_app/features/auth/presentation/pages/register_page.dart';
import 'package:mobile_app/features/auth/presentation/pages/splash_page.dart';
import 'package:mobile_app/features/home/presentation/pages/home_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/profile_page.dart';

final GoRouter appRouter = GoRouter(
  initialLocation: AppRoutes.splash,
  routes: [
    // ── Auth (standalone, không có Bottom Nav) ────────────────────────────
    GoRoute(
      path: AppRoutes.splash,
      builder: (context, state) => const SplashPage(),
    ),
    GoRoute(
      path: AppRoutes.login,
      builder: (context, state) => const LoginPage(),
    ),
    GoRoute(
      path: AppRoutes.register,
      builder: (context, state) => const RegisterPage(),
    ),
    GoRoute(
      path: AppRoutes.otp,
      builder: (context, state) => OtpPage(email: state.extra as String),
    ),
    GoRoute(
      path: AppRoutes.fillProfile,
      builder: (context, state) => const FillProfilePage(),
    ),
    // ── Shell chứa Bottom Navigation Bar cho các tab chính ────────────────
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
          builder: (context, state) =>
              const _PlaceholderPage(title: 'Lịch hẹn'),
        ),
        GoRoute(
          path: AppRoutes.medicalRecords,
          builder: (context, state) =>
              const _PlaceholderPage(title: 'Hồ sơ bệnh án'),
        ),
        GoRoute(
          path: AppRoutes.profile,
          builder: (context, state) => const ProfilePage(),
        ),
      ],
    ),
    // ── Màn hình standalone khác ──────────────────────────────────────────
    GoRoute(
      path: AppRoutes.payment,
      builder: (context, state) =>
          const _PlaceholderPage(title: 'Thanh toán'),
    ),
    GoRoute(
      path: AppRoutes.chat,
      builder: (context, state) =>
          const _PlaceholderPage(title: 'Hỏi đáp AI'),
    ),
  ],
);

abstract class AppRoutes {
  static const splash = '/splash';
  static const login = '/login';
  static const register = '/register';
  static const otp = '/otp';
  static const fillProfile = '/fill-profile';
  static const home = '/';
  static const appointments = '/appointments';
  static const medicalRecords = '/medical-records';
  static const profile = '/profile';
  static const payment = '/payment';
  static const chat = '/chat';
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
