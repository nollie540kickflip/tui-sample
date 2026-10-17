using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TuiSample.Models;
using TuiSample.Services;

namespace TuiSample.ViewModels;

public sealed record CategoryCount(string Name, int TotalCount, int ActiveCount);

public sealed partial class MainWindowViewModel : ObservableObject
{
    public static IReadOnlyList<string> StatusFilterOptions { get; } =
    [
        "All",
        nameof(UserStatus.Active),
        nameof(UserStatus.Inactive)
    ];

    private static readonly string[] SampleLastNames = ["佐藤", "鈴木", "高橋", "田中", "伊藤", "渡辺", "山本", "中村", "小林", "加藤", "吉田", "山田", "佐々木", "山口", "松本"];
    private static readonly string[] SampleFirstNames = ["翔太", "美咲", "健太", "結衣", "大輔", "彩", "拓海", "莉子", "蓮", "陽菜", "颯太", "葵", "悠真", "凛", "直樹"];
    private static readonly string[] SampleDepartments = ["開発部", "プロダクト企画", "インフラ部", "デザイン部", "営業部", "カスタマーサポート", "セキュリティ室", "マーケティング", "データ分析室", "人事・総務"];
    private static readonly UserRole[] SampleRoles = Enum.GetValues<UserRole>();

    private readonly IUserRepository _repository;
    private readonly ICsvExporter _csvExporter;
    private readonly IActivityLogger _logger;
    private int _generatedCounter = 1;
    private bool _suppressRefresh;

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private int _selectedStatusFilterIndex;

    [ObservableProperty]
    private string? _departmentFilter;

    [ObservableProperty]
    private UserRole? _roleFilter;

    [ObservableProperty]
    private UserSortColumn _sortColumn = UserSortColumn.Id;

    [ObservableProperty]
    private bool _sortAscending = true;

    [ObservableProperty]
    private IReadOnlyList<User> _displayedUsers = [];

    [ObservableProperty]
    private string _summaryText = string.Empty;

    [ObservableProperty]
    private string _statusCountText = "表示: 0件";

    public int TotalUserCount => _repository.GetAll().Count;
    public IActivityLogger Logger => _logger;

    public MainWindowViewModel(IUserRepository repository, ICsvExporter csvExporter, IActivityLogger? logger = null)
    {
        _repository = repository;
        _csvExporter = csvExporter;
        _logger = logger ?? NullActivityLogger.Instance;
        Refresh();
    }

    partial void OnSearchKeywordChanged(string value)
    {
        if (!_suppressRefresh) Refresh();
    }

    partial void OnSelectedStatusFilterIndexChanged(int value)
    {
        if (!_suppressRefresh) Refresh();
    }

    partial void OnDepartmentFilterChanged(string? value)
    {
        if (!_suppressRefresh) Refresh();
    }

    partial void OnRoleFilterChanged(UserRole? value)
    {
        if (!_suppressRefresh) Refresh();
    }

    /// <summary>
    /// ツリービューで選択されたフィルタ条件（全体・部署別・ロール別）を一括適用します。
    /// </summary>
    public void ApplyTreeFilter(string? department, UserRole? role)
    {
        _suppressRefresh = true;
        try
        {
            DepartmentFilter = department;
            RoleFilter = role;
        }
        finally
        {
            _suppressRefresh = false;
        }
        Refresh();
    }

    [RelayCommand]
    public void ClearFilter()
    {
        _suppressRefresh = true;
        try
        {
            SearchKeyword = string.Empty;
            SelectedStatusFilterIndex = 0;
            DepartmentFilter = null;
            RoleFilter = null;
        }
        finally
        {
            _suppressRefresh = false;
        }

        _logger.Log("FILTER", "検索条件とツリー絞り込みをすべてクリアしました。");
        Refresh();
    }

