using TuiSample.Models;

namespace TuiSample.Services;

public sealed class UserRepository : IUserRepository
{
    private readonly Lock _syncRoot = new();
    private readonly List<User> _users = [];
    private int _nextId = 1;

    public UserRepository()
    {
        SeedSampleUsers();
    }

    private void SeedSampleUsers()
    {
        var samples = new (string Name, string Email, string Department, UserRole Role, UserStatus Status, int DaysAgo)[]
        {
            ("山田 太郎", "taro.yamada@example.com", "開発部", UserRole.Admin, UserStatus.Active, 120),
            ("佐藤 花子", "hanako.sato@example.com", "プロダクト企画", UserRole.Editor, UserStatus.Active, 95),
            ("鈴木 一郎", "ichiro.suzuki@example.com", "インフラ部", UserRole.Admin, UserStatus.Active, 88),
            ("高橋 美咲", "misaki.takahashi@example.com", "デザイン部", UserRole.Editor, UserStatus.Active, 75),
            ("田中 健太", "kenta.tanaka@example.com", "営業部", UserRole.Viewer, UserStatus.Active, 64),
            ("伊藤 直樹", "naoki.ito@example.com", "開発部", UserRole.Editor, UserStatus.Inactive, 60),
            ("渡辺 結衣", "yui.watanabe@example.com", "カスタマーサポート", UserRole.Viewer, UserStatus.Active, 52),
            ("山本 大輔", "daisuke.yamamoto@example.com", "セキュリティ室", UserRole.Admin, UserStatus.Active, 45),
            ("中村 彩", "aya.nakamura@example.com", "マーケティング", UserRole.Editor, UserStatus.Active, 38),
            ("小林 翔太", "shota.kobayashi@example.com", "開発部", UserRole.Editor, UserStatus.Active, 30),
            ("加藤 恵", "megumi.kato@example.com", "人事・総務", UserRole.Viewer, UserStatus.Inactive, 21),
            ("吉田 拓海", "takumi.yoshida@example.com", "データ分析室", UserRole.Editor, UserStatus.Active, 14),
            ("山田 次郎", "jiro.yamada@example.com", "営業部", UserRole.Viewer, UserStatus.Active, 7),
            ("松本 莉子", "riko.matsumoto@example.com", "デザイン部", UserRole.Viewer, UserStatus.Active, 3),
            ("井上 蓮", "ren.inoue@example.com", "開発部", UserRole.Editor, UserStatus.Active, 1),
        };

        var now = DateTime.Today;
        lock (_syncRoot)
        {
            foreach (var s in samples)
            {
                _users.Add(new User
                {
                    Id = _nextId++,
                    Name = s.Name,
                    Email = s.Email,
                    Department = s.Department,
                    Role = s.Role,
                    Status = s.Status,
                    CreatedAt = now.AddDays(-s.DaysAgo)
                });
            }
        }
    }

    public IReadOnlyList<User> GetAll()
    {
        lock (_syncRoot)
        {
            return _users.Select(u => u.Clone()).ToList();
        }
    }

    public IReadOnlyList<User> Search(string? keyword, UserRole? roleFilter = null, UserStatus? statusFilter = null)
    {
        lock (_syncRoot)
        {
            IEnumerable<User> query = _users;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                query = query.Where(u =>
                    u.Id.ToString().Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    u.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    u.Department.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    u.Role.ToString().Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    u.Status.ToString().Contains(kw, StringComparison.OrdinalIgnoreCase));
            }

            if (roleFilter.HasValue)
            {
                query = query.Where(u => u.Role == roleFilter.Value);
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(u => u.Status == statusFilter.Value);
            }

            return query.Select(u => u.Clone()).ToList();
        }
    }

    public User? GetById(int id)
    {
        lock (_syncRoot)
        {
            return _users.FirstOrDefault(u => u.Id == id)?.Clone();
        }
    }

    public User Add(User user)
    {
        lock (_syncRoot)
        {
            var created = user.Clone();
            created.Id = _nextId++;
            if (created.CreatedAt == default)
            {
                created.CreatedAt = DateTime.Today;
            }
            _users.Add(created);
            return created.Clone();
        }
    }

    public bool Update(User updated)
    {
        lock (_syncRoot)
        {
            var existing = _users.FirstOrDefault(u => u.Id == updated.Id);
            if (existing is null)
            {
                return false;
            }

            existing.Name = updated.Name;
            existing.Email = updated.Email;
            existing.Department = updated.Department;
            existing.Role = updated.Role;
            existing.Status = updated.Status;
            return true;
        }
    }

    public bool Delete(int id)
    {
        lock (_syncRoot)
        {
            var existing = _users.FirstOrDefault(u => u.Id == id);
            if (existing is null)
            {
                return false;
            }

            return _users.Remove(existing);
        }
    }
}
