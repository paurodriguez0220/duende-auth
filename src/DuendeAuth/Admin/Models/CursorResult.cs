using System.ComponentModel;

namespace DuendeAuth.Admin.Models;

/// <summary>A cursor-paginated collection response.</summary>
public record CursorResult<T>(
    [property: Description("Items in this page.")] IReadOnlyList<T> Data,
    [property: Description("Pagination metadata.")] CursorPaginationMeta Pagination);

/// <summary>Cursor pagination metadata.</summary>
public record CursorPaginationMeta(
    [property: Description("Opaque cursor for the next page. Null when there are no more results.")] string? NextCursor,
    [property: Description("Whether more results exist after this page.")] bool HasMore);
