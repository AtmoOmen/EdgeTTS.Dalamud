using EdgeTTS.Models;

namespace EdgeTTS.Dalamud.ViewModels;

/// <summary>
///     设置面板的状态与逻辑, 管理音素替换表和音频设备选择
/// </summary>
internal sealed class SettingsViewModel
{
    private readonly EdgeTTSEngine   engine;
    private readonly EdgeTTSSettings settings;
    private readonly Action          save;

    public SettingsViewModel
    (
        EdgeTTSEngine   engine,
        EdgeTTSSettings settings,
        Action          save
    )
    {
        this.engine   = engine;
        this.settings = settings;
        this.save     = save;
    }

    // ── 音素替换 ──

    /// <summary>
    ///     新增替换的键, 由 UI 双向绑定
    /// </summary>
    public string ReplacementKey { get; set; } = string.Empty;

    /// <summary>
    ///     新增替换的值, 由 UI 双向绑定
    /// </summary>
    public string ReplacementValue { get; set; } = string.Empty;

    /// <summary>
    ///     当前所有音素替换条目
    /// </summary>
    public Dictionary<string, string> Replacements => settings.PhonemeReplacements;

    /// <summary>
    ///     添加替换, key 为空时静默忽略
    /// </summary>
    public void AddReplacement()
    {
        if (string.IsNullOrWhiteSpace(ReplacementKey))
            return;

        settings.PhonemeReplacements[ReplacementKey] = ReplacementValue;
        ReplacementKey                                = string.Empty;
        ReplacementValue                              = string.Empty;
        save();
    }

    /// <summary>
    ///     删除替换条目
    /// </summary>
    public void RemoveReplacement
    (
        string key
    )
    {
        settings.PhonemeReplacements.Remove(key);
        save();
    }

    // ── 音频设备 ──

    /// <summary>
    ///     所有可用音频设备
    /// </summary>
    public Dictionary<int, AudioDevice> Devices => engine.AudioDevices;

    /// <summary>
    ///     当前选中的设备 ID
    /// </summary>
    public int SelectedDeviceId => settings.DeviceID;

    /// <summary>
    ///     切换到指定设备
    /// </summary>
    public void SelectDevice
    (
        int deviceId
    )
    {
        if (settings.DeviceID == deviceId)
            return;

        settings.DeviceID = deviceId;
        save();
    }

    /// <summary>
    ///     获取设备的显示名称
    /// </summary>
    public string GetDeviceDisplayName
    (
        int deviceId
    ) =>
        engine.AudioDevices.TryGetValue(deviceId, out var device) ?
            $"{deviceId + 1}. {device.Name}" :
            Loc.Get("Window.DefaultDevice");
}
