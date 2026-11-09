namespace RecordLabel.Domain.Releases;

public class Release
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public ReleaseFormat Format { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
