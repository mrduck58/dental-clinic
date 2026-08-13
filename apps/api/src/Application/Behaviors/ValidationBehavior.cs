using FluentValidation;
using MediatR;
using ValidationException = DentalClinic.API.Domain.Exceptions.ValidationException;

namespace DentalClinic.API.Application.Behaviors;

/// <summary>
/// Chạy mọi <see cref="IValidator{T}"/> đăng ký cho <typeparamref name="TRequest"/> TRƯỚC khi vào
/// handler — chuẩn hóa validate FluentValidation cho MediatR command/query, thay cho rải rác
/// <c>throw new ValidationException(...)</c> thủ công trong từng handler. Request không có validator
/// nào đăng ký (đa số hiện tại) đi qua bình thường, không đổi hành vi.
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).ToList();

        if (failures.Count == 0)
            return await next();

        var errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());
        throw new ValidationException(errors);
    }
}
