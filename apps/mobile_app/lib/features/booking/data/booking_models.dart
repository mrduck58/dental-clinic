import 'package:mobile_app/features/home/data/models/service_model.dart';

// ─── Patient ─────────────────────────────────────────────────────────────────

class PatientInfo {
  final String id;
  final String name;
  final String relationship;
  final String? phone;
  final String? dob;
  final String gender;
  final String? patientCode;
  final String? avatarUrl;

  const PatientInfo({
    required this.id,
    required this.name,
    required this.relationship,
    this.phone,
    this.dob,
    this.gender = 'Nam',
    this.patientCode,
    this.avatarUrl,
  });
}

// ─── Service ─────────────────────────────────────────────────────────────────

class ServiceInfo {
  final String id;
  final String name;
  final String description;
  final String? note;
  final String price;
  final String? imageUrl;
  final String? iconUrl;
  final int durationMinutes;
  final List<ServiceOptionModel> options;

  const ServiceInfo({
    required this.id,
    required this.name,
    required this.description,
    this.note,
    required this.price,
    this.imageUrl,
    this.iconUrl,
    this.durationMinutes = 0,
    this.options = const [],
  });

  String get durationText {
    if (durationMinutes <= 0) return '';
    if (durationMinutes < 60) return '~$durationMinutes phút';
    final h = durationMinutes ~/ 60;
    final m = durationMinutes % 60;
    return m == 0 ? '~$h giờ' : '~$h giờ $m phút';
  }

  String durationTextLocalized(bool isVi) {
    if (durationMinutes <= 0) return '';
    if (isVi) {
      if (durationMinutes < 60) return '~$durationMinutes phút';
      final h = durationMinutes ~/ 60;
      final m = durationMinutes % 60;
      return m == 0 ? '~$h giờ' : '~$h giờ $m phút';
    } else {
      if (durationMinutes < 60) return '~$durationMinutes mins';
      final h = durationMinutes ~/ 60;
      final m = durationMinutes % 60;
      return m == 0 ? '~$h hr' : '~$h hr $m mins';
    }
  }
}

// ─── Time slot ───────────────────────────────────────────────────────────────

class TimeSlot {
  final String range; // e.g. '07:30 - 08:30'
  final bool isBooked;
  final bool isHeld;
  final bool isHeldByMe;
  final int holdRemainingSeconds;

  const TimeSlot({
    required this.range,
    this.isBooked = false,
    this.isHeld = false,
    this.isHeldByMe = false,
    this.holdRemainingSeconds = 0,
  });
}

class SlotHoldResult {
  final bool isSuccess;
  final String holdId;
  final DateTime? expiresAt;
  final int remainingSeconds;
  final int failedHoldsToday;
  final String message;

  const SlotHoldResult({
    required this.isSuccess,
    this.holdId = '',
    this.expiresAt,
    this.remainingSeconds = 0,
    this.failedHoldsToday = 0,
    this.message = '',
  });

  factory SlotHoldResult.fromJson(Map<String, dynamic> json) {
    return SlotHoldResult(
      isSuccess: json['isSuccess'] as bool? ?? true,
      holdId: json['holdId'] as String? ?? json['id'] as String? ?? '',
      expiresAt: json['expiresAt'] != null ? DateTime.tryParse(json['expiresAt'].toString()) : null,
      remainingSeconds: (json['remainingSeconds'] as num?)?.toInt() ?? 0,
      failedHoldsToday: (json['failedHoldsToday'] as num?)?.toInt() ?? 0,
      message: json['message'] as String? ?? '',
    );
  }
}

// ─── Doctor ──────────────────────────────────────────────────────────────────

enum DoctorSession { morning, afternoon }

class DoctorInfo {
  final String id;
  final String name;
  final String title; // BSCKII, ThS BS, ...
  final String specialty;
  final String room;
  final DoctorSession session;
  final double rating;
  final int reviewCount;
  final String? avatarUrl;

  const DoctorInfo({
    required this.id,
    required this.name,
    required this.title,
    required this.specialty,
    required this.room,
    required this.session,
    required this.rating,
    required this.reviewCount,
    this.avatarUrl,
  });

  String get fullName {
    final cleanName = name.replaceAll(RegExp(r'^[.\s,:-]+'), '').trim();
    if (title.isEmpty) return cleanName;
    return '$title $cleanName';
  }
  String get sessionLabel =>
      session == DoctorSession.morning ? 'Buổi sáng' : 'Buổi chiều';
}

// ─── BookingDraft ─────────────────────────────────────────────────────────────

class BookingDraft {
  final PatientInfo? patient;
  final ServiceInfo? service;
  final DoctorInfo? doctor;
  final DateTime? date;
  final TimeSlot? timeSlot;
  final bool? hasInsurance;
  final bool? hasPrivateInsurance;
  final String? symptoms;
  final String? appointmentId;
  final String? appointmentCode;
  final DateTime? holdExpiresAt;

