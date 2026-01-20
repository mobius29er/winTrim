using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using WinTrim.Core.Models;

namespace WinTrim.Avalonia.Views;

/// <summary>
/// Quick Clean dialog with hierarchical selectable cleanup items
/// </summary>
public partial class QuickCleanDialog : Window
{
    public ObservableCollection<SelectableCleanupCategory> Items { get; } = new();
    public bool Confirmed { get; private set; }

    public QuickCleanDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    public QuickCleanDialog(IEnumerable<CleanupSuggestion> suggestions) : this()
    {
        foreach (var suggestion in suggestions)
        {
            var category = new SelectableCleanupCategory(suggestion);
            category.PropertyChanged += Category_PropertyChanged;
            Items.Add(category);
        }

        UpdateSummary();
    }

    private void Category_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableCleanupCategory.IsSelected) ||
            e.PropertyName == nameof(SelectableCleanupCategory.SelectedSize))
        {
            UpdateSummary();
        }
    }

    private void UpdateSummary()
    {
        var selectedCategories = Items.Where(i => i.IsSelected || i.HasPartialSelection).ToList();
        var totalFiles = Items.Sum(i => i.SelectedFileCount);
        var totalSize = Items.Sum(i => i.SelectedSize);
        
        SelectedCountText.Text = $"{selectedCategories.Count} categories";
        SelectedFilesText.Text = $"{totalFiles} files";
        SelectedSizeText.Text = FormatBytes(totalSize);
        
        CleanButton.IsEnabled = totalFiles > 0;
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:N2} {suffixes[suffixIndex]}";
    }

    private void SelectAll_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var category in Items)
            category.SelectAll();
        UpdateSummary();
    }

    private void SelectNone_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var category in Items)
            category.SelectNone();
        UpdateSummary();
    }

    private void ExpandAll_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var category in Items)
            category.IsExpanded = true;
    }

    private void CollapseAll_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var category in Items)
            category.IsExpanded = false;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Confirmed = false;
        Close(false);
    }

    private async void Clean_Click(object? sender, RoutedEventArgs e)
    {
        // Get ONLY the selected files BEFORE we do anything
        var selectedFiles = new List<(string path, long size, string categoryName)>();
        
        foreach (var category in Items)
        {
            foreach (var file in category.Files.Where(f => f.IsSelected))
            {
                selectedFiles.Add((file.FullPath, file.Size, category.Description));
            }
        }
        
        if (!selectedFiles.Any())
        {
            await ShowMessageAsync("Nothing Selected", "Please select files to clean first.");
            return;
        }
        
        // Show enhanced warning confirmation BEFORE deleting
        var confirmResult = await ShowConfirmAsync(
            "⚠️ Confirm Permanent Deletion", 
            $"You are about to PERMANENTLY DELETE {selectedFiles.Count} files.\n\n" +
            $"Space to recover: {FormatBytes(selectedFiles.Sum(f => f.size))}\n\n" +
            "⚠️ WARNING:\n" +
            "• Files will be permanently deleted (not sent to Recycle Bin)\n" +
            "• This action CANNOT be undone\n" +
            "• Deleted files may NOT be recoverable\n\n" +
            "Are you absolutely sure you want to proceed?");
        
        if (!confirmResult)
            return;
        
        // NOW perform the actual deletion - ONLY the files we captured above
        var deletedCount = 0;
        var deletedSize = 0L;
        var errors = new List<string>();
        var deletedPaths = new HashSet<string>();
        
        foreach (var (path, size, categoryName) in selectedFiles)
        {
            try
            {
                Console.WriteLine($"[QuickClean] Attempting to delete: {path}");
                
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                    deletedCount++;
                    deletedSize += size;
                    deletedPaths.Add(path);
                    Console.WriteLine($"[QuickClean] Successfully deleted file: {path}");
                }
                else if (System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.Delete(path, true);
                    deletedCount++;
                    deletedSize += size;
                    deletedPaths.Add(path);
                    Console.WriteLine($"[QuickClean] Successfully deleted directory: {path}");
                }
                else
                {
                    Console.WriteLine($"[QuickClean] Path does not exist: {path}");
                    errors.Add($"{System.IO.Path.GetFileName(path)}: File not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[QuickClean] Error deleting {path}: {ex.Message}");
                errors.Add($"{System.IO.Path.GetFileName(path)}: {ex.Message}");
            }
        }
        
        // Remove deleted files from the UI (only the ones that were actually deleted)
        foreach (var category in Items.ToList())
        {
            var filesToRemove = category.Files.Where(f => deletedPaths.Contains(f.FullPath)).ToList();
            
            foreach (var file in filesToRemove)
            {
                category.Files.Remove(file);
            }
            
            // Remove empty categories
            if (!category.Files.Any())
            {
                Items.Remove(category);
            }
            else
            {
                category.UpdateSelectionState();
            }
        }
        
        UpdateSummary();
        
        // Show result popup (dialog stays open!)
        string message;
        if (deletedCount == 0 && errors.Any())
        {
            message = $"⚠ No files were deleted.\n\n{errors.Count} errors occurred:\n" + 
                      string.Join("\n", errors.Take(8));
            if (errors.Count > 8)
                message += $"\n...and {errors.Count - 8} more";
        }
        else if (deletedCount == 0)
        {
            message = "⚠ No files were deleted.\nFiles may have already been removed.";
        }
        else
        {
            message = $"✓ Deleted {deletedCount} files\n✓ Freed {FormatBytes(deletedSize)}";
            if (errors.Any())
            {
                message += $"\n\n⚠ {errors.Count} files could not be deleted:\n" + 
                           string.Join("\n", errors.Take(5));
                if (errors.Count > 5)
                    message += $"\n...and {errors.Count - 5} more";
            }
        }
        
        await ShowMessageAsync("Cleanup Complete", message);
        
        // If all items are cleaned, close the dialog
        if (!Items.Any())
        {
            Confirmed = true;
            Close(true);
        }
    }
    
    private async Task ShowMessageAsync(string title, string message)
    {
        var msgBox = new Window
        {
            Title = title,
            Width = 400,
            Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = Brush.Parse("#1E1E2E")
        };
        
        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        
        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
            Foreground = Brush.Parse("#CDD6F4"),
            FontSize = 14
        };
        Grid.SetRow(text, 0);
        grid.Children.Add(text);
        
        var okButton = new Button
        {
            Content = "OK",
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
            Padding = new Thickness(24, 10),
            Margin = new Thickness(0, 16, 0, 0),
            Background = Brush.Parse("#4CAF50"),
            Foreground = Brushes.White
        };
        okButton.Click += (s, e) => msgBox.Close();
        Grid.SetRow(okButton, 1);
        grid.Children.Add(okButton);
        
        msgBox.Content = grid;
        await msgBox.ShowDialog(this);
    }
    
    private async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var result = false;
        var msgBox = new Window
        {
            Title = title,
            Width = 450,
            Height = 250,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Background = Brush.Parse("#1E1E2E")
        };
        
        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        
        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
            Foreground = Brush.Parse("#CDD6F4"),
            FontSize = 14
        };
        Grid.SetRow(text, 0);
        grid.Children.Add(text);
        
        var buttonPanel = new StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Thickness(0, 16, 0, 0)
        };
        
        var cancelButton = new Button
        {
            Content = "Cancel",
            Padding = new Thickness(24, 10),
            Margin = new Thickness(0, 0, 12, 0),
            Background = Brush.Parse("#45475A"),
            Foreground = Brushes.White
        };
        cancelButton.Click += (s, e) => { result = false; msgBox.Close(); };
        buttonPanel.Children.Add(cancelButton);
        
        var confirmButton = new Button
        {
            Content = "Delete Files",
            Padding = new Thickness(24, 10),
            Background = Brush.Parse("#EF4444"),
            Foreground = Brushes.White
        };
        confirmButton.Click += (s, e) => { result = true; msgBox.Close(); };
        buttonPanel.Children.Add(confirmButton);
        
        Grid.SetRow(buttonPanel, 1);
        grid.Children.Add(buttonPanel);
        
        msgBox.Content = grid;
        await msgBox.ShowDialog(this);
        return result;
    }

    /// <summary>
    /// Get the selected suggestions with their filtered file lists
    /// </summary>
    public IEnumerable<CleanupSuggestion> GetSelectedItems()
    {
        foreach (var category in Items)
        {
            var selectedFiles = category.Files.Where(f => f.IsSelected).Select(f => f.FullPath).ToList();
            if (selectedFiles.Any())
            {
                yield return new CleanupSuggestion
                {
                    Description = category.Description,
                    Path = category.Path,
                    PotentialSavings = category.SelectedSize,
                    RiskLevel = category.RiskLevel,
                    Type = category.Type,
                    AffectedFiles = selectedFiles
                };
            }
        }
    }
}

