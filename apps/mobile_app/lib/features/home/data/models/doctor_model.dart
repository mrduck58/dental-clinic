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

  factory DoctorModel.fromJson(Map<String, dynamic> json) {
    return DoctorModel(
      id: json['id'].toString(),
      fullName: json['fullName'] as String? ?? '',
      specialty: json['specialty'] as String?,
      profilePictureUrl: json['profilePictureUrl'] as String?,
      yearsOfExperience: json['yearsOfExperience'] as int?,
      bio: json['bio'] as String?,
    );
  }
}
