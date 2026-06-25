import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_app/app/main_shell.dart';
import 'package:mobile_app/features/auth/presentation/pages/fill_profile_page.dart';
import 'package:mobile_app/features/auth/presentation/pages/login_page.dart';
import 'package:mobile_app/features/auth/presentation/pages/otp_page.dart';
import 'package:mobile_app/features/auth/presentation/pages/register_page.dart';
import 'package:mobile_app/features/auth/presentation/pages/splash_page.dart';
import 'package:mobile_app/features/appointment/presentation/screens/appointment_list_screen.dart';
import 'package:mobile_app/features/booking/data/booking_models.dart';
import 'package:mobile_app/features/booking/presentation/pages/booking_success_page.dart';
import 'package:mobile_app/features/booking/presentation/pages/review_booking_page.dart';
import 'package:mobile_app/features/booking/presentation/pages/select_datetime_page.dart';
import 'package:mobile_app/features/booking/presentation/pages/select_doctor_page.dart';
import 'package:mobile_app/features/booking/presentation/pages/select_patient_page.dart';
import 'package:mobile_app/features/booking/presentation/pages/select_service_page.dart';
import 'package:mobile_app/features/booking/presentation/pages/service_detail_page.dart';
import 'package:mobile_app/features/home/presentation/pages/home_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/profile_page.dart';
import 'package:mobile_app/features/home/presentation/pages/dentist_profile_page.dart';
import 'package:mobile_app/features/home/presentation/pages/dentist_reviews_page.dart';
import 'package:mobile_app/features/home/presentation/pages/write_review_page.dart';
import 'package:mobile_app/features/home/data/models/doctor_model.dart';
import 'package:mobile_app/features/home/presentation/pages/services_list_page.dart';
import 'package:mobile_app/features/home/presentation/pages/posts_list_page.dart';
import 'package:mobile_app/features/home/presentation/pages/post_detail_page.dart';
import 'package:mobile_app/features/home/data/models/post_model.dart';

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
          builder: (context, state) => const AppointmentListScreen(),
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
      path: AppRoutes.dentistProfile,
      builder: (context, state) {
        final doctor = state.extra as DoctorModel?;
        if (doctor != null) {
          return DentistProfilePage(doctor: doctor);
        }
        return const _PlaceholderPage(title: 'Hồ sơ nha sĩ');
      },
    ),
    GoRoute(
      path: AppRoutes.dentistReviews,
      builder: (context, state) {
        final doctor = state.extra as DoctorModel?;
        if (doctor != null) {
          return DentistReviewsPage(doctor: doctor);
        }
        return const _PlaceholderPage(title: 'Đánh giá nha sĩ');
      },
    ),
    GoRoute(
      path: AppRoutes.writeReview,
      builder: (context, state) {
        final doctor = state.extra as DoctorModel?;
        if (doctor != null) {
          return WriteReviewPage(doctor: doctor);
        }
        return const _PlaceholderPage(title: 'Viết đánh giá');
      },
    ),
    GoRoute(
      path: AppRoutes.servicesList,
      builder: (context, state) => const ServicesListPage(),
    ),
    GoRoute(
      path: AppRoutes.postsList,
      builder: (context, state) => const PostsListPage(),
    ),
    GoRoute(
      path: AppRoutes.postDetail,
      builder: (context, state) {
        final post = state.extra as PostModel?;
        if (post != null) {
          return PostDetailPage(post: post);
        }
        return const _PlaceholderPage(title: 'Chi tiết bài viết');
      },
    ),
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

    // ── Booking flow (standalone, không có Bottom Nav) ────────────────────
    GoRoute(
      path: AppRoutes.bookingSelectPatient,
      builder: (context, state) => const SelectPatientPage(),
    ),
    GoRoute(
      path: AppRoutes.bookingSelectService,
      builder: (context, state) =>
          SelectServicePage(draft: state.extra as BookingDraft),
    ),
    GoRoute(
      path: AppRoutes.bookingSelectDoctor,
      builder: (context, state) =>
          SelectDoctorPage(draft: state.extra as BookingDraft),
    ),
    GoRoute(
      path: AppRoutes.bookingSelectDatetime,
      builder: (context, state) =>
          SelectDatetimePage(draft: state.extra as BookingDraft),
    ),
    GoRoute(
      path: AppRoutes.bookingReview,
      builder: (context, state) =>
          ReviewBookingPage(draft: state.extra as BookingDraft),
    ),
    GoRoute(
      path: AppRoutes.bookingSuccess,
      builder: (context, state) =>
          BookingSuccessPage(draft: state.extra as BookingDraft),
    ),
    GoRoute(
      path: AppRoutes.bookingServiceDetail,
      builder: (context, state) =>
          ServiceDetailPage(service: state.extra as ServiceInfo),
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
  static const dentistProfile = '/dentist/profile';
  static const dentistReviews = '/dentist/reviews';
  static const writeReview = '/dentist/reviews/write';
  static const servicesList = '/services';
  static const postsList = '/posts';
  static const postDetail = '/post/detail';
  static const payment = '/payment';
  static const chat = '/chat';

  // ── Booking flow ────────────────────────────────────────────────────────────
  static const bookingSelectPatient = '/booking/patient';
  static const bookingSelectService = '/booking/service';
  static const bookingSelectDoctor = '/booking/doctor';
  static const bookingSelectDatetime = '/booking/datetime';
  static const bookingReview = '/booking/review';
  static const bookingSuccess = '/booking/success';
  static const bookingServiceDetail = '/booking/service-detail';
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
