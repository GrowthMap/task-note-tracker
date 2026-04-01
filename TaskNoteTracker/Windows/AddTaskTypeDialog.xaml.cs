using System.Windows;
using System.Windows.Input;

namespace TaskNoteTracker.Windows;

public partial class AddTaskTypeDialog : Window
{
    public string TypeName { get; private set; } = string.Empty;

    public AddTaskTypeDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => NameBox.Focus();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Please enter a name.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        TypeName = name;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) =>
        DialogResult = false;

    private void NameBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape) DialogResult = false;
    }
}
