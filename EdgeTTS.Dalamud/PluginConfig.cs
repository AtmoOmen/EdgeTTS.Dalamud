using Dalamud.Configuration;
using EdgeTTS.Models;
using Lumina.Data;
using OmenTools.OmenService;

namespace EdgeTTS.Dalamud;

[Serializable]
public sealed class PluginConfig : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    public EdgeTTSSettings Settings { get; set; } = new();

    public Language Language { get; set; } = GameState.ClientLanguge;
}
