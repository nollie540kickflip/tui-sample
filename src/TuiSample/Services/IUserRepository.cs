using TuiSample.Models;

namespace TuiSample.Services;

public interface IUserRepository
{
    IReadOnlyList<User> GetAll();
    IReadOnlyList<User> Search(string? keyword, UserRole? roleFilter = null, UserStatus? statusFilter = null);
    User? GetById(int id);
    User Add(User user);
    bool Update(User updated);
    bool Delete(int id);
}
