using System.Globalization;
using LunaPlayer.Equalizer;
using LunaPlayer.Playback;
using WxSharp;

namespace LunaPlayer.UI.Equalizer;

/// <summary>What the user settled on, or null when they cancelled.</summary>
internal sealed record EqualizerEditResult(string Name, IReadOnlyList<Band> Slots);

/// <summary>Everything the editor needs to open on one preset.</summary>
/// <param name="Preview">Applies what is on screen to what is playing. Null when the preset being edited
/// is not the one in force, since previewing it would change the sound to something the user did not
/// ask for.</param>
internal sealed record EqualizerEditContext(
    string Title,
    string Name,
    bool CanRename,
    string BasePresetId,
    IReadOnlyList<Band> Slots,
    Library Library,
    Action<IReadOnlyList<Band>>? Preview);
