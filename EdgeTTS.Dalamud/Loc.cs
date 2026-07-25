using System.Collections.Frozen;
using Lumina.Data;
using OmenTools.Localization;
using OmenTools.Localization.Parsers;
using OmenTools.OmenService;

namespace EdgeTTS.Dalamud;

internal static class Loc
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
                    [Language.ChineseSimplified] = "简体中文"
                }.ToFrozenDictionary(),
                DefaultLanguage  = Language.ChineseSimplified,
                FileNameResolver = static language => $"{language}.json",
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
