namespace data.Entities;

public class ApiKey
{
    public string Id { get; set; } = default!;
    public string HashedKey { get; set; } = default!;
    public string TierId { get; set; } = default!;
    public bool Active { get; set; } = true;
}