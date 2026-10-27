using MorningLedger.Ledger;
using StardewValley;

namespace MorningLedger.Collectors;

internal static class BundleCollector
{
    /// <summary>Returns null when the Community Center isn't relevant: bundles not readable yet, Joja route, or already restored.</summary>
    public static IReadOnlyList<BundleEntry>? Collect()
    {
        Farmer master = Game1.MasterPlayer;
        if (!Game1.player.mailReceived.Contains("canReadJunimoText")
            || master.mailReceived.Contains("JojaMember")
            || master.mailReceived.Contains("ccIsComplete")
            || master.hasCompletedCommunityCenter())
            return null;

        // Some bundles ask for the same item in more than one slot (Construction wants Wood twice), so merge those.
        var needs = new Dictionary<(string Bundle, string Room, string ItemId, int Quality), int>();
        var progress = Game1.netWorldState.Value.Bundles;

        // Keys look like "Pantry/0"; values are "name/reward/id count quality .../color/required/sprite/displayName".
        foreach ((string key, string value) in Game1.netWorldState.Value.BundleData)
        {
            string[] keyParts = key.Split('/');
            if (keyParts.Length < 2 || !int.TryParse(keyParts[1], out int index))
                continue;
            string room = keyParts[0];
            if (room == "Abandoned Joja Mart") // only appears after the Community Center is done
                continue;

            string[] fields = value.Split('/');
            if (fields.Length < 3)
                continue;
            string[] items = fields[2].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int slots = items.Length / 3;
            bool[] done = progress.ContainsKey(index) ? progress[index] : new bool[slots];
            int required = fields.Length > 4 && int.TryParse(fields[4], out int r) && r > 0 ? r : slots;
            if (done.Count(d => d) >= required)
                continue;

            string bundleName = fields.Length > 6 && !string.IsNullOrWhiteSpace(fields[6]) ? fields[6] : fields[0];
            for (int slot = 0; slot < slots; slot++)
            {
                if (slot < done.Length && done[slot])
                    continue;

                string id = items[slot * 3];
                if (id.StartsWith('-')) // gold for the vault, not an item
                    continue;
                string? qualifiedId = ItemRegistry.QualifyItemId(id);
                if (qualifiedId is null)
                    continue;
                int.TryParse(items[slot * 3 + 1], out int needed);
                int.TryParse(items[slot * 3 + 2], out int quality);

                var needKey = (bundleName, RoomDisplayName(room), qualifiedId, quality);
                needs[needKey] = needs.GetValueOrDefault(needKey) + Math.Max(needed, 1);
            }
        }

        var result = new List<BundleEntry>();
        foreach (((string bundle, string roomName, string itemId, int quality), int needed) in needs)
        {
            int carried = Game1.player.Items
                .Where(item => item is not null && item.QualifiedItemId == itemId && item.Quality >= quality)
                .Sum(item => item.Stack);
            if (carried > 0)
                result.Add(new BundleEntry(bundle, roomName, itemId, needed, quality, carried));
        }

        return result.OrderBy(e => e.Room).ThenBy(e => e.BundleName).ToList();
    }

    private static string RoomDisplayName(string room)
    {
        int area = StardewValley.Locations.CommunityCenter.getAreaNumberFromName(room);
        return area >= 0 ? StardewValley.Locations.CommunityCenter.getAreaDisplayNameFromNumber(area) : room;
    }
}
