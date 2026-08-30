import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';

/// Phiếu khám răng miệng đầy đủ — khớp DiagnosisDto ở dentist app (apps/admin_website), chỉ đọc.
class MedicalHistoryDiagnosisItem {
  final String description;
  // Tình trạng lợi – niêm mạc
  final String? gumCondition;
  final String? oralMucosaCondition;
  final String? gumBleeding;
  final String? painOnChewing;
  // Tình trạng răng
  final String? teethCount;
  final String? decayedTeeth;
  final String? wornOrBrokenTeeth;
  final String? looseTeeth;
  // Vệ sinh răng miệng
  final String? tartar;
  final String? plaque;
  final String? badBreath;
  // Khớp thái dương hàm / khớp cắn
  final String? tmjSymptoms;
  final String? occlusion;
  final String? occlusionDeviation;
  final String? conclusion;
  final DateTime createdAt;

  const MedicalHistoryDiagnosisItem({
    required this.description,
    this.gumCondition,
    this.oralMucosaCondition,
    this.gumBleeding,
    this.painOnChewing,
    this.teethCount,
    this.decayedTeeth,
    this.wornOrBrokenTeeth,
    this.looseTeeth,
    this.tartar,
    this.plaque,
    this.badBreath,
    this.tmjSymptoms,
    this.occlusion,
    this.occlusionDeviation,
    this.conclusion,
    required this.createdAt,
  });

  factory MedicalHistoryDiagnosisItem.fromJson(Map<String, dynamic> json) =>
      MedicalHistoryDiagnosisItem(
        description: json['description'] as String? ?? '',
        gumCondition: json['gumCondition'] as String?,
        oralMucosaCondition: json['oralMucosaCondition'] as String?,
        gumBleeding: json['gumBleeding'] as String?,
        painOnChewing: json['painOnChewing'] as String?,
        teethCount: json['teethCount'] as String?,
        decayedTeeth: json['decayedTeeth'] as String?,
        wornOrBrokenTeeth: json['wornOrBrokenTeeth'] as String?,
        looseTeeth: json['looseTeeth'] as String?,
        tartar: json['tartar'] as String?,
        plaque: json['plaque'] as String?,
        badBreath: json['badBreath'] as String?,
        tmjSymptoms: json['tmjSymptoms'] as String?,
        occlusion: json['occlusion'] as String?,
        occlusionDeviation: json['occlusionDeviation'] as String?,
        conclusion: json['conclusion'] as String?,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}

class MedicalHistoryPlanSummary {
  final String description;
  final String status; // "InProgress" | "Completed" | "Cancelled" ...
  final double? estimatedCost;

  const MedicalHistoryPlanSummary({
    required this.description,
    required this.status,
    this.estimatedCost,
  });

  factory MedicalHistoryPlanSummary.fromJson(Map<String, dynamic> json) =>
      MedicalHistoryPlanSummary(
        description: json['description'] as String? ?? '',
        status: json['status'] as String? ?? '',
        estimatedCost: (json['estimatedCost'] as num?)?.toDouble(),
      );
}

class MedicalHistoryPrescriptionItem {
  final String medicineName;
  final String dosage;
  final int quantity;
  final String unit;
  final String usage;
  final String? notes;

  const MedicalHistoryPrescriptionItem({
    required this.medicineName,
    required this.dosage,
    required this.quantity,
    required this.unit,
    required this.usage,
    this.notes,
  });

  factory MedicalHistoryPrescriptionItem.fromJson(Map<String, dynamic> json) =>
      MedicalHistoryPrescriptionItem(
        medicineName: json['medicineName'] as String? ?? '',
        dosage: json['dosage'] as String? ?? '',
        quantity: (json['quantity'] as num?)?.toInt() ?? 0,
        unit: json['unit'] as String? ?? '',
        usage: json['usage'] as String? ?? '',
        notes: json['notes'] as String?,
      );
}

/// Ảnh chụp chiếu lúc khám (X-quang...) — không có máy tích hợp nên chỉ là ảnh chụp tay kèm ghi chú.
class MedicalHistoryPhotoItem {
  final String url;
  final String? note;
  final DateTime createdAt;

  const MedicalHistoryPhotoItem({
    required this.url,
    this.note,
    required this.createdAt,
  });

