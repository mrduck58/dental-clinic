using DentalClinic.API.Domain.Constants;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using MediatR;

namespace DentalClinic.API.Application.UseCases.Inventory;

public class MaterialRequestItemDto
{
    public Guid Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    /// <summary>Số lượng thực nhận lúc nhập kho — null nếu chưa xử lý xong (Pending/Ordered).</summary>
    public int? ActualQuantity { get; set; }
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
    public DateTimeOffset? OrderedAt { get; set; }
    public string? OrderedBy { get; set; }
    public string? SupplierNote { get; set; }
    public DateTimeOffset? HandledAt { get; set; }
    public string? HandledBy { get; set; }
}

public record MaterialRequestItemInput(string ItemName, int Quantity, string Unit);

public record CreateMaterialRequestRequest(Guid AppointmentId, List<MaterialRequestItemInput> Items) : IRequest<MaterialRequestDto>;

/// <summary>Bác sĩ gửi yêu cầu vật tư (nhiều dòng) từ buổi khám → sang trang nhập–xuất vật tư của staff.</summary>
public class CreateMaterialRequestHandler(
    IAppointmentSummaryReader appointmentSummaryReader,
    IMaterialRequestRepository materialRequestRepository)
    : IRequestHandler<CreateMaterialRequestRequest, MaterialRequestDto>
{
    public async Task<MaterialRequestDto> Handle(CreateMaterialRequestRequest request, CancellationToken ct)
    {
        ValidateItems(request.Items);

        var appt = await appointmentSummaryReader.GetSummaryAsync(request.AppointmentId, ct)
            ?? throw new NotFoundException("Không tìm thấy lịch hẹn.");

        var mr = MaterialRequest.Create(
            courseName: appt.ServiceName ?? "Khám tổng quát",
            patientName: appt.PatientName,
            dentistName: appt.DentistName,
            items: request.Items.Select(i => (i.ItemName.Trim(), i.Quantity, i.Unit)),
            patientId: appt.PatientId);

        await materialRequestRepository.AddAsync(mr, ct);

        return ToDto(mr);
    }

    internal static void ValidateItems(List<MaterialRequestItemInput> items)
    {
        if (items is not { Count: > 0 })
            throw new ValidationException("Yêu cầu vật tư phải có ít nhất 1 vật tư.");

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.ItemName))
                throw new ValidationException("Tên vật tư không được để trống.");
            if (item.Quantity <= 0)
                throw new ValidationException($"Số lượng của \"{item.ItemName}\" phải lớn hơn 0.");
            if (!InventoryConstants.AllowedUnits.Contains(item.Unit))
                throw new ValidationException($"Đơn vị của \"{item.ItemName}\" không hợp lệ. Vui lòng chọn từ danh sách.");
        }
    }

    internal static MaterialRequestDto ToDto(MaterialRequest mr) => new()
    {
        Id = mr.Id,
        CourseName = mr.CourseName,
        PatientName = mr.PatientName,
        DentistName = mr.DentistName,
        Items = mr.Items.Select(i => new MaterialRequestItemDto
        {
            Id = i.Id,
            ItemName = i.ItemName,
            Quantity = i.Quantity,
            Unit = i.Unit,
            ActualQuantity = i.ActualQuantity,
        }).ToList(),
        Status = mr.Status.ToString(),
        CreatedAt = mr.CreatedAt,
        OrderedAt = mr.OrderedAt,
        OrderedBy = mr.OrderedBy,
        SupplierNote = mr.SupplierNote,
        HandledAt = mr.HandledAt,
        HandledBy = mr.HandledBy,
    };
}

/// <summary>Staff tự khởi tạo yêu cầu đặt vật tư riêng cho bệnh nhân — không cần đi qua buổi khám của
/// bác sĩ (vd: staff biết trước cần đặt răng sứ cho ca hẹn tuần sau). PatientName do FE gửi kèm luôn
/// (đã có sẵn từ kết quả tra cứu bệnh nhân), tránh phải phụ thuộc cấu trúc Patient/User ở đây.</summary>
public record CreateMaterialRequestByStaffRequest(
    Guid PatientId,
    string PatientName,
    string Description,
    List<MaterialRequestItemInput> Items) : IRequest<MaterialRequestDto>;

public class CreateMaterialRequestByStaffHandler(IMaterialRequestRepository materialRequestRepository)
    : IRequestHandler<CreateMaterialRequestByStaffRequest, MaterialRequestDto>
{
    public async Task<MaterialRequestDto> Handle(CreateMaterialRequestByStaffRequest request, CancellationToken ct)
    {
        CreateMaterialRequestHandler.ValidateItems(request.Items);

        if (request.PatientId == Guid.Empty || string.IsNullOrWhiteSpace(request.PatientName))
            throw new ValidationException("Phải chọn bệnh nhân cho yêu cầu vật tư.");

        if (string.IsNullOrWhiteSpace(request.Description))
            throw new ValidationException("Phải nhập mô tả cho yêu cầu vật tư.");

        var mr = MaterialRequest.Create(
            courseName: request.Description.Trim(),
            patientName: request.PatientName.Trim(),
            dentistName: string.Empty,
            items: request.Items.Select(i => (i.ItemName.Trim(), i.Quantity, i.Unit)),
            patientId: request.PatientId);

        await materialRequestRepository.AddAsync(mr, ct);

        return CreateMaterialRequestHandler.ToDto(mr);
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

        return rows.Select(CreateMaterialRequestHandler.ToDto).ToList();
    }
}

