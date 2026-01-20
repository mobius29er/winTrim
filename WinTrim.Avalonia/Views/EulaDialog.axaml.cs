using Avalonia.Controls;
using Avalonia.Interactivity;

namespace WinTrim.Avalonia.Views;

/// <summary>
/// EULA/Terms of Service dialog shown on first launch
/// </summary>
public partial class EulaDialog : Window
{
    public bool Accepted { get; private set; }

    public EulaDialog()
    {
        InitializeComponent();
        
        // Enable Accept button only when checkbox is checked
        AcceptCheckBox.IsCheckedChanged += (s, e) =>
        {
            AcceptButton.IsEnabled = AcceptCheckBox.IsChecked == true;
        };
    }

    private void Accept_Click(object? sender, RoutedEventArgs e)
    {
        Accepted = true;
        Close(true);
    }

    private void Decline_Click(object? sender, RoutedEventArgs e)
    {
        Accepted = false;
        Close(false);
    }
}
