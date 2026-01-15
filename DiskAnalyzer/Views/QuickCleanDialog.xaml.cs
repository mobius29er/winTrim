using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DiskAnalyzer.Models;

namespace DiskAnalyzer.Views;

/// <summary>
/// Quick Clean dialog with hierarchical selectable cleanup items
/// </summary>
public partial class QuickCleanDialog : Window
{
    public ObservableCollection<SelectableCleanupCategory> Items { get; } = new();
    public bool Confirmed { get; private set; }

    public QuickCleanDialog(IEnumerable<CleanupSuggestion> suggestions)
    {
        InitializeComponent();
        DataContext = this;

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

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var category in Items)
            category.SelectAll();
        UpdateSummary();
    }

    private void SelectNone_Click(object sender, RoutedEventArgs e)
    {
        foreach (var category in Items)
            category.SelectNone();
        UpdateSummary();
    }

    private void ExpandAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var category in Items)
            category.IsExpanded = true;
    }

    private void CollapseAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var category in Items)
            category.IsExpanded = false;
    }

    private void CategoryCheckbox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is SelectableCleanupCategory category)
        {
            // When category checkbox is clicked, select/deselect all its files
            if (cb.IsChecked == true)
                category.SelectAll();
            else
                category.SelectNone();
        }
        UpdateSummary();
    }

    private void FileCheckbox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is SelectableFile file)
        {
            file.Parent?.UpdateSelectionState();
        }
        UpdateSummary();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        DialogResult = false;
        Close();
    }

    private void Clean_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        DialogResult = true;
        Close();
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
                // Create a modified suggestion with only selected files
                yield return new CleanupSuggestion
                {
                    Description = category.Description,
                    Path = category.Path,
                    PotentialSavings = category.SelectedSize,
                    RiskLevel = category.RiskLevel,
                    Type = category.Suggestion.Type,
                    AffectedFiles = selectedFiles
                };
            }
        }
    }
}

/// <summary>
/// Represents a cleanup category with selectable child files
/// </summary>
public class SelectableCleanupCategory : INotifyPropertyChanged
{
    private bool _isSelected = true;
    private bool _isExpanded = false;

