using System.Globalization;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using EdgeTTS.Models;

namespace EdgeTTS.Dalamud;

internal sealed class MainWindow : Window
{
    private const string TEST_TEXT = "异国的诗人提出疑问。如果欧米茄作为兵器不断地获得力量并且一直战斗下去的话, 它真的能够找到渴求的答案吗?";

    private string testText = TEST_TEXT;
    private string replacementKey = string.Empty;
    private string replacementValue = string.Empty;
    private string voiceSearchQuery = string.Empty;
    private bool optionsInitialized;

    public MainWindow() : base(Loc.Get("Window.Title"))
    {
        Size = new(720f, 600f);
        SizeCondition = ImGuiCond.FirstUseEver;
        Flags = ImGuiWindowFlags.NoCollapse;
    }

    public override void Draw()
    {
        var settings = Plugin.PluginConfig.Settings;

        if (!optionsInitialized)
        {
            testText = TEST_TEXT;
            optionsInitialized = true;
        }

        DrawHeader(settings);
        
        ImGui.Spacing();

        using var tabBar = ImRaii.TabBar("EdgeTTSMainTabBar");
        if (!tabBar)
            return;

        using (var tab = ImRaii.TabItem(Loc.Get("Window.TabVoices")))
        {
            if (tab)
                DrawVoiceExplorer(settings);
        }

        using (var tab = ImRaii.TabItem(Loc.Get("Window.TabOptions")))
        {
            if (tab)
                DrawVoiceOptions(settings);
        }

        using (var tab = ImRaii.TabItem(Loc.Get("Window.TabPhonemes")))
        {
            if (tab)
                DrawPhonemeReplacements(settings);
        }

        using (var tab = ImRaii.TabItem(Loc.Get("Window.TabDevice")))
        {
            if (tab)
                DrawAudioDevice(settings);
        }
    }

    private static void DrawHeader
    (
        EdgeTTSSettings settings
    )
    {
        var selectedVoice = GetSelectedVoice(settings);
        var voiceName = selectedVoice?.FriendlyName ?? settings.Voice;
        var locale = selectedVoice?.Locale ?? string.Empty;
        var gender = selectedVoice?.GenderName ?? string.Empty;

        ImGui.TextUnformatted($"{Loc.Get("Window.CurrentVoice")}: {voiceName}");
        if (!string.IsNullOrEmpty(locale))
        {
            ImGui.SameLine();
            ImGui.TextDisabled($"({new CultureInfo(locale).DisplayName} / {gender})");
        }
    }
    
