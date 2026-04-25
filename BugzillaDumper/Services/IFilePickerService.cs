using System.Threading.Tasks;

namespace BugzillaDumper.Services;

public interface IFilePickerService
{
    Task<string?> SaveFileAsync(string title, string suggestedFileName, string filterName, string filterExt);
}
