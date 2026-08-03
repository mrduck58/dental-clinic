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

  const ServiceInfo({
    required this.id,
    required this.name,
    required this.description,
    this.note,
    required this.price,
  });
}

// ─── Time slot ───────────────────────────────────────────────────────────────

class TimeSlot {
  final String range; // e.g. '07:30 - 08:30'
  final bool isBooked;

  const TimeSlot({required this.range, this.isBooked = false});
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

  String get fullName => '$title. $name';
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

  /// Gợi ý bác sĩ từ chatbot AI — chỉ dùng để làm nổi bật lựa chọn ở màn chọn bác sĩ,
  /// không tự động chọn thay người dùng (vẫn cần xác nhận khung giờ thủ công).
  final String? preferredDentistId;

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
    this.preferredDentistId,
  });

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
    String? preferredDentistId,
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
      preferredDentistId: preferredDentistId ?? this.preferredDentistId,
    );
  }
}

// ─── API response models ──────────────────────────────────────────────────────

class ApiTimeSlot {
  final String range;
  final bool isBooked;
  final String period;

  const ApiTimeSlot({required this.range, required this.isBooked, required this.period});

  factory ApiTimeSlot.fromJson(Map<String, dynamic> json) => ApiTimeSlot(
        range: json['range'] as String,
        isBooked: json['isBooked'] as bool,
        period: json['period'] as String? ?? '',
      );

  TimeSlot toTimeSlot() => TimeSlot(range: range, isBooked: isBooked);
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
      );

  DateTime get parsedDate => DateTime.parse(appointmentDate).toLocal();
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

