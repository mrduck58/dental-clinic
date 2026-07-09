import 'package:flutter/material.dart';

// ─── Patient ─────────────────────────────────────────────────────────────────

class PatientInfo {
  final String id;
  final String name;
  final String relationship;
  final String? phone;
  final String? dob;
  final String gender;
  final String? patientCode;

  const PatientInfo({
    required this.id,
    required this.name,
    required this.relationship,
    this.phone,
    this.dob,
    this.gender = 'Nam',
    this.patientCode,
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

// ─── Mock data ────────────────────────────────────────────────────────────────

class BookingMockData {
  static const List<PatientInfo> patients = [
    PatientInfo(
      id: 'self',
      name: 'NGUYỄN VĂN ANH',
      relationship: 'Tôi',
      phone: '090****567',
      dob: '15/03/1995',
      gender: 'Nam',
      patientCode: 'W26-0182265',
    ),
    PatientInfo(
      id: 'p1',
      name: 'NGUYỄN THỊ BÌNH',
      relationship: 'Mẹ',
      phone: '091****678',
      dob: '20/07/1965',
      gender: 'Nữ',
      patientCode: 'W26-0192301',
    ),
    PatientInfo(
      id: 'p2',
      name: 'NGUYỄN VĂN CƯỜNG',
      relationship: 'Con trai',
      phone: null,
      dob: '10/12/2015',
      gender: 'Nam',
      patientCode: null,
    ),
  ];

  static const List<ServiceInfo> services = [
    ServiceInfo(
      id: 's1',
      name: 'KHÁM TỔNG QUÁT',
      description:
          'Kiểm tra sức khỏe răng miệng toàn diện, chẩn đoán và tư vấn kế hoạch điều trị.',
      price: '200.000đ',
    ),
    ServiceInfo(
      id: 's2',
      name: 'LẤY CAO RĂNG',
      description:
          'Loại bỏ mảng bám, vôi răng bằng sóng siêu âm, làm sạch và đánh bóng răng.',
      price: '300.000đ',
    ),
    ServiceInfo(
      id: 's3',
      name: 'TRÁM RĂNG',
      note: '(Áp dụng cho răng sâu độ 1-3)',
      description:
          'Phục hồi răng sâu, mẻ vỡ bằng vật liệu composite cao cấp, thẩm mỹ cao.',
      price: '500.000đ',
    ),
    ServiceInfo(
      id: 's4',
      name: 'NHỔ RĂNG',
      note: '(Bao gồm răng sữa, răng khôn thẳng)',
      description:
          'Nhổ răng sữa, răng không bảo tồn được với thủ thuật nhẹ nhàng, giảm đau.',
      price: '400.000đ',
    ),
    ServiceInfo(
      id: 's5',
      name: 'TẨY TRẮNG RĂNG',
      description:
          'Công nghệ tẩy trắng Zoom hoặc laser, an toàn, hiệu quả, bền màu lâu dài.',
      price: '2.500.000đ',
    ),
    ServiceInfo(
      id: 's6',
      name: 'NIỀNG RĂNG',
      note: '(Tư vấn miễn phí lần đầu)',
      description:
          'Chỉnh nha với mắc cài kim loại, sứ hoặc máng trong suốt Invisalign.',
      price: 'Từ 15.000.000đ',
    ),
    ServiceInfo(
      id: 's7',
      name: 'CẤY GHÉP IMPLANT',
      note: '(Chỉ dành cho người đủ 18 tuổi)',
      description:
          'Phục hồi răng mất bằng trụ titanium tích hợp xương, bền chắc như răng thật.',
      price: 'Từ 10.000.000đ',
    ),
  ];

  static const List<DoctorInfo> doctors = [
    DoctorInfo(
      id: 'd1',
      name: 'Trần Minh Khoa',
      title: 'BSCKII',
      specialty: 'Nha khoa tổng quát',
      room: 'Phòng 12 - Tầng 1 - Buổi sáng',
      session: DoctorSession.morning,
      rating: 4.9,
      reviewCount: 128,
    ),
    DoctorInfo(
      id: 'd2',
      name: 'Lê Thị Hoa',
      title: 'ThS.BS',
      specialty: 'Chỉnh nha',
      room: 'Phòng 15 - Tầng 1 - Buổi chiều',
      session: DoctorSession.afternoon,
      rating: 4.8,
      reviewCount: 96,
    ),
    DoctorInfo(
      id: 'd3',
      name: 'Phạm Văn Đức',
      title: 'BSCKII',
      specialty: 'Phẫu thuật răng miệng',
      room: 'Phòng 08 - Tầng 2 - Buổi sáng',
      session: DoctorSession.morning,
      rating: 4.7,
      reviewCount: 74,
    ),
    DoctorInfo(
      id: 'd4',
      name: 'Nguyễn Thu Hà',
      title: 'ThS.BS',
      specialty: 'Nha khoa thẩm mỹ',
      room: 'Phòng 20 - Tầng 2 - Buổi chiều',
      session: DoctorSession.afternoon,
      rating: 4.8,
      reviewCount: 112,
    ),
  ];

  static const List<TimeSlot> morningSlots = [
    TimeSlot(range: '07:30 - 08:30', isBooked: true),
    TimeSlot(range: '08:30 - 09:30'),
    TimeSlot(range: '09:30 - 10:30'),
    TimeSlot(range: '10:30 - 11:30'),
  ];

  static const List<TimeSlot> afternoonSlots = [
    TimeSlot(range: '13:30 - 14:30'),
    TimeSlot(range: '14:30 - 15:30', isBooked: true),
    TimeSlot(range: '15:30 - 16:30'),
    TimeSlot(range: '16:30 - 17:30'),
  ];

  static List<TimeSlot> slotsForDoctor(DoctorInfo doctor) =>
      doctor.session == DoctorSession.morning ? morningSlots : afternoonSlots;
}
