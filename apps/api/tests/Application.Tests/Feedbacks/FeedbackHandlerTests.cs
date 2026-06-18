using DentalClinic.API.Application.DTOs.Feedbacks;
using DentalClinic.API.Application.UseCases.Feedbacks;
using DentalClinic.API.Domain.Entities;
using DentalClinic.API.Domain.Enums;
using DentalClinic.API.Domain.Exceptions;
using DentalClinic.API.Domain.Interfaces.Repositories;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace DentalClinic.API.Application.Tests.Feedbacks;

[TestFixture]
public class FeedbackHandlerTests
{
    private IFeedbackRepository _repo = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IFeedbackRepository>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CreateFeedbackHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tạo feedback hợp lệ (rating 1-5) phải gọi AddAsync 1 lần và trả về DTO.
    /// </summary>
    [Test]
    public async Task Create_ValidRating_CallsAddAsyncAndReturnsDto()
    {
        var handler = new CreateFeedbackHandler(_repo);

        var result = await handler.HandleAsync(new CreateFeedbackRequest("Nguyễn Văn A", 5, "Rất tốt!"));

        await _repo.Received(1).AddAsync(Arg.Any<Feedback>(), Arg.Any<CancellationToken>());
        result.Should().NotBeNull();
        result.CustomerName.Should().Be("Nguyễn Văn A");
        result.Rating.Should().Be(5);
    }

    /// <summary>
    /// Feedback mới tạo phải có trạng thái Pending mặc định,
    /// chưa được duyệt hay ẩn đi.
    /// </summary>
    [Test]
    public async Task Create_NewFeedback_StatusIsPending()
    {
        var handler = new CreateFeedbackHandler(_repo);

        var result = await handler.HandleAsync(new CreateFeedbackRequest("Khách Hàng", 4, "Ổn"));

        result.Status.Should().Be("Pending");
    }

