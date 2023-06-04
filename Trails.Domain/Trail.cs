namespace Trails.Domain;

public class Trail
{
    public long Id { get; set; }
    public string? StateForestName { get; set; }
    public string? Name { get; set; }
    public string? MapUrl { get; set; }
    public string? Length { get; set; }
    public string? Type { get; set; }
    public string? TrailheadLocation { get; set; }
    public string? StateForestInfoUrl { get; set; }
}