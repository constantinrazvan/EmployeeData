using System.Collections.Generic;

namespace EmployeeData.Helpers;

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public int StartIndex => (PageNumber - 1) * PageSize + 1;
    public int EndIndex => System.Math.Min(PageNumber * PageSize, TotalCount);

    public PagedResult()
    {
        Items = new List<T>();
    }

    public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
