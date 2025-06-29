using Newtonsoft.Json;

namespace JxAdScrcpy.Models
{
    public class CommandRequest
    {
        [JsonProperty("action")]
        public string Action { get; set; } = string.Empty;

        [JsonProperty("comm")]
        public CommandParams? Comm { get; set; }
    }

    public class CommandParams
    {
        [JsonProperty("deviceIds")]
        public string DeviceIds { get; set; } = string.Empty;

        [JsonProperty("savePath")]
        public string? SavePath { get; set; }

        [JsonProperty("onlyDeviceName")]
        public int? OnlyDeviceName { get; set; }

        [JsonProperty("mask")]
        public string? Mask { get; set; }

        [JsonProperty("x")]
        public string? X { get; set; }

        [JsonProperty("y")]
        public string? Y { get; set; }

        [JsonProperty("endx")]
        public string? EndX { get; set; }

        [JsonProperty("endy")]
        public string? EndY { get; set; }

        [JsonProperty("delta")]
        public string? Delta { get; set; }

        [JsonProperty("content")]
        public string? Content { get; set; }

        [JsonProperty("command")]
        public string? Command { get; set; }

        [JsonProperty("filePath")]
        public string? FilePath { get; set; }
    }
}