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
    final d = durationMinutes > 0 ? durationMinutes : 30;
    if (d < 60) return '~$d phút';
    final h = d ~/ 60;
    final m = d % 60;
    return m == 0 ? '~$h giờ' : '~$h giờ $m phút';
  }

  String durationTextLocalized(bool isVi) {
    final d = durationMinutes > 0 ? durationMinutes : 30;
    if (isVi) {
      if (d < 60) return '~$d phút';
      final h = d ~/ 60;
      final m = d % 60;
      return m == 0 ? '~$h giờ' : '~$h giờ $m phút';
    } else {
      if (d < 60) return '~$d mins';
      final h = d ~/ 60;
      final m = d % 60;
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
    final remainingSec = (json['remainingSeconds'] as num?)?.toInt() ?? 0;
    // Tính expiresAt theo đồng hồ thiết bị dựa trên remainingSeconds của server để triệt tiêu lệch giờ (clock drift)
    final localExpiresAt = remainingSec > 0
        ? DateTime.now().add(Duration(seconds: remainingSec))
        : (json['expiresAt'] != null ? DateTime.tryParse(json['expiresAt'].toString())?.toLocal() : null);

    return SlotHoldResult(
      isSuccess: json['isSuccess'] as bool? ?? true,
      holdId: json['holdId'] as String? ?? json['id'] as String? ?? '',
      expiresAt: localExpiresAt,
      remainingSeconds: remainingSec,
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

  /// Kiểm tra ca khám đang được giữ chỗ và còn hạn thời gian
  bool get isHoldActive {
    if (holdExpiresAt == null) return false;
    return holdExpiresAt!.isAfter(DateTime.now());
  }

  /// Kiểm tra thông tin đặt lịch đã đủ các bước (bệnh nhân, bác sĩ, ngày, giờ)
  bool get isComplete {
    return patient != null && doctor != null && date != null && timeSlot != null;
  }

  /// Khung giờ thực tế hiển thị bao gồm toàn bộ thời lượng dịch vụ
  /// Ví dụ: Khung giờ bắt đầu 08:00, dịch vụ 60 phút -> '08:00 - 09:00'
  String get displayTimeRange {
    if (timeSlot == null) return '';
    final rawRange = timeSlot!.range;
    final duration = service?.durationMinutes ?? 0;
    if (duration <= 30) return rawRange;

    try {
      final startPart = rawRange.split(' - ').first.trim();
      final parts = startPart.split(':');
      final startH = int.parse(parts[0]);
      final startM = int.parse(parts[1]);

      final totalEndMinutes = startH * 60 + startM + duration;
      final endH = (totalEndMinutes ~/ 60) % 24;
      final endM = totalEndMinutes % 60;

      final startStr = '${startH.toString().padLeft(2, '0')}:${startM.toString().padLeft(2, '0')}';
      final endStr = '${endH.toString().padLeft(2, '0')}:${endM.toString().padLeft(2, '0')}';
      return '$startStr - $endStr';
    } catch (_) {
      return rawRange;
    }
  }

  BookingDraft copyWith({
    PatientInfo? patient,
    bool clearPatient = false,
    ServiceInfo? service,
    bool clearService = false,
    DoctorInfo? doctor,
    bool clearDoctor = false,
    DateTime? date,
    bool clearDate = false,
    TimeSlot? timeSlot,
    bool clearTimeSlot = false,
    bool? hasInsurance,
    bool? hasPrivateInsurance,
    String? symptoms,
    String? appointmentId,
    String? appointmentCode,
    DateTime? holdExpiresAt,
    bool clearHold = false,
    String? preferredDentistId,
    String? reschedulingAppointmentId,
  }) {
    return BookingDraft(
      patient: clearPatient ? null : (patient ?? this.patient),
      service: clearService ? null : (service ?? this.service),
      doctor: clearDoctor ? null : (doctor ?? this.doctor),
      date: clearDate ? null : (date ?? this.date),
      timeSlot: clearTimeSlot ? null : (timeSlot ?? this.timeSlot),
      hasInsurance: hasInsurance ?? this.hasInsurance,
      hasPrivateInsurance: hasPrivateInsurance ?? this.hasPrivateInsurance,
      symptoms: symptoms ?? this.symptoms,
      appointmentId: appointmentId ?? this.appointmentId,
      appointmentCode: appointmentCode ?? this.appointmentCode,
      holdExpiresAt: clearHold ? null : (holdExpiresAt ?? this.holdExpiresAt),
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
  final String? serviceId;
  final int? serviceDurationMinutes;
  final String? patientName;
  final String? patientRelationship;
  final String? patientId;
  final String? createdAt;
  final int rescheduledCount;
  final String? selfManagementUnlockedUntil;

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
    this.serviceId,
    this.serviceDurationMinutes,
    this.patientName,
    this.patientRelationship,
    this.patientId,
    this.createdAt,
    this.rescheduledCount = 0,
    this.selfManagementUnlockedUntil,
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
        serviceId: json['serviceId']?.toString(),
        serviceDurationMinutes: json['serviceDurationMinutes'] as int?,
        patientName: json['patientName'] as String?,
        patientRelationship: json['patientRelationship'] as String?,
        patientId: json['patientId']?.toString(),
        createdAt: json['createdAt']?.toString(),
        rescheduledCount: json['rescheduledCount'] as int? ?? 0,
        selfManagementUnlockedUntil: json['selfManagementUnlockedUntil']?.toString(),
      );

  DateTime get parsedDate => DateTime.parse(appointmentDate).toLocal();
  DateTime? get parsedCreatedAt => createdAt != null ? DateTime.tryParse(createdAt!)?.toLocal() : null;

  /// Đã đến hoặc đã qua giờ hẹn khám.
  bool get isPastAppointmentDate {
    final now = DateTime.now();
    return now.isAfter(parsedDate) || now.isAtSameMomentAs(parsedDate);
  }

  /// Đã quá 24 giờ kể từ thời điểm đặt lịch.
  bool get isPast24HoursCreation {
    final created = parsedCreatedAt;
    if (created == null) return false;
    final now = DateTime.now();
    return now.difference(created).inHours >= 24;
  }

  /// Được phòng khám phê duyệt mở khóa tự dời lịch (trong thời hạn).
  bool get isRescheduleUnlocked {
    if (selfManagementUnlockedUntil == null) return false;
    final unlocked = DateTime.tryParse(selfManagementUnlockedUntil!)?.toLocal();
    if (unlocked == null) return false;
    return DateTime.now().isBefore(unlocked);
  }

  /// Cho phép tự hủy/dời trong vòng 24 giờ kể từ thời điểm đặt lịch (hoặc sau khi được duyệt mở khóa) VÀ trước thời gian khám.
  bool get canSelfManage => !isPastAppointmentDate && (!isPast24HoursCreation || isRescheduleUnlocked);
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

class AppointmentChangeRequestItem {
  final String id;
  final String appointmentId;
  final String type; // "Cancel" or "Reschedule"
  final String status; // "Pending", "Approved", "Rejected"
  final String reason;
  final DateTime? desiredDate;
  final String? desiredTimeSlot;
  final String? desiredDentistId;
  final String? desiredDentistName;
  final String? staffNote;
  final DateTime createdAt;
  final DateTime? processedAt;

  const AppointmentChangeRequestItem({
    required this.id,
    required this.appointmentId,
    required this.type,
    required this.status,
    required this.reason,
    this.desiredDate,
    this.desiredTimeSlot,
    this.desiredDentistId,
    this.desiredDentistName,
    this.staffNote,
    required this.createdAt,
    this.processedAt,
  });

  factory AppointmentChangeRequestItem.fromJson(Map<String, dynamic> json) =>
      AppointmentChangeRequestItem(
        id: json['id'] as String,
        appointmentId: json['appointmentId'] as String,
        type: json['type'] as String? ?? 'Cancel',
        status: json['status'] as String? ?? 'Pending',
        reason: json['reason'] as String? ?? '',
        desiredDate: json['desiredDate'] != null
            ? DateTime.parse(json['desiredDate'] as String).toLocal()
            : null,
        desiredTimeSlot: json['desiredTimeSlot'] as String?,
        desiredDentistId: json['desiredDentistId'] as String?,
        desiredDentistName: json['desiredDentistName'] as String?,
        staffNote: json['staffNote'] as String?,
        createdAt: json['createdAt'] != null
            ? DateTime.parse(json['createdAt'] as String).toLocal()
            : DateTime.now(),
        processedAt: json['processedAt'] != null
            ? DateTime.parse(json['processedAt'] as String).toLocal()
            : null,
      );

  bool get isPending => status.toLowerCase() == 'pending';
  bool get isApproved => status.toLowerCase() == 'approved';
  bool get isRejected => status.toLowerCase() == 'rejected';
  bool get isCancel => type.toLowerCase() == 'cancel';
  bool get isReschedule => type.toLowerCase() == 'reschedule';
}


