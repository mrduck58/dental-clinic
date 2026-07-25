class DentistDetailModel {
  final String id;
  final String fullName;
  final String? specialty;
  final String? profilePictureUrl;
  final int? yearsOfExperience;
  final String? bio;
  final String? education;
  final String? certificateIssuedBy;
  final int patientCount;

  const DentistDetailModel({
    required this.id,
    required this.fullName,
    this.specialty,
    this.profilePictureUrl,
    this.yearsOfExperience,
    this.bio,
    this.education,
    this.certificateIssuedBy,
    required this.patientCount,
  });

  factory DentistDetailModel.fromJson(Map<String, dynamic> json) => DentistDetailModel(
        id: json['id'].toString(),
        fullName: json['fullName'] as String? ?? '',
        specialty: json['specialty'] as String?,
        profilePictureUrl: json['profilePictureUrl'] as String?,
        yearsOfExperience: json['yearsOfExperience'] as int?,
        bio: json['bio'] as String?,
        education: json['education'] as String?,
        certificateIssuedBy: json['certificateIssuedBy'] as String?,
        patientCount: (json['patientCount'] as num?)?.toInt() ?? 0,
      );
}
