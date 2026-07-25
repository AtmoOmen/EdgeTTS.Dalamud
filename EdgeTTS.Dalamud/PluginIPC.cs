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
        string text,
        int?   speed = null,
        int?   pitch = null,
        int?   volume = null
    ) =>
        Plugin.Speak(text, speed, pitch, volume);

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
        int?              speed = null,
        int?              pitch = null,
        int?              volume = null,
        CancellationToken cancellationToken = default
    ) =>
        Plugin.SpeakAsync(text, speed, pitch, volume, cancellationToken);

    [IPCProvider("EdgeTTS.Synthesize")]
    public static void Synthesize
    (
        string text
    ) =>
        Plugin.Synthesize(text);

    [IPCProvider("EdgeTTS.SynthesizeWithOptions")]
    public static void SynthesizeWithOptions
    (
        string text,
        int?   speed = null,
        int?   pitch = null,
        int?   volume = null
    ) =>
        Plugin.Synthesize(text, speed, pitch, volume);

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
        int?              speed = null,
        int?              pitch = null,
        int?              volume = null,
        CancellationToken cancellationToken = default
    ) =>
        Plugin.SynthesizeAsync(text, speed, pitch, volume, cancellationToken);
}
