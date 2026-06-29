using DentalClinic.API.Application.DTOs.Services;
using DentalClinic.API.Application.UseCases.Services;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using DentalClinic.API.Domain.Interfaces.Services;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Services;

[TestFixture]
public class CreateServiceHandlerTests
{
    private IServiceRepository _repo = null!;
    private IActivityLogService _activityLog = null!;
    private ICurrentUserService _currentUser = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IServiceRepository>();
        _activityLog = Substitute.For<IActivityLogService>();
        _currentUser = Substitute.For<ICurrentUserService>();
    }

    /// <summary>
    /// Tạo dịch vụ hợp lệ phải gọi AddAsync 1 lần và trả về ServiceDto.
    /// </summary>
    [Test]
    public async Task HandleAsync_ValidRequest_CallsAddAsyncAndReturnsDto()
    {
        var handler = new CreateServiceHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(new CreateServiceRequest("Nhổ răng", 200000m, 30, "Mô tả", null));

        await _repo.Received(1).AddAsync(Arg.Any<Service>(), Arg.Any<CancellationToken>());
        result.Name.Should().Be("Nhổ răng");
        result.Price.Should().Be(200000m);
    }

    /// <summary>
    /// Dịch vụ mới tạo mặc định phải có IsActive = true.
    /// </summary>
    [Test]
    public async Task HandleAsync_NewService_IsActiveByDefault()
    {
        var handler = new CreateServiceHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(new CreateServiceRequest("Trồng răng sứ", 5000000m, 120, "Mô tả", null));

        result.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Tạo dịch vụ với imageUrl phải lưu URL vào DTO.
    /// </summary>
    [Test]
    public async Task HandleAsync_WithImageUrl_ReturnsImageUrl()
    {
        var handler = new CreateServiceHandler(_repo, _activityLog, _currentUser);

        var result = await handler.HandleAsync(new CreateServiceRequest("Tẩy trắng", 800000m, 60, "Mô tả", "https://img.com/teeth.jpg"));

        result.ImageUrl.Should().Be("https://img.com/teeth.jpg");
    }
}
