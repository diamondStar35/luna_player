using LunaPlayer.Configuration;
using WxSharp;

namespace LunaPlayer.UI.Selections;

/// <summary>One format offered when selected parts are written to media files.</summary>
internal readonly record struct SelectionFileFormat(string Label, string Extension);

/// <summary>The naming, location, format and concurrency chosen for a batch of selection files.</summary>
internal readonly record struct SelectionSaveOptions(
    string Name,
    string Folder,
    string Extension,
    int MaxConcurrency);
