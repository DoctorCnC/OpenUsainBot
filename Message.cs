using System.Text.Json.Serialization;

namespace OpenCryptShot
{
  public class Message
  {
    [JsonInclude]
    public string id { get; set; }

    [JsonIgnore]
    public int type { get; set; }

    [JsonInclude]
    public string content { get; set; }

    [JsonIgnore]
    public string channel_id { get; set; }

    [JsonIgnore]
    public string[][] author { get; set; }

    [JsonIgnore]
    public string[] attachments { get; set; }

    [JsonIgnore]
    public string[] embeds { get; set; }

    [JsonIgnore]
    public string[] mentions { get; set; }

    [JsonIgnore]
    public string[] mention_roles { get; set; }

    [JsonIgnore]
    public bool pinned { get; set; }

    [JsonIgnore]
    public bool mention_everyone { get; set; }

    [JsonIgnore]
    public bool tts { get; set; }

    [JsonIgnore]
    public string timestamp { get; set; }

    [JsonIgnore]
    public string edited_timestamp { get; set; }

    [JsonIgnore]
    public int flags { get; set; }
  }
}
