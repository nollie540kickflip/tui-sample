using CommunityToolkit.Mvvm.ComponentModel;
using TuiSample.Models;

namespace TuiSample.ViewModels;

public sealed partial class UserFormViewModel : ObservableObject
{
    public static IReadOnlyList<UserRole> AvailableRoles { get; } = Enum.GetValues<UserRole>();
    public static IReadOnlyList<UserStatus> AvailableStatuses { get; } = Enum.GetValues<UserStatus>();

    private readonly User _baseUser;

    public bool IsEditMode { get; }
    public string DialogTitle => IsEditMode ? $"ユーザ編集 (ID: {_baseUser.Id})" : "新規ユーザ追加";

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _email;

    [ObservableProperty]
    private string _department;

    [ObservableProperty]
    private int _selectedRoleIndex;

    [ObservableProperty]
    private int _selectedStatusIndex;

    public UserFormViewModel(User? existingUser = null)
    {
        IsEditMode = existingUser is not null;
        _baseUser = existingUser?.Clone() ?? new User
        {
            Role = UserRole.Viewer,
            Status = UserStatus.Active,
            CreatedAt = DateTime.Today
        };

        _name = _baseUser.Name;
        _email = _baseUser.Email;
        _department = _baseUser.Department;

        var roleIdx = AvailableRoles
            .Select((role, index) => (role, index, found: role == _baseUser.Role))
            .FirstOrDefault(x => x.found);
        _selectedRoleIndex = roleIdx.found ? roleIdx.index : 2;

        var statusIdx = AvailableStatuses
            .Select((status, index) => (status, index, found: status == _baseUser.Status))
            .FirstOrDefault(x => x.found);
        _selectedStatusIndex = statusIdx.found ? statusIdx.index : 0;
    }

    public bool TryBuildUser(out User user, out string errorMessage)
    {
        var trimmedName = Name?.Trim() ?? string.Empty;
        var trimmedEmail = Email?.Trim() ?? string.Empty;
        var trimmedDept = Department?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            user = _baseUser.Clone();
            errorMessage = "「氏名」は必須項目です。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(trimmedEmail) || !trimmedEmail.Contains('@'))
        {
            user = _baseUser.Clone();
            errorMessage = "有効な「メールアドレス」を入力してください。";
            return false;
        }

        var result = _baseUser.Clone();
        result.Name = trimmedName;
        result.Email = trimmedEmail;
        result.Department = string.IsNullOrWhiteSpace(trimmedDept) ? "未所属" : trimmedDept;
        result.Role = AvailableRoles[Math.Clamp(SelectedRoleIndex, 0, AvailableRoles.Count - 1)];
        result.Status = AvailableStatuses[Math.Clamp(SelectedStatusIndex, 0, AvailableStatuses.Count - 1)];

        user = result;
        errorMessage = string.Empty;
        return true;
    }
}
