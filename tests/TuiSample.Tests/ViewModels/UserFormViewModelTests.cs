using TuiSample.Models;
using TuiSample.ViewModels;
using Xunit;

namespace TuiSample.Tests.ViewModels;

public sealed class UserFormViewModelTests
{
    [Fact]
    public void TryBuildUser_ShouldFail_WhenNameIsEmpty()
    {
        var vm = new UserFormViewModel
        {
            Name = "   ",
            Email = "valid@example.com"
        };

        var isValid = vm.TryBuildUser(out _, out var error);

        Assert.False(isValid);
        Assert.Contains("氏名", error);
    }

    [Fact]
    public void TryBuildUser_ShouldFail_WhenEmailIsInvalid()
    {
        var vm = new UserFormViewModel
        {
            Name = "山田 太郎",
            Email = "invalid-email-without-at"
        };

        var isValid = vm.TryBuildUser(out _, out var error);

        Assert.False(isValid);
        Assert.Contains("メールアドレス", error);
    }

    [Fact]
    public void TryBuildUser_ShouldSucceed_AndDefaultDepartmentWhenEmpty()
    {
        var vm = new UserFormViewModel
        {
            Name = "  佐々木 健  ",
            Email = "  ken.sasaki@example.com ",
            Department = "",
            SelectedRoleIndex = 0,   // Admin
            SelectedStatusIndex = 1  // Inactive
        };

        var isValid = vm.TryBuildUser(out var user, out var error);

        Assert.True(isValid);
        Assert.Empty(error);
        Assert.Equal("佐々木 健", user.Name);
        Assert.Equal("ken.sasaki@example.com", user.Email);
        Assert.Equal("未所属", user.Department);
        Assert.Equal(UserRole.Admin, user.Role);
        Assert.Equal(UserStatus.Inactive, user.Status);
    }

    [Fact]
    public void Constructor_WithExistingUser_ShouldPopulateFieldsInEditMode()
    {
        var existing = new User
        {
            Id = 42,
            Name = "既存 ユーザ",
            Email = "existing@example.com",
            Department = "開発部",
            Role = UserRole.Editor,
            Status = UserStatus.Inactive
        };

        var vm = new UserFormViewModel(existing);

        Assert.True(vm.IsEditMode);
        Assert.Contains("42", vm.DialogTitle);
        Assert.Equal("既存 ユーザ", vm.Name);
        Assert.Equal(1, vm.SelectedRoleIndex);   // Editor
        Assert.Equal(1, vm.SelectedStatusIndex); // Inactive
    }
}
