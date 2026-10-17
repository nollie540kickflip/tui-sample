namespace TuiSample.Models;

public sealed class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Viewer;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public User Clone() => new()
    {
        Id = Id,
        Name = Name,
        Email = Email,
        Department = Department,
        Role = Role,
        Status = Status,
        CreatedAt = CreatedAt
    };
}
