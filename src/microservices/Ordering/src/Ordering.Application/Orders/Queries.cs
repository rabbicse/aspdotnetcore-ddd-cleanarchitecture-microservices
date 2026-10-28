using MediatR;
using Mehedi.Application.SharedKernel.Responses;
using Ordering.Application.Abstractions;

namespace Ordering.Application.Orders;

/// <summary>Gets one order with its lines.</summary>
public sealed record GetOrderQuery(Guid OrderId) : IRequest<Result<OrderDetails>>;

public sealed class GetOrderQueryHandler(IOrderQueries queries) : IRequestHandler<GetOrderQuery, Result<OrderDetails>>
{
    public async Task<Result<OrderDetails>> Handle(GetOrderQuery query, CancellationToken cancellationToken) =>
        await queries.GetOrderAsync(query.OrderId, cancellationToken) is { } order
            ? Result<OrderDetails>.Success(order)
            : Result<OrderDetails>.NotFound();
}

/// <summary>Lists a customer's orders, newest first.</summary>
public sealed record GetCustomerOrdersQuery(Guid CustomerId, int Page = 1, int PageSize = 20) : IRequest<Result<IReadOnlyList<OrderSummary>>>
{
    /// <summary>The largest page a caller may ask for.</summary>
    public const int MaxPageSize = 100;
}

public sealed class GetCustomerOrdersQueryValidator : FluentValidation.AbstractValidator<GetCustomerOrdersQuery>
{
    public GetCustomerOrdersQueryValidator()
    {
        RuleFor(query => query.CustomerId).NotEmpty();
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, GetCustomerOrdersQuery.MaxPageSize);
    }
}

public sealed class GetCustomerOrdersQueryHandler(IOrderQueries queries)
    : IRequestHandler<GetCustomerOrdersQuery, Result<IReadOnlyList<OrderSummary>>>
{
    public async Task<Result<IReadOnlyList<OrderSummary>>> Handle(GetCustomerOrdersQuery query, CancellationToken cancellationToken) =>
        Result<IReadOnlyList<OrderSummary>>.Success(
            await queries.GetCustomerOrdersAsync(query.CustomerId, query.Page, query.PageSize, cancellationToken));
}

/// <summary>Gets a customer as Ordering knows it (replicated from KYC).</summary>
public sealed record GetKnownCustomerQuery(Guid CustomerId) : IRequest<Result<KnownCustomerDetails>>;

public sealed class GetKnownCustomerQueryHandler(IOrderQueries queries) : IRequestHandler<GetKnownCustomerQuery, Result<KnownCustomerDetails>>
{
    public async Task<Result<KnownCustomerDetails>> Handle(GetKnownCustomerQuery query, CancellationToken cancellationToken) =>
        await queries.GetKnownCustomerAsync(query.CustomerId, cancellationToken) is { } customer
            ? Result<KnownCustomerDetails>.Success(customer)
            : Result<KnownCustomerDetails>.NotFound();
}