  factory MedicalHistoryPhotoItem.fromJson(Map<String, dynamic> json) =>
      MedicalHistoryPhotoItem(
        url: json['url'] as String? ?? '',
        note: json['note'] as String?,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}

/// Một buổi khám đã hoàn tất — dữ liệu thật từ `GET /appointments/my/medical-history`.
class MedicalHistoryEvent {
  final String appointmentId;
  final String appointmentCode;
  final DateTime appointmentDate;
  final String dentistId;
  final String dentistName;
  final String serviceName;
  final String? dentistAvatarUrl;
  final String? serviceId;
  final String? symptoms;
  final String patientId;
  final String patientName;
  final String patientRelationship; // "Tôi" cho chính bệnh nhân
  final DateTime? followUpDate;
  final String? followUpNote;
  final List<String> relatedAppointmentIds;
  final List<MedicalHistoryDiagnosisItem> diagnoses;
  final List<MedicalHistoryPlanSummary> treatmentPlans;
  final List<MedicalHistoryPrescriptionItem> prescriptionItems;
  final List<MedicalHistoryPhotoItem> photos;

  const MedicalHistoryEvent({
    required this.appointmentId,
    required this.appointmentCode,
    required this.appointmentDate,
    required this.dentistId,
    required this.dentistName,
    this.dentistAvatarUrl,
    required this.serviceName,
    this.serviceId,
    this.symptoms,
    required this.patientId,
    required this.patientName,
    required this.patientRelationship,
    this.followUpDate,
    this.followUpNote,
    this.relatedAppointmentIds = const [],
    required this.diagnoses,
    required this.treatmentPlans,
    required this.prescriptionItems,
    this.photos = const [],
  });

  bool get isJourney => treatmentPlans.any((p) => p.status == 'InProgress');

  Set<String> get treatmentChainIds => {appointmentId, ...relatedAppointmentIds};

