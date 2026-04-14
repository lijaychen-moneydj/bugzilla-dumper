using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BugzillaDumper.Models;

public class BugListResponse
{
    [JsonPropertyName("bugs")]
    public List<BugSummary> Bugs { get; set; } = [];
}

public class BugSummary
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    [JsonPropertyName("assigned_to")]
    public string AssignedTo { get; set; } = string.Empty;

    [JsonPropertyName("creator")]
    public string Creator { get; set; } = string.Empty;

    [JsonPropertyName("creation_time")]
    public string CreationTime { get; set; } = string.Empty;

    [JsonPropertyName("last_change_time")]
    public string LastChangeTime { get; set; } = string.Empty;

    [JsonPropertyName("component")]
    public string Component { get; set; } = string.Empty;

    [JsonPropertyName("product")]
    public string Product { get; set; } = string.Empty;

    [JsonPropertyName("resolution")]
    public string Resolution { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("target_milestone")]
    public string TargetMilestone { get; set; } = string.Empty;

    [JsonPropertyName("op_sys")]
    public string OpSys { get; set; } = string.Empty;

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;
}

public class BugDetailResponse
{
    [JsonPropertyName("bugs")]
    public List<BugDetail> Bugs { get; set; } = [];
}

public class BugDetail : BugSummary
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("whiteboard")]
    public string Whiteboard { get; set; } = string.Empty;

    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = [];

    [JsonPropertyName("blocks")]
    public List<int> Blocks { get; set; } = [];

    [JsonPropertyName("depends_on")]
    public List<int> DependsOn { get; set; } = [];

    [JsonPropertyName("cc")]
    public List<string> Cc { get; set; } = [];

    [JsonPropertyName("flags")]
    public List<BugFlag> Flags { get; set; } = [];

    [JsonPropertyName("comments")]
    public List<BugComment> Comments { get; set; } = [];
}

public class BugFlag
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("setter")]
    public string Setter { get; set; } = string.Empty;
}

public class BugCommentResponse
{
    [JsonPropertyName("bugs")]
    public Dictionary<string, BugCommentData> Bugs { get; set; } = [];
}

public class BugCommentData
{
    [JsonPropertyName("comments")]
    public List<BugComment> Comments { get; set; } = [];
}

public class BugComment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("creator")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("creation_time")]
    public string CreationTime { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class SearchCriteria
{
    public string Product { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public string Reporter { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int Limit { get; set; } = 0;
    public bool NewestFirst { get; set; } = true;
}

public class AppSettings
{
    public string BugzillaUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
