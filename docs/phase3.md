Treemap becomes a method of analysis provide sub tabs which has the treemap operate byy file type, file category, file sizes, file locations and allow the user to switch between these different views.
Is the pie chart in file categories necessary or is it better just to do the same as the largest folder card with the category names with hte bar behind the name to indicate the size?
File Explorer still needs sorting and filtering options to better explore the actual files and folders.
developer tools is still running completley seperately it now shows up before the complete sacan is complete.  Are we able to populate the app as the scan progresses so the user can see everything updateing as it scans or is that too resource inefficient?
text size in settingshshould affec tthe entire app
what are the rules governing quick clean? is it 100% safe? if not then we should make it 100% safe



This is a **fantastic idea**, but only if you execute it as **"Data Pivoting"** rather than just simple filtering.

Most disk analyzers (WinDirStat, WizTree) are strictly bound to the **Physical Directory Structure** (`C:\Users\Name\...`).
If you allow users to view a Treemap based on **Logical Structure** (e.g., `Videos > .mp4 > Files`), you are offering a feature that is rare in the free market (DaisyDisk on Mac does this well, but it's paid).

Here is the strategic breakdown of how to implement **Treemap Sub-tabs** effectively.

### 1. The Strategy: "Physical" vs. "Logical" Views

Do not just filter the existing tree. You need to **reconstruct the tree** in memory based on the selected mode.

| Tab Name | Hierarchy Structure | Why it's a "Blue Ocean" Feature |
| --- | --- | --- |
| **Folder View** (Default) | `Root` → `Folder` → `Subfolder` → `File` | Standard behavior. Essential for cleaning specific directories. |
| **File Types** | `Root` → `.mp4` → `Files` | **Killer Feature.** Users can instantly see *"Wow, `.log` files are taking up 20% of my drive?"* |
| **Categories** | `Root` → `Video` → `.mkv` → `Files` | Great for gamers/editors. "Delete all 'Cache' files" becomes a visual action. |
| **By Age** (New Idea) | `Root` → `2024` → `October` → `Files` | **The "Digital Rot" Detector.** Instantly visualizes old, forgotten projects. |

### 2. The UX Design (Blade Runner Style)

Instead of standard browser tabs (which look clunky), use a **"View Mode" Switcher** in your cyberpunk UI.

* **UI Element:** A segmented control or "Toggle Group" at the top of the Treemap.
* **Labels:** `[ DIR_STRUCT ]` `[ EXTENSION ]` `[ CATEGORY ]` `[ CHRONO ]`

### 3. The Implementation: `TreePivotService`

You need a service that takes your physical scan result (`FileSystemItem root`) and returns a *new* virtual root organized by your requested logic.

Here is the C# code for the `TreePivotService`. You can drop this into your `Services` folder.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using DiskAnalyzer.Models;

namespace DiskAnalyzer.Services;

public class TreePivotService
{
    /// <summary>
    /// Pivots the physical folder tree into a "Category > Extension > File" hierarchy.
    /// </summary>
    public FileSystemItem PivotByCategory(FileSystemItem physicalRoot)
    {
        var virtualRoot = new FileSystemItem 
        { 
            Name = "Root (By Category)", 
            IsFolder = true, 
            FullPath = "VirtualRoot" 
        };

        // 1. Flatten the entire tree into a list of files
        var allFiles = FlattenTree(physicalRoot);

        // 2. Group by Category
        var categoryGroups = allFiles
            .GroupBy(f => f.Category)
            .OrderByDescending(g => g.Sum(f => f.Size));

        foreach (var catGroup in categoryGroups)
        {
            var catNode = new FileSystemItem
            {
                Name = catGroup.Key.ToString(),
                IsFolder = true,
                Category = catGroup.Key,
                FullPath = $"Category:{catGroup.Key}"
            };

            // 3. Sub-group by Extension inside Category
            var extGroups = catGroup
                .GroupBy(f => System.IO.Path.GetExtension(f.Name).ToLowerInvariant())
                .OrderByDescending(g => g.Sum(f => f.Size));

            foreach (var extGroup in extGroups)
            {
                string extName = string.IsNullOrEmpty(extGroup.Key) ? "No Extension" : extGroup.Key;
                
                var extNode = new FileSystemItem
                {
                    Name = extName,
                    IsFolder = true,
                    Category = catGroup.Key,
                    FullPath = $"Ext:{extName}"
                };

                // Add the actual files as children
                // Clone them so we don't break the original tree references
                extNode.Children = new System.Collections.ObjectModel.ObservableCollection<FileSystemItem>(
                    extGroup.Select(f => CloneItem(f)).OrderByDescending(f => f.Size)
                );
                
                extNode.Size = extNode.Children.Sum(c => c.Size);
                catNode.Children.Add(extNode);
            }

            catNode.Size = catNode.Children.Sum(c => c.Size);
            virtualRoot.Children.Add(catNode);
        }

        virtualRoot.Size = virtualRoot.Children.Sum(c => c.Size);
        return virtualRoot;
    }

    /// <summary>
    /// Helper to flatten tree (non-recursive stack-based to avoid stack overflow)
    /// </summary>
    private List<FileSystemItem> FlattenTree(FileSystemItem root)
    {
        var result = new List<FileSystemItem>();
        var stack = new Stack<FileSystemItem>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var item = stack.Pop();
            if (!item.IsFolder)
            {
                result.Add(item);
            }
            else
            {
                foreach (var child in item.Children)
                {
                    stack.Push(child);
                }
            }
        }
        return result;
    }

    private FileSystemItem CloneItem(FileSystemItem source)
    {
        return new FileSystemItem
        {
            Name = source.Name,
            FullPath = source.FullPath,
            Size = source.Size,
            IsFolder = false,
            Category = source.Category,
            LastModified = source.LastModified
        };
    }
}

```

### 4. Critical Warning: The "Context" Problem

When users switch to "Category View," the `FullPath` property of the folders becomes virtual (e.g., `Category:Video`).

* **The Trap:** If a user Right-Clicks a "Category Folder" and clicks "Open in Explorer," it will crash because that folder doesn't exist.
* **The Fix:** In your Treemap UI logic, check:
```csharp
if (selectedItem.FullPath.StartsWith("Category:") || selectedItem.FullPath.StartsWith("Ext:")) 
{
    // Disable "Open in Explorer"
    // Disable "Delete Folder"
}

```


*Only allow deletion on the **leaf nodes** (the actual files) in these views.*

### 5. Alternative to "Tabs": Heatmaps

If implementing full pivoting is too complex for v1.0, consider **Heatmap Modes** instead.

* **Structure:** Stays as "Folder View."
* **Color Logic:** Changes based on the tab.
* *Default Tab:* Color by File Type (Blue = Doc, Red = Video).
* *Age Tab:* Color by Date (Red = New, Blue = Ancient).
* *Growth Tab:* Color by "Created Recently" (useful for "Why did my disk fill up yesterday?").



**My Advice:**
Go with the **Pivot View (Category)**. It aligns perfectly with your "Smart/Context-Aware" branding. The code above handles the hardest part (the transformation). Just wire it up to a toggle button that calls `RebuildTreemap(pivotedRoot)`.