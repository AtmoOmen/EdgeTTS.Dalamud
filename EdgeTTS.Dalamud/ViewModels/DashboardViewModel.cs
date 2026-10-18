using EdgeTTS.Models;

namespace EdgeTTS.Dalamud.ViewModels;

/// <summary>
///     合成面板的状态与逻辑, 管理滑块、测试文本与播放
/// </summary>
internal sealed class DashboardViewModel
{
    private const string DefaultTestText = "异国的诗人提出疑问。如果欧米茄作为兵器不断地获得力量并且一直战斗下去的话, 它真的能够找到渴求的答案吗?";

    private readonly EdgeTTSEngine   engine;
    private readonly EdgeTTSSettings settings;
    private readonly Action          save;

    // 本地可变副本, 供 ImGui 滑块 ref 绑定
    private int speed;
    private int pitch;
    private int volume;

    public DashboardViewModel
    (
        EdgeTTSEngine   engine,
        EdgeTTSSettings settings,
        Action          save
    )
    {
        this.engine   = engine;
        this.settings = settings;
        this.save     = save;
        SyncFromSettings();
    }

    /// <summary>
    ///     测试文本
    /// </summary>
    public string TestText { get; set; } = DefaultTestText;

    /// <summary>
    ///     当前选中音色
    /// </summary>
    public VoiceInfo? SelectedVoice =>
        engine.Voices.Values
              .SelectMany(g => g.Values)
              .SelectMany(v => v)
              .FirstOrDefault(v => string.Equals(v.ShortName, settings.Voice, StringComparison.OrdinalIgnoreCase));

    // ── 参数 ref 属性, 指向本地可变副本供 ImGui 滑块直接修改 ──

    public ref int SpeedRef  => ref speed;
    public ref int PitchRef  => ref pitch;
    public ref int VolumeRef => ref volume;

    // ── 从 settings 同步本地副本 ──

    public void SyncFromSettings()
    {
        speed  = settings.Speed;
        pitch  = settings.Pitch;
        volume = settings.Volume;
    }

    /// <summary>
    ///     朗读测试文本
    /// </summary>
    public void SpeakTest()
    {
        if (!string.IsNullOrWhiteSpace(TestText))
            Plugin.Speak(TestText);
    }

    /// <summary>
    ///     将本地副本写回 settings 并持久化 (仅在值变更时)
    /// </summary>
    public void Flush()
    {
        var dirty = false;

        if (speed != settings.Speed)
        {
            settings.Speed = speed;
            dirty          = true;
        }

        if (pitch != settings.Pitch)
        {
            settings.Pitch = pitch;
            dirty          = true;
        }

        if (volume != settings.Volume)
        {
            settings.Volume = volume;
            dirty           = true;
        }

        if (dirty)
            save();
    }
}
