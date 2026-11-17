namespace achiev_hub.Server.Application.Common;

public class PagedResultDto<T>
{
    public IReadOnlyList<T> Data { get; set; } = [];
    public PaginationDto Pagination { get; set; } = PaginationDto.Empty();

    public static PagedResultDto<T> From(IReadOnlyList<T> data, PaginationDto pagination) => new()
    {
        Data = data,
        Pagination = pagination
    };

    public static PagedResultDto<T> Empty(int pageSize = 25) => new()
    {
        Data = [],
        Pagination = PaginationDto.Empty(pageSize)
    };
}
