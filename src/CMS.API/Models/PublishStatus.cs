namespace CMS.API.Models;

/// <summary>
/// PublishStatus 發布狀態 — response model.
/// PK is <see cref="Pkid"/> (tinyint), a <b>user-assigned</b> display code — not an IDENTITY column.
/// </summary>
public class PublishStatus
{
    public byte Pkid { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsDraft { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDiscontinued { get; set; }
}
