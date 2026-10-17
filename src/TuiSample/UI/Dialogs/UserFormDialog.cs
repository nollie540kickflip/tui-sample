using NStack;
using Terminal.Gui;
using TuiSample.Models;
using TuiSample.UI.Layout;
using TuiSample.ViewModels;

namespace TuiSample.UI.Dialogs;

public sealed class UserFormDialog : Dialog
{
    private readonly UserFormViewModel _viewModel;
    private readonly TextField _nameField;
    private readonly TextField _emailField;
    private readonly TextField _departmentField;
    private readonly RadioGroup _roleRadioGroup;
    private readonly RadioGroup _statusRadioGroup;

    public bool IsSaved { get; private set; }
    public User ResultUser { get; private set; } = new();

    public UserFormDialog(UserFormViewModel viewModel)
        : base(viewModel.DialogTitle, 64, 20)
    {
        _viewModel = viewModel;

        _nameField = new TextField(_viewModel.Name);
        _emailField = new TextField(_viewModel.Email);
        _departmentField = new TextField(_viewModel.Department);

        _roleRadioGroup = new RadioGroup(UserFormViewModel.AvailableRoles.Select(r => ustring.Make(r.ToString())).ToArray())
        {
            SelectedItem = _viewModel.SelectedRoleIndex
        };

        _statusRadioGroup = new RadioGroup(UserFormViewModel.AvailableStatuses.Select(s => ustring.Make(s.ToString())).ToArray())
        {
            SelectedItem = _viewModel.SelectedStatusIndex
        };

        // 座標指定なしでフォームを構築
        var formView = new FormLayout(labelColumnWidth: 16, rowGap: 1)
            .AddField("氏名 *:", _nameField)
            .AddField("メールアドレス *:", _emailField)
            .AddField("部署:", _departmentField)
            .AddChoice("ロール:", _roleRadioGroup, UserFormViewModel.AvailableRoles.Count)
            .AddChoice("ステータス:", _statusRadioGroup, UserFormViewModel.AvailableStatuses.Count)
            .Build();

        Add(formView);

        var saveButton = new Button("保存 (Enter)", is_default: true);
        saveButton.Clicked += OnSaveClicked;

        var cancelButton = new Button("キャンセル (Esc)");
        cancelButton.Clicked += () =>
        {
            IsSaved = false;
            Application.RequestStop();
        };

        AddButton(saveButton);
        AddButton(cancelButton);

        _nameField.SetFocus();
    }

    private void OnSaveClicked()
    {
        _viewModel.Name = _nameField.Text?.ToString() ?? string.Empty;
        _viewModel.Email = _emailField.Text?.ToString() ?? string.Empty;
        _viewModel.Department = _departmentField.Text?.ToString() ?? string.Empty;
        _viewModel.SelectedRoleIndex = _roleRadioGroup.SelectedItem;
        _viewModel.SelectedStatusIndex = _statusRadioGroup.SelectedItem;

        if (!_viewModel.TryBuildUser(out var builtUser, out var errorMessage))
        {
            MessageBox.ErrorQuery("入力エラー", errorMessage, "OK");
            if (string.IsNullOrWhiteSpace(_viewModel.Name))
            {
                _nameField.SetFocus();
            }
            else
            {
                _emailField.SetFocus();
            }
            return;
        }

        ResultUser = builtUser;
        IsSaved = true;
        Application.RequestStop();
    }
}
