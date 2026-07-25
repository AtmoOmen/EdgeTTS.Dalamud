using System.Collections.Frozen;
using Lumina.Data;
using OmenTools.Localization;
using OmenTools.Localization.Parsers;
using OmenTools.OmenService;

namespace EdgeTTS.Dalamud;

internal static class Lang
{
    public static void Initialize
    (
        string directory
    ) =>
        LocalizationManager.Instance().Configure
        (
            new()
            {
                SupportedLanguages = new Dictionary<Language, string>
                {
                    [Language.ChineseSimplified]  = "简体中文",
                    [Language.TraditionalChinese] = "繁體中文",
                    [Language.Japanese]           = "日本語",
                    [Language.Korean]             = "한국어",
                    [Language.English]            = "English",
                    [Language.French]             = "Français",
                    [Language.German]             = "Deutsch"
                }.ToFrozenDictionary(),
                DefaultLanguage  = Language.ChineseSimplified,
                FileNameResolver = static language =>
                {
                    if (language == Language.ChineseTraditional)
                        language = Language.TraditionalChinese;
                    return $"{language}.json";
                },
                Source           = new FileLocalizationSource(directory),
                Parser           = new JsonDictionaryLocalizationParser(),
                FallbackResolver = static _ => [],
                LoggerTag        = "EdgeTTS.Dalamud"
            },
            Language.ChineseSimplified,
            Language.ChineseSimplified
        );

    public static string Get
    (
        string key
    ) =>
        LocalizationManager.Instance().Get(key);
}
