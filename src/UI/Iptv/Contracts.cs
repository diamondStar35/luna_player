using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Favorites;
using LunaPlayer.Equalizer;
using LunaPlayer.Iptv;
using LunaPlayer.Playback;
using LunaPlayer.UI.Equalizer;
using LunaPlayer.YouTube;

namespace LunaPlayer.UI.Iptv;
internal enum IptvSourceAction { Open, Add, Edit, Remove }


/// <summary>One row in the IPTV source manager: a saved source shown by name and kind.</summary>
/// <param name="Id">The store's id, carried back on the request so the handler knows which source was
/// chosen without the window knowing anything about the store.</param>
/// <param name="Name">What the source is called.</param>
/// <param name="Type">The kind of source, worded for the user ("Xtream Codes", "M3U web address"...).</param>
/// <param name="Detail">A second column giving the server or file the source points at.</param>
internal readonly record struct IptvSourceListItem(string Id, string Name, string Type, string Detail);
internal readonly record struct IptvSourceRequest(LunaPlayer.UI.Iptv.IptvSourceAction Action, string Id);

/// <summary>Everything the channel browser needs to open: the source's loaded channels and categories, and
/// a way to speak its own help. The browser filters the channels itself and answers with the index of the
/// one the user chose to play, or null when they closed it without playing.</summary>
/// <param name="Title">The window title, naming the source being browsed.</param>
/// <param name="Categories">The groups the channels fall into, for the category filter.</param>
/// <param name="Channels">Every channel the source holds, in load order.</param>
/// <param name="SpeakHelp">Speaks a line of help, for the F1 key, on the UI thread.</param>
/// <param name="Guide">The programme guide, when one was loaded, for the "Now" column and the on-demand
/// now-and-next announcement. Null when the source carried no guide.</param>
internal readonly record struct ChannelBrowserPrompt(
    string Title,
    IReadOnlyList<IptvCategory> Categories,
    IReadOnlyList<IptvChannel> Channels,
    Action<string> SpeakHelp,
    EpgGuide? Guide = null);
