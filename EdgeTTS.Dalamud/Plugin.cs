using Dalamud.Plugin;
using EdgeTTS.Models;
using OmenTools;
using OmenTools.Dalamud;
using OmenTools.OmenService;

namespace EdgeTTS.Dalamud;

public sealed class Plugin : IAsyncDalamudPlugin
{
    private static IDalamudPluginInterface pluginInterface = null!;

    internal static PluginConfig PluginConfig { get; private set; } = null!;

    internal static EdgeTTSEngine Engine { get; private set; } = null!;

    public Plugin
    (
        IDalamudPluginInterface pi
    ) =>
        pluginInterface = pi;

    public Task LoadAsync
    (
        CancellationToken cancellationToken
    )
    {
        DService.Init(pluginInterface);
        
        Loc.Initialize(Path.Combine(pluginInterface.AssemblyLocation.DirectoryName, "Assets", "Langs"));
        
        PluginConfig = pluginInterface.GetPluginConfig() as PluginConfig ?? new();
        
        Engine = new
        (
            Path.Combine(pluginInterface.GetPluginConfigDirectory(), "Cache"),
            pluginInterface.AssemblyLocation.DirectoryName,
            DLog.Debug
        );
        
        Save();
        
        WindowManager.Instance().AddWindow<MainWindow>();
        
        CommandManager.Instance().MainCommand  =  new("/edgetts", new(OnCommand) { HelpMessage = Loc.Get("Command.MainHelp") });
        
        pluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        
        IPCAttributeRegistry.RegStaticIPCs(typeof(PluginIPC));
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IPCAttributeRegistry.UnregStaticIPCs(typeof(PluginIPC));
        pluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        Engine.Dispose();
        DService.Uninit();
        return ValueTask.CompletedTask;
    }

    internal static void Save() =>
        pluginInterface.SavePluginConfig(PluginConfig);

    internal static void Speak
    (
        string text
    ) =>
        Speak(text, null, null, null);

    internal static void Speak
    (
        string text,
        int    speed,
        int    pitch,
        int    volume
    ) =>
        Speak(text, speed, pitch, (int?)volume);

    internal static void Speak
    (
        string text,
        int?   speed,
        int?   pitch,
        int?   volume
    )
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var settings = CopySettings(PluginConfig.Settings);
        settings.Speed  = Math.Clamp(speed  ?? settings.Speed,  1, 200);
        settings.Pitch  = Math.Clamp(pitch  ?? settings.Pitch,  1, 200);
        settings.Volume = Math.Clamp(volume ?? settings.Volume, 0, 100);
        Engine.Speak(text, settings);
    }

    internal static Task SpeakAsync
    (
        string            text,
        CancellationToken cancellationToken
    ) =>
        SpeakAsync(text, null, null, null, cancellationToken);

    internal static Task SpeakAsync
    (
        string            text,
        int               speed,
        int               pitch,
        int               volume,
        CancellationToken cancellationToken
    ) =>
        SpeakAsync(text, speed, pitch, (int?)volume, cancellationToken);

    internal static async Task SpeakAsync
    (
        string            text,
        int?              speed,
        int?              pitch,
        int?              volume,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var settings = CopySettings(PluginConfig.Settings);
        settings.Speed  = Math.Clamp(speed  ?? settings.Speed,  1, 200);
        settings.Pitch  = Math.Clamp(pitch  ?? settings.Pitch,  1, 200);
        settings.Volume = Math.Clamp(volume ?? settings.Volume, 0, 100);
        await Engine.SpeakAsync(text, settings, cancellationToken).ConfigureAwait(false);
    }

    internal static void Synthesize
    (
        string text
    ) =>
        Synthesize(text, null, null, null);

    internal static void Synthesize
    (
        string text,
        int    speed,
        int    pitch,
        int    volume
    ) =>
        Synthesize(text, speed, pitch, (int?)volume);

    internal static void Synthesize
    (
        string text,
        int?   speed,
        int?   pitch,
        int?   volume
    )
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var settings = CopySettings(PluginConfig.Settings);
        settings.Speed  = Math.Clamp(speed  ?? settings.Speed,  1, 200);
        settings.Pitch  = Math.Clamp(pitch  ?? settings.Pitch,  1, 200);
        settings.Volume = Math.Clamp(volume ?? settings.Volume, 0, 100);
        Engine.Synthesize(text, settings);
    }

    internal static Task SynthesizeAsync
    (
        string            text,
        CancellationToken cancellationToken
    ) =>
        SynthesizeAsync(text, null, null, null, cancellationToken);

    internal static Task SynthesizeAsync
    (
        string            text,
        int               speed,
        int               pitch,
        int               volume,
        CancellationToken cancellationToken
    ) =>
        SynthesizeAsync(text, speed, pitch, (int?)volume, cancellationToken);

    internal static Task SynthesizeAsync
    (
        string            text,
        int?              speed,
        int?              pitch,
        int?              volume,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(text))
            return Task.CompletedTask;

        var settings = CopySettings(PluginConfig.Settings);
        settings.Speed  = Math.Clamp(speed  ?? settings.Speed,  1, 200);
        settings.Pitch  = Math.Clamp(pitch  ?? settings.Pitch,  1, 200);
        settings.Volume = Math.Clamp(volume ?? settings.Volume, 0, 100);
        return Engine.SynthesizeAsync(text, settings, cancellationToken);
    }

    private static EdgeTTSSettings CopySettings
    (
        EdgeTTSSettings source
    ) =>
        new()
        {
            Voice               = source.Voice,
            Speed               = source.Speed,
            Pitch               = source.Pitch,
            Volume              = source.Volume,
            DeviceID            = source.DeviceID,
            Style               = source.Style,
            StyleDegree         = source.StyleDegree,
            Role                = source.Role,
            ContentCategories   = [.. source.ContentCategories],
            VoicePersonalities  = [.. source.VoicePersonalities],
            PhonemeReplacements = new(source.PhonemeReplacements)
        };

    private static void OpenConfigUi()
    {
        if (WindowManager.Instance().Get<MainWindow>() is { } window)
            window.IsOpen = true;
    }

    private static void OnCommand
    (
        string command,
        string args
    )
    {
        if (WindowManager.Instance().Get<MainWindow>() is { } window)
            window.IsOpen ^= true;
    }
}