    /// <summary>
    /// Rating dưới 1 phải ném ValidationException trước khi gọi AddAsync.
    /// </summary>
    [Test]
    public async Task Create_RatingBelowOne_ThrowsValidationException()
    {
        var handler = new CreateFeedbackHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(new CreateFeedbackRequest("A", 0, "Quá tệ"));

        await act.Should().ThrowAsync<ValidationException>();
        await _repo.DidNotReceive().AddAsync(Arg.Any<Feedback>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Rating trên 5 phải ném ValidationException trước khi gọi AddAsync.
    /// </summary>
    [Test]
    public async Task Create_RatingAboveFive_ThrowsValidationException()
    {
        var handler = new CreateFeedbackHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(new CreateFeedbackRequest("A", 6, "Xuất sắc"));

        await act.Should().ThrowAsync<ValidationException>();
        await _repo.DidNotReceive().AddAsync(Arg.Any<Feedback>(), Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ApproveFeedbackHandler (toggle Featured ↔ Pending)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Duyệt feedback ở trạng thái Pending phải chuyển sang Featured.
    /// </summary>
    [Test]
    public async Task Approve_PendingFeedback_StatusBecomesFeatured()
    {
        var feedback = MakeFeedback();
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new ApproveFeedbackHandler(_repo);

        var result = await handler.HandleAsync(feedback.Id);

        result.Status.Should().Be("Featured");
    }

    /// <summary>
    /// Duyệt feedback đã ở trạng thái Featured phải toggle về Pending (bỏ nổi bật).
    /// </summary>
    [Test]
    public async Task Approve_FeaturedFeedback_StatusBecomesPending()
    {
        var feedback = MakeFeedback();
        feedback.Feature();
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new ApproveFeedbackHandler(_repo);

        var result = await handler.HandleAsync(feedback.Id);

        result.Status.Should().Be("Pending");
    }

    /// <summary>
    /// Duyệt feedback không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task Approve_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Feedback?)null);
        var handler = new ApproveFeedbackHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    /// <summary>
    /// Sau khi toggle, phải gọi UpdateAsync đúng 1 lần để lưu thay đổi.
    /// </summary>
    [Test]
    public async Task Approve_ValidFeedback_CallsUpdateAsyncOnce()
    {
        var feedback = MakeFeedback();
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new ApproveFeedbackHandler(_repo);

        await handler.HandleAsync(feedback.Id);

        await _repo.Received(1).UpdateAsync(feedback, Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HideFeedbackHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ẩn feedback hợp lệ phải chuyển trạng thái sang Hidden và gọi UpdateAsync.
    /// </summary>
    [Test]
    public async Task Hide_ValidFeedback_StatusBecomesHidden()
    {
        var feedback = MakeFeedback();
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new HideFeedbackHandler(_repo);

        var result = await handler.HandleAsync(feedback.Id);

        result.Status.Should().Be("Hidden");
        await _repo.Received(1).UpdateAsync(feedback, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Ẩn feedback không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task Hide_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Feedback?)null);
        var handler = new HideFeedbackHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ReplyFeedbackHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Trả lời feedback hợp lệ phải lưu ReplyText và đặt trạng thái Featured.
    /// </summary>
    [Test]
    public async Task Reply_ValidText_SetsReplyAndStatusFeatured()
    {
        var feedback = MakeFeedback();
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new ReplyFeedbackHandler(_repo);

        var result = await handler.HandleAsync(feedback.Id, new ReplyFeedbackRequest("Cảm ơn bạn!"));

        result.ReplyText.Should().Be("Cảm ơn bạn!");
        result.Status.Should().Be("Featured");
    }

    /// <summary>
    /// Trả lời feedback đang bị ẩn (Hidden) phải giữ trạng thái Hidden,
    /// không tự động chuyển sang Featured.
    /// </summary>
    [Test]
    public async Task Reply_HiddenFeedback_StatusRemainsHidden()
    {
        var feedback = MakeFeedback();
        feedback.Hide();
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new ReplyFeedbackHandler(_repo);

        var result = await handler.HandleAsync(feedback.Id, new ReplyFeedbackRequest("Xin lỗi về sự bất tiện"));

        result.Status.Should().Be("Hidden");
        result.ReplyText.Should().Be("Xin lỗi về sự bất tiện");
    }

    /// <summary>
    /// Nội dung trả lời trống phải ném ValidationException trước khi tìm feedback.
    /// </summary>
    [Test]
    public async Task Reply_EmptyText_ThrowsValidationException()
    {
        var handler = new ReplyFeedbackHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), new ReplyFeedbackRequest("   "));

        await act.Should().ThrowAsync<ValidationException>();
        await _repo.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Trả lời feedback không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task Reply_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Feedback?)null);
        var handler = new ReplyFeedbackHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid(), new ReplyFeedbackRequest("Reply"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetFeedbacksHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Không có filter trả về toàn bộ danh sách feedback.
    /// </summary>
    [Test]
    public async Task GetFeedbacks_NoFilters_ReturnsAll()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Feedback>
        {
            MakeFeedback("Khách A"), MakeFeedback("Khách B"), MakeFeedback("Khách C"),
        });
        var handler = new GetFeedbacksHandler(_repo);

        var result = await handler.HandleAsync(null, null);

        result.Should().HaveCount(3);
    }

    /// <summary>
    /// Filter theo status "Pending" chỉ trả về feedback ở trạng thái Pending.
    /// </summary>
    [Test]
    public async Task GetFeedbacks_FilterByStatus_ReturnsOnlyMatchingStatus()
    {
        var pending = MakeFeedback("A");
        var featured = MakeFeedback("B");
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
    public async Task GetFeedbacks_SearchByCustomerName_ReturnsMatching()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Feedback>
        {
            MakeFeedback("Nguyễn Văn An"),
            MakeFeedback("Trần Thị Bình"),
            MakeFeedback("Lê Văn An"),
        });
        var handler = new GetFeedbacksHandler(_repo);

        var result = await handler.HandleAsync(null, search: "văn an");

        result.Should().HaveCount(2);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // GetFeedbackByIdHandler
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy feedback theo ID hợp lệ phải trả về DTO với đầy đủ thông tin.
    /// </summary>
    [Test]
    public async Task GetById_ExistingFeedback_ReturnsDto()
    {
        var feedback = MakeFeedback("Nguyễn Test");
        _repo.GetByIdAsync(feedback.Id, Arg.Any<CancellationToken>()).Returns(feedback);
        var handler = new GetFeedbackByIdHandler(_repo);

        var result = await handler.HandleAsync(feedback.Id);

        result.Id.Should().Be(feedback.Id);
        result.CustomerName.Should().Be("Nguyễn Test");
    }

    /// <summary>
    /// ID không tồn tại phải ném NotFoundException.
    /// </summary>
    [Test]
    public async Task GetById_NotFound_ThrowsNotFoundException()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Feedback?)null);
        var handler = new GetFeedbackByIdHandler(_repo);

        Func<Task> act = () => handler.HandleAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private static Feedback MakeFeedback(string name = "Khách Hàng")
        => Feedback.Create(name, 5, "Dịch vụ tốt");
}
