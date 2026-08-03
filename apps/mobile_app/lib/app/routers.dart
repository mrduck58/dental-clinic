import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:mobile_app/app/main_shell.dart';
import 'package:mobile_app/app/settings_manager.dart';
import 'package:mobile_app/features/auth/presentation/pages/fill_profile_page.dart';
import 'package:mobile_app/features/auth/presentation/pages/forgot_password_page.dart';
import 'package:mobile_app/features/auth/presentation/pages/login_page.dart';
import 'package:mobile_app/features/auth/presentation/pages/otp_page.dart';
import 'package:mobile_app/features/auth/presentation/pages/register_page.dart';
import 'package:mobile_app/features/auth/presentation/pages/reset_new_password_page.dart';
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
import 'package:mobile_app/features/home/presentation/pages/queue_page.dart';
import 'package:mobile_app/features/home/presentation/pages/chatbot_page.dart';
import 'package:mobile_app/features/home/presentation/pages/reminders_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/profile_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/family_members_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/payment_history_page.dart';
import 'package:mobile_app/features/payment/data/payment_service.dart';
import 'package:mobile_app/features/payment/presentation/pages/payment_checkout_page.dart';
import 'package:mobile_app/features/payment/presentation/pages/payment_gateway_select_page.dart';
import 'package:mobile_app/features/home/presentation/pages/dentist_profile_page.dart';
import 'package:mobile_app/features/home/presentation/pages/dentist_reviews_page.dart';
import 'package:mobile_app/features/home/presentation/pages/write_review_page.dart';
import 'package:mobile_app/features/home/presentation/pages/clinic_feedback_page.dart';
import 'package:mobile_app/features/home/data/models/doctor_model.dart';
import 'package:mobile_app/features/home/presentation/pages/services_list_page.dart';
import 'package:mobile_app/features/home/presentation/pages/posts_list_page.dart';
import 'package:mobile_app/features/home/presentation/pages/dentists_list_page.dart';
import 'package:mobile_app/features/home/presentation/pages/post_detail_page.dart';
import 'package:mobile_app/features/home/data/models/post_model.dart';
import 'package:mobile_app/features/profile/presentation/pages/edit_profile_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/medical_history_page.dart';
import 'package:mobile_app/features/profile/data/family_member.dart';
import 'package:mobile_app/features/profile/presentation/pages/add_member_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/edit_member_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/security_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/change_password_page.dart';
import 'package:mobile_app/features/home/presentation/pages/notifications_page.dart';
import 'package:mobile_app/features/home/presentation/pages/search_page.dart';
import 'package:mobile_app/features/appointment/presentation/screens/appointment_details_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/examine_history_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/examination_detail_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/diagnosis_detail_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/followup_detail_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/treatment_plan_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/phase_detail_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/prescription_detail_page.dart';
import 'package:mobile_app/features/profile/presentation/pages/medicine_detail_page.dart';
import 'package:mobile_app/features/profile/data/medical_record_service.dart';

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
    GoRoute(
      path: AppRoutes.forgotPassword,
      builder: (context, state) => const ForgotPasswordPage(),
    ),
    GoRoute(
      path: AppRoutes.resetOtp,
      builder: (context, state) => OtpPage(
        email: state.extra as String,
        purpose: OtpPurpose.passwordReset,
      ),
    ),
    GoRoute(
      path: AppRoutes.resetNewPassword,
      builder: (context, state) {
        final args = state.extra as Map<String, dynamic>;
        return ResetNewPasswordPage(
          email: args['email'] as String,
          resetToken: args['resetToken'] as String,
        );
      },
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
          builder: (context, state) {
            final args = state.extra as Map<String, dynamic>?;
            return AppointmentListScreen(
              filterPatientId: args?['patientId'] as String?,
              filterPatientName: args?['patientName'] as String?,
            );
          },
        ),
        GoRoute(
          path: AppRoutes.medicalRecords,
          builder: (context, state) => const ExamineHistoryPage(),
        ),
        GoRoute(
          path: AppRoutes.profile,
          builder: (context, state) => const ProfilePage(),
        ),
        GoRoute(
          path: AppRoutes.editProfile,
          builder: (context, state) => const EditProfilePage(),
        ),
        GoRoute(
          path: AppRoutes.medicalHistory,
          builder: (context, state) => const MedicalHistoryPage(),
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
      path: AppRoutes.clinicFeedback,
      builder: (context, state) => const ClinicFeedbackPage(),
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
      path: AppRoutes.dentistsList,
      builder: (context, state) => const DentistsListPage(),
    ),
    GoRoute(
      path: AppRoutes.search,
      builder: (context, state) => const SearchPage(),
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
      path: AppRoutes.addFamilyMember,
      builder: (context, state) => const AddMemberPage(),
    ),
    GoRoute(
      path: AppRoutes.editFamilyMember,
      builder: (context, state) {
        final member = state.extra as FamilyMember?;
        if (member != null) {
          return EditMemberPage(member: member);
        }
        // Fallback khi reload trang Web (F5) khiến state.extra bị null
        final id = state.uri.queryParameters['id'];
        final found = FamilyService().getMembers().firstWhere(
              (m) => m.id == id,
              orElse: () => FamilyService().getMembers().first,
            );
        return EditMemberPage(member: found);
      },
    ),
    GoRoute(
      path: AppRoutes.security,
      builder: (context, state) => const SecurityPage(),
    ),
    GoRoute(
      path: AppRoutes.changePassword,
      builder: (context, state) => const ChangePasswordPage(),
    ),
    GoRoute(
      path: AppRoutes.payment,
      builder: (context, state) {
        final args = state.extra as PaymentCheckoutArgs?;
        if (args != null) {
          return PaymentCheckoutPage(invoice: args.invoice, gateway: args.gateway);
        }
        return const _PlaceholderPage(title: 'Thanh toán');
      },
    ),
    GoRoute(
      path: AppRoutes.paymentGatewaySelect,
      builder: (context, state) {
        final invoice = state.extra as MyInvoiceDto?;
        if (invoice != null) {
          return PaymentGatewaySelectPage(invoice: invoice);
        }
        return const _PlaceholderPage(title: 'Chọn cổng thanh toán');
      },
    ),
    GoRoute(
      path: AppRoutes.chat,
      builder: (context, state) => const ChatbotPage(),
    ),
    GoRoute(
      path: AppRoutes.notifications,
      builder: (context, state) => const NotificationsPage(),
    ),
    GoRoute(
      path: AppRoutes.paymentHistory,
      builder: (context, state) => const PaymentHistoryPage(),
    ),
    GoRoute(
      path: AppRoutes.familyMembers,
      builder: (context, state) => const FamilyMembersPage(),
    ),
    GoRoute(
      path: AppRoutes.reminders,
      builder: (context, state) => const RemindersPage(),
    ),
    GoRoute(
      path: AppRoutes.queue,
      builder: (context, state) => const QueuePage(),
    ),
    GoRoute(
      path: AppRoutes.appointmentDetails,
      builder: (context, state) => AppointmentDetailsPage(item: state.extra as MyAppointmentItem),
    ),
    GoRoute(
      path: AppRoutes.examinationDetail,
      builder: (context, state) => ExaminationDetailPage(event: state.extra as MedicalHistoryEvent),
    ),
    GoRoute(
      path: AppRoutes.diagnosisDetail,
      builder: (context, state) => DiagnosisDetailPage(event: state.extra as MedicalHistoryEvent),
    ),
    GoRoute(
      path: AppRoutes.followUpDetail,
      builder: (context, state) => FollowUpDetailPage(event: state.extra as MedicalHistoryEvent),
    ),
    GoRoute(
      path: AppRoutes.treatmentPlan,
      builder: (context, state) => TreatmentPlanPage(event: state.extra as MedicalHistoryEvent),
    ),
    GoRoute(
      path: AppRoutes.phaseDetail,
      builder: (context, state) => PhaseDetailPage(phase: state.extra as RealTreatmentPlan),
    ),
    GoRoute(
      path: AppRoutes.prescriptionDetail,
      builder: (context, state) => PrescriptionDetailPage(event: state.extra as MedicalHistoryEvent),
    ),
    GoRoute(
      path: AppRoutes.medicineDetail,
      builder: (context, state) => MedicineDetailPage(medicine: state.extra as MedicalHistoryPrescriptionItem),
    ),

    // ── Booking flow (standalone, không có Bottom Nav) ────────────────────
    GoRoute(
      path: AppRoutes.bookingSelectPatient,
      builder: (context, state) =>
          SelectPatientPage(initialDraft: state.extra as BookingDraft?),
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
  static const forgotPassword = '/forgot-password';
  static const resetOtp = '/reset-otp';
  static const resetNewPassword = '/reset-new-password';
  static const home = '/';
  static const appointments = '/appointments';
  static const medicalRecords = '/medical-records';
  static const profile = '/profile';
  static const dentistProfile = '/dentist/profile';
  static const dentistReviews = '/dentist/reviews';
  static const writeReview = '/dentist/reviews/write';
  static const clinicFeedback = '/clinic-feedback';
  static const servicesList = '/services';
  static const postsList = '/posts';
  static const dentistsList = '/dentists';
  static const postDetail = '/post/detail';
  static const editProfile = '/profile/edit';
  static const medicalHistory = '/profile/medical-history';
  static const addFamilyMember = '/profile/family/add';
  static const editFamilyMember = '/profile/family/edit';
  static const security = '/profile/security';
  static const changePassword = '/profile/security/change-password';
  static const payment = '/payment';
  static const paymentGatewaySelect = '/payment/select-gateway';
  static const chat = '/chat';
  static const paymentHistory = '/profile/payment-history';
  static const notifications = '/notifications';
  static const search = '/search';
  static const familyMembers = '/profile/family';
  static const reminders = '/reminders';
  static const queue = '/queue';
  static const appointmentDetails = '/appointments/details';
  static const examinationDetail = '/medical-records/exam-detail';
  static const diagnosisDetail = '/medical-records/diagnosis-detail';
  static const followUpDetail = '/medical-records/followup-detail';
  static const treatmentPlan = '/medical-records/treatment-plan';
  static const phaseDetail = '/medical-records/phase-detail';
  static const prescriptionDetail = '/medical-records/prescription-detail';
  static const medicineDetail = '/medical-records/medicine-detail';

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
          title,
          style: TextStyle(
            color: context.textPrimary,
            fontWeight: FontWeight.bold,
          ),
        ),
      ),
      body: Center(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 24.0),
          child: Text(
            isVi 
                ? '🚧 Màn hình "$title" đang được xây dựng'
                : '🚧 Screen "$title" is under development',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: context.textPrimary,
              fontSize: 16,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
      ),
    );
  }
}
