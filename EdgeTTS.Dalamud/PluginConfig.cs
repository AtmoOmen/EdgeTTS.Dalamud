using Dalamud.Configuration;
using EdgeTTS.Models;

namespace EdgeTTS.Dalamud;

[Serializable]
public sealed class PluginConfig : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public EdgeTTSSettings Settings { get; set; } = new();
}
