class MedicationReminder {
  final String prescriptionItemId;
  final String medicineName;
  final String dosage;
  final String usage;
  final String time; // "HH:mm"
  final String patientId;
  final String patientName;
  final String patientRelationship;

  const MedicationReminder({
    required this.prescriptionItemId,
    required this.medicineName,
    required this.dosage,
    required this.usage,
    required this.time,
    required this.patientId,
    required this.patientName,
    required this.patientRelationship,
  });

  factory MedicationReminder.fromJson(Map<String, dynamic> json) => MedicationReminder(
        prescriptionItemId: json['prescriptionItemId'].toString(),
        medicineName: json['medicineName'] as String? ?? '',
        dosage: json['dosage'] as String? ?? '',
        usage: json['usage'] as String? ?? '',
        time: json['time'] as String? ?? '08:00',
        patientId: json['patientId'].toString(),
        patientName: json['patientName'] as String? ?? '',
        patientRelationship: json['patientRelationship'] as String? ?? '',
      );

  int get hour => int.tryParse(time.split(':').first) ?? 8;
}
