using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace BugzillaDumper.Services;

public class AvaloniaFilePickerService(Window window) : IFilePickerService
{
    public async Task<string?> SaveFileAsync(string title, string suggestedFileName, string filterName, string filterExt)
    {
        var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title             = title,
            SuggestedFileName = suggestedFileName,
            DefaultExtension  = filterExt,
            FileTypeChoices   = new List<FilePickerFileType>
            {
                new(filterName) { Patterns = new[] { $"*.{filterExt}" } }
            }
        });
        return file?.TryGetLocalPath();
    }
}
