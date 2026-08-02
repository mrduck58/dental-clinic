using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;

namespace DentalClinic.API.Application.UseCases.Patients;

/// <summary>Tra cứu/khởi tạo hồ sơ Patient chính chủ của user hiện tại, và kiểm tra quyền truy cập
/// hồ sơ (chính chủ hoặc thành viên gia đình) — dùng chung bởi các handler trong UseCases/Patients.</summary>
public class PatientAccessHelper(IPatientRepository patientRepository, IUserRepository userRepository)
{
    public async Task<Patient?> GetOrCreatePrimaryPatientAsync(Guid userId, CancellationToken ct)
    {
        var patient = await patientRepository.GetByUserIdAsync(userId, ct);
        if (patient == null)
        {
            var user = await userRepository.GetByIdAsync(userId, ct);
            if (user == null) return null;

            patient = Patient.Create(
                userId: user.Id,
                dateOfBirth: null
            );
            patient.User = user;

            await patientRepository.AddAsync(patient, ct);
        }
        return patient;
    }

    /// <summary>Chính chủ hoặc thành viên gia đình thuộc về [primaryPatient] mới được xem/sửa [patient].</summary>
    public static bool IsSelfOrFamilyMember(Patient patient, Patient primaryPatient) =>
        patient.Id == primaryPatient.Id || patient.PrimaryPatientId == primaryPatient.Id;
}