/// <summary>
/// Selectable cleanup category with expandable file list
/// </summary>
public class SelectableCleanupCategory : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isExpanded;
    private bool _isUpdatingInternally; // Prevent cascade during internal updates
    
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public CleanupRisk RiskLevel { get; set; }
    public CleanupType Type { get; set; }
    public ObservableCollection<SelectableFile> Files { get; } = new();

    public string SavingsFormatted => FormatBytes(SelectedSize);
    public string FileCountText => $"{SelectedFileCount}/{Files.Count} files";
    public string RiskLevelText => RiskLevel.ToString();
    
    public IBrush RiskLevelColor => RiskLevel switch
    {
        CleanupRisk.Safe => Brush.Parse("#10B981"),
        CleanupRisk.Low => Brush.Parse("#3B82F6"),
        CleanupRisk.Medium => Brush.Parse("#F59E0B"),
        CleanupRisk.High => Brush.Parse("#EF4444"),
        _ => Brush.Parse("#6B7280")
    };

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
                
                // Only cascade to files if this was a user-initiated change (not internal update)
                if (!_isUpdatingInternally)
                {
                    foreach (var file in Files)
                        file.IsSelected = value;
                }
                
                OnPropertyChanged(nameof(SelectedSize));
                OnPropertyChanged(nameof(SelectedFileCount));
                OnPropertyChanged(nameof(SavingsFormatted));
                OnPropertyChanged(nameof(FileCountText));
            }
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }
    }

    public bool HasPartialSelection => Files.Any(f => f.IsSelected) && !Files.All(f => f.IsSelected);
    public long SelectedSize => Files.Where(f => f.IsSelected).Sum(f => f.Size);
    public int SelectedFileCount => Files.Count(f => f.IsSelected);

    public SelectableCleanupCategory(CleanupSuggestion suggestion)
    {
        Description = suggestion.Description;
        Path = suggestion.Path;
        TotalSize = suggestion.PotentialSavings;
        RiskLevel = suggestion.RiskLevel;
        Type = suggestion.Type;
        
        // Load files from AffectedFiles list
        foreach (var filePath in suggestion.AffectedFiles)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists)
                {
                    Files.Add(new SelectableFile(this)
                    {
                        FullPath = filePath,
                        DisplayName = fileInfo.Name,
                        Size = fileInfo.Length,
                        IsSelected = true
                    });
                }
            }
            catch
            {
                // Skip files that can't be accessed
            }
        }
        
        // If no individual files listed, add a placeholder for the whole path
        if (!Files.Any() && Directory.Exists(Path))
        {
            try
            {
                var dirInfo = new DirectoryInfo(Path);
                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories).Take(100))
                {
                    Files.Add(new SelectableFile(this)
                    {
                        FullPath = file.FullName,
                        DisplayName = file.Name,
                        Size = file.Length,
                        IsSelected = true
                    });
                }
            }
            catch
            {
                // Skip on error
            }
        }
        
        _isSelected = Files.Any() && Files.All(f => f.IsSelected);
    }

    public void SelectAll()
    {
        foreach (var file in Files)
            file.IsSelected = true;
        _isSelected = true;
        OnPropertyChanged(nameof(IsSelected));
        OnPropertyChanged(nameof(SelectedSize));
        OnPropertyChanged(nameof(SelectedFileCount));
        OnPropertyChanged(nameof(SavingsFormatted));
        OnPropertyChanged(nameof(FileCountText));
    }

    public void SelectNone()
    {
        foreach (var file in Files)
            file.IsSelected = false;
        _isSelected = false;
        OnPropertyChanged(nameof(IsSelected));
        OnPropertyChanged(nameof(SelectedSize));
        OnPropertyChanged(nameof(SelectedFileCount));
        OnPropertyChanged(nameof(SavingsFormatted));
        OnPropertyChanged(nameof(FileCountText));
    }

    public void UpdateSelectionState()
    {
        _isUpdatingInternally = true;
        try
        {
            _isSelected = Files.Any() && Files.All(f => f.IsSelected);
            OnPropertyChanged(nameof(IsSelected));
            OnPropertyChanged(nameof(HasPartialSelection));
            OnPropertyChanged(nameof(SelectedSize));
            OnPropertyChanged(nameof(SelectedFileCount));
            OnPropertyChanged(nameof(SavingsFormatted));
            OnPropertyChanged(nameof(FileCountText));
        }
        finally
        {
            _isUpdatingInternally = false;
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:N2} {suffixes[suffixIndex]}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Selectable file within a cleanup category
/// </summary>
public class SelectableFile : INotifyPropertyChanged
{
    private bool _isSelected;
    
    public SelectableCleanupCategory? Parent { get; set; }
    public string FullPath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public long Size { get; set; }

    public string SizeFormatted => FormatBytes(Size);

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
                Parent?.UpdateSelectionState();
            }
        }
    }

    public SelectableFile(SelectableCleanupCategory parent)
    {
        Parent = parent;
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:N2} {suffixes[suffixIndex]}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
