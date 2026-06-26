class FamilyMember {
  final String id;
  final String fullName;
  final String relationship;
  final DateTime dateOfBirth;
  final String gender;
  final String? phoneNumber;
  final String? profilePictureUrl;

  FamilyMember({
    required this.id,
    required this.fullName,
    required this.relationship,
    required this.dateOfBirth,
    required this.gender,
    this.phoneNumber,
    this.profilePictureUrl,
  });
}

class FamilyService {
  static final FamilyService _instance = FamilyService._internal();
  factory FamilyService() => _instance;
  FamilyService._internal();

  final List<FamilyMember> _members = [
    FamilyMember(
      id: 'member_1',
      fullName: 'Emma Reed',
      relationship: 'Em gái',
      dateOfBirth: DateTime(2009, 10, 12),
      gender: 'Nữ',
      phoneNumber: '+1 555-0199',
      profilePictureUrl: 'assets/images/bac_si_4.png',
    ),
  ];

  List<FamilyMember> getMembers() => List.unmodifiable(_members);

  void addMember(FamilyMember member) {
    _members.add(member);
  }

  void updateMember(FamilyMember updated) {
    final idx = _members.indexWhere((m) => m.id == updated.id);
    if (idx != -1) {
      _members[idx] = updated;
    }
  }

  void removeMember(String id) {
    _members.removeWhere((m) => m.id == id);
  }
}
