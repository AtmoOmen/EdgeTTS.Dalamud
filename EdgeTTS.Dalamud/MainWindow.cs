using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using EdgeTTS.Dalamud.ViewModels;
using EdgeTTS.Models;
using OmenTools.Extensions;

namespace EdgeTTS.Dalamud;

internal sealed class MainWindow : Window
{
    private readonly VoiceExplorerViewModel voiceVM;
    private readonly DashboardViewModel     dashboardVM;
    private readonly SettingsViewModel      settingsVM;

    public MainWindow() : base(Loc.Get("Window.Title"))
    {
        var settings = Plugin.PluginConfig.Settings;
        var engine   = Plugin.Engine;
        var save     = Plugin.Save;

        voiceVM     = new(engine, settings, save);
        dashboardVM = new(engine, settings, save);
        settingsVM  = new(engine, settings, save);

        Size          = new(800f, 650f);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        DrawHeader();

        ImGui.Spacing();

        using var tabBar = ImRaii.TabBar("EdgeTTSMainTabBar");
        if (!tabBar)
            return;

        using (var tab = ImRaii.TabItem(Loc.Get("Window.TabDashboard")))
        {
            if (tab)
                DrawDashboard();
        }

        using (var tab = ImRaii.TabItem(Loc.Get("Window.TabVoices")))
        {
            if (tab)
                DrawVoiceExplorer();
        }

        using (var tab = ImRaii.TabItem(Loc.Get("Window.TabSettings")))
        {
            if (tab)
                DrawSettings();
        }
    }

    private void DrawHeader()
    {
        var voice = dashboardVM.SelectedVoice;

        if (voice == null)
        {
            ImGui.TextDisabled(Loc.Get("Window.NoVoiceSelected"));
            ImGui.Separator();
            return;
        }
        
        ImGui.TextUnformatted($"{Loc.Get("Window.CurrentVoice")}: ");
        
        ImGui.SameLine();
        ImGui.TextColored(KnownColor.LightSkyBlue.ToUInt(), voice.FriendlyName);
        
        ImGui.SameLine();
        ImGui.TextDisabled($"({voice.LocaleInfo.DisplayName} / {Loc.Get($"Gender.{voice.Gender}")})");
        
        ImGui.Spacing();
        ImGui.Separator();
    }

    private void DrawDashboard()
    {
        dashboardVM.SyncFromSettings();

        using var child = ImRaii.Child("DashboardChild", new(0f, 0f), false, ImGuiWindowFlags.NoScrollbar);
        if (!child)
            return;

        // ── 基础参数 ──
        ImGui.TextUnformatted(Loc.Get("Window.VoiceOptions"));
        ImGui.Separator();

        DrawSliderWithDefault(Loc.Get("Window.Speed"),  "##SpeedSlider",  ref dashboardVM.SpeedRef,  1, 200);
        DrawSliderWithDefault(Loc.Get("Window.Pitch"),  "##PitchSlider",  ref dashboardVM.PitchRef,  1, 200);
        DrawSliderWithDefault(Loc.Get("Window.Volume"), "##VolumeSlider", ref dashboardVM.VolumeRef, 0, 100);

        ImGui.Spacing();
        ImGui.Separator();

        // ── 表现参数 ──
        var tag       = dashboardVM.CurrentVoiceTag;
        var hasStyles = tag.Styles.Count > 0;
        var hasRoles  = tag.Roles.Count  > 0;

        // 风格
        if (hasStyles)
            DrawSingleSelect(Loc.Get("Window.Style"), "StyleSelect", tag.Styles, settingsStyle, dashboardVM.SetStyle);
        else if (settingsStyle != null)
            dashboardVM.SetStyle(null);

        // 风格强度: 仅在有风格可选时显示
        if (hasStyles)
            DrawSliderWithDefault(Loc.Get("Window.StyleDegree"), "##StyleDegreeSlider", ref dashboardVM.StyleDegreeRef, 1, 200);

        // 角色
        if (hasRoles)
            DrawSingleSelect(Loc.Get("Window.Role"), "RoleSelect", tag.Roles, settingsRole, dashboardVM.SetRole);
        else if (settingsRole != null)
            dashboardVM.SetRole(null);

        // ── 多选标签 ──
        var changed = DrawMultiSelect
        (
            Loc.Get("Window.ContentCategories"),
            "ContentCategories",
            tag.ContentCategories,
            settingsContentCategories
        );

        if (changed)
            Plugin.Save();

        changed = DrawMultiSelect
        (
            Loc.Get("Window.VoicePersonalities"),
            "VoicePersonalities",
            tag.VoicePersonalities,
            settingsVoicePersonalities
        );

        if (changed)
            Plugin.Save();

        ImGui.Spacing();
        ImGui.Separator();

        // ── 测试文本与播放 ──
        ImGui.TextUnformatted(Loc.Get("Window.TestText"));

        var testText = dashboardVM.TestText;
        ImGui.SetNextItemWidth(-1f);

        if (ImGui.InputTextMultiline
            (
                "##EdgeTTSTestTextInput",
                ref testText,
                2048,
                new Vector2(-1f, 100f) * GlobalUIScale
            ))
            dashboardVM.TestText = testText;

        if (ImGui.Button(Loc.Get("Window.ReadTest"), new(ImGui.GetContentRegionAvail().X, 0f)))
            dashboardVM.SpeakTest();
    }