    public CleanupSuggestion Suggestion { get; }
    public ObservableCollection<SelectableFile> Files { get; } = new();

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
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
                OnPropertyChanged(nameof(IsExpanded));
            }
        }
    }

    public bool HasPartialSelection => Files.Any(f => f.IsSelected) && !Files.All(f => f.IsSelected);
    
    public string Description => Suggestion.Description;
    public string Path => Suggestion.Path;
    public string SavingsFormatted => FormatBytes(SelectedSize);
    public CleanupRisk RiskLevel => Suggestion.RiskLevel;
    public long TotalSize => Suggestion.PotentialSavings;
    
    public long SelectedSize => Files.Where(f => f.IsSelected).Sum(f => f.Size);
    public int SelectedFileCount => Files.Count(f => f.IsSelected);
    public string FileCountText => $"{SelectedFileCount}/{Files.Count} files";

    public event PropertyChangedEventHandler? PropertyChanged;

    public SelectableCleanupCategory(CleanupSuggestion suggestion)
    {
        Suggestion = suggestion;
        LoadFiles();
    }

    private void LoadFiles()
    {
        // Try to enumerate actual files from the path
        try
        {
            if (Suggestion.AffectedFiles?.Any() == true)
            {
                // Use the pre-populated affected files list
                foreach (var filePath in Suggestion.AffectedFiles)
                {
                    if (File.Exists(filePath))
                    {
                        var file = new SelectableFile(filePath, this);
                        file.PropertyChanged += File_PropertyChanged;
                        Files.Add(file);
                    }
                }
                
                // If we got files, we're done
                if (Files.Any())
                    return;
            }
            
            // Fallback: Try to find files based on suggestion type
            if (Suggestion.Type == CleanupType.OldLogFiles)
            {
                // Search for log files
                var logFiles = FindLogFiles(Suggestion.Path, 50);
                foreach (var filePath in logFiles)
                {
                    var file = new SelectableFile(filePath, this);
                    file.PropertyChanged += File_PropertyChanged;
                    Files.Add(file);
                }
                if (Files.Any())
                    return;
            }
            
            if (Directory.Exists(Suggestion.Path))
            {
                // Don't enumerate root drives - too slow
                if (Suggestion.Path.Length <= 3)
                {
                    Files.Add(new SelectableFile($"Multiple files in {Suggestion.Path}", this, estimatedSize: TotalSize));
                    return;
                }
                
                // Enumerate files in the directory
                var files = Directory.GetFiles(Suggestion.Path, "*", SearchOption.AllDirectories)
                    .Take(100) // Limit to prevent UI lag
                    .ToList();
                
                foreach (var filePath in files)
                {
                    var file = new SelectableFile(filePath, this);
                    file.PropertyChanged += File_PropertyChanged;
                    Files.Add(file);
                }
                
                if (files.Count == 100)
                {
                    // Add placeholder for remaining files
                    Files.Add(new SelectableFile($"... and more files in {Suggestion.Path}", this, isPlaceholder: true));
                }
            }
            else if (File.Exists(Suggestion.Path))
            {
                // Single file
                var file = new SelectableFile(Suggestion.Path, this);
                file.PropertyChanged += File_PropertyChanged;
                Files.Add(file);
            }
            else
            {
                // Create placeholder based on total size
                Files.Add(new SelectableFile($"{Suggestion.Path}", this, estimatedSize: TotalSize));
            }
        }
        catch
        {
            // On error, create a single entry for the whole category
            Files.Add(new SelectableFile(Suggestion.Path, this, estimatedSize: TotalSize));
        }
        
        // Ensure we have at least one entry
        if (!Files.Any())
        {
            Files.Add(new SelectableFile($"{Suggestion.Path}", this, estimatedSize: TotalSize));
        }
    }
    
    private List<string> FindLogFiles(string rootPath, int maxFiles)
    {
        var logFiles = new List<string>();
        var searchPaths = new List<string>();
        
        // Common log file locations
        if (rootPath.Length <= 3) // Root drive
        {
            var windowsPath = System.IO.Path.Combine(rootPath, "Windows", "Logs");
            var programDataPath = System.IO.Path.Combine(rootPath, "ProgramData");
            var usersPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local");
            
            if (Directory.Exists(windowsPath)) searchPaths.Add(windowsPath);
            if (Directory.Exists(programDataPath)) searchPaths.Add(programDataPath);
            if (Directory.Exists(usersPath)) searchPaths.Add(usersPath);
        }
        else
        {
            searchPaths.Add(rootPath);
        }
        
        foreach (var searchPath in searchPaths)
        {
            try
            {
                var files = Directory.GetFiles(searchPath, "*.log", SearchOption.AllDirectories)
                    .Take(maxFiles - logFiles.Count)
                    .ToList();
                logFiles.AddRange(files);
                
                if (logFiles.Count >= maxFiles)
                    break;
            }
            catch { }
        }
        
        return logFiles.OrderByDescending(f => {
            try { return new FileInfo(f).Length; } catch { return 0; }
        }).Take(maxFiles).ToList();
    }

    private void File_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectableFile.IsSelected))
        {
            UpdateSelectionState();
        }
    }

    public void UpdateSelectionState()
    {
        var allSelected = Files.All(f => f.IsSelected);
        var noneSelected = Files.All(f => !f.IsSelected);
        
        _isSelected = allSelected;
        OnPropertyChanged(nameof(IsSelected));
        OnPropertyChanged(nameof(SelectedSize));
        OnPropertyChanged(nameof(SelectedFileCount));
        OnPropertyChanged(nameof(FileCountText));
        OnPropertyChanged(nameof(SavingsFormatted));
        OnPropertyChanged(nameof(HasPartialSelection));
    }

    public void SelectAll()
    {
        foreach (var file in Files)
            file.IsSelected = true;
        _isSelected = true;
        OnPropertyChanged(nameof(IsSelected));
        OnPropertyChanged(nameof(SelectedSize));
        OnPropertyChanged(nameof(SelectedFileCount));
        OnPropertyChanged(nameof(FileCountText));
        OnPropertyChanged(nameof(SavingsFormatted));
    }

    public void SelectNone()
    {
        foreach (var file in Files)
            file.IsSelected = false;
        _isSelected = false;
        OnPropertyChanged(nameof(IsSelected));
        OnPropertyChanged(nameof(SelectedSize));
        OnPropertyChanged(nameof(SelectedFileCount));
        OnPropertyChanged(nameof(FileCountText));
        OnPropertyChanged(nameof(SavingsFormatted));
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

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Represents an individual file that can be selected for cleanup
/// </summary>
public class SelectableFile : INotifyPropertyChanged
{
    private bool _isSelected = true;
    private readonly bool _isPlaceholder;

    public SelectableCleanupCategory? Parent { get; }
    public string FullPath { get; }
    public string DisplayName { get; }
    public long Size { get; }
    public string SizeFormatted => FormatBytes(Size);

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value && !_isPlaceholder)
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SelectableFile(string path, SelectableCleanupCategory? parent, bool isPlaceholder = false, long estimatedSize = 0)
    {
        Parent = parent;
        FullPath = path;
        _isPlaceholder = isPlaceholder;
        
        if (isPlaceholder)
        {
            DisplayName = path;
            Size = 0;
            _isSelected = true; // Placeholders are always "selected"
        }
        else
        {
            DisplayName = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(DisplayName))
                DisplayName = path;
                
            try
            {
                if (estimatedSize > 0)
                {
                    Size = estimatedSize;
                }
                else if (File.Exists(path))
                {
                    Size = new FileInfo(path).Length;
                }
            }
            catch
            {
                Size = estimatedSize;
            }
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes == 0) return "";
        
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:N1} {suffixes[suffixIndex]}";
    }
}
