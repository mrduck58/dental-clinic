using DentalClinic.API.Application.UseCases.Feedbacks;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Feedbacks;

[TestFixture]
public class GetFeedbacksHandlerTests
{
    private IFeedbackRepository _repo = null!;

    [SetUp]
    public void SetUp() => _repo = Substitute.For<IFeedbackRepository>();

    /// <summary>
    /// Không có filter trả về toàn bộ danh sách feedback.
    /// </summary>
    [Test]
    public async Task HandleAsync_NoFilters_ReturnsAll()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Feedback>
        {
            Feedback.Create("A", 5, "Tốt"), Feedback.Create("B", 4, "Khá"), Feedback.Create("C", 3, "Bình thường"),
        });
        var handler = new GetFeedbacksHandler(_repo);

        var result = await handler.HandleAsync(null, null);

        result.Should().HaveCount(3);
    }

    /// <summary>
    /// Filter theo status "Pending" chỉ trả về feedback ở trạng thái Pending.
    /// </summary>
    [Test]
    public async Task HandleAsync_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        var pending = Feedback.Create("A", 5, "Tốt");
        var featured = Feedback.Create("B", 4, "Khá");
        featured.Feature();
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Feedback> { pending, featured });
        var handler = new GetFeedbacksHandler(_repo);

        var result = await handler.HandleAsync(status: "Pending", null);

        result.Should().HaveCount(1);
        result.First().Status.Should().Be("Pending");
    }

    /// <summary>
    /// Tìm kiếm theo tên khách hàng không phân biệt hoa thường.
    /// </summary>
    [Test]
    public async Task HandleAsync_SearchByCustomerName_ReturnsMatching()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Feedback>
        {
            Feedback.Create("Nguyễn Văn An", 5, "Tốt"),
            Feedback.Create("Trần Thị Bình", 4, "Khá"),
            Feedback.Create("Lê Văn An", 3, "Bình thường"),
        });
        var handler = new GetFeedbacksHandler(_repo);

        var result = await handler.HandleAsync(null, search: "văn an");

        result.Should().HaveCount(2);
    }
}
