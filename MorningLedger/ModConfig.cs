using StardewModdingAPI.Utilities;

namespace MorningLedger;

public sealed class ModConfig
{
    /// <summary>Key that opens or closes the ledger.</summary>
    public KeybindList ToggleKey { get; set; } = KeybindList.Parse("L");

    /// <summary>Open the ledger automatically when you wake up.</summary>
    public bool AutoOpen { get; set; } = true;

    /// <summary>Don't auto-open on days when every section is empty.</summary>
    public bool SkipIfEmpty { get; set; } = true;

    /// <summary>How many days ahead to list birthdays.</summary>
    public int BirthdayLookaheadDays { get; set; } = 7;

    /// <summary>Within this many days of season's end, list every crop that won't survive, not only the ones that can't mature.</summary>
    public int SeasonEndWarningDays { get; set; } = 7;

    /// <summary>Also count machines with finished output in the harvest section.</summary>
    public bool IncludeMachines { get; set; } = false;

    public bool ShowHarvest { get; set; } = true;
    public bool ShowBirthdays { get; set; } = true;
    public bool ShowBundles { get; set; } = true;
    public bool ShowSeasonEnd { get; set; } = true;
}
