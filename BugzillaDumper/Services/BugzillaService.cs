using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using BugzillaDumper.Models;

namespace BugzillaDumper.Services;

public class BugzillaService(HttpClient httpClient)
{
    private string _baseUrl = string.Empty;
    private string _apiKey = string.Empty;

    public void Configure(string baseUrl, string apiKey)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _apiKey = apiKey;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_baseUrl) && !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<List<BugSummary>> SearchBugsAsync(SearchCriteria criteria)
    {
        // When Limit <= 0, fetch all pages automatically
        if (criteria.Limit <= 0)
            return await SearchAllBugsAsync(criteria);

        var url = BuildSearchUrl(criteria, limit: criteria.Limit, offset: 0);
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BugListResponse>();
        return result?.Bugs ?? [];
    }

    private async Task<List<BugSummary>> SearchAllBugsAsync(SearchCriteria criteria)
    {
        const int pageSize = 500;
        var all = new List<BugSummary>();
        int offset = 0;

        while (true)
        {
            var url = BuildSearchUrl(criteria, limit: pageSize, offset: offset);
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BugListResponse>();
            var page = result?.Bugs ?? [];
            all.AddRange(page);

            // Fewer results than page size means we've reached the last page
            if (page.Count < pageSize)
                break;

            offset += pageSize;
        }

        return all;
    }

    private string BuildSearchUrl(SearchCriteria criteria, int limit, int offset)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["api_key"] = _apiKey;
        query["include_fields"] = "id,summary,status,severity,priority,assigned_to,creator,creation_time,last_change_time,component,product,resolution,version,target_milestone,op_sys,platform";

        if (!string.IsNullOrWhiteSpace(criteria.Product))
            query["product"] = criteria.Product;
        if (!string.IsNullOrWhiteSpace(criteria.Component))
            query["component"] = criteria.Component;
        if (!string.IsNullOrWhiteSpace(criteria.AssignedTo))
            query["assigned_to"] = criteria.AssignedTo;
        if (!string.IsNullOrWhiteSpace(criteria.Summary))
            query["summary"] = criteria.Summary;

        query["limit"] = limit.ToString();
        query["offset"] = offset.ToString();

        // NameValueCollection merges duplicate keys into "status=A%2CB" which Bugzilla rejects.
        // Append each status value as a separate &status=X segment instead.
        var statusSegment = string.Empty;
        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            foreach (var s in criteria.Status.Split(',', StringSplitOptions.RemoveEmptyEntries))
                statusSegment += $"&status={Uri.EscapeDataString(s.Trim())}";
        }

        return $"{_baseUrl}/rest/bug?{query}{statusSegment}";
    }

    public async Task<BugDetail?> GetBugDetailAsync(int bugId)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["api_key"] = _apiKey;
        query["include_fields"] = "_default,whiteboard,keywords,blocks,depends_on,cc,flags";

        var url = $"{_baseUrl}/rest/bug/{bugId}?{query}";
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BugDetailResponse>();
        return result?.Bugs.Count > 0 ? result.Bugs[0] : null;
    }

    public async Task<List<BugComment>> GetBugCommentsAsync(int bugId)
    {
        var url = $"{_baseUrl}/rest/bug/{bugId}/comment?api_key={_apiKey}";
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var bugs = doc.RootElement.GetProperty("bugs");
        var bugData = bugs.GetProperty(bugId.ToString());
        var commentsJson = bugData.GetProperty("comments").GetRawText();
        return JsonSerializer.Deserialize<List<BugComment>>(commentsJson) ?? [];
    }

    public async Task<BugDetail?> GetBugDetailWithCommentsAsync(int bugId)
    {
        var detail = await GetBugDetailAsync(bugId);
        if (detail is null) return null;

        detail.Comments = await GetBugCommentsAsync(bugId);
        return detail;
    }
}
