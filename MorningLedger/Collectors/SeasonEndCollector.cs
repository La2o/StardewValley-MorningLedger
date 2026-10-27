using MorningLedger.Ledger;
using StardewValley;
using StardewValley.GameData.Crops;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace MorningLedger.Collectors;

internal static class SeasonEndCollector
{
    /// <summary>Crops that won't survive into next season. Crops that can't mature in time are always listed; the rest only within the warning window.</summary>
    public static IReadOnlyList<SeasonEndEntry> Collect(int warningDays)
    {
        int daysLeft = 28 - Game1.dayOfMonth;
        Season nextSeason = (Season)(((int)Game1.season + 1) % 4);
        var counts = new Dictionary<(string Location, string ItemId, SeasonEndStatus Status, int Days), int>();

        void Check(GameLocation location, HoeDirt dirt)
        {
            Crop? crop = dirt.crop;
            if (crop is null || crop.dead.Value)
                return;
            CropData? data = crop.GetData();
            if (data?.Seasons is null || data.Seasons.Contains(nextSeason))
                return;
            string? itemId = HarvestCollector.CropHarvestId(crop);
            if (itemId is null)
                return;

            int daysUntilReady = HarvestCollector.DaysUntilReady(dirt);
            SeasonEndStatus status = daysUntilReady == 0
                ? SeasonEndStatus.ReadyNow
                : daysUntilReady <= daysLeft ? SeasonEndStatus.MaturesInTime : SeasonEndStatus.WontMature;
            if (status != SeasonEndStatus.WontMature && daysLeft > warningDays)
                return;

            var key = (location.DisplayName, ItemRegistry.QualifyItemId(itemId) ?? itemId, status, status == SeasonEndStatus.MaturesInTime ? daysUntilReady : 0);
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        Utility.ForEachLocation(location =>
        {
            // Greenhouse, Ginger Island, and indoor pots keep growing across seasons.
            if (!location.IsOutdoors || location.SeedsIgnoreSeasonsHere())
                return true;

            foreach (TerrainFeature feature in location.terrainFeatures.Values)
            {
                if (feature is HoeDirt dirt)
                    Check(location, dirt);
            }
            foreach (SObject obj in location.objects.Values)
            {
                if (obj is IndoorPot pot && pot.hoeDirt.Value is HoeDirt potDirt)
                    Check(location, potDirt);
            }
            return true;
        });

        return counts
            .Select(p => new SeasonEndEntry(p.Key.Location, p.Key.ItemId, p.Value, p.Key.Status, p.Key.Days))
            .OrderByDescending(e => e.Status)
            .ThenBy(e => e.DaysUntilReady)
            .ThenBy(e => e.Location)
            .ToList();
    }
}
