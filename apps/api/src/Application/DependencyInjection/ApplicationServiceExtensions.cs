using DentalClinic.API.Application.Behaviors;
using DentalClinic.API.Application.Validators.Auth;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DentalClinic.API.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Auto-discovers IRequestHandler<> implementations bằng cách quét CHÍNH assembly Application
        // (nơi toàn bộ handler thực sự nằm) — trước đây quét nhầm assembly Infrastructure lúc còn
        // chung 1 project nên không lộ ra; giờ tách project, quét sai assembly sẽ khiến ISender.Send
        // không tìm được handler nào ở runtime.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceExtensions).Assembly);
            // Chạy validator FluentValidation (nếu command/query có đăng ký) TRƯỚC handler — request
            // không có validator nào đi qua bình thường, không đổi hành vi các handler chưa có validator.
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssemblyContaining<LoginValidator>();

        return services;
    }
}
