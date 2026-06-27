class PatientRecord {
  final String id;
  final String name;
  final String relation;
  const PatientRecord({required this.id, required this.name, required this.relation});
}

class MedicineModel {
  final String name;
  final String type; // e.g. "Kháng sinh", "Giảm đau", "Nước súc miệng"
  final String form; // e.g. "Viên nén", "Hỗn dịch"
  final String components;
  final String uses;
  final String dosage;
  final String duration;
  final String sideEffects;
  final String notes;

  const MedicineModel({
    required this.name,
    required this.type,
    required this.form,
    required this.components,
    required this.uses,
    required this.dosage,
    required this.duration,
    required this.sideEffects,
    required this.notes,
  });
}

class TreatmentPlanPhaseTask {
  final String title;
  final bool isCompleted;
  const TreatmentPlanPhaseTask({required this.title, required this.isCompleted});
}

class TreatmentPlanPhase {
  final String id;
  final String title;
  final String subtitle;
  final String status; // "Completed", "In Progress", "Upcoming"
  final String durationText;
  final String finishedDate;
  final double progress; // e.g. 0.4
  final List<TreatmentPlanPhaseTask> tasks;

  const TreatmentPlanPhase({
    required this.id,
    required this.title,
    required this.subtitle,
    required this.status,
    required this.durationText,
    required this.finishedDate,
    required this.progress,
    required this.tasks,
  });
}

class MedicalRecordEvent {
  final String id;
  final String title;
  final String status; // "Active", "Completed"
  final String doctorName;
  final String doctorSpecialty;
  final String dateStr;
  final int year;
  final bool isJourney;
  final String? progressText;
  final double? progressPercent;

  // Detail fields
  final String? diagnosis;
  final List<String>? xRays; // names of x-rays
  final List<TreatmentPlanPhase>? phases;

  const MedicalRecordEvent({
    required this.id,
    required this.title,
    required this.status,
    required this.doctorName,
    required this.doctorSpecialty,
    required this.dateStr,
    required this.year,
    this.isJourney = false,
    this.progressText,
    this.progressPercent,
    this.diagnosis,
    this.xRays,
    this.phases,
  });
}

class MedicalRecordMock {
  static const List<PatientRecord> patients = [
    PatientRecord(id: 'alex', name: 'Alex Reed (Myself)', relation: 'Primary Member'),
    PatientRecord(id: 'binh', name: 'Nguyễn Thị Bình', relation: 'Mẹ'),
    PatientRecord(id: 'cuong', name: 'Nguyễn Văn Cường', relation: 'Con trai'),
  ];