    private void DrawVoiceExplorer()
    {
        // 搜索栏
        var search = voiceVM.SearchQuery;
        ImGui.SetNextItemWidth(-1f);

        if (ImGui.InputTextWithHint("##VoiceSearchInput", Loc.Get("Window.SearchVoice"), ref search, 256))
            voiceVM.SearchQuery = search;

        ImGui.Spacing();

        // 语音树
        using var child = ImRaii.Child("VoiceTreeChild", new(0f, 0f), true);
        if (!child)
            return;

        var hasSearch = !string.IsNullOrWhiteSpace(voiceVM.SearchQuery.Trim());

        foreach (var (locale, genders) in voiceVM.GetFilteredTree())
        {
            var treeFlags = hasSearch ?
                                ImGuiTreeNodeFlags.DefaultOpen :
                                ImGuiTreeNodeFlags.None;

            using var localeNode = ImRaii.TreeNode(locale, treeFlags);
            if (!localeNode)
                continue;

            foreach (var (gender, voices) in genders)
            {
                using var genderNode = ImRaii.TreeNode($"{Loc.Get($"Gender.{gender}")}###{locale}-{gender}", treeFlags);
                if (!genderNode)
                    continue;

                foreach (var voice in voices)
                {
                    var selected = voiceVM.IsSelected(voice.ShortName);

                    if (ImGui.Selectable($"{voice.FriendlyName}###{voice.ShortName}", selected))
                    {
                        voiceVM.SelectVoice(voice);
                        ImGui.SetScrollHereY();
                    }

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip($"{voice.FriendlyName}\n{voice.ShortName}\n{voice.Gender} / {voice.Locale}");
                }
            }
        }
    }

    private void DrawSettings()
    {
        using var child = ImRaii.Child("SettingsChild", new(0f, 0f), false);
        if (!child)
            return;

        // ── 音素替换 ──
        ImGui.TextUnformatted(Loc.Get("Window.PhonemeReplacements"));
        ImGui.Separator();

        var replacements = settingsVM.Replacements;

        if (replacements.Count == 0)
            ImGui.TextDisabled(Loc.Get("Window.NoReplacements"));
        else
        {
            using var table = ImRaii.Table
            (
                "EdgeTTSPhonemeTable",
                3,
                ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit
            );

            if (table)
            {
                ImGui.TableSetupColumn(Loc.Get("Window.OriginalText"),    ImGuiTableColumnFlags.WidthStretch, 0.4f);
                ImGui.TableSetupColumn(Loc.Get("Window.ReplacementText"), ImGuiTableColumnFlags.WidthStretch, 0.4f);
                ImGui.TableSetupColumn
                (
                    "##Actions",
                    ImGuiTableColumnFlags.WidthFixed,
                    ImGui.CalcTextSize(Loc.Get("Window.Remove")).X + (20f * GlobalUIScale)
                );
                
                ImGui.TableHeadersRow();

                foreach (var pair in replacements.ToArray())
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(pair.Key);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(pair.Value);
                    ImGui.TableNextColumn();

                    if (ImGui.Button($"{Loc.Get("Window.Remove")}###Remove_{pair.Key}"))
                        settingsVM.RemoveReplacement(pair.Key);
                }
            }
        }

        ImGui.Spacing();