public record MarkMaterialRequestOrderedCommand(Guid Id, string OrderedBy, string? SupplierNote) : IRequest<MaterialRequestDto>;

/// <summary>Staff xác nhận ĐÃ ĐẶT HÀNG với nhà cung cấp/lab — chưa nhập kho, chỉ đánh dấu đang chờ hàng về.</summary>
public class MarkMaterialRequestOrderedHandler(IMaterialRequestRepository materialRequestRepository)
    : IRequestHandler<MarkMaterialRequestOrderedCommand, MaterialRequestDto>
{
    public async Task<MaterialRequestDto> Handle(MarkMaterialRequestOrderedCommand command, CancellationToken ct)
    {
        var request = await materialRequestRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy yêu cầu vật tư.");

        if (request.Status != MaterialRequestStatus.Pending)
            throw new ValidationException("Chỉ có thể đặt hàng cho yêu cầu đang ở trạng thái chờ xử lý.");

        request.MarkOrdered(command.OrderedBy, string.IsNullOrWhiteSpace(command.SupplierNote) ? null : command.SupplierNote.Trim());
        await materialRequestRepository.UpdateAsync(request, ct);

        return CreateMaterialRequestHandler.ToDto(request);
    }
}

public record MaterialRequestItemPriceInput(Guid MaterialRequestItemId, decimal UnitPrice, int? ActualQuantity = null);

public record MarkMaterialRequestDoneCommand(Guid Id, string HandledBy, List<MaterialRequestItemPriceInput> ItemPrices) : IRequest;

/// <summary>
/// Xác nhận ĐÃ NHẬN HÀNG THẬT và nhập kho — staff nhập giá + số lượng thực nhận (mặc định bằng số lượng
/// đã xin nếu không sửa) cho TỪNG item, sau đó nhập thẳng từng vật tư vào kho (tái dùng StockImportHandler
/// cho mỗi dòng) với OrderType "custom" (đặt riêng cho bệnh nhân — vật tư này gắn với 1 yêu cầu điều trị cụ
/// thể, không phải hàng tồn dùng chung), trong 1 transaction để không nửa vời nếu lỗi giữa chừng.
/// Có thể gọi trực tiếp từ Pending (bỏ qua bước đặt hàng) hoặc từ Ordered.
/// </summary>
public class MarkMaterialRequestDoneHandler(
    IMaterialRequestRepository materialRequestRepository,
    ISender sender) : IRequestHandler<MarkMaterialRequestDoneCommand>
{
    // Chỉ dùng khi vật tư chưa từng có trong kho (xem StockImportHandler) — vật tư qua Yêu cầu vật tư hầu như
    // luôn là hàng đặt riêng theo option dịch vụ (mão sứ, veneer...) nên mặc định đúng danh mục "Vật tư chính"
    // (OrderType "custom" được StockImportHandler tự suy ra từ danh mục này).
    private const string DefaultCategory = InventoryConstants.CategoryMain;

    public async Task Handle(MarkMaterialRequestDoneCommand command, CancellationToken ct)
    {
        var request = await materialRequestRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("Không tìm thấy yêu cầu vật tư.");

        if (request.Status == MaterialRequestStatus.Done)
            throw new ValidationException("Yêu cầu này đã được nhập kho rồi.");

        var priceByItemId = command.ItemPrices.ToDictionary(p => p.MaterialRequestItemId, p => p.UnitPrice);
        var actualQtyByItemId = command.ItemPrices.ToDictionary(p => p.MaterialRequestItemId, p => p.ActualQuantity);
        foreach (var item in request.Items)
        {
            if (!priceByItemId.TryGetValue(item.Id, out var price))
                throw new ValidationException($"Thiếu đơn giá cho vật tư \"{item.ItemName}\".");
            if (price < 0)
                throw new ValidationException($"Đơn giá của \"{item.ItemName}\" không được âm.");

            var actualQty = actualQtyByItemId.GetValueOrDefault(item.Id) ?? item.Quantity;
            if (actualQty <= 0)
                throw new ValidationException($"Số lượng thực nhận của \"{item.ItemName}\" phải lớn hơn 0.");
        }

        // BeginTransactionAsync trả về null trên InMemory provider (dùng trong unit test, không hỗ trợ
        // transaction) — chỉ bọc transaction thật khi chạy trên Postgres, tránh nửa vời nếu 1 item lỗi giữa
        // vòng lặp.
        var transaction = await materialRequestRepository.BeginTransactionAsync(ct);
        try
        {
            foreach (var item in request.Items)
            {
                var actualQty = actualQtyByItemId.GetValueOrDefault(item.Id) ?? item.Quantity;

                var note = string.IsNullOrWhiteSpace(request.DentistName)
                    ? $"Theo yêu cầu vật tư: {request.CourseName}"
                    : $"Theo yêu cầu vật tư: {request.CourseName} · BS {request.DentistName}";
                var tx = await sender.Send(
                    new StockImportCommand(
                        item.ItemName,
                        item.Unit,
                        DefaultCategory,
                        actualQty,
                        note,
                        priceByItemId[item.Id],
                        command.HandledBy),
                    ct);
                item.ConfirmReceived(actualQty);
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
