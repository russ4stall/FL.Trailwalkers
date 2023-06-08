namespace Trails.Domain;

public class HikeLog
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Trail { get; set; }
    public decimal Length { get; set; }
}