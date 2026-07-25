class PatientQueueStep {
  final int queueNumber;
  final String status;
  final bool isYours;

  PatientQueueStep({
    required this.queueNumber,
    required this.status,
    required this.isYours,
  });

  factory PatientQueueStep.fromJson(Map<String, dynamic> json) => PatientQueueStep(
        queueNumber: json['queueNumber'] as int,
        status: json['status'] as String,
        isYours: json['isYours'] as bool,
      );
}

class PatientQueueResponse {
  final bool hasActiveQueue;
  final String? appointmentId;
  final String? appointmentCode;
  final int? queueNumber;
  final String? status;
  final String? roomName;
  final String? dentistName;
  final int? currentServingNumber;
  final int? peopleAhead;
  final int? estWaitMinutes;
  final List<PatientQueueStep>? steps;

  PatientQueueResponse({
    required this.hasActiveQueue,
    this.appointmentId,
    this.appointmentCode,
    this.queueNumber,
    this.status,
    this.roomName,
    this.dentistName,
    this.currentServingNumber,
    this.peopleAhead,
    this.estWaitMinutes,
    this.steps,
  });

  factory PatientQueueResponse.fromJson(Map<String, dynamic> json) => PatientQueueResponse(
        hasActiveQueue: json['hasActiveQueue'] as bool,
        appointmentId: json['appointmentId'] as String?,
        appointmentCode: json['appointmentCode'] as String?,
        queueNumber: json['queueNumber'] as int?,
        status: json['status'] as String?,
        roomName: json['roomName'] as String?,
        dentistName: json['dentistName'] as String?,
        currentServingNumber: json['currentServingNumber'] as int?,
        peopleAhead: json['peopleAhead'] as int?,
        estWaitMinutes: json['estWaitMinutes'] as int?,
        steps: json['steps'] != null
            ? (json['steps'] as List<dynamic>)
                .map((e) => PatientQueueStep.fromJson(e as Map<String, dynamic>))
                .toList()
            : null,
      );
}
