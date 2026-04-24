using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BugzillaDumper.Models;
using BugzillaDumper.Services;
using Microsoft.Win32;

namespace BugzillaDumper.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly BugzillaService _bugzillaService;
    private readonly UpdateService _updateService;
    private CancellationTokenSource? _fetchCts;

    [ObservableProperty] private string _bugzillaUrl = string.Empty;
    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private string _searchProduct = string.Empty;
    [ObservableProperty] private string _searchComponent = string.Empty;
    [ObservableProperty] private bool _statusConfirmed = true;
    [ObservableProperty] private bool _statusInProgress = true;
    [ObservableProperty] private bool _statusResolved = false;
    [ObservableProperty] private bool _statusReopened = true;
    [ObservableProperty] private bool _statusVerified = false;
    [ObservableProperty] private string _searchAssignedTo = string.Empty;
    [ObservableProperty] private string _searchReporter = string.Empty;
    [ObservableProperty] private string _searchSummary = string.Empty;
    [ObservableProperty] private int _searchLimit = 0;
    [ObservableProperty] private bool _sortNewestFirst = true;

    private string BuildStatusCriteria()
    {
        var statuses = new List<string>();
        if (StatusConfirmed)  statuses.Add("CONFIRMED");
        if (StatusInProgress) statuses.Add("IN_PROGRESS");
        if (StatusResolved)   statuses.Add("RESOLVED");
        if (StatusReopened)   statuses.Add("REOPENED");
        if (StatusVerified)   statuses.Add("VERIFIED");
        return string.Join(",", statuses);
    }

    [ObservableProperty] private ObservableCollection<BugSummary> _bugs = [];
    [ObservableProperty] private BugSummary? _selectedBug;
    [ObservableProperty] private BugDetail? _selectedBugDetail;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDetailLoading;
    [ObservableProperty] private bool _isFetchingAll;
    [ObservableProperty] private int _fetchProgress;
    [ObservableProperty] private int _fetchTotal;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private bool _isConnected;

    // Update
    [ObservableProperty] private bool _updateAvailable;
    [ObservableProperty] private string _updateVersionText = string.Empty;
    [ObservableProperty] private bool _isUpdating;

    // List keyword search
    [ObservableProperty] private string _listKeyword = string.Empty;
    [ObservableProperty] private int _listMatchCount;

    partial void OnListKeywordChanged(string _) => RefreshMatchCount();

    [RelayCommand]
    private void ClearListKeyword() => ListKeyword = string.Empty;

    private void RefreshMatchCount()
    {
        if (string.IsNullOrWhiteSpace(ListKeyword)) { ListMatchCount = 0; return; }
        ListMatchCount = 0;
        foreach (var b in Bugs)
            if (BugMatchesKeyword(b, ListKeyword)) ListMatchCount++;
    }

    public static bool BugMatchesKeyword(BugSummary bug, string keyword)
        => bug.Summary.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        || bug.Id.ToString().Contains(keyword)
        || bug.Component.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        || bug.Product.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        || bug.AssignedTo.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        || bug.Creator.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        || bug.Status.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        || bug.Resolution.Contains(keyword, StringComparison.OrdinalIgnoreCase);

    public MainViewModel(BugzillaService bugzillaService, UpdateService updateService)
    {
        _bugzillaService = bugzillaService;
        _updateService = updateService;
        LoadSettings();
    }

    public void SetUpdateAvailable(string version)
    {
        UpdateVersionText = $"v{version}";
        UpdateAvailable = true;
    }

    [RelayCommand]
    private async Task ApplyUpdateAsync()
    {
        IsUpdating = true;
        StatusMessage = "正在下載更新…";
        try
        {
            await Task.Run(() => _updateService.ApplyUpdate(msg =>
                System.Windows.Application.Current.Dispatcher.Invoke(() => StatusMessage = msg)));
        }
        catch (Exception ex)
        {
            StatusMessage = $"更新失敗：{ex.Message}";
            IsUpdating = false;
        }
    }

    private void LoadSettings()
    {
        var settings = SettingsService.Load();
        BugzillaUrl = settings.BugzillaUrl;
        ApiKey = settings.ApiKey;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        SettingsService.Save(new AppSettings { BugzillaUrl = BugzillaUrl, ApiKey = ApiKey });
        _bugzillaService.Configure(BugzillaUrl, ApiKey);
        IsConnected = true;
        StatusMessage = "Settings saved.";
    }

    [RelayCommand]
    private async Task SearchBugsAsync()
    {
        if (string.IsNullOrWhiteSpace(BugzillaUrl) || string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusMessage = "Please enter Bugzilla URL and API Key first.";
            return;
        }

        _bugzillaService.Configure(BugzillaUrl, ApiKey);
        IsLoading = true;
        StatusMessage = "Searching bugs...";
        Bugs.Clear();
        SelectedBugDetail = null;

        try
        {
            var criteria = new SearchCriteria
            {
                Product = SearchProduct,
                Component = SearchComponent,
                Status = BuildStatusCriteria(),
                AssignedTo = SearchAssignedTo,
                Reporter = SearchReporter,
                Summary = SearchSummary,
                Limit = SearchLimit,
                NewestFirst = SortNewestFirst
            };

            var results = await _bugzillaService.SearchBugsAsync(criteria);
            foreach (var bug in results)
                Bugs.Add(bug);

            StatusMessage = $"Found {results.Count} bug(s).";
            IsConnected = true;
            RefreshMatchCount();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsConnected = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadBugDetailAsync(BugSummary? bug)
    {
        if (bug is null) return;

        IsDetailLoading = true;
        StatusMessage = $"Loading bug #{bug.Id}...";
        SelectedBugDetail = null;

        try
        {
            SelectedBugDetail = await _bugzillaService.GetBugDetailWithCommentsAsync(bug.Id);
            StatusMessage = $"Loaded bug #{bug.Id}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading bug detail: {ex.Message}";
        }
        finally
        {
            IsDetailLoading = false;
        }
    }

    // ── Full-dump: fetch all details then export ──────────────────────────────

    [RelayCommand]
    private async Task ExportFullDumpToJsonAsync()
    {
        var details = await FetchAllDetailsAsync();
        if (details is null) return;

        var dlg = new SaveFileDialog
        {
            Title = "Export Full Dump to JSON",
            Filter = "JSON Files (*.json)|*.json",
            FileName = $"bugs_full_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                ExportService.ExportFullDumpToJson(details, dlg.FileName);
                StatusMessage = $"Exported {details.Count} bugs (with details) to {dlg.FileName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task ExportFullDumpToExcelAsync()
    {
        var details = await FetchAllDetailsAsync();
        if (details is null) return;

        var dlg = new SaveFileDialog
        {
            Title = "Export Full Dump to Excel",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"bugs_full_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                StatusMessage = "Writing Excel file...";
                await Task.Run(() => ExportService.ExportFullDumpToExcel(details, dlg.FileName));
                StatusMessage = $"Exported {details.Count} bugs (with details) to {dlg.FileName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export error: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private async Task ExportFullDumpToFolderAsync()
    {
        var details = await FetchAllDetailsAsync();
        if (details is null) return;

        var outputFolder = Path.Combine(
            AppContext.BaseDirectory,
            $"bugs_full_{DateTime.Now:yyyyMMdd_HHmmss}");

        IsFetchingAll = true;
        FetchProgress = 0;
        FetchTotal = details.Count;

        try
        {
            await ExportService.ExportToFolderAsync(
                details, outputFolder,
                fetchAttachments: id => _bugzillaService.GetBugAttachmentsAsync(id),
                onStatus: msg => System.Windows.Application.Current.Dispatcher.Invoke(() => StatusMessage = msg),
                onProgress: i  => System.Windows.Application.Current.Dispatcher.Invoke(() => FetchProgress = i));

            System.Diagnostics.Process.Start("explorer.exe", outputFolder);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
        finally
        {
            IsFetchingAll = false;
        }
    }

    [RelayCommand]
    private void CancelFetch()
    {
        _fetchCts?.Cancel();
        StatusMessage = "Cancelled.";
    }

    /// <summary>Fetches full details (including comments) for every bug in the current list.</summary>
    private async Task<List<BugDetail>?> FetchAllDetailsAsync()
    {
        if (Bugs.Count == 0)
        {
            StatusMessage = "No bugs in list. Run a search first.";
            return null;
        }

        _fetchCts = new CancellationTokenSource();
        IsFetchingAll = true;
        FetchProgress = 0;
        FetchTotal = Bugs.Count;

        var details = new List<BugDetail>();

        try
        {
            for (int i = 0; i < Bugs.Count; i++)
            {
                _fetchCts.Token.ThrowIfCancellationRequested();

                var bug = Bugs[i];
                StatusMessage = $"Fetching details {i + 1}/{FetchTotal}  (Bug #{bug.Id})...";

                var detail = await _bugzillaService.GetBugDetailWithCommentsAsync(bug.Id);
                if (detail is not null)
                    details.Add(detail);

                FetchProgress = i + 1;
            }

            StatusMessage = $"Fetched {details.Count} bugs. Ready to export.";
            return details;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Fetch cancelled.";
            return null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fetch error: {ex.Message}";
            return null;
        }
        finally
        {
            IsFetchingAll = false;
            _fetchCts.Dispose();
            _fetchCts = null;
        }
    }

    // ── Individual exports ────────────────────────────────────────────────────

    [RelayCommand]
    private void ExportBugsToJson()
    {
        if (Bugs.Count == 0) { StatusMessage = "No bugs to export."; return; }

        var dlg = new SaveFileDialog
        {
            Title = "Export Bug List to JSON",
            Filter = "JSON Files (*.json)|*.json",
            FileName = $"bugs_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };
        if (dlg.ShowDialog() == true)
        {
            try { ExportService.ExportBugsToJson([.. Bugs], dlg.FileName); StatusMessage = $"Exported {Bugs.Count} bugs to {dlg.FileName}"; }
            catch (Exception ex) { StatusMessage = $"Export error: {ex.Message}"; }
        }
    }

    [RelayCommand]
    private void ExportBugsToExcel()
    {
        if (Bugs.Count == 0) { StatusMessage = "No bugs to export."; return; }

        var dlg = new SaveFileDialog
        {
            Title = "Export Bug List to Excel",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"bugs_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        };
        if (dlg.ShowDialog() == true)
        {
            try { ExportService.ExportBugsToExcel([.. Bugs], dlg.FileName); StatusMessage = $"Exported {Bugs.Count} bugs to {dlg.FileName}"; }
            catch (Exception ex) { StatusMessage = $"Export error: {ex.Message}"; }
        }
    }

    [RelayCommand]
    private void ExportDetailToJson()
    {
        if (SelectedBugDetail is null) { StatusMessage = "No bug detail to export."; return; }

        var dlg = new SaveFileDialog
        {
            Title = "Export Bug Detail to JSON",
            Filter = "JSON Files (*.json)|*.json",
            FileName = $"bug_{SelectedBugDetail.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };
        if (dlg.ShowDialog() == true)
        {
            try { ExportService.ExportBugDetailToJson(SelectedBugDetail, dlg.FileName); StatusMessage = $"Exported bug #{SelectedBugDetail.Id} to {dlg.FileName}"; }
            catch (Exception ex) { StatusMessage = $"Export error: {ex.Message}"; }
        }
    }

    [RelayCommand]
    private void ExportDetailToExcel()
    {
        if (SelectedBugDetail is null) { StatusMessage = "No bug detail to export."; return; }

        var dlg = new SaveFileDialog
        {
            Title = "Export Bug Detail to Excel",
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"bug_{SelectedBugDetail.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        };
        if (dlg.ShowDialog() == true)
        {
            try { ExportService.ExportBugDetailToExcel(SelectedBugDetail, dlg.FileName); StatusMessage = $"Exported bug #{SelectedBugDetail.Id} detail to {dlg.FileName}"; }
            catch (Exception ex) { StatusMessage = $"Export error: {ex.Message}"; }
        }
    }

    partial void OnSelectedBugChanged(BugSummary? value)
    {
        if (value is not null)
            _ = LoadBugDetailAsync(value);
    }
}
