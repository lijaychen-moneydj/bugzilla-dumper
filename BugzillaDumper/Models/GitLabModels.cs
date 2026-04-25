using System.Text.Json.Serialization;

namespace BugzillaDumper.Models;

public class GitLabProject
{
    [JsonPropertyName("id")]                  public int    Id                { get; set; }
    [JsonPropertyName("name")]                public string Name              { get; set; } = string.Empty;
    [JsonPropertyName("path_with_namespace")] public string PathWithNamespace { get; set; } = string.Empty;
    [JsonPropertyName("web_url")]             public string WebUrl            { get; set; } = string.Empty;
}

public class GitLabIssue
{
    [JsonPropertyName("iid")]     public int    Iid    { get; set; }
    [JsonPropertyName("web_url")] public string WebUrl { get; set; } = string.Empty;
}

public class GitLabUpload
{
    [JsonPropertyName("alt")]      public string Alt      { get; set; } = string.Empty;
    [JsonPropertyName("url")]      public string Url      { get; set; } = string.Empty;
    [JsonPropertyName("markdown")] public string Markdown { get; set; } = string.Empty;
}
