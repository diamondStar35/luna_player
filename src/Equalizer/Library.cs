using LunaPlayer.Configuration;

namespace LunaPlayer.Equalizer;

/// <summary>One preset as the rest of the player sees it, wherever it came from.</summary>
/// <param name="IsSystem">Whether the player ships it. A shipped preset can be edited but not renamed or
/// deleted; one the user made can be both.</param>
internal readonly record struct PresetEntry(string Id, string Name, bool IsSystem);

/// <summary>Every preset there is: the ones the player ships, what the user has changed about them, and
/// the ones the user made.</summary>
///
/// <remarks>
/// Three sources and one list. The menu, the two dialogs and the action handler all ask this rather than
/// any of the three, because which of them a preset came from is a question only this class should have
/// to answer - and because the answer changes while the player is running.
///
/// The two kinds are kept apart on disk for a reason. A preset the user made is a whole preset and lives
/// in its own file. What the user changed about a shipped preset is a difference, is meaningless without
/// the preset it differs from, and lives with the settings. Storing the difference rather than the result
/// is what lets a later release improve a preset for everyone who did not touch that particular band.
/// </remarks>
internal sealed class Library
{
    private readonly PlayerSettings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly PresetStore _store;
    private List<PresetEntry> _entries = [];
    private Dictionary<string, Band[]> _userBands = new(StringComparer.Ordinal);

    internal Library(
        PlayerSettings settings, SettingsStore settingsStore, PresetStore store)
    {
        _settings = settings;
        _settingsStore = settingsStore;
        _store = store;
        Reload();
    }

    /// <summary>Raised when a preset is added, changed or removed, so the menu can be built again.
    /// </summary>
    internal event Action? Changed;

    /// <summary>Every preset, shipped ones first in the order the player lists them, then the user's in
    /// the order they made them.</summary>
    internal IReadOnlyList<PresetEntry> All => _entries;

    /// <summary>Why the last write failed, or an empty string when it did not.</summary>
    internal string LastError { get; private set; } = string.Empty;

    internal static bool IsSystem(string? id) => Presets.Find(id) is not null;

    internal PresetEntry? Entry(string? id)
    {
        foreach (var entry in _entries)
        {
            if (string.Equals(entry.Id, id, StringComparison.Ordinal))
                return entry;
        }
        return null;
    }

    /// <summary>The preset a name refers to, with whatever the user changed about it already applied, or
    /// null when it names nothing.</summary>
    internal Preset? Effective(string? id)
    {
        if (Entry(id) is not PresetEntry entry)
            return null;
        return new Preset(entry.Id, entry.Name, Slots(entry.Id));
    }

    /// <summary>The seventeen slots a preset works out as, ready to hand to the engine or to fill an
    /// editor with.</summary>
    internal Band[] Slots(string id)
    {
        if (_userBands.TryGetValue(id, out var bands))
            return [.. bands];
        var slots = DefaultSlots(id);
        if (!_settings.Equalizer.Overrides.TryGetValue(id, out var overrides))
            return slots;
        foreach (var change in overrides)
        {
            if (change is null || change.Slot < 0 || change.Slot >= slots.Length)
                continue;
            var slot = slots[change.Slot];
            slots[change.Slot] = slot with
            {
                Frequency = change.Frequency ?? slot.Frequency,
                Q = change.Q ?? slot.Q,
                Gain = change.Gain ?? slot.Gain,
            };
        }
        return slots;
    }

    /// <summary>The slots a preset has before the user changed anything about it. This is what the
    /// editor's reset button puts back.</summary>
    /// <remarks>
    /// A preset the user made has no earlier state to go back to, so it answers with what it currently
    /// is. The editor never resets to that anyway: it resets to whichever preset its base list is on.
    /// </remarks>
    internal Band[] DefaultSlots(string id)
        => Presets.Find(id) is Preset shipped
            ? Bands.Slots(shipped)
            : _userBands.TryGetValue(id, out var bands) ? [.. bands] : Bands.Slots(null);

    /// <summary>Whether the user has changed anything about a preset the player ships.</summary>
    internal bool IsEdited(string id)
        => _settings.Equalizer.Overrides.TryGetValue(id, out var overrides) && overrides.Count > 0;

