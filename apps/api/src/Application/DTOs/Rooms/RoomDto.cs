using DentalClinic.API.Domain.Enums;

namespace DentalClinic.API.Application.DTOs.Rooms;

public record RoomDto(
    Guid Id,
    string Code,
    string Name,
    string Floor,
    string Status,
    string ActiveStatus,
    string Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record CreateRoomRequest(
    string Code,
    string Name,
    string Floor,
    string Description);

public record UpdateRoomRequest(
    string Code,
    string Name,
    string Floor,
    string Description);

public record ChangeRoomStatusRequest(string Status);

public static class RoomStatusMapper
{
    public static string ToVietnamese(this RoomStatus status) => status switch
    {
        RoomStatus.Trong => "Trống",
        RoomStatus.DangKham => "Đang khám",
        RoomStatus.DangVeSinh => "Đang vệ sinh",
        RoomStatus.BaoTri => "Bảo trì",
        RoomStatus.NgungHoatDong => "Ngừng hoạt động",
        _ => "Không xác định"
    };

    public static string ToActiveStatus(this RoomStatus status)
        => status == RoomStatus.NgungHoatDong ? "Ngừng hoạt động" : "Hoạt động";

    public static RoomStatus FromVietnamese(string status) => status switch
    {
        "Trống" => RoomStatus.Trong,
        "Đang khám" => RoomStatus.DangKham,
        "Đang vệ sinh" => RoomStatus.DangVeSinh,
        "Bảo trì" => RoomStatus.BaoTri,
        "Ngừng hoạt động" => RoomStatus.NgungHoatDong,
        _ => throw new ArgumentException($"Trạng thái không hợp lệ: {status}")
    };
}
