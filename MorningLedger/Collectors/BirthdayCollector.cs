using MorningLedger.Ledger;
using StardewValley;
using SObject = StardewValley.Object;

namespace MorningLedger.Collectors;

internal static class BirthdayCollector
{
    private const int DaysPerYear = 4 * 28;

    public static IReadOnlyList<BirthdayEntry> Collect(int lookaheadDays)
    {
        var result = new List<BirthdayEntry>();
        var seen = new HashSet<string>();
        int today = Game1.seasonIndex * 28 + Game1.dayOfMonth;
        List<Item> giftable = Game1.player.Items
            .Where(item => item is SObject && item.canBeGivenAsGift())
            .ToList();

        Utility.ForEachVillager(npc =>
        {
            if (!npc.CanSocialize || string.IsNullOrEmpty(npc.Birthday_Season) || npc.Birthday_Day <= 0 || !seen.Add(npc.Name))
                return true;

            int season = Utility.getSeasonNumber(npc.Birthday_Season);
            if (season < 0)
                return true;

            int daysUntil = ((season * 28 + npc.Birthday_Day) - today + DaysPerYear) % DaysPerYear;
            if (daysUntil > lookaheadDays)
                return true;

            bool gifted = Game1.player.friendshipData.TryGetValue(npc.Name, out Friendship friendship) && friendship.GiftsToday > 0;
            result.Add(new BirthdayEntry(npc.Name, npc.displayName, season, npc.Birthday_Day, daysUntil, gifted, FindGifts(npc, giftable)));
            return true;
        });

        return result.OrderBy(e => e.DaysUntil).ThenBy(e => e.DisplayName).ToList();
    }

    private static IReadOnlyList<GiftSuggestion> FindGifts(NPC npc, List<Item> giftable)
    {
        var gifts = new Dictionary<string, GiftSuggestion>();
        foreach (Item item in giftable)
        {
            int taste;
            try
            {
                taste = npc.getGiftTasteForThisItem(item);
            }
            catch
            {
                continue; // some custom NPCs have no gift taste data
            }

            if (taste == NPC.gift_taste_love)
                gifts[item.QualifiedItemId] = new GiftSuggestion(item.QualifiedItemId, true);
            else if (taste == NPC.gift_taste_like && !gifts.ContainsKey(item.QualifiedItemId))
                gifts[item.QualifiedItemId] = new GiftSuggestion(item.QualifiedItemId, false);
        }
        return gifts.Values.OrderByDescending(g => g.Loved).ToList();
    }
}
