using MorningLedger.Collectors;
using StardewModdingAPI;

namespace MorningLedger.Ledger;

internal static class LedgerBuilder
{
    public static LedgerSnapshot Build(ModConfig config, IMonitor monitor)
    {
        return new LedgerSnapshot(
            Harvest: config.ShowHarvest ? Safe(() => HarvestCollector.Collect(config.IncludeMachines), "harvest", monitor) : null,
            Birthdays: config.ShowBirthdays ? Safe(() => BirthdayCollector.Collect(config.BirthdayLookaheadDays), "birthdays", monitor) : null,
            Bundles: config.ShowBundles ? Safe(BundleCollector.Collect, "bundles", monitor) : null,
            SeasonEnd: config.ShowSeasonEnd ? Safe(() => SeasonEndCollector.Collect(config.SeasonEndWarningDays), "season end", monitor) : null);
    }

    /// <summary>One broken section (say, from another mod's odd data) shouldn't take down the whole ledger.</summary>
    private static IReadOnlyList<T>? Safe<T>(Func<IReadOnlyList<T>?> collect, string section, IMonitor monitor)
    {
        try
        {
            return collect();
        }
        catch (Exception ex)
        {
            monitor.Log($"Couldn't build the {section} section:\n{ex}", LogLevel.Error);
            return Array.Empty<T>();
        }
    }
}
