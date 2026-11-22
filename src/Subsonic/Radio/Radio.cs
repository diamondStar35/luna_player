using LunaPlayer.Subsonic.Models;
using LunaPlayer.Subsonic.Transport;

namespace LunaPlayer.Subsonic.Radio;

internal sealed class Radio(Session session) : Feature(session)
{
    internal async Task<RadioStation[]> GetAllAsync(CancellationToken token = default)
        => Required((await Session.ReadAsync("getInternetRadioStations", token: token).ConfigureAwait(false)).InternetRadioStations, "getInternetRadioStations").Stations;
    internal Task CreateAsync(string streamUrl, string name, string? homePageUrl = null, CancellationToken token = default)
        => Change("createInternetRadioStation", new Parameters().With("streamUrl", Url(streamUrl)).With("name", Id(name))
            .With("homepageUrl", homePageUrl is null ? null : Url(homePageUrl)), token);
    internal Task UpdateAsync(string id, string streamUrl, string name, string? homePageUrl = null, CancellationToken token = default)
        => Change("updateInternetRadioStation", new Parameters().With("id", Id(id)).With("streamUrl", Url(streamUrl))
            .With("name", Id(name)).With("homepageUrl", homePageUrl is null ? null : Url(homePageUrl)), token);
    internal Task DeleteAsync(string id, CancellationToken token = default)
        => Change("deleteInternetRadioStation", new Parameters().With("id", Id(id)), token);
    internal static string Url(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new ArgumentException(nameof(value));
        return value;
    }
}
