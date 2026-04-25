using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BugzillaDumper.Models;

namespace BugzillaDumper.Services;

public class GitLabService(HttpClient httpClient)
{
    private string _baseUrl    = string.Empty;
    private string _token      = string.Empty;
    private int    _projectId  = 0;

    public bool IsProjectConfigured => _projectId > 0 && !string.IsNullOrWhiteSpace(_token);

    public void Configure(string baseUrl, string token)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _token   = token;
        httpClient.DefaultRequestHeaders.Remove("PRIVATE-TOKEN");
        if (!string.IsNullOrWhiteSpace(token))
            httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", token);
    }

    public async Task<GitLabProject> ValidateProjectAsync(string path)
    {
        var encoded = Uri.EscapeDataString(path.Trim());
        var json    = await httpClient.GetStringAsync($"{_baseUrl}/api/v4/projects/{encoded}");
        var project = JsonSerializer.Deserialize<GitLabProject>(json)
                      ?? throw new Exception("Empty response from GitLab");
        _projectId = project.Id;
        return project;
    }

    public async Task<string> UploadImageAsync(string fileName, byte[] data, string contentType)
    {
        using var form    = new MultipartFormDataContent();
        var       fc      = new ByteArrayContent(data);
        fc.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(fc, "file", fileName);

        var resp = await httpClient.PostAsync(
            $"{_baseUrl}/api/v4/projects/{_projectId}/uploads", form);
        resp.EnsureSuccessStatusCode();

        var upload = JsonSerializer.Deserialize<GitLabUpload>(
            await resp.Content.ReadAsStringAsync())
            ?? throw new Exception("Invalid upload response");

        // Use the markdown field if it looks like an absolute URL; otherwise build one
        if (!string.IsNullOrEmpty(upload.Markdown) && upload.Markdown.Contains("http"))
            return upload.Markdown;

        return $"![{upload.Alt}]({_baseUrl}{upload.Url})";
    }

    public async Task<GitLabIssue> CreateIssueAsync(string title, string description)
    {
        var payload = JsonSerializer.Serialize(new { title, description });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var resp = await httpClient.PostAsync(
            $"{_baseUrl}/api/v4/projects/{_projectId}/issues", content);
        resp.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<GitLabIssue>(
            await resp.Content.ReadAsStringAsync())
            ?? throw new Exception("Invalid issue response");
    }

    public async Task AddNoteAsync(int issueIid, string body)
    {
        var payload = JsonSerializer.Serialize(new { body });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var resp = await httpClient.PostAsync(
            $"{_baseUrl}/api/v4/projects/{_projectId}/issues/{issueIid}/notes", content);
        resp.EnsureSuccessStatusCode();
    }

    // ── Attachment type helpers ───────────────────────────────────────────────

    private static readonly HashSet<string> VideoExtensions =
        [".mp4", ".avi", ".mov", ".mkv", ".wmv", ".webm", ".flv", ".m4v"];

    public static bool IsImageAttachment(BugAttachment att) =>
        att.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    public static bool IsVideoAttachment(BugAttachment att) =>
        att.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
        || VideoExtensions.Contains(
               Path.GetExtension(att.FileName).ToLowerInvariant());
}
