using System.Windows;
using System.Windows.Input;

namespace KoExplorer;

public partial class RenameDialog : Window
{
    public string NewName { get; private set; } = string.Empty;

    public RenameDialog(string currentName)
    {
        InitializeComponent();
        NameTextBox.Text = currentName;
        // 拡張子の前までを選択
        var dotIndex = currentName.LastIndexOf('.');
        NameTextBox.SelectionStart = 0;
        NameTextBox.SelectionLength = dotIndex > 0 ? dotIndex : currentName.Length;
        NameTextBox.Focus();
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("名前を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        NewName = name;
        DialogResult = true;
    }

    private void NameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) OK_Click(sender, e);
    }
}
