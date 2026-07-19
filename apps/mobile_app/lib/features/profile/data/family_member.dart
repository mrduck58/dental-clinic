import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:mobile_app/features/auth/data/auth_service.dart';

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

  Map<String, dynamic> toJson() => {
        'id': id,
        'fullName': fullName,
        'relationship': relationship,
        'dateOfBirth': dateOfBirth.toIso8601String(),
        'gender': gender,
        'phoneNumber': phoneNumber,
        'profilePictureUrl': profilePictureUrl,
      };

  factory FamilyMember.fromJson(Map<String, dynamic> json) => FamilyMember(
        id: json['id'] as String,
        fullName: json['fullName'] as String,
        relationship: json['relationship'] as String,
        dateOfBirth: DateTime.parse(json['dateOfBirth'] as String),
        gender: json['gender'] as String,
        phoneNumber: json['phoneNumber'] as String?,
        profilePictureUrl: json['profilePictureUrl'] as String?,
      );
}

class FamilyService {
  static final FamilyService _instance = FamilyService._internal();
  factory FamilyService() => _instance;
  FamilyService._internal();

  final List<FamilyMember> _members = [];

  final _auth = AuthService();

  List<FamilyMember> getMembers() => List.unmodifiable(_members);

  /// Ném lại lỗi thay vì nuốt âm thầm — nếu không, danh sách cũ (có thể rỗng)
  /// sẽ bị hiển thị như thể tải thành công, khiến người dùng không biết là lỗi mạng.
  Future<void> loadFromServer() async {
    try {
      final list = await _auth.getFamilyMembers();
      _members.clear();
      _members.addAll(list.map((item) {
        final dobStr = item['dateOfBirth'] as String;
        final dob = DateTime.tryParse(dobStr) ?? DateTime(2000, 1, 1);
        return FamilyMember(
          id: item['id'].toString(),
          fullName: item['fullName'] as String,
          relationship: item['relationship'] as String,
          dateOfBirth: dob,
          gender: item['gender'] as String,
          phoneNumber: item['phoneNumber'] as String?,
          profilePictureUrl: item['profilePictureUrl'] as String?,
        );
      }));
    } catch (e) {
      debugPrint("FamilyService: Failed to load from server: $e");
      rethrow;
    }
  }

  Future<void> addMember(FamilyMember member) async {
    try {
      final data = {
        'fullName': member.fullName,
        'relationship': member.relationship,
        'dateOfBirth': "${member.dateOfBirth.year.toString().padLeft(4, '0')}-${member.dateOfBirth.month.toString().padLeft(2, '0')}-${member.dateOfBirth.day.toString().padLeft(2, '0')}",
        'gender': member.gender,
        'phoneNumber': member.phoneNumber,
        'profilePictureUrl': member.profilePictureUrl,
      };
      final res = await _auth.createFamilyMember(data);
      final created = FamilyMember(
        id: res['id'].toString(),
        fullName: res['fullName'] as String,
        relationship: res['relationship'] as String,
        dateOfBirth: member.dateOfBirth,
        gender: res['gender'] as String,
        phoneNumber: res['phoneNumber'] as String?,
        profilePictureUrl: res['profilePictureUrl'] as String?,
      );
      _members.add(created);
    } catch (e) {
      debugPrint("FamilyService: Failed to create family member on server: $e");
      rethrow;
    }
  }

  Future<void> updateMember(FamilyMember updated) async {
    try {
      final data = {
        'fullName': updated.fullName,
        'relationship': updated.relationship,
        'dateOfBirth': "${updated.dateOfBirth.year.toString().padLeft(4, '0')}-${updated.dateOfBirth.month.toString().padLeft(2, '0')}-${updated.dateOfBirth.day.toString().padLeft(2, '0')}",
        'gender': updated.gender,
        'phoneNumber': updated.phoneNumber,
        'profilePictureUrl': updated.profilePictureUrl,
      };
      await _auth.updateFamilyMember(updated.id, data);
      final idx = _members.indexWhere((m) => m.id == updated.id);
      if (idx != -1) {
        _members[idx] = updated;
      }
    } catch (e) {
      debugPrint("FamilyService: Failed to update family member on server: $e");
      rethrow;
    }
  }

  Future<void> removeMember(String id) async {
    try {
      await _auth.deleteFamilyMember(id);
      _members.removeWhere((m) => m.id == id);
    } catch (e) {
      debugPrint("FamilyService: Failed to delete family member on server: $e");
      rethrow;
    }
  }
}
