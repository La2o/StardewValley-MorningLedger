namespace MorningLedger.Ledger;

internal sealed record HarvestEntry(string Location, string ItemId, int Count, bool FromMachine);

internal sealed record GiftSuggestion(string ItemId, bool Loved);

internal sealed record BirthdayEntry(string NpcName, string DisplayName, int Season, int Day, int DaysUntil, bool GiftedToday, IReadOnlyList<GiftSuggestion> Gifts);

internal sealed record BundleEntry(string BundleName, string Room, string ItemId, int Needed, int Quality, int Carried);

internal enum SeasonEndStatus
{
    ReadyNow,
    MaturesInTime,
    WontMature
}

internal sealed record SeasonEndEntry(string Location, string ItemId, int Count, SeasonEndStatus Status, int DaysUntilReady);

/// <summary>Everything the ledger shows for one morning. A null list means that section is turned off or not relevant.</summary>
internal sealed record LedgerSnapshot(
    IReadOnlyList<HarvestEntry>? Harvest,
    IReadOnlyList<BirthdayEntry>? Birthdays,
    IReadOnlyList<BundleEntry>? Bundles,
    IReadOnlyList<SeasonEndEntry>? SeasonEnd)
{
    public bool IsEmpty =>
        (Harvest?.Count ?? 0) == 0
        && (Birthdays?.Count ?? 0) == 0
        && (Bundles?.Count ?? 0) == 0
        && (SeasonEnd?.Count ?? 0) == 0;
}
