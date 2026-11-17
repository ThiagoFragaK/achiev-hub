namespace achiev_hub.Server.Application.Common;

public sealed class PaginationDto
{
    public int CurrentPage { get; init; } = 1;
    public int LastPage { get; init; } = 1;
    public int PerPage { get; init; }
    public int TotalCount { get; init; }

    public int Skip => Math.Max(0, (CurrentPage - 1) * PerPage);

    public static PaginationDto Create(int page, int pageSize, int totalCount, int defaultPageSize = 25)
    {
        var size = pageSize <= 0 ? defaultPageSize : pageSize;
        var lastPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)size));
        var currentPage = page <= 0 ? 1 : Math.Min(page, lastPage);

        return new PaginationDto
        {
            CurrentPage = currentPage,
            LastPage = lastPage,
            PerPage = size,
            TotalCount = totalCount
        };
    }

    public static PaginationDto Empty(int pageSize = 25) => new()
    {
        CurrentPage = 1,
        LastPage = 1,
        PerPage = pageSize <= 0 ? 25 : pageSize,
        TotalCount = 0
    };
}
