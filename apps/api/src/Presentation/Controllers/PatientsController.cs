using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.API.Presentation.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController(
    IPatientRepository patientRepository,
    IUserRepository userRepository,
    ICurrentUserService currentUser) : ControllerBase
{
    private async Task<Patient?> GetOrCreatePrimaryPatientAsync(Guid userId, CancellationToken ct)
    {
        var patient = await patientRepository.GetByUserIdAsync(userId, ct);
        if (patient == null)
        {
            var user = await userRepository.GetByIdAsync(userId, ct);
            if (user == null) return null;

            patient = Patient.Create(
                fullName: user.FullName ?? user.Username ?? user.Email,
                dateOfBirth: user.DateOfBirth ?? new DateOnly(2000, 1, 1),
                gender: user.Gender ?? "Nam",
                userId: user.Id,
                phoneNumber: user.PhoneNumber
            );

            await patientRepository.AddAsync(patient, ct);
        }
        return patient;
    }

    [HttpGet("my-medical-history")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyMedicalHistory(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == null) return Unauthorized();

        var patient = await GetOrCreatePrimaryPatientAsync(userId.Value, ct);
        if (patient == null) return NotFound("Patient profile not found.");

        return Ok(new { medicalHistory = patient.MedicalHistory });
    }

    [HttpPut("my-medical-history")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> UpdateMyMedicalHistory([FromBody] UpdateMedicalHistoryRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == null) return Unauthorized();

        var patient = await GetOrCreatePrimaryPatientAsync(userId.Value, ct);
        if (patient == null) return NotFound("Patient profile not found.");

        patient.UpdateMedicalHistory(request.MedicalHistory ?? string.Empty);
        await patientRepository.UpdateAsync(patient, ct);

        return NoContent();
    }

    [HttpGet("{patientId:guid}/medical-history")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetFamilyMemberMedicalHistory(Guid patientId, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == null) return Unauthorized();

        var primaryPatient = await GetOrCreatePrimaryPatientAsync(userId.Value, ct);
        if (primaryPatient == null) return NotFound("Patient profile not found.");

        var patient = await patientRepository.GetByIdAsync(patientId, ct);
        if (patient == null) return NotFound("Patient not found.");

        // Security check: must be self or family member
        if (patient.Id != primaryPatient.Id && patient.PrimaryPatientId != primaryPatient.Id)
            return Forbid();

        return Ok(new { medicalHistory = patient.MedicalHistory });
    }

    [HttpPut("{patientId:guid}/medical-history")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> UpdateFamilyMemberMedicalHistory(Guid patientId, [FromBody] UpdateMedicalHistoryRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == null) return Unauthorized();

        var primaryPatient = await GetOrCreatePrimaryPatientAsync(userId.Value, ct);
        if (primaryPatient == null) return NotFound("Patient profile not found.");

        var patient = await patientRepository.GetByIdAsync(patientId, ct);
        if (patient == null) return NotFound("Patient not found.");

        // Security check: must be self or family member
        if (patient.Id != primaryPatient.Id && patient.PrimaryPatientId != primaryPatient.Id)
            return Forbid();

        patient.UpdateMedicalHistory(request.MedicalHistory ?? string.Empty);
        await patientRepository.UpdateAsync(patient, ct);

        return NoContent();
    }

    [HttpGet("family-members")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetFamilyMembers(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == null) return Unauthorized();

        var primaryPatient = await GetOrCreatePrimaryPatientAsync(userId.Value, ct);
        if (primaryPatient == null) return NotFound("Patient profile not found.");

        var members = await patientRepository.GetFamilyMembersAsync(primaryPatient.Id, ct);
        
        return Ok(members.Select(m => new FamilyMemberDto(
            m.Id,
            m.FullName,
            m.Relationship ?? string.Empty,
            m.DateOfBirth,
            m.Gender,
            m.PhoneNumber,
            m.ProfilePictureUrl
        )));
    }

    [HttpPost("family-members")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> CreateFamilyMember([FromBody] CreateFamilyMemberRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == null) return Unauthorized();

        var primaryPatient = await GetOrCreatePrimaryPatientAsync(userId.Value, ct);
        if (primaryPatient == null) return NotFound("Patient profile not found.");

        var member = Patient.Create(
            fullName: request.FullName,
            dateOfBirth: request.DateOfBirth,
            gender: request.Gender,
            phoneNumber: request.PhoneNumber,
            primaryPatientId: primaryPatient.Id,
            relationship: request.Relationship,
            profilePictureUrl: request.ProfilePictureUrl
        );

        await patientRepository.AddAsync(member, ct);

        return CreatedAtAction(null, new FamilyMemberDto(
            member.Id,
            member.FullName,
            member.Relationship ?? string.Empty,
            member.DateOfBirth,
            member.Gender,
            member.PhoneNumber,
            member.ProfilePictureUrl
        ));
    }

    [HttpPut("family-members/{id:guid}")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> UpdateFamilyMember(Guid id, [FromBody] UpdateFamilyMemberRequest request, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == null) return Unauthorized();

        var primaryPatient = await GetOrCreatePrimaryPatientAsync(userId.Value, ct);
        if (primaryPatient == null) return NotFound("Patient profile not found.");

        var member = await patientRepository.GetByIdAsync(id, ct);
        if (member == null || member.PrimaryPatientId != primaryPatient.Id)
            return NotFound("Family member not found.");

        member.SetFullName(request.FullName);
        member.SetDateOfBirth(request.DateOfBirth);
        member.SetGender(request.Gender);
        member.SetPhoneNumber(request.PhoneNumber);
        member.UpdateFamilyRelation(primaryPatient.Id, request.Relationship);
        member.UpdateProfilePicture(request.ProfilePictureUrl);

        await patientRepository.UpdateAsync(member, ct);

        return NoContent();
    }

    [HttpDelete("family-members/{id:guid}")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> DeleteFamilyMember(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        if (userId == null) return Unauthorized();

        var primaryPatient = await GetOrCreatePrimaryPatientAsync(userId.Value, ct);
        if (primaryPatient == null) return NotFound("Patient profile not found.");

        var member = await patientRepository.GetByIdAsync(id, ct);
        if (member == null || member.PrimaryPatientId != primaryPatient.Id)
            return NotFound("Family member not found.");

        await patientRepository.DeleteAsync(member, ct);

        return NoContent();
    }
}

public record UpdateMedicalHistoryRequest(string? MedicalHistory);

public record FamilyMemberDto(
    Guid Id,
    string FullName,
    string Relationship,
    DateOnly DateOfBirth,
    string Gender,
    string? PhoneNumber,
    string? ProfilePictureUrl
);

public record CreateFamilyMemberRequest(
    string FullName,
    string Relationship,
    DateOnly DateOfBirth,
    string Gender,
    string? PhoneNumber,
    string? ProfilePictureUrl
);

public record UpdateFamilyMemberRequest(
    string FullName,
    string Relationship,
    DateOnly DateOfBirth,
    string Gender,
    string? PhoneNumber,
    string? ProfilePictureUrl
);