    /// <summary>Saves an edited preset, whichever kind it is.</summary>
    internal bool Save(string id, string name, IReadOnlyList<Band> slots)
    {
        LastError = string.Empty;
        if (Presets.Find(id) is Preset shipped)
        {
            var changes = Differences(Bands.Slots(shipped), slots);
            if (changes.Count == 0)
                _settings.Equalizer.Overrides.Remove(id);
            else
                _settings.Equalizer.Overrides[id] = changes;
            if (!_settingsStore.SaveExplicit(_settings))
            {
                LastError = _settingsStore.LastError;
                return false;
            }
        }
        else if (!_store.Update(id, name, slots))
        {
            LastError = _store.LastError;
            return false;
        }
        Reload();
        return true;
    }

    /// <summary>Saves a preset the user has just made, giving back what it was filed under.</summary>
    internal string? Add(string name, IReadOnlyList<Band> slots)
    {
        LastError = string.Empty;
        var id = _store.Add(name, slots);
        if (id is null)
        {
            LastError = _store.LastError;
            return null;
        }
        Reload();
        return id;
    }

    /// <summary>Removes a preset the user made. A preset the player ships cannot be removed; the most
    /// that can be done to one is putting it back the way it shipped.</summary>
    internal bool Delete(string id)
    {
        LastError = string.Empty;
        if (Presets.Find(id) is not null)
            return false;
        if (!_store.Delete(id))
        {
            LastError = _store.LastError;
            return false;
        }
        Reload();
        return true;
    }

    /// <summary>Puts a shipped preset back the way it shipped, by forgetting what was changed about it
    /// rather than by writing the defaults back over it.</summary>
    internal bool ResetToDefaults(string id)
    {
        LastError = string.Empty;
        if (!_settings.Equalizer.Overrides.Remove(id))
            return true;
        if (!_settingsStore.SaveExplicit(_settings))
        {
            LastError = _settingsStore.LastError;
            return false;
        }
        Reload();
        return true;
    }

    /// <summary>What is different between the defaults and what the user has now, field by field, so that
    /// a band left alone keeps following whatever the player ships.</summary>
    private static List<EqualizerBandOverride> Differences(
        IReadOnlyList<Band> defaults, IReadOnlyList<Band> edited)
    {
        var changes = new List<EqualizerBandOverride>();
        for (var slot = 0; slot < defaults.Count && slot < edited.Count; slot++)
        {
            var (original, current) = (defaults[slot], edited[slot]);
            if (original == current)
                continue;
            changes.Add(new EqualizerBandOverride
            {
                Slot = slot,
                Frequency = original.Frequency == current.Frequency ? null : current.Frequency,
                Q = original.Q == current.Q ? null : current.Q,
                Gain = original.Gain == current.Gain ? null : current.Gain,
            });
        }
        return changes;
    }

    private void Reload()
    {
        var entries = new List<PresetEntry>();
        foreach (var preset in Presets.All)
            entries.Add(new PresetEntry(preset.Id, preset.Name, IsSystem: true));

        var bands = new Dictionary<string, Band[]>(StringComparer.Ordinal);
        foreach (var stored in _store.ListAll())
        {
            // A file edited by hand could name a preset the player already ships, which would leave two
            // entries answering to one name and the menu unable to say which was ticked. The shipped one
            // wins, because it is the one settings and shortcuts may already refer to.
            if (string.IsNullOrWhiteSpace(stored.Id) || Presets.Find(stored.Id) is not null)
                continue;
            bands[stored.Id] = ToSlots(stored);
            entries.Add(new PresetEntry(stored.Id, stored.Name, IsSystem: false));
        }
        _entries = entries;
        _userBands = bands;
        Changed?.Invoke();
    }

    /// <summary>A stored preset as slots, with anything the file did not say filled in from where the
    /// player puts that slot.</summary>
    private static Band[] ToSlots(StoredPreset stored)
    {
        var slots = Bands.Slots(null);
        var peaking = Bands.FirstPeakingSlot;
        foreach (var band in stored.Bands)
        {
            var slot = band.Type switch
            {
                BandType.LowShelf => Bands.LowShelfSlot,
                BandType.HighShelf => Bands.HighShelfSlot,
                _ => peaking++,
            };
            if (slot < 0 || slot >= slots.Length)
                continue;
            slots[slot] = new Band(
                Math.Clamp(band.Frequency, Bands.MinimumFrequency, Bands.MaximumFrequency),
                Math.Clamp(band.Q, Bands.MinimumQ, Bands.MaximumQ),
                Math.Clamp(band.Gain, Bands.MinimumGain, Bands.MaximumGain),
                band.Type);
        }
        return slots;
    }
}
