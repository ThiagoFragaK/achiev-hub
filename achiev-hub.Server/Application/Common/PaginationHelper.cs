namespace achiev_hub.Server.Application.Common;

public static class PaginationHelper
{
    public static PagedResultDto<T> Paginate<T>(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int defaultPageSize = 25)
    {
        var pagination = PaginationDto.Create(page, pageSize, items.Count, defaultPageSize);
        var slice = items
            .Skip(pagination.Skip)
            .Take(pagination.PerPage)
            .ToList();

        return PagedResultDto<T>.From(slice, pagination);
    }
}