  static final List<MedicalRecordEvent> events = [
    MedicalRecordEvent(
      id: 'e1',
      title: 'Orthodontics Journey (Metal Braces)',
      status: 'Active',
      doctorName: 'Dr. Aris Thorne',
      doctorSpecialty: 'Chỉnh nha chuyên sâu',
      dateStr: 'Jan 12, 2024',
      year: 2024,
      isJourney: true,
      progressText: 'Month 5 of 18',
      progressPercent: 0.27,
      diagnosis: 'Detected moderate dental crowding, overbite, and misaligned jaw structure.',
      xRays: ['Side View', 'Top Arch', 'Front View'],
      phases: [
        const TreatmentPlanPhase(
          id: 'p1',
          title: 'Phase 1: Initial Alignment',
          subtitle: 'Giai đoạn dàn đều răng sơ bộ',
          status: 'Completed',
          durationText: '2 Weeks',
          finishedDate: 'finished June 12',
          progress: 1.0,
          tasks: [
            TreatmentPlanPhaseTask(title: 'X-Ray Scan (3D Digital Mapping)', isCompleted: true),
            TreatmentPlanPhaseTask(title: 'Consultation (Treatment strategy review)', isCompleted: true),
          ],
        ),
        const TreatmentPlanPhase(
          id: 'p2',
          title: 'Phase 2: Refinement',
          subtitle: 'Điều trị tủy lần 2',
          status: 'In Progress',
          durationText: '6-8 Weeks',
          finishedDate: 'Estimated Nov 2024',
          progress: 0.40,
          tasks: [
            TreatmentPlanPhaseTask(title: 'Aligner Fitting (Active refinement period)', isCompleted: true),
            TreatmentPlanPhaseTask(title: 'Progress Checkup (Mid phase evaluation)', isCompleted: false),
          ],
        ),
        const TreatmentPlanPhase(
          id: 'p3',
          title: 'Phase 3: Stabilization',
          subtitle: 'Giai đoạn duy trì và cố định',
          status: 'Upcoming',
          durationText: '2 Weeks',
          finishedDate: 'Estimated Oct 2025',
          progress: 0.0,
          tasks: [
            TreatmentPlanPhaseTask(title: 'Retainer Fabrication', isCompleted: false),
            TreatmentPlanPhaseTask(title: 'Final Assessment', isCompleted: false),
          ],
        ),
      ],
    ),
    const MedicalRecordEvent(
      id: 'e2',
      title: 'Standard Check-up & Scaling',
      status: 'Completed',
      doctorName: 'Dr. Aris Thorne',
      doctorSpecialty: 'Nha khoa tổng quát',
      dateStr: 'Oct 24, 2023',
      year: 2023,
      diagnosis: 'Teeth scaling completed. Found moderate tartar and food plaque on back molars. Dental health is generally stable.',
      xRays: ['Panoramic View'],
    ),
    const MedicalRecordEvent(
      id: 'e3',
      title: 'Cavity Restoration (#14, #16)',
      status: 'Completed',
      doctorName: 'Dr. Sarah Miller',
      doctorSpecialty: 'Nha khoa thẩm mỹ',
      dateStr: 'Aug 05, 2023',
      year: 2023,
      diagnosis: 'Detected deep cavity on Tooth #14 and #16. Recommended immediate composite filling. Completed composite restoration.',
      xRays: ['Bite-wing X-Ray'],
    ),
  ];

  static const List<MedicineModel> medicines = [
    MedicineModel(
      name: 'Amoxicillin 500mg',
      type: 'Kháng sinh',
      form: 'Viên nén',
      components: 'Amoxicillin 500mg',
      uses: 'Kháng sinh nhóm penicillin, tiêu diệt vi khuẩn, phòng ngừa và điều trị nhiễm trùng răng miệng.',
      dosage: 'Uống 1 viên/lần, 2 lần/ngày, sau khi ăn.',
      duration: '5 ngày liên tục theo chỉ định.',
      sideEffects: 'Có thể gây buồn nôn, tiêu chảy nhẹ, hoặc phát ban nếu dị ứng với penicillin.',
      notes: 'Uống đúng liều lượng và đủ thời gian chỉ định để tránh đề kháng kháng sinh.',
    ),
    MedicineModel(
      name: 'Paracetamol 500mg',
      type: 'Giảm đau, hạ sốt',
      form: 'Viên nén',
      components: 'Paracetamol 500mg',
      uses: 'Giảm các triệu chứng đau răng, nhức đầu, ê buốt răng sau khi làm thủ thuật điều trị tủy.',
      dosage: 'Uống 1 viên/lần khi đau hoặc sốt trên 38.5 độ, cách mỗi 4-6 tiếng (tối đa 4 viên/ngày).',
      duration: '3 ngày hoặc uống khi đau.',
      sideEffects: 'Có thể ảnh hưởng gan nếu dùng quá liều hoặc dùng kèm bia rượu.',
      notes: 'Không sử dụng quá liều quy định. Tránh xa tầm tay trẻ em.',
    ),
    MedicineModel(
      name: 'Chlorhexidine 0.12%',
      type: 'Nước súc miệng diệt khuẩn',
      form: 'Hỗn dịch súc miệng',
      components: 'Chlorhexidine Digluconate 0.12%',
      uses: 'Sát khuẩn khoang miệng, giúp làm sạch mảng bám và thúc đẩy phục hồi mô nướu sau điều trị.',
      dosage: 'Súc miệng 10ml/lần, ngậm khoảng 30 giây rồi nhổ đi, ngày 2 lần sáng và tối.',
      duration: '7 ngày liên tục.',
      sideEffects: 'Có thể gây đổi màu răng tạm thời hoặc thay đổi vị giác nhẹ trong thời gian sử dụng.',
      notes: 'Không nuốt nước súc miệng. Không ăn uống trong vòng 30 phút sau khi súc miệng.',
    ),
  ];
}
