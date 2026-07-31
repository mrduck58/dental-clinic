import 'package:mobile_app/features/booking/data/booking_models.dart';

class DoctorModel {
  final String id;
  final String fullName;
  final String? specialty;
  final String? profilePictureUrl;
  final int? yearsOfExperience;
  final String? bio;

  const DoctorModel({
    required this.id,
    required this.fullName,
    this.specialty,
    this.profilePictureUrl,
    this.yearsOfExperience,
    this.bio,
  });

  DoctorInfo toDoctorInfo() {
    return DoctorInfo(
      id: id,
      name: fullName,
      title: '',
      specialty: specialty ?? '',
      room: '',
      session: DoctorSession.morning,
      rating: 5.0,
      reviewCount: 0,
      avatarUrl: profilePictureUrl,
    );
  }

  factory DoctorModel.fromJson(Map<String, dynamic> json) {
    final rawId = json['id'] ?? json['dentistId'] ?? json['userId'];
    final rawName = json['fullName'] ?? json['name'] ?? json['staffName'] ?? '';
    final rawSpecialty = json['specialty'] ?? json['specialization'];
    final rawAvatar = json['profilePictureUrl'] ?? json['avatarUrl'];
    final rawExp = json['yearsOfExperience'] ?? json['experienceYears'];
    final rawBio = json['bio'] ?? json['biography'];

    int? expYears;
    if (rawExp is int) {
      expYears = rawExp;
    } else if (rawExp != null) {
      expYears = int.tryParse(rawExp.toString());
    }

    return DoctorModel(
      id: rawId != null ? rawId.toString() : '',
      fullName: rawName.toString(),
      specialty: rawSpecialty?.toString(),
      profilePictureUrl: rawAvatar?.toString(),
      yearsOfExperience: expYears,
      bio: rawBio?.toString(),
    );
  }
}