    private void DrawVoiceExplorer
    (
        EdgeTTSSettings settings
    )
    {
        ImGui.SetNextItemWidth(-1f);
        ImGui.InputTextWithHint("##VoiceSearchInput", Loc.Get("Window.SearchVoice"), ref voiceSearchQuery, 256);
        ImGui.Spacing();

        using var child = ImRaii.Child("VoiceTreeChild", new(0f, 0f), true);
        if (!child)
            return;

        var search = voiceSearchQuery.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        foreach (var (locale, genders) in Plugin.Engine.Voices)
        {
            var localeMatches = hasSearch && locale.Contains(search, StringComparison.OrdinalIgnoreCase);

            var matchesInLocale = false;
            if (hasSearch && !localeMatches)
            {
                foreach (var voiceList in genders.Values)
                {
                    if (voiceList.Any(v => v.FriendlyName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                           v.ShortName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                           v.Gender.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    {
                        matchesInLocale = true;
                        break;
                    }
                }

                if (!matchesInLocale)
                    continue;
            }

            var treeFlags = hasSearch ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
            using var localeNode = ImRaii.TreeNode(locale, treeFlags);
            if (!localeNode)
                continue;

            foreach (var (gender, voices) in genders)
            {
                IReadOnlyList<VoiceInfo> matchingVoices = hasSearch && !localeMatches ?
                    voices.Where(v => v.FriendlyName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                      v.ShortName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                      v.Gender.Contains(search, StringComparison.OrdinalIgnoreCase)).ToArray() :
                    voices;

                if (hasSearch && !localeMatches && matchingVoices.Count == 0)
                    continue;

                using var genderNode = ImRaii.TreeNode($"{gender}###{locale}-{gender}", treeFlags);
                if (!genderNode)
                    continue;

                foreach (var voice in matchingVoices)
                {
                    var selected = string.Equals(settings.Voice, voice.ShortName, StringComparison.OrdinalIgnoreCase);

                    if (ImGui.Selectable($"{voice.FriendlyName}###{voice.ShortName}", selected))
                    {
                        settings.Voice = voice.ShortName;
                        var tag = voice.VoiceTag ?? new();
                        settings.ContentCategories.RemoveAll(value => !tag.ContentCategories.Contains(value, StringComparer.OrdinalIgnoreCase));
                        settings.VoicePersonalities.RemoveAll(value => !tag.VoicePersonalities.Contains(value, StringComparer.OrdinalIgnoreCase));
                        if (tag.Styles.Count == 0 || !tag.Styles.Contains(settings.Style ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                            settings.Style = null;
                        if (tag.Roles.Count == 0 || !tag.Roles.Contains(settings.Role ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                            settings.Role = null;
                        Plugin.Save();
                    }

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip($"{voice.FriendlyName}\n{voice.ShortName}");
                }
            }
        }
    }

    private void DrawVoiceOptions
    (
        EdgeTTSSettings settings
    )
    {
        using var child = ImRaii.Child("VoiceOptionsChild", new(0f, 0f), false);
        if (!child)
            return;

        var changed = false;

        ImGui.TextUnformatted(Loc.Get("Window.VoiceOptions"));
        ImGui.Separator();

        var speed = settings.Speed;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 80f);
        if (ImGui.SliderInt(Loc.Get("Window.Speed"), ref speed, 1, 200))
        {
            settings.Speed = speed;
            changed = true;
        }

        var pitch = settings.Pitch;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 80f);
        if (ImGui.SliderInt(Loc.Get("Window.Pitch"), ref pitch, 1, 200))
        {
            settings.Pitch = pitch;
            changed = true;
        }

        var volume = settings.Volume;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 80f);
        if (ImGui.SliderInt(Loc.Get("Window.Volume"), ref volume, 0, 100))
        {
            settings.Volume = volume;
            changed = true;
        }

        ImGui.Spacing();
        ImGui.Separator();

        var voiceTag = GetSelectedVoice(settings)?.VoiceTag ?? new();
        var style = settings.Style;

        if (voiceTag.Styles.Count != 0 && DrawSingleSelection(Loc.Get("Window.Style"), "Style", voiceTag.Styles, ref style))
        {
            settings.Style = style;
            changed = true;
        }
        else if (voiceTag.Styles.Count == 0 && settings.Style != null)
        {
            settings.Style = null;
            changed = true;
        }

        var styleDegree = settings.StyleDegree;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 80f);
        if (ImGui.SliderInt(Loc.Get("Window.StyleDegree"), ref styleDegree, 1, 200))
        {
            settings.StyleDegree = styleDegree;
            changed = true;
        }

        var role = settings.Role;

        if (voiceTag.Roles.Count != 0 && DrawSingleSelection(Loc.Get("Window.Role"), "Role", voiceTag.Roles, ref role))
        {
            settings.Role = role;
            changed = true;
        }
        else if (voiceTag.Roles.Count == 0 && settings.Role != null)
        {
            settings.Role = null;
            changed = true;
        }

        changed |= DrawMultiSelection(Loc.Get("Window.ContentCategories"), "ContentCategories", voiceTag.ContentCategories, settings.ContentCategories);
        changed |= DrawMultiSelection(Loc.Get("Window.VoicePersonalities"), "VoicePersonalities", voiceTag.VoicePersonalities, settings.VoicePersonalities);

        ImGui.Spacing();
        ImGui.Separator();

        ImGui.TextUnformatted(Loc.Get("Window.TestText"));
        ImGui.InputTextMultiline("##EdgeTTSTestTextInput", ref testText, 2048, new(-1f, 100f));

        if (ImGui.Button(Loc.Get("Window.ReadTest")))
            Plugin.Speak(testText);

        if (changed)
            Plugin.Save();
    }

    private void DrawPhonemeReplacements
    (
        EdgeTTSSettings settings
    )
    {
        using var child = ImRaii.Child("PhonemeChild", new(0f, 0f), false);
        if (!child)
            return;

        ImGui.TextUnformatted(Loc.Get("Window.PhonemeReplacements"));
        ImGui.Separator();

        using (var table = ImRaii.Table("EdgeTTSPhonemeTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            if (table)
            {
                ImGui.TableSetupColumn(Loc.Get("Window.OriginalText"), ImGuiTableColumnFlags.WidthStretch, 0.4f);
                ImGui.TableSetupColumn(Loc.Get("Window.ReplacementText"), ImGuiTableColumnFlags.WidthStretch, 0.4f);
                ImGui.TableSetupColumn(Loc.Get("Window.Remove"), ImGuiTableColumnFlags.WidthFixed, 80f);
                ImGui.TableHeadersRow();

                foreach (var pair in settings.PhonemeReplacements.ToArray())
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(pair.Key);
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(pair.Value);
                    ImGui.TableNextColumn();

                    if (ImGui.Button($"{Loc.Get("Window.Remove")}###{pair.Key}"))
                    {
                        settings.PhonemeReplacements.Remove(pair.Key);
                        Plugin.Save();
                    }
                }
            }
        }

        ImGui.Spacing();
        ImGui.SetNextItemWidth(180f);
        ImGui.InputText("##EdgeTTSReplacementKey", ref replacementKey, 256);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(180f);
        ImGui.InputText("##EdgeTTSReplacementValue", ref replacementValue, 256);
        ImGui.SameLine();

        if (ImGui.Button(Loc.Get("Window.AddReplacement")) && !string.IsNullOrWhiteSpace(replacementKey))
        {
            settings.PhonemeReplacements[replacementKey] = replacementValue;
            replacementKey = string.Empty;
            replacementValue = string.Empty;
            Plugin.Save();
        }
    }

    private static void DrawAudioDevice
    (
        EdgeTTSSettings settings
    )
    {
        using var child = ImRaii.Child("AudioDeviceChild", new(0f, 0f), false);
        if (!child)
            return;

        ImGui.TextUnformatted(Loc.Get("Window.AudioDevice"));
        ImGui.Separator();

        var changed = false;
        ImGui.SetNextItemWidth(350f);
        if (ImGui.BeginCombo("##AudioDeviceCombo", GetAudioDeviceName(settings.DeviceID)))
        {
            foreach (var (id, device) in Plugin.Engine.AudioDevices)
            {
                if (ImGui.Selectable($"{id + 1}. {device.Name}", id == settings.DeviceID))
                {
                    settings.DeviceID = id;
                    changed = true;
                }
            }

            ImGui.EndCombo();
        }

        if (changed)
            Plugin.Save();
    }

    private static bool DrawSingleSelection
    (
        string label,
        string id,
        IReadOnlyList<string> options,
        ref string? value
    )
    {
        var changed = false;
        using var combo = ImRaii.Combo($"{label}###{id}", value ?? string.Empty);

        if (combo)
        {
            foreach (var option in options)
            {
                if (ImGui.Selectable(option, string.Equals(value, option, StringComparison.OrdinalIgnoreCase)))
                {
                    value = option;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static bool DrawMultiSelection
    (
        string label,
        string id,
        IReadOnlyList<string> options,
        List<string> values
    )
    {
        var changed = false;
        var preview = values.Count == 0 ?
            string.Empty :
            string.Join(", ", values);
        using var combo = ImRaii.Combo($"{label}###{id}", preview, ImGuiComboFlags.HeightLargest);

        if (combo)
        {
            foreach (var option in options)
            {
                var selected = values.Contains(option, StringComparer.OrdinalIgnoreCase);
                if (!ImGui.Selectable(option, selected, ImGuiSelectableFlags.DontClosePopups))
                    continue;

                if (selected)
                    values.RemoveAll(value => string.Equals(value, option, StringComparison.OrdinalIgnoreCase));
                else
                    values.Add(option);
                changed = true;
            }
        }

        return changed;
    }

    private static VoiceInfo? GetSelectedVoice
    (
        EdgeTTSSettings settings
    ) =>
        Plugin.Engine.Voices.Values
            .SelectMany(genders => genders.Values)
            .SelectMany(voices => voices)
            .FirstOrDefault(voice => string.Equals(voice.ShortName, settings.Voice, StringComparison.OrdinalIgnoreCase));

    private static string GetAudioDeviceName
    (
        int deviceId
    ) =>
        Plugin.Engine.AudioDevices.TryGetValue(deviceId, out var device) ?
            $"{deviceId + 1}. {device.Name}" :
            Loc.Get("Window.DefaultDevice");
}
