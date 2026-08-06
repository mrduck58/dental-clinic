using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Queue;

public record PatientQueueStepDto(
    int QueueNumber,
    string Status,
    bool IsYours
);

public record PatientQueueResponse(
    bool HasActiveQueue,
    Guid? AppointmentId = null,
    string? AppointmentCode = null,
    int? QueueNumber = null,
    string? Status = null,
    string? RoomName = null,
    string? DentistName = null,
    int? CurrentServingNumber = null,
    int? PeopleAhead = null,
    int? EstWaitMinutes = null,
    List<PatientQueueStepDto>? Steps = null
);

public record GetPatientQueueQuery(Guid UserId, Guid? PatientId) : IRequest<PatientQueueResponse>;

/// <remarks>
/// Gọi <see cref="GetWaitingQueueQuery"/> qua <c>ISender</c> để dùng CHÍNH XÁC cùng thuật toán đánh
/// số/xếp thứ tự hàng đợi phòng mà màn hình lễ tân đang thấy (cùng <see cref="GetWaitingQueueHandler"/>).
/// </remarks>
public class GetPatientQueueHandler(
    IPatientRepository patientRepository,
    IUserRepository userRepository,
    ISender sender,
    IAppointmentRepository appointmentRepository) : IRequestHandler<GetPatientQueueQuery, PatientQueueResponse>
{
    private static readonly TimeZoneInfo VietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    private async Task<Patient?> GetOrCreatePrimaryPatientAsync(Guid userId, CancellationToken ct)
    {
        var patient = await patientRepository.GetByUserIdAsync(userId, ct);
        if (patient == null)
        {
            var user = await userRepository.GetByIdAsync(userId, ct);
            if (user == null) return null;

            patient = Patient.Create(
                userId: user.Id,
                dateOfBirth: null);
            patient.User = user;

            await patientRepository.AddAsync(patient, ct);
        }
        return patient;
    }

    public async Task<PatientQueueResponse> Handle(GetPatientQueueQuery request, CancellationToken ct)
    {
        var userId = request.UserId;
        var patientId = request.PatientId;

        Console.WriteLine($"[PatientQueue] HandleAsync: userId={userId}, patientId={patientId}");
        var primaryPatient = await GetOrCreatePrimaryPatientAsync(userId, ct);
        if (primaryPatient is null)
        {
            Console.WriteLine("[PatientQueue] Primary patient profile not found.");
            return new PatientQueueResponse(false);
        }
        Console.WriteLine($"[PatientQueue] Primary patient resolved: Id={primaryPatient.Id}, Name={primaryPatient.FullName}");

        Guid targetPatientId = primaryPatient.Id;
        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            if (patientId.Value != primaryPatient.Id)
            {
                var familyMembers = await patientRepository.GetFamilyMembersAsync(primaryPatient.Id, ct);
                if (!familyMembers.Any(f => f.Id == patientId.Value))
                {
                    Console.WriteLine($"[PatientQueue] PatientId={patientId.Value} is not a family member of primary patient {primaryPatient.Id}");
                    return new PatientQueueResponse(false);
                }
                targetPatientId = patientId.Value;
            }
        }
        Console.WriteLine($"[PatientQueue] Target patient resolved: Id={targetPatientId}");

        var nowUtc = DateTimeOffset.UtcNow;
        var nowVietnam = TimeZoneInfo.ConvertTime(nowUtc, VietnamTimeZone);
        var date = DateOnly.FromDateTime(nowVietnam.DateTime);

        var vietnamDateStart = new DateTimeOffset(date.Year, date.Month, date.Day, 0, 0, 0, VietnamTimeZone.BaseUtcOffset);
        var utcStart = vietnamDateStart.ToUniversalTime();
        var utcEnd = utcStart.AddDays(1);
        Console.WriteLine($"[PatientQueue] Date range: {utcStart:yyyy-MM-dd HH:mm:ss} to {utcEnd:yyyy-MM-dd HH:mm:ss} (VN Date: {date:yyyy-MM-dd})");

        // Find active appointment for today (CheckedIn or InProgress)
        var appointment = await appointmentRepository.GetActiveTodayByPatientAsync(targetPatientId, utcStart, utcEnd, ct);

        if (appointment is null)
        {
            var anyAppointmentsToday = await appointmentRepository.GetByPatientAndDateRangeAsync(targetPatientId, utcStart, utcEnd, ct);

            Console.WriteLine($"[PatientQueue] No CheckedIn/InProgress appointment today. Found {anyAppointmentsToday.Count} total appointments today:");
            foreach (var app in anyAppointmentsToday)
            {
                Console.WriteLine($"  -> Appointment Id: {app.Id}, Status: {app.Status}, Date: {app.AppointmentDate}");
            }
            return new PatientQueueResponse(false);
        }
        Console.WriteLine($"[PatientQueue] Found active appointment: Id={appointment.Id}, Status={appointment.Status}");

        // Get full clinic waiting queue for today
        var clinicQueue = await sender.Send(new GetWaitingQueueQuery(date), ct);

        // Find the room and the patient's queue entry
        RoomQueueDto? targetRoom = null;
        QueuePatientDto? targetPatientEntry = null;

        foreach (var room in clinicQueue.Rooms)
        {
            var entry = room.Patients.FirstOrDefault(p => p.AppointmentId == appointment.Id);
            if (entry != null)
            {
                targetRoom = room;
                targetPatientEntry = entry;
                break;
            }
        }

        if (targetRoom is null || targetPatientEntry is null)
        {
            return new PatientQueueResponse(false);
        }

        // Current serving number in this room (InProgress status)
        int? currentServingNumber = targetRoom.Patients
            .FirstOrDefault(p => p.Status == AppointmentStatus.InProgress.ToString())?.QueueNumber;

        // Calculate people ahead
        int peopleAhead = 0;
        foreach (var p in targetRoom.Patients)
        {
            if (p.AppointmentId == appointment.Id)
                break;
            if (p.Status == AppointmentStatus.CheckedIn.ToString())
                peopleAhead++;
        }

        int estWaitMinutes = peopleAhead * 15;

        // Map steps for timeline
        var steps = targetRoom.Patients.Select(p => new PatientQueueStepDto(
            p.QueueNumber,
            p.Status,
            p.AppointmentId == appointment.Id
        )).ToList();

        return new PatientQueueResponse(
            HasActiveQueue: true,
            AppointmentId: appointment.Id,
            AppointmentCode: targetPatientEntry.AppointmentCode,
            QueueNumber: targetPatientEntry.QueueNumber,
            Status: targetPatientEntry.Status,
            RoomName: targetRoom.RoomName ?? "Phòng khám chung",
            DentistName: targetPatientEntry.DentistName,
            CurrentServingNumber: currentServingNumber,
            PeopleAhead: peopleAhead,
            EstWaitMinutes: estWaitMinutes,
            Steps: steps
        );
    }
}
