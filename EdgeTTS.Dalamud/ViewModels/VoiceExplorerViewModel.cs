using EdgeTTS.Models;

namespace EdgeTTS.Dalamud.ViewModels;

/// <summary>
///     音色浏览器的状态与逻辑, 负责搜索过滤、语音选择及副作用清理
/// </summary>
internal sealed class VoiceExplorerViewModel
(
    EdgeTTSEngine   engine,
    EdgeTTSSettings settings,
    Action          save
)
{
    /// <summary>
    ///     搜索查询文本, 由 UI 双向绑定
    /// </summary>
    public string SearchQuery { get; set; } = string.Empty;

    /// <summary>
    ///     扁平化的全部语音列表 (懒加载缓存)
    /// </summary>
    public IReadOnlyList<VoiceInfo> AllVoices
    {
        get
        {
            if (field != null)
                return field;

            return field = engine.Voices.Values
                                 .SelectMany(g => g.Values)
                                 .SelectMany(v => v)
                                 .ToArray();
        }
    }

    /// <summary>
    ///     当前选中的音色, 若短名称不匹配则返回 null
    /// </summary>
    public VoiceInfo? SelectedVoice =>
        AllVoices.FirstOrDefault(v => string.Equals(v.ShortName, settings.Voice, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    ///     按搜索词过滤后的分组树: 地区 → (性别, 语音列表)
    /// </summary>
    public IEnumerable<(string Locale, IEnumerable<(string Gender, IReadOnlyList<VoiceInfo> Voices)> Genders)> GetFilteredTree()
    {
        var query   = SearchQuery.Trim();
        var noQuery = string.IsNullOrWhiteSpace(query);

        foreach (var (locale, genders) in engine.Voices)
        {
            var localeMatches   = !noQuery && locale.Contains(query, StringComparison.OrdinalIgnoreCase);
            var filteredGenders = new List<(string Gender, IReadOnlyList<VoiceInfo> Voices)>();

            foreach (var (gender, voices) in genders)
            {
                IReadOnlyList<VoiceInfo> matching;

                if (noQuery || localeMatches)
                    matching = voices;
                else
                {
                    matching = voices.Where
                    (v => v.FriendlyName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                          v.ShortName.Contains(query, StringComparison.OrdinalIgnoreCase)    ||
                          v.Gender.Contains(query, StringComparison.OrdinalIgnoreCase)
                    ).ToArray();

                    if (matching.Count == 0)
                        continue;
                }

                filteredGenders.Add((gender, matching));
            }

            if (filteredGenders.Count == 0)
                continue;

            yield return (locale, filteredGenders);
        }
    }

    /// <summary>
    ///     选中指定音色, 处理兼容性清理并持久化
    /// </summary>
    public void SelectVoice
    (
        VoiceInfo voice
    )
    {
        settings.SelectVoice(voice);
        save();
    }

    /// <summary>
    ///     指定短名称的语音是否为当前选中项
    /// </summary>
    public bool IsSelected
    (
        string shortName
    ) =>
        string.Equals(settings.Voice, shortName, StringComparison.OrdinalIgnoreCase);
}
