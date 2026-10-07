using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace HappyBoxx.BuildingBlocks.Pagination;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public interface IPagedQuery
{
    int Page { get; }

    int PageSize { get; }
}

public static class PaginationExtensions
{
    public const int MaxPageSize = 100;

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        IPagedQuery paging,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, paging.Page, paging.PageSize, totalCount);
    }

    public static void AddPagingRules<T>(this AbstractValidator<T> validator)
        where T : IPagedQuery
    {
        validator.RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        validator.RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
    }
}