  /// Gợi ý bác sĩ từ chatbot AI — chỉ dùng để làm nổi bật lựa chọn ở màn chọn bác sĩ,
  /// không tự động chọn thay người dùng (vẫn cần xác nhận khung giờ thủ công).
  final String? preferredDentistId;

  /// Khác null nghĩa là luồng này đang DỜI lịch hẹn đó chứ không đặt lịch mới. Cùng một wizard
  /// (chọn bác sĩ → chọn giờ → xác nhận), chỉ khác thao tác ở bước cuối — nhờ vậy không phải dựng
  /// thêm một màn chọn ngày giờ thứ hai chỉ để đổi lịch.
  final String? reschedulingAppointmentId;

  const BookingDraft({
    this.patient,
    this.service,
    this.doctor,
    this.date,
    this.timeSlot,
    this.hasInsurance,
    this.hasPrivateInsurance,
    this.symptoms,
    this.appointmentId,
    this.appointmentCode,
    this.holdExpiresAt,
    this.preferredDentistId,
    this.reschedulingAppointmentId,
  });

  bool get isRescheduling => reschedulingAppointmentId != null;

  BookingDraft copyWith({
    PatientInfo? patient,
    ServiceInfo? service,
    DoctorInfo? doctor,
    DateTime? date,
    TimeSlot? timeSlot,
    bool? hasInsurance,
    bool? hasPrivateInsurance,
    String? symptoms,
    String? appointmentId,
    String? appointmentCode,
    DateTime? holdExpiresAt,
    String? preferredDentistId,
    String? reschedulingAppointmentId,
  }) {
    return BookingDraft(
      patient: patient ?? this.patient,
      service: service ?? this.service,
      doctor: doctor ?? this.doctor,
      date: date ?? this.date,
      timeSlot: timeSlot ?? this.timeSlot,
      hasInsurance: hasInsurance ?? this.hasInsurance,
      hasPrivateInsurance: hasPrivateInsurance ?? this.hasPrivateInsurance,
      symptoms: symptoms ?? this.symptoms,
      appointmentId: appointmentId ?? this.appointmentId,
      appointmentCode: appointmentCode ?? this.appointmentCode,
      holdExpiresAt: holdExpiresAt ?? this.holdExpiresAt,
      preferredDentistId: preferredDentistId ?? this.preferredDentistId,
      reschedulingAppointmentId: reschedulingAppointmentId ?? this.reschedulingAppointmentId,
    );
  }
}

// ─── API response models ──────────────────────────────────────────────────────

class ApiTimeSlot {
  final String range;
  final bool isBooked;
  final String period;
  final bool isHeld;
  final bool isHeldByMe;
  final int holdRemainingSeconds;

  const ApiTimeSlot({
    required this.range,
    required this.isBooked,
    required this.period,
    this.isHeld = false,
    this.isHeldByMe = false,
    this.holdRemainingSeconds = 0,
  });

  factory ApiTimeSlot.fromJson(Map<String, dynamic> json) => ApiTimeSlot(
        range: json['range'] as String? ?? '',
        isBooked: json['isBooked'] as bool? ?? false,
        period: json['period'] as String? ?? '',
        isHeld: json['isHeld'] as bool? ?? false,
        isHeldByMe: json['isHeldByMe'] as bool? ?? false,
        holdRemainingSeconds: (json['holdRemainingSeconds'] as num?)?.toInt() ?? 0,
      );

  TimeSlot toTimeSlot() => TimeSlot(
        range: range,
        isBooked: isBooked,
        isHeld: isHeld,
        isHeldByMe: isHeldByMe,
        holdRemainingSeconds: holdRemainingSeconds,
      );
}

class ApiDoctorWithSlots {
  final String dentistId;
  final String fullName;
  final String specialization;
  final String? avatarUrl;
  final String shift;
  final int experienceYears;
  final List<ApiTimeSlot> slots;

  const ApiDoctorWithSlots({
    required this.dentistId,
    required this.fullName,
    required this.specialization,
    this.avatarUrl,
    required this.shift,
    required this.experienceYears,
    required this.slots,
  });

