using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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

    private void Clean_Click(object? sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close(true);
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
                // When category selection changes, update all files
                foreach (var file in Files)
                    file.IsSelected = value;
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
        _isSelected = Files.Any() && Files.All(f => f.IsSelected);
        OnPropertyChanged(nameof(IsSelected));
        OnPropertyChanged(nameof(HasPartialSelection));
        OnPropertyChanged(nameof(SelectedSize));
        OnPropertyChanged(nameof(SelectedFileCount));
        OnPropertyChanged(nameof(SavingsFormatted));
        OnPropertyChanged(nameof(FileCountText));
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
