using EdgeTTS.Models;

namespace EdgeTTS.Dalamud;

/// <summary>
///     生成音色的显示名, 直接使用接口返回的本地化音色名
/// </summary>
internal static class VoiceDisplayName
{
    public static string Get
    (
        VoiceInfo voice
    ) =>
        string.IsNullOrWhiteSpace(voice.LocalName) ?
            voice.FriendlyName ?? string.Empty :
            voice.LocalName;
}
