using OmenTools.Dalamud.Attributes;

namespace EdgeTTS.Dalamud;

public static class PluginIPC
{
    [IPCProvider("EdgeTTS.Speak")]
    public static void Speak
    (
        string text
    ) =>
        Plugin.Speak(text);

    [IPCProvider("EdgeTTS.SpeakWithOptions")]
    public static void SpeakWithOptions
    (
        string  text,
        object? speed  = null,
        object? pitch  = null,
        object? volume = null
    ) =>
        Plugin.Speak(text, ToOption(speed), ToOption(pitch), ToOption(volume));

    [IPCProvider("EdgeTTS.SpeakAsync")]
    public static Task SpeakAsync
    (
        string            text,
        CancellationToken cancellationToken
    ) =>
        Plugin.SpeakAsync(text, cancellationToken);

    [IPCProvider("EdgeTTS.SpeakWithOptionsAsync")]
    public static Task SpeakWithOptionsAsync
    (
        string            text,
        object?           speed             = null,
        object?           pitch             = null,
        object?           volume            = null,
        CancellationToken cancellationToken = default
    ) =>
        Plugin.SpeakAsync(text, ToOption(speed), ToOption(pitch), ToOption(volume), cancellationToken);

    [IPCProvider("EdgeTTS.Synthesize")]
    public static void Synthesize
    (
        string text
    ) =>
        Plugin.Synthesize(text);

    [IPCProvider("EdgeTTS.SynthesizeWithOptions")]
    public static void SynthesizeWithOptions
    (
        string  text,
        object? speed  = null,
        object? pitch  = null,
        object? volume = null
    ) =>
        Plugin.Synthesize(text, ToOption(speed), ToOption(pitch), ToOption(volume));

    [IPCProvider("EdgeTTS.SynthesizeAsync")]
    public static Task SynthesizeAsync
    (
        string            text,
        CancellationToken cancellationToken
    ) =>
        Plugin.SynthesizeAsync(text, cancellationToken);

    [IPCProvider("EdgeTTS.SynthesizeWithOptionsAsync")]
    public static Task SynthesizeWithOptionsAsync
    (
        string            text,
        object?           speed             = null,
        object?           pitch             = null,
        object?           volume            = null,
        CancellationToken cancellationToken = default
    ) =>
        Plugin.SynthesizeAsync(text, ToOption(speed), ToOption(pitch), ToOption(volume), cancellationToken);

    private static int? ToOption
    (
        object? value
    ) =>
        value as int?;
}