    [RelayCommand]
    public void CycleSort()
    {
        (SortColumn, SortAscending) = (SortColumn, SortAscending) switch
        {
            (UserSortColumn.Id, true) => (UserSortColumn.Id, false),
            (UserSortColumn.Id, false) => (UserSortColumn.Name, true),
            (UserSortColumn.Name, true) => (UserSortColumn.Department, true),
            (UserSortColumn.Department, true) => (UserSortColumn.CreatedAt, false),
            _ => (UserSortColumn.Id, true)
        };
        var dir = SortAscending ? "昇順(▲)" : "降順(▼)";
        _logger.Log("SORT", $"ソート順を [{SortColumn} {dir}] に切り替えました。");
        Refresh();
    }

    public User? GetUserAtRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= DisplayedUsers.Count)
        {
            return null;
        }
        return DisplayedUsers[rowIndex];
    }

    public int FindRowIndexByUserId(int userId)
    {
        for (var i = 0; i < DisplayedUsers.Count; i++)
        {
            if (DisplayedUsers[i].Id == userId)
            {
                return i;
            }
        }
        return -1;
    }

    public User AddUser(User user)
    {
        var created = _repository.Add(user);
        _logger.Log("CREATE", $"ユーザを追加しました: ID={created.Id} 「{created.Name}」({created.Department} / {created.Role})");
        Refresh();
        return created;
    }

    public bool UpdateUser(User user)
    {
        var updated = _repository.Update(user);
        if (updated)
        {
            _logger.Log("UPDATE", $"ユーザを更新しました: ID={user.Id} 「{user.Name}」({user.Department} / {user.Role} / {user.Status})");
            Refresh();
        }
        return updated;
    }

    public bool DeleteUser(int userId)
    {
        var target = _repository.GetById(userId);
        var deleted = _repository.Delete(userId);
        if (deleted)
        {
            _logger.Log("DELETE", $"ユーザを削除しました: ID={userId} 「{target?.Name ?? "不明"}」");
            Refresh();
        }
        return deleted;
    }

    public User? ToggleUserStatus(int userId)
    {
        var existing = _repository.GetById(userId);
        if (existing is null)
        {
            return null;
        }

        var updated = existing.Clone();
        updated.Status = existing.Status == UserStatus.Active
            ? UserStatus.Inactive
            : UserStatus.Active;

        _repository.Update(updated);
        _logger.Log("STATUS", $"ID={userId} 「{updated.Name}」のステータスを {existing.Status} -> {updated.Status} に切り替えました。");
        Refresh();
        return updated;
    }

    public User? DuplicateUser(int userId)
    {
        var existing = _repository.GetById(userId);
        if (existing is null)
        {
            return null;
        }

        var copy = existing.Clone();
        copy.Name = $"{existing.Name} (コピー)";
        copy.CreatedAt = DateTime.Today;

        var created = _repository.Add(copy);
        _logger.Log("CLONE", $"ID={userId} を複製して ID={created.Id} 「{created.Name}」を作成しました。");
        Refresh();
        return created;
    }

    public User GenerateAndAddDummyUser(int seedOffset = 0)
    {
        var idx = Interlocked.Increment(ref _generatedCounter) + seedOffset;
        var lastName = SampleLastNames[idx % SampleLastNames.Length];
        var firstName = SampleFirstNames[(idx * 3) % SampleFirstNames.Length];
        var dept = SampleDepartments[(idx * 7) % SampleDepartments.Length];
        var role = SampleRoles[idx % SampleRoles.Length];
        var status = (idx % 5 == 0) ? UserStatus.Inactive : UserStatus.Active;

        var dummy = new User
        {
            Name = $"{lastName} {firstName}",
            Email = $"user{idx}.{DateTime.Now:fff}@example.com",
            Department = dept,
            Role = role,
            Status = status,
            CreatedAt = DateTime.Today.AddDays(-(idx % 90))
        };

        return _repository.Add(dummy);
    }

    public void NotifyBulkGenerationCompleted(int addedCount, bool wasCancelled)
    {
        var msg = wasCancelled
            ? $"ダミーユーザ生成を中断しました（途中まで {addedCount} 件追加）。"
            : $"非同期バッチでダミーユーザを {addedCount} 件一括生成しました。";
        _logger.Log("BATCH", msg);
        Refresh();
    }

    public int ExportCsv(string filePath, bool displayedOnly, out string normalizedPath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("出力先ファイルパスを入力してください。", nameof(filePath));
        }

        normalizedPath = filePath.Trim();
        if (!normalizedPath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath += ".csv";
        }

        var targetUsers = displayedOnly ? DisplayedUsers : _repository.GetAll();
        _csvExporter.ExportToFile(targetUsers, normalizedPath);
        _logger.Log("EXPORT", $"CSVを出力しました ({targetUsers.Count}件): {normalizedPath}");
        return targetUsers.Count;
    }

    public int ImportCsv(string filePath)
    {
        var importedUsers = _csvExporter.ImportFromFile(filePath);
        var count = 0;
        foreach (var u in importedUsers)
        {
            _repository.Add(u);
            count++;
        }

        _logger.Log("IMPORT", $"CSVから {count} 件のユーザをインポートしました: {filePath}");
        Refresh();
        return count;
    }

    public IReadOnlyList<CategoryCount> GetDepartmentStats()
    {
        return _repository.GetAll()
            .GroupBy(u => string.IsNullOrWhiteSpace(u.Department) ? "未所属" : u.Department)
            .Select(g => new CategoryCount(
                g.Key,
                g.Count(),
                g.Count(u => u.Status == UserStatus.Active)))
            .OrderByDescending(c => c.TotalCount)
            .ThenBy(c => c.Name, StringComparer.CurrentCulture)
            .ToList();
    }

    public IReadOnlyList<CategoryCount> GetRoleStats()
    {
        return _repository.GetAll()
            .GroupBy(u => u.Role)
            .Select(g => new CategoryCount(
                g.Key.ToString(),
                g.Count(),
                g.Count(u => u.Status == UserStatus.Active)))
            .OrderByDescending(c => c.TotalCount)
            .ToList();
    }

    public void Refresh()
    {
        UserStatus? statusFilter = SelectedStatusFilterIndex switch
        {
            1 => UserStatus.Active,
            2 => UserStatus.Inactive,
            _ => null
        };

        IEnumerable<User> filtered = _repository.Search(SearchKeyword, roleFilter: RoleFilter, statusFilter: statusFilter);

        if (!string.IsNullOrWhiteSpace(DepartmentFilter) && DepartmentFilter != "All")
        {
            filtered = filtered.Where(u => string.Equals(u.Department, DepartmentFilter, StringComparison.OrdinalIgnoreCase));
        }

        DisplayedUsers = ApplySorting(filtered).ToList();

        var totalAll = _repository.GetAll().Count;
        var activeCount = DisplayedUsers.Count(u => u.Status == UserStatus.Active);
        var sortArrow = SortAscending ? "▲" : "▼";

        var treeBadge = !string.IsNullOrWhiteSpace(DepartmentFilter)
            ? $" [部署:{DepartmentFilter}]"
            : RoleFilter.HasValue
                ? $" [ロール:{RoleFilter.Value}]"
                : "";

        SummaryText = $"表示: {DisplayedUsers.Count} / 全 {totalAll} 件 (Active: {activeCount}件){treeBadge}  |  ソート: {SortColumn} {sortArrow}";
        StatusCountText = $"表示: {DisplayedUsers.Count}/{totalAll}件";
    }

    private IEnumerable<User> ApplySorting(IEnumerable<User> source)
    {
        return (SortColumn, SortAscending) switch
        {
            (UserSortColumn.Name, true) => source.OrderBy(u => u.Name, StringComparer.CurrentCulture),
            (UserSortColumn.Name, false) => source.OrderByDescending(u => u.Name, StringComparer.CurrentCulture),
            (UserSortColumn.Department, true) => source.OrderBy(u => u.Department, StringComparer.CurrentCulture).ThenBy(u => u.Id),
            (UserSortColumn.Department, false) => source.OrderByDescending(u => u.Department, StringComparer.CurrentCulture).ThenBy(u => u.Id),
            (UserSortColumn.CreatedAt, true) => source.OrderBy(u => u.CreatedAt).ThenBy(u => u.Id),
            (UserSortColumn.CreatedAt, false) => source.OrderByDescending(u => u.CreatedAt).ThenBy(u => u.Id),
            (_, false) => source.OrderByDescending(u => u.Id),
            _ => source.OrderBy(u => u.Id)
        };
    }
}
