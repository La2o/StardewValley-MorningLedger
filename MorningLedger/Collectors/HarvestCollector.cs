using MorningLedger.Ledger;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace MorningLedger.Collectors;

internal static class HarvestCollector
{
    public static IReadOnlyList<HarvestEntry> Collect(bool includeMachines)
    {
        var counts = new Dictionary<(string Location, string ItemId, bool FromMachine), int>();

        void Add(GameLocation location, string? itemId, int count, bool fromMachine)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0)
                return;
            var key = (location.DisplayName, ItemRegistry.QualifyItemId(itemId) ?? itemId, fromMachine);
            counts[key] = counts.GetValueOrDefault(key) + count;
        }

        Utility.ForEachLocation(location =>
        {
            foreach (TerrainFeature feature in location.terrainFeatures.Values)
            {
                if (feature is HoeDirt dirt && dirt.crop is not null && dirt.readyForHarvest())
                    Add(location, CropHarvestId(dirt.crop), 1, false);
                else if (feature is FruitTree tree && tree.fruit.Count > 0)
                    Add(location, tree.fruit[0].QualifiedItemId, tree.fruit.Count, false);
            }

            foreach (SObject obj in location.objects.Values)
            {
                if (obj is IndoorPot pot)
                {
                    if (pot.hoeDirt.Value is HoeDirt potDirt && potDirt.crop is not null && potDirt.readyForHarvest())
                        Add(location, CropHarvestId(potDirt.crop), 1, false);
                }
                else if (includeMachines && obj.bigCraftable.Value && obj.readyForHarvest.Value && obj.heldObject.Value is SObject output)
                    Add(location, output.QualifiedItemId, 1, true);
            }

            return true;
        });

        return counts
            .Select(p => new HarvestEntry(p.Key.Location, p.Key.ItemId, p.Value, p.Key.FromMachine))
            .OrderBy(e => e.FromMachine)
            .ThenBy(e => e.Location)
            .ThenByDescending(e => e.Count)
            .ToList();
    }

    /// <summary>The item a planted crop produces. Wild forage crops (spring onions, ginger) return null so they aren't listed as the player's harvest.</summary>
    internal static string? CropHarvestId(Crop crop)
    {
        return crop.forageCrop.Value ? null : crop.indexOfHarvest.Value;
    }

    /// <summary>Nights of growth left before the crop can be harvested, assuming it gets watered.</summary>
    internal static int DaysUntilReady(HoeDirt dirt)
    {
        Crop crop = dirt.crop;
        if (dirt.readyForHarvest())
            return 0;
        if (crop.fullyGrown.Value)
            return Math.Max(crop.dayOfCurrentPhase.Value, 1);

        // The last phase is the harvestable one (its length is a 99999 placeholder), so stop before it.
        int days = 0;
        for (int phase = crop.currentPhase.Value; phase < crop.phaseDays.Count - 1; phase++)
            days += crop.phaseDays[phase];
        days -= crop.dayOfCurrentPhase.Value;
        return Math.Max(days, 1);
    }
}
