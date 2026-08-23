using DentalClinic.API.Application.DTOs.ClinicalRecords;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using MediatR;

namespace DentalClinic.API.Application.UseCases.ClinicalRecords;

public record AddAppointmentPhotoRequest(Guid AppointmentId, string Section, string Url, string? Note) : IRequest<AppointmentPhotoDto>;

public record GetAppointmentPhotosQuery(Guid AppointmentId, string? Section = null) : IRequest<IEnumerable<AppointmentPhotoDto>>;

public record UpdateAppointmentPhotoNoteRequest(Guid PhotoId, string? Note) : IRequest<AppointmentPhotoDto>;

public record DeleteAppointmentPhotoCommand(Guid PhotoId) : IRequest;

public class AddAppointmentPhotoHandler(
    IAppointmentRepository appointmentRepository,
    IAppointmentPhotoRepository photoRepository,
    ICurrentUserService currentUser) : IRequestHandler<AddAppointmentPhotoRequest, AppointmentPhotoDto>
{
    public async Task<AppointmentPhotoDto> Handle(AddAppointmentPhotoRequest request, CancellationToken ct)
    {
        if (request.Section != AppointmentPhoto.SectionExam && request.Section != AppointmentPhoto.SectionMaterialRequest)
            throw new ValidationException("Khu vực ảnh không hợp lệ.");

        if (string.IsNullOrWhiteSpace(request.Url))
            throw new ValidationException("Thiếu đường dẫn ảnh.");

        _ = await appointmentRepository.GetByIdAsync(request.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        var photo = AppointmentPhoto.Create(
            request.AppointmentId, request.Section, request.Url,
            ClinicalRecordMappers.NormalizeText(request.Note), currentUser.UserName);

        await photoRepository.AddAsync(photo, ct);

        return ClinicalRecordMappers.ToDto(photo);
    }
}

public class GetAppointmentPhotosHandler(IAppointmentPhotoRepository photoRepository)
    : IRequestHandler<GetAppointmentPhotosQuery, IEnumerable<AppointmentPhotoDto>>
{
    public async Task<IEnumerable<AppointmentPhotoDto>> Handle(GetAppointmentPhotosQuery request, CancellationToken ct)
    {
        var photos = await photoRepository.GetByAppointmentIdAsync(request.AppointmentId, request.Section, ct);

        return photos.Select(ClinicalRecordMappers.ToDto);
    }
}

public class UpdateAppointmentPhotoNoteHandler(IAppointmentPhotoRepository photoRepository)
    : IRequestHandler<UpdateAppointmentPhotoNoteRequest, AppointmentPhotoDto>
{
    public async Task<AppointmentPhotoDto> Handle(UpdateAppointmentPhotoNoteRequest request, CancellationToken ct)
    {
        var photo = await photoRepository.GetByIdAsync(request.PhotoId, ct)
            ?? throw new NotFoundException("Không tìm thấy ảnh.");

        photo.UpdateNote(ClinicalRecordMappers.NormalizeText(request.Note));
        await photoRepository.UpdateAsync(photo, ct);

        return ClinicalRecordMappers.ToDto(photo);
    }
}

public class DeleteAppointmentPhotoHandler(IAppointmentPhotoRepository photoRepository)
    : IRequestHandler<DeleteAppointmentPhotoCommand>
{
    public async Task Handle(DeleteAppointmentPhotoCommand command, CancellationToken ct)
    {
        var photo = await photoRepository.GetByIdAsync(command.PhotoId, ct)
            ?? throw new NotFoundException("Không tìm thấy ảnh.");

        await photoRepository.DeleteAsync(photo, ct);
    }
}
