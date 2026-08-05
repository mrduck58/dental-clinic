using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DentalClinic.API.Application.UseCases.Inventory;

public class MaterialRequestItemDto
{
    public Guid Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class MaterialRequestDto
{
    public Guid Id { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DentistName { get; set; } = string.Empty;
    public List<MaterialRequestItemDto> Items { get; set; } = [];
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? HandledAt { get; set; }
    public string? HandledBy { get; set; }
}

public record MaterialRequestItemInput(string ItemName, int Quantity, string Unit);

public record CreateMaterialRequestRequest(Guid AppointmentId, List<MaterialRequestItemInput> Items) : IRequest<MaterialRequestDto>;

/// <summary>Bác sĩ gửi yêu cầu vật tư (nhiều dòng) từ buổi khám → sang trang nhập–xuất vật tư của staff.</summary>
public class CreateMaterialRequestHandler(AppDbContext dbContext, IMaterialRequestRepository materialRequestRepository)
    : IRequestHandler<CreateMaterialRequestRequest, MaterialRequestDto>
{
    public async Task<MaterialRequestDto> Handle(CreateMaterialRequestRequest request, CancellationToken ct)
    {
        if (request.Items is not { Count: > 0 })
            throw new ValidationException("Yêu cầu vật tư phải có ít nhất 1 vật tư.");

        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ItemName))
                throw new ValidationException("Tên vật tư không được để trống.");
            if (item.Quantity <= 0)
                throw new ValidationException($"Số lượng của \"{item.ItemName}\" phải lớn hơn 0.");
            if (!InventoryConstants.AllowedUnits.Contains(item.Unit))
                throw new ValidationException($"Đơn vị của \"{item.ItemName}\" không hợp lệ. Vui lòng chọn từ danh sách.");
        }

        var appt = await dbContext.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Dentist)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        var mr = MaterialRequest.Create(
            courseName: appt.Service?.Name ?? "Khám tổng quát",
            patientName: appt.Patient.FullName,
            dentistName: appt.Dentist.FullName,
            items: request.Items.Select(i => (i.ItemName.Trim(), i.Quantity, i.Unit)),
            courseId: appt.PatientId); // dùng CourseId (cột cũ, không còn dùng cho course) để lưu PatientId

        await materialRequestRepository.AddAsync(mr, ct);

        return new MaterialRequestDto
        {
            Id = mr.Id,
            CourseName = mr.CourseName,
            PatientName = mr.PatientName,
            DentistName = mr.DentistName,
            Items = mr.Items.Select(i => new MaterialRequestItemDto { Id = i.Id, ItemName = i.ItemName, Quantity = i.Quantity, Unit = i.Unit }).ToList(),
            Status = mr.Status.ToString(),
            CreatedAt = mr.CreatedAt
        };
    }
}

public record GetMaterialRequestsQuery(
    string? Status = null,
    Guid? PatientId = null,
    string? PatientName = null) : IRequest<List<MaterialRequestDto>>;

/// <summary>Danh sách yêu cầu vật tư từ bác sĩ (cho trang nhập–xuất vật tư của staff).</summary>
public class GetMaterialRequestsHandler(IMaterialRequestRepository materialRequestRepository)
    : IRequestHandler<GetMaterialRequestsQuery, List<MaterialRequestDto>>
{
    public async Task<List<MaterialRequestDto>> Handle(GetMaterialRequestsQuery query, CancellationToken ct)
    {
        var rows = await materialRequestRepository.SearchAsync(
            query.Status, query.PatientId, query.PatientName, ct);

        return rows.Select(m => new MaterialRequestDto
        {
            Id = m.Id,
            CourseName = m.CourseName,
            PatientName = m.PatientName,
            DentistName = m.DentistName,
            Items = m.Items.Select(i => new MaterialRequestItemDto { Id = i.Id, ItemName = i.ItemName, Quantity = i.Quantity, Unit = i.Unit }).ToList(),
            Status = m.Status.ToString(),
            CreatedAt = m.CreatedAt,
            HandledAt = m.HandledAt,
            HandledBy = m.HandledBy
        }).ToList();
    }
}

public record MaterialRequestItemPriceInput(Guid MaterialRequestItemId, decimal UnitPrice);

public record MarkMaterialRequestDoneCommand(Guid Id, string HandledBy, List<MaterialRequestItemPriceInput> ItemPrices) : IRequest;

/// <summary>
/// Đánh dấu một yêu cầu vật tư đã được kho xử lý — staff phải nhập giá cho TỪNG item trước, sau đó
/// nhập thẳng từng vật tư vào kho (tái dùng StockImportHandler cho mỗi dòng) với OrderType "custom"
/// (đặt riêng cho bệnh nhân — vật tư này gắn với 1 yêu cầu điều trị cụ thể, không phải hàng tồn dùng chung),
/// trong 1 transaction để không nửa vời nếu lỗi giữa chừng.
/// </summary>
public class MarkMaterialRequestDoneHandler(
    AppDbContext dbContext,
    IMaterialRequestRepository materialRequestRepository,
    ISender sender) : IRequestHandler<MarkMaterialRequestDoneCommand>
{
    private const string DefaultCategory = "Vật liệu"; // chỉ dùng khi vật tư chưa từng có trong kho (xem StockImportHandler)
    private const string PatientOrderType = "custom"; // "Đặt riêng cho bệnh nhân" — xem SupplyItem.OrderType

    public async Task Handle(MarkMaterialRequestDoneCommand command, CancellationToken ct)
    {
        var request = await materialRequestRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy yêu cầu vật tư.");

        var priceByItemId = command.ItemPrices.ToDictionary(p => p.MaterialRequestItemId, p => p.UnitPrice);
        foreach (var item in request.Items)
        {
            if (!priceByItemId.TryGetValue(item.Id, out var price))
                throw new ValidationException($"Thiếu đơn giá cho vật tư \"{item.ItemName}\".");
            if (price < 0)
                throw new ValidationException($"Đơn giá của \"{item.ItemName}\" không được âm.");
        }

        // IsRelational(): InMemory provider (dùng trong unit test) không hỗ trợ transaction —
        // chỉ bọc transaction thật khi chạy trên Postgres, tránh nửa vời nếu 1 item lỗi giữa vòng lặp.
        var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(ct)
            : null;
        try
        {
            foreach (var item in request.Items)
            {
                var tx = await sender.Send(
                    new StockImportCommand(
                        item.ItemName,
                        item.Unit,
                        DefaultCategory,
                        item.Quantity,
                        $"Theo yêu cầu vật tư: {request.CourseName} · BS {request.DentistName}",
                        priceByItemId[item.Id],
                        PatientOrderType,
                        command.HandledBy),
                    ct);
                item.LinkSupplyTransaction(tx.Id);
            }

            request.MarkDone(command.HandledBy);
            await materialRequestRepository.UpdateAsync(request, ct);

            if (transaction is not null) await transaction.CommitAsync(ct);
        }
        finally
        {
            if (transaction is not null) await transaction.DisposeAsync();
        }
    }
}
