import 'package:mobile_app/core/constants/api_constants.dart';
import 'package:mobile_app/core/network/api_client.dart';
import 'package:mobile_app/features/home/data/models/dentist_detail_model.dart';
import 'package:mobile_app/features/home/data/models/doctor_model.dart';
import 'package:mobile_app/features/home/data/models/post_model.dart';
import 'package:mobile_app/features/home/data/models/service_model.dart';

class HomeService {
  static final HomeService _instance = HomeService._internal();
  factory HomeService() => _instance;
  HomeService._internal();

  final _client = ApiClient();

  Future<List<DoctorModel>> getDentists() async {
    final res = await _client.get(ApiConstants.dentists);
    final list = res.data as List<dynamic>;
    return list
        .map((e) => DoctorModel.fromJson(e as Map<String, dynamic>))
        .where((d) => d.fullName.isNotEmpty)
        .toList();
  }

  Future<DentistDetailModel> getDentistDetail(String dentistId) async {
    final res = await _client.get(ApiConstants.dentistDetail(dentistId));
    return DentistDetailModel.fromJson(res.data as Map<String, dynamic>);
  }

  Future<List<ServiceModel>> getServices() async {
    final res = await _client.get(
      ApiConstants.services,
      queryParameters: {'status': 'Active'},
    );
    final list = res.data as List<dynamic>;
    return list
        .map((e) => ServiceModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<List<PostModel>> getPosts() async {
    final res = await _client.get(
      ApiConstants.posts,
      queryParameters: {'status': 'published'},
    );
    final list = res.data as List<dynamic>;
    return list
        .map((e) => PostModel.fromJson(e as Map<String, dynamic>))
        .toList();
  }
}
