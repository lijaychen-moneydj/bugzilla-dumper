using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;
using BugzillaDumper.Models;
using ClosedXML.Excel;

namespace BugzillaDumper.Services;

public static class ExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    // ── Single-bug exports ────────────────────────────────────────────────────

    public static void ExportBugsToJson(List<BugSummary> bugs, string filePath)
    {
        File.WriteAllText(filePath, JsonSerializer.Serialize(bugs, JsonOptions));
    }

    public static void ExportBugDetailToJson(BugDetail bug, string filePath)
    {
        File.WriteAllText(filePath, JsonSerializer.Serialize(bug, JsonOptions));
    }

    public static void ExportBugsToExcel(List<BugSummary> bugs, string filePath)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Bugs");
        WriteBugListSheet(ws, bugs);
        wb.SaveAs(filePath);
    }

    public static void ExportBugDetailToExcel(BugDetail bug, string filePath)
    {
        using var wb = new XLWorkbook();
        var wsInfo = wb.Worksheets.Add("Bug Info");
        AddBugInfoSheet(wsInfo, bug);
        var wsComments = wb.Worksheets.Add("Comments");
        AddCommentsSheet(wsComments, bug.Comments, includeBugIdColumn: false);
        wb.SaveAs(filePath);
    }

    // ── Full-dump exports (list + all details + all comments) ────────────────

    public static void ExportFullDumpToJson(List<BugDetail> details, string filePath)
    {
        File.WriteAllText(filePath, JsonSerializer.Serialize(details, JsonOptions));
    }

    public static void ExportFullDumpToExcel(List<BugDetail> details, string filePath)
    {
        using var wb = new XLWorkbook();

        // Sheet 1 – summary list
        var wsList = wb.Worksheets.Add("Bugs");
        WriteBugListSheet(wsList, [.. details.ConvertAll(d => (BugSummary)d)]);

        // Sheet 2 – detail fields (one row per bug)
        var wsDetails = wb.Worksheets.Add("Details");
        WriteDetailSheet(wsDetails, details);

        // Sheet 3 – all comments flat table
        var wsComments = wb.Worksheets.Add("Comments");
        var allComments = new List<(int BugId, BugComment Comment)>();
        foreach (var d in details)
            foreach (var c in d.Comments)
                allComments.Add((d.Id, c));
        AddCommentsSheet(wsComments, allComments);

        wb.SaveAs(filePath);
    }

    // ── Folder export (full dump + attachments) ──────────────────────────────

    public static async Task ExportToFolderAsync(
        List<BugDetail> details,
        string outputFolder,
        Func<int, Task<List<BugAttachment>>> fetchAttachments,
        Action<string> onStatus,
        Action<int> onProgress)
    {
        Directory.CreateDirectory(outputFolder);

        onStatus("Writing data.json...");
        await File.WriteAllTextAsync(
            Path.Combine(outputFolder, "data.json"),
            JsonSerializer.Serialize(details, JsonOptions));

        var attachmentsRoot = Path.Combine(outputFolder, "attachments");

        for (int i = 0; i < details.Count; i++)
        {
            var bug = details[i];
            onStatus($"Fetching attachments {i + 1}/{details.Count}  (Bug #{bug.Id})...");

            List<BugAttachment> attachments;
            try   { attachments = await fetchAttachments(bug.Id); }
            catch { attachments = []; }

            if (attachments.Count > 0)
            {
                var bugFolder = Path.Combine(attachmentsRoot, bug.Id.ToString());
                Directory.CreateDirectory(bugFolder);

                for (int j = 0; j < attachments.Count; j++)
                {
                    var att = attachments[j];
                    if (string.IsNullOrEmpty(att.Data)) continue;

                    var safeName = string.Concat(att.FileName.Split(Path.GetInvalidFileNameChars()));
                    var filePath = Path.Combine(bugFolder, $"{j + 1:D3}_{safeName}");
                    await File.WriteAllBytesAsync(filePath, Convert.FromBase64String(att.Data));
                }
            }

            onProgress(i + 1);
        }
    }

    // ── Sheet writers ─────────────────────────────────────────────────────────

    private static void WriteBugListSheet(IXLWorksheet ws, List<BugSummary> bugs)
    {
        string[] headers =
        [
            "ID", "Summary", "Status", "Resolution", "Severity", "Priority",
            "Component", "Product", "Version", "Assigned To", "Creator",
            "Created", "Last Changed", "OS", "Platform", "Target Milestone"
        ];

        WriteHeader(ws, 1, headers);

        for (int r = 0; r < bugs.Count; r++)
        {
            var bug = bugs[r];
            int row = r + 2;
            ws.Cell(row, 1).Value  = bug.Id;
            ws.Cell(row, 2).Value  = bug.Summary;
            ws.Cell(row, 3).Value  = bug.Status;
            ws.Cell(row, 4).Value  = bug.Resolution;
            ws.Cell(row, 5).Value  = bug.Severity;
            ws.Cell(row, 6).Value  = bug.Priority;
            ws.Cell(row, 7).Value  = bug.Component;
            ws.Cell(row, 8).Value  = bug.Product;
            ws.Cell(row, 9).Value  = bug.Version;
            ws.Cell(row, 10).Value = bug.AssignedTo;
            ws.Cell(row, 11).Value = bug.Creator;
            ws.Cell(row, 12).Value = bug.CreationTime;
            ws.Cell(row, 13).Value = bug.LastChangeTime;
            ws.Cell(row, 14).Value = bug.OpSys;
            ws.Cell(row, 15).Value = bug.Platform;
            ws.Cell(row, 16).Value = bug.TargetMilestone;
            if (r % 2 == 1)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
        }

        ws.Columns().AdjustToContents();
        ws.Column(2).Width = 60;
        ws.SheetView.FreezeRows(1);
        if (bugs.Count > 0) ws.RangeUsed()?.SetAutoFilter();
    }

    private static void WriteDetailSheet(IXLWorksheet ws, List<BugDetail> details)
    {
        string[] headers =
        [
            "ID", "Summary", "Status", "Resolution", "Severity", "Priority",
            "Component", "Product", "Version", "Assigned To", "Creator",
            "Created", "Last Changed", "OS", "Platform", "Target Milestone",
            "Whiteboard", "Keywords", "CC", "Blocks", "Depends On"
        ];

        WriteHeader(ws, 1, headers);

        for (int r = 0; r < details.Count; r++)
        {
            var d = details[r];
            int row = r + 2;
            ws.Cell(row, 1).Value  = d.Id;
            ws.Cell(row, 2).Value  = d.Summary;
            ws.Cell(row, 3).Value  = d.Status;
            ws.Cell(row, 4).Value  = d.Resolution;
            ws.Cell(row, 5).Value  = d.Severity;
            ws.Cell(row, 6).Value  = d.Priority;
            ws.Cell(row, 7).Value  = d.Component;
            ws.Cell(row, 8).Value  = d.Product;
            ws.Cell(row, 9).Value  = d.Version;
            ws.Cell(row, 10).Value = d.AssignedTo;
            ws.Cell(row, 11).Value = d.Creator;
            ws.Cell(row, 12).Value = d.CreationTime;
            ws.Cell(row, 13).Value = d.LastChangeTime;
            ws.Cell(row, 14).Value = d.OpSys;
            ws.Cell(row, 15).Value = d.Platform;
            ws.Cell(row, 16).Value = d.TargetMilestone;
            ws.Cell(row, 17).Value = d.Whiteboard;
            ws.Cell(row, 18).Value = string.Join(", ", d.Keywords);
            ws.Cell(row, 19).Value = string.Join(", ", d.Cc);
            ws.Cell(row, 20).Value = string.Join(", ", d.Blocks);
            ws.Cell(row, 21).Value = string.Join(", ", d.DependsOn);
            if (r % 2 == 1)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
        }

        ws.Columns().AdjustToContents();
        ws.Column(2).Width = 60;
        ws.SheetView.FreezeRows(1);
        if (details.Count > 0) ws.RangeUsed()?.SetAutoFilter();
    }

    // Comments with Bug ID column (full dump)
    private static void AddCommentsSheet(IXLWorksheet ws, List<(int BugId, BugComment Comment)> items)
    {
        string[] headers = ["Bug ID", "#", "Author", "Date", "Comment"];
        WriteHeader(ws, 1, headers);

        for (int r = 0; r < items.Count; r++)
        {
            var (bugId, c) = items[r];
            int row = r + 2;
            ws.Cell(row, 1).Value = bugId;
            ws.Cell(row, 2).Value = c.Count;
            ws.Cell(row, 3).Value = c.Author;
            ws.Cell(row, 4).Value = c.CreationTime;
            ws.Cell(row, 5).Value = c.Text;
            ws.Cell(row, 5).Style.Alignment.WrapText = true;
            if (r % 2 == 1)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
        }

        ws.Column(1).Width = 10;
        ws.Column(2).Width = 5;
        ws.Column(3).Width = 30;
        ws.Column(4).Width = 25;
        ws.Column(5).Width = 100;
        ws.SheetView.FreezeRows(1);
        if (items.Count > 0) ws.RangeUsed()?.SetAutoFilter();
    }

    // Comments without Bug ID column (single-bug detail)
    private static void AddCommentsSheet(IXLWorksheet ws, List<BugComment> comments, bool includeBugIdColumn)
    {
        string[] headers = ["#", "Author", "Date", "Comment"];
        WriteHeader(ws, 1, headers);

        for (int r = 0; r < comments.Count; r++)
        {
            var c = comments[r];
            int row = r + 2;
            ws.Cell(row, 1).Value = c.Count;
            ws.Cell(row, 2).Value = c.Author;
            ws.Cell(row, 3).Value = c.CreationTime;
            ws.Cell(row, 4).Value = c.Text;
            ws.Cell(row, 4).Style.Alignment.WrapText = true;
            if (r % 2 == 1)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
        }

        ws.Column(1).Width = 5;
        ws.Column(2).Width = 30;
        ws.Column(3).Width = 25;
        ws.Column(4).Width = 100;
        ws.SheetView.FreezeRows(1);
        if (comments.Count > 0) ws.RangeUsed()?.SetAutoFilter();
    }

    private static void AddBugInfoSheet(IXLWorksheet ws, BugDetail bug)
    {
        var fields = new (string Label, string Value)[]
        {
            ("Bug ID", bug.Id.ToString()),
            ("Summary", bug.Summary),
            ("Status", bug.Status),
            ("Resolution", bug.Resolution),
            ("Severity", bug.Severity),
            ("Priority", bug.Priority),
            ("Component", bug.Component),
            ("Product", bug.Product),
            ("Version", bug.Version),
            ("Target Milestone", bug.TargetMilestone),
            ("Assigned To", bug.AssignedTo),
            ("Creator", bug.Creator),
            ("Created", bug.CreationTime),
            ("Last Changed", bug.LastChangeTime),
            ("OS", bug.OpSys),
            ("Platform", bug.Platform),
            ("Whiteboard", bug.Whiteboard),
            ("Keywords", string.Join(", ", bug.Keywords)),
            ("CC", string.Join(", ", bug.Cc)),
            ("Blocks", string.Join(", ", bug.Blocks)),
            ("Depends On", string.Join(", ", bug.DependsOn)),
        };

        for (int i = 0; i < fields.Length; i++)
        {
            int row = i + 1;
            var labelCell = ws.Cell(row, 1);
            labelCell.Value = fields[i].Label;
            labelCell.Style.Font.Bold = true;
            labelCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            labelCell.Style.Font.FontColor = XLColor.White;

            var valueCell = ws.Cell(row, 2);
            valueCell.Value = fields[i].Value;
            valueCell.Style.Alignment.WrapText = true;
        }

        ws.Column(1).Width = 20;
        ws.Column(2).Width = 80;
    }

    private static void WriteHeader(IXLWorksheet ws, int row, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4472C4");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }
    }
}