        // 添加表单
        {
            var key   = settingsVM.ReplacementKey;
            var value = settingsVM.ReplacementValue;

            ImGui.SetNextItemWidth(100f * GlobalUIScale);
            ImGui.InputTextWithHint("##ReplacementKeyInput", Loc.Get("Window.OriginalText"), ref key, 256);
            settingsVM.ReplacementKey = key;

            ImGui.SameLine();
            ImGui.SetNextItemWidth(100f * GlobalUIScale);
            ImGui.InputTextWithHint("##ReplacementValueInput", Loc.Get("Window.ReplacementText"), ref value, 256);
            settingsVM.ReplacementValue = value;

            ImGui.SameLine();

            if (ImGui.Button(Loc.Get("Window.AddReplacement")))
                settingsVM.AddReplacement();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ── 音频设备 ──
        ImGui.TextUnformatted(Loc.Get("Window.AudioDevice"));
        ImGui.Separator();
        ImGui.SetNextItemWidth(-1f);

        var currentDeviceName = settingsVM.GetDeviceDisplayName(settingsVM.SelectedDeviceId);

        using (var combo = ImRaii.Combo("##AudioDeviceCombo", currentDeviceName))
        {
            if (combo)
            {
                // 默认设备选项
                if (ImGui.Selectable(Loc.Get("Window.DefaultDevice"), settingsVM.SelectedDeviceId == -1))
                    settingsVM.SelectDevice(-1);

                foreach (var (id, device) in settingsVM.Devices)
                {
                    var label = $"{id + 1}. {device.Name}";

                    if (ImGui.Selectable(label, id == settingsVM.SelectedDeviceId))
                        settingsVM.SelectDevice(id);
                }
            }
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  复用渲染组件
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    ///     渲染带默认值标记的滑块, 变更时自动持久化
    /// </summary>
    private void DrawSliderWithDefault
    (
        string  label,
        string  id,
        ref int value,
        int     min,
        int     max
    )
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(label).X - ImGui.GetFrameHeight());

        if (ImGui.SliderInt($"{label}###{id}", ref value, min, max))
            dashboardVM.Flush();
    }

    /// <summary>
    ///     单选下拉框
    /// </summary>
    private static void DrawSingleSelect
    (
        string                label,
        string                id,
        IReadOnlyList<string> options,
        string?               currentValue,
        Action<string?>       onSelect
    )
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(label).X - ImGui.GetFrameHeight());
        using var combo = ImRaii.Combo($"{label}###{id}", currentValue ?? Loc.Get("Window.DefaultOption"));

        if (!combo)
            return;

        if (ImGui.Selectable(Loc.Get("Window.DefaultOption"), currentValue == null))
            onSelect(null);

        foreach (var option in options)
        {
            if (ImGui.Selectable(option, string.Equals(currentValue, option, StringComparison.OrdinalIgnoreCase)))
                onSelect(option);
        }
    }

    /// <summary>
    ///     多选下拉框, 返回是否有变更
    /// </summary>
    private static bool DrawMultiSelect
    (
        string                label,
        string                id,
        IReadOnlyList<string> options,
        List<string>          values
    )
    {
        var changed = false;
        var preview = values.Count == 0 ?
                          Loc.Get("Window.NoneSelected") :
                          string.Join(", ", values);

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(label).X - ImGui.GetFrameHeight());
        using var combo = ImRaii.Combo($"{label}###{id}", preview, ImGuiComboFlags.HeightLargest);

        if (!combo)
            return false;

        if (options.Count == 0)
        {
            ImGui.TextDisabled(Loc.Get("Window.NoOptionsAvailable"));
            return false;
        }

        foreach (var option in options)
        {
            var selected = values.Contains(option, StringComparer.OrdinalIgnoreCase);

            if (!ImGui.Selectable(option, selected, ImGuiSelectableFlags.DontClosePopups))
                continue;

            if (selected)
                values.RemoveAll(v => string.Equals(v, option, StringComparison.OrdinalIgnoreCase));
            else
                values.Add(option);

            changed = true;
        }

        return changed;
    }

    // ════════════════════════════════════════════════════════════════
    //  快捷属性 — settings 字段的简写
    // ════════════════════════════════════════════════════════════════

    private EdgeTTSSettings settings                   => Plugin.PluginConfig.Settings;
    private string?         settingsStyle              => settings.Style;
    private string?         settingsRole               => settings.Role;
    private List<string>    settingsContentCategories  => settings.ContentCategories;
    private List<string>    settingsVoicePersonalities => settings.VoicePersonalities;
}