  factory ApiDoctorWithSlots.fromJson(Map<String, dynamic> json) {
    return ApiDoctorWithSlots(
      dentistId: json['dentistId'].toString(),
      fullName: json['fullName'] as String? ?? '',
      specialization: json['specialization'] as String? ?? '',
      avatarUrl: json['avatarUrl'] as String?,
      shift: json['shift'] as String? ?? 'morning',
      experienceYears: json['experienceYears'] as int? ?? 0,
      slots: (json['slots'] as List<dynamic>)
          .map((e) => ApiTimeSlot.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }

  DoctorInfo toDoctorInfo() {
    final parts = fullName.split('. ');
    final title = parts.length > 1 ? parts.first : '';
    final name = parts.length > 1 ? parts.sublist(1).join('. ') : fullName;
    return DoctorInfo(
      id: dentistId,
      name: name,
      title: title,
      specialty: specialization,
      room: 'Phòng khám',
      session: shift == 'afternoon' ? DoctorSession.afternoon : DoctorSession.morning,
      rating: 0,
      reviewCount: 0,
      avatarUrl: avatarUrl,
    );
  }
}

class MyAppointmentItem {
  final String appointmentId;
  final String appointmentCode;
  final String dentistId;
  final String dentistName;
  final String? dentistAvatarUrl;
  final String specialization;
  final String appointmentDate; // ISO8601
  final String status;
  final String? symptoms;
  final String? serviceName;
  final String? patientName;
  final String? patientRelationship;
  final String? patientId;
  final String? createdAt;
  final int rescheduledCount;

  const MyAppointmentItem({
    required this.appointmentId,
    required this.appointmentCode,
    this.dentistId = '',
    required this.dentistName,
    this.dentistAvatarUrl,
    required this.specialization,
    required this.appointmentDate,
    required this.status,
    this.symptoms,
    this.serviceName,
    this.patientName,
    this.patientRelationship,
    this.patientId,
    this.createdAt,
    this.rescheduledCount = 0,
  });

  factory MyAppointmentItem.fromJson(Map<String, dynamic> json) =>
      MyAppointmentItem(
        appointmentId: json['appointmentId'].toString(),
        appointmentCode: json['appointmentCode'] as String,
        dentistId: json['dentistId']?.toString() ?? '',
        dentistName: json['dentistName'] as String? ?? '',
        dentistAvatarUrl: json['dentistAvatarUrl'] as String?,
        specialization: json['specialization'] as String? ?? '',
        appointmentDate: json['appointmentDate'] as String,
        status: json['status'] as String,
        symptoms: json['symptoms'] as String?,
        serviceName: json['serviceName'] as String?,
        patientName: json['patientName'] as String?,
        patientRelationship: json['patientRelationship'] as String?,
        patientId: json['patientId']?.toString(),
        createdAt: json['createdAt']?.toString(),
        rescheduledCount: json['rescheduledCount'] as int? ?? 0,
      );

  DateTime get parsedDate => DateTime.parse(appointmentDate).toLocal();
  DateTime? get parsedCreatedAt => createdAt != null ? DateTime.tryParse(createdAt!)?.toLocal() : null;

  /// Cho phép tự hủy/dời trong vòng 24 giờ kể từ thời điểm đặt lịch VÀ trước thời gian khám.
  bool get canSelfManage {
    final now = DateTime.now();
    if (now.isAfter(parsedDate) || now.isAtSameMomentAs(parsedDate)) return false;
    final created = parsedCreatedAt;
    if (created == null) return true;
    return now.difference(created).inHours < 24;
  }
}

class BookingEligibility {
  final int activeBookingCount;
  final int maxActiveBookings;
  final bool canBookNew;
  final bool isInCooldown;
  final int cooldownRemainingSeconds;
  final int cancellationCount;
  final int rescheduleCount;

  const BookingEligibility({
    required this.activeBookingCount,
    required this.maxActiveBookings,
    required this.canBookNew,
    required this.isInCooldown,
    required this.cooldownRemainingSeconds,
    required this.cancellationCount,
    required this.rescheduleCount,
  });

  factory BookingEligibility.fromJson(Map<String, dynamic> json) =>
      BookingEligibility(
        activeBookingCount: json['activeBookingCount'] as int? ?? 0,
        maxActiveBookings: json['maxActiveBookings'] as int? ?? 2,
        canBookNew: json['canBookNew'] as bool? ?? true,
        isInCooldown: json['isInCooldown'] as bool? ?? false,
        cooldownRemainingSeconds: json['cooldownRemainingSeconds'] as int? ?? 0,
        cancellationCount: json['cancellationCount'] as int? ?? 0,
        rescheduleCount: json['rescheduleCount'] as int? ?? 0,
      );

  int get cooldownRemainingMinutes => (cooldownRemainingSeconds / 60).ceil();
}

class ApiAppointmentResult {
  final String appointmentId;
  final String appointmentCode;
  final String status;

  const ApiAppointmentResult({
    required this.appointmentId,
    required this.appointmentCode,
    required this.status,
  });

  factory ApiAppointmentResult.fromJson(Map<String, dynamic> json) =>
      ApiAppointmentResult(
        appointmentId: json['appointmentId'].toString(),
        appointmentCode: json['appointmentCode'] as String,
        status: json['status'] as String,
      );
}