  factory MedicalHistoryEvent.fromJson(Map<String, dynamic> json) =>
      MedicalHistoryEvent(
        appointmentId: json['appointmentId'].toString(),
        appointmentCode: json['appointmentCode'] as String? ?? '',
        appointmentDate: DateTime.parse(json['appointmentDate'] as String),
        dentistId: json['dentistId']?.toString() ?? '',
        dentistName: json['dentistName'] as String? ?? '',
        dentistAvatarUrl: json['dentistAvatarUrl'] as String?,
        serviceName: json['serviceName'] as String? ?? '',
        serviceId: json['serviceId']?.toString(),
        symptoms: json['symptoms'] as String?,
        patientId: json['patientId'].toString(),
        patientName: json['patientName'] as String? ?? '',
        patientRelationship: json['patientRelationship'] as String? ?? '',
        followUpDate: json['followUpDate'] == null ? null : DateTime.parse(json['followUpDate'] as String),
        followUpNote: json['followUpNote'] as String?,
        relatedAppointmentIds: (json['relatedAppointmentIds'] as List<dynamic>? ?? [])
            .map((e) => e.toString())
            .toList(),
        diagnoses: (json['diagnoses'] as List<dynamic>? ?? [])
            .map((e) => MedicalHistoryDiagnosisItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        treatmentPlans: (json['treatmentPlans'] as List<dynamic>? ?? [])
            .map((e) => MedicalHistoryPlanSummary.fromJson(e as Map<String, dynamic>))
            .toList(),
        prescriptionItems: (json['prescriptionItems'] as List<dynamic>? ?? [])
            .map((e) => MedicalHistoryPrescriptionItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        photos: (json['photos'] as List<dynamic>? ?? [])
            .map((e) => MedicalHistoryPhotoItem.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

/// Một mục nhật ký tiến độ điều trị đã thực hiện (dữ liệu thật, không phải checklist giả định).
class StepProgressEntry {
  final int stepNumber;
  final String stepName;
  final int percent;
  final DateTime date;
  final String dentistName;
  final String? note;

  const StepProgressEntry({
    required this.stepNumber,
    required this.stepName,
    required this.percent,
    required this.date,
    required this.dentistName,
    this.note,
  });

  factory StepProgressEntry.fromJson(Map<String, dynamic> json) => StepProgressEntry(
        stepNumber: (json['stepNumber'] as num?)?.toInt() ?? 0,
        stepName: json['stepName'] as String? ?? '',
        percent: (json['percent'] as num?)?.toInt() ?? 0,
        date: DateTime.parse(json['date'] as String),
        dentistName: json['dentistName'] as String? ?? '',
        note: json['note'] as String?,
      );
}

/// Một liệu trình điều trị (1 dòng dịch vụ trong kế hoạch) kèm nhật ký tiến độ thật.
class RealTreatmentPlan {
  final String id;
  final String patientId;
  final String dentistName;
  final String? appointmentId;
  final String serviceName;
  final double unitPrice;
  final int quantity;
  final String? teeth;
  final String status;
  final DateTime? warrantyUntil;
  final String? notes;
  final double totalCost;
  final double amountPaid;
  final List<StepProgressEntry> stepProgress;
  final int totalSteps;
  final int completedSteps;
  final int progressPercent;
  final DateTime createdAt;
  final DateTime? completedAt;

  const RealTreatmentPlan({
    required this.id,
    required this.patientId,
    required this.dentistName,
    this.appointmentId,
    required this.serviceName,
    required this.unitPrice,
    required this.quantity,
    this.teeth,
    required this.status,
    this.warrantyUntil,
    this.notes,
    required this.totalCost,
    required this.amountPaid,
    required this.stepProgress,
    this.totalSteps = 0,
    this.completedSteps = 0,
    this.progressPercent = 0,
    required this.createdAt,
    this.completedAt,
  });

  factory RealTreatmentPlan.fromJson(Map<String, dynamic> json) => RealTreatmentPlan(
        id: json['id'].toString(),
        patientId: json['patientId'].toString(),
        dentistName: json['dentistName'] as String? ?? '',
        appointmentId: json['appointmentId']?.toString(),
        serviceName: json['serviceName'] as String? ?? '',
        unitPrice: (json['unitPrice'] as num?)?.toDouble() ?? 0,
        quantity: (json['quantity'] as num?)?.toInt() ?? 1,
        teeth: json['teeth'] as String?,
        status: json['status'] as String? ?? '',
        warrantyUntil: json['warrantyUntil'] == null ? null : DateTime.parse(json['warrantyUntil'] as String),
        notes: json['notes'] as String?,
        totalCost: (json['totalCost'] as num?)?.toDouble() ?? 0,
        amountPaid: (json['amountPaid'] as num?)?.toDouble() ?? 0,
        stepProgress: (json['stepProgress'] as List<dynamic>? ?? [])
            .map((e) => StepProgressEntry.fromJson(e as Map<String, dynamic>))
            .toList(),
        totalSteps: (json['totalSteps'] as num?)?.toInt() ?? 0,
        completedSteps: (json['completedSteps'] as num?)?.toInt() ?? 0,
        progressPercent: (json['progressPercent'] as num?)?.toInt() ?? 0,
        createdAt: DateTime.parse(json['createdAt'] as String),
        completedAt: json['completedAt'] == null ? null : DateTime.parse(json['completedAt'] as String),
      );
}

class MedicalRecordService {
  static final MedicalRecordService _instance = MedicalRecordService._internal();
  factory MedicalRecordService() => _instance;
  MedicalRecordService._internal();

  final _client = ApiClient();
  final _auth = AuthService();

  /// Lịch sử khám của chính bệnh nhân (và thành viên gia đình nếu [patientId] bỏ trống).
  Future<List<MedicalHistoryEvent>> getMyMedicalHistory({String? patientId}) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');
    final res = await _client.get(
      ApiConstants.myMedicalHistoryRecords,
      token: token,
      queryParameters: patientId == null ? null : {'patientId': patientId},
    );
    final list = res.data as List<dynamic>;
    return list
        .map((e) => MedicalHistoryEvent.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Liệu trình điều trị (kèm nhật ký tiến độ thật) của chính bệnh nhân/thành viên gia đình.
  Future<List<RealTreatmentPlan>> getMyTreatmentPlans({String? patientId}) async {
    final token = await _auth.getToken();
    if (token == null) throw Exception('Chưa đăng nhập.');
    final res = await _client.get(
      ApiConstants.myTreatmentPlans,
      token: token,
      queryParameters: patientId == null ? null : {'patientId': patientId},
    );
    final list = res.data as List<dynamic>;
    return list
        .map((e) => RealTreatmentPlan.fromJson(e as Map<String, dynamic>))
        .toList();
  }
}
