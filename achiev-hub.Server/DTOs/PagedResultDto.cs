namespace achiev_hub.Server.DTOs;

public class PagedResultDto<T>
{
    public IReadOnlyList<T> Data { get; set; } = [];
    public int CurrentPage { get; set; }
    public int LastPage { get; set; }
    public int PerPage { get; set; }
    public int TotalCount { get; set; }
}
