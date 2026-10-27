using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace MorningLedger.Ledger;

internal sealed record LedgerLine(string Text, string? ItemId = null, string? NpcName = null, Color? Color = null);

internal sealed record LedgerSection(string Key, string Title, int EntryCount, IReadOnlyList<LedgerLine> Lines, string EmptyText);

/// <summary>Turns a snapshot into translated lines, shared by the menu and the console command.</summary>
internal static class LedgerFormatter
{
    private static readonly Color Warning = new(170, 30, 30);
    private static readonly Color Good = new(30, 110, 40);

    public static IReadOnlyList<LedgerSection> Format(LedgerSnapshot snapshot, ITranslationHelper i18n)
    {
        var sections = new List<LedgerSection>();
        if (snapshot.Harvest is not null)
            sections.Add(FormatHarvest(snapshot.Harvest, i18n));
        if (snapshot.Birthdays is not null)
            sections.Add(FormatBirthdays(snapshot.Birthdays, i18n));
        if (snapshot.Bundles is not null)
            sections.Add(FormatBundles(snapshot.Bundles, i18n));
        if (snapshot.SeasonEnd is not null)
            sections.Add(FormatSeasonEnd(snapshot.SeasonEnd, i18n));
        return sections;
    }

    private static LedgerSection FormatHarvest(IReadOnlyList<HarvestEntry> entries, ITranslationHelper i18n)
    {
        var lines = entries
            .Select(e => new LedgerLine(
                i18n.Get(e.FromMachine ? "harvest.machine-line" : "harvest.line", new { location = e.Location, count = e.Count, item = ItemName(e.ItemId) }),
                e.ItemId))
            .ToList();
        return new LedgerSection("harvest", i18n.Get("section.harvest"), entries.Count, lines, i18n.Get("harvest.empty"));
    }

    private static LedgerSection FormatBirthdays(IReadOnlyList<BirthdayEntry> entries, ITranslationHelper i18n)
    {
        var lines = new List<LedgerLine>();
        foreach (BirthdayEntry e in entries)
        {
            string text = e.DaysUntil switch
            {
                0 => i18n.Get("birthday.today", new { name = e.DisplayName }),
                1 => i18n.Get("birthday.tomorrow", new { name = e.DisplayName }),
                _ => i18n.Get("birthday.upcoming", new { name = e.DisplayName, days = e.DaysUntil, date = $"{Utility.getSeasonNameFromNumber(e.Season)} {e.Day}" })
            };

            string giftText;
            if (e.DaysUntil == 0 && e.GiftedToday)
                giftText = i18n.Get("birthday.gifted");
            else if (e.Gifts.Any(g => g.Loved))
                giftText = i18n.Get("birthday.loved", new { items = JoinNames(e.Gifts.Where(g => g.Loved)) });
            else if (e.Gifts.Count > 0)
                giftText = i18n.Get("birthday.liked", new { items = JoinNames(e.Gifts) });
            else
                giftText = i18n.Get("birthday.no-gift");

            Color? color = e.DaysUntil == 0 && !e.GiftedToday ? Warning : null;
            lines.Add(new LedgerLine($"{text} {giftText}", NpcName: e.NpcName, Color: color));
        }
        return new LedgerSection("birthdays", i18n.Get("section.birthdays"), entries.Count, lines, i18n.Get("birthday.empty"));
    }

    private static LedgerSection FormatBundles(IReadOnlyList<BundleEntry> entries, ITranslationHelper i18n)
    {
        var lines = entries
            .Select(e => new LedgerLine(
                i18n.Get("bundle.line", new
                {
                    item = ItemName(e.ItemId) + QualityText(e.Quality, i18n),
                    carried = e.Carried,
                    needed = e.Needed,
                    bundle = e.BundleName,
                    room = e.Room
                }),
                e.ItemId,
                Color: e.Carried >= e.Needed ? Good : null))
            .ToList();
        return new LedgerSection("bundles", i18n.Get("section.bundles"), entries.Count, lines, i18n.Get("bundle.empty"));
    }

    private static LedgerSection FormatSeasonEnd(IReadOnlyList<SeasonEndEntry> entries, ITranslationHelper i18n)
    {
        int daysLeft = 28 - Game1.dayOfMonth;
        string season = Utility.getSeasonNameFromNumber(Game1.seasonIndex);
        var lines = new List<LedgerLine>();
        if (entries.Count > 0)
        {
            lines.Add(daysLeft == 0
                ? new LedgerLine(i18n.Get("season.last-day", new { season }), Color: Warning)
                : new LedgerLine(i18n.Get("season.days-left", new { days = daysLeft, season })));
        }

        foreach (SeasonEndEntry e in entries)
        {
            var tokens = new { location = e.Location, count = e.Count, item = ItemName(e.ItemId), days = e.DaysUntilReady };
            lines.Add(e.Status switch
            {
                SeasonEndStatus.ReadyNow => new LedgerLine(i18n.Get("season.ready", tokens), e.ItemId, Color: daysLeft == 0 ? Warning : null),
                SeasonEndStatus.MaturesInTime => new LedgerLine(i18n.Get("season.in-time", tokens), e.ItemId),
                _ => new LedgerLine(i18n.Get("season.wont-mature", tokens), e.ItemId, Color: Warning)
            });
        }
        return new LedgerSection("season-end", i18n.Get("section.season-end"), entries.Count, lines, i18n.Get("season.empty"));
    }

    private static string ItemName(string itemId) => ItemRegistry.GetDataOrErrorItem(itemId).DisplayName;

    private static string JoinNames(IEnumerable<GiftSuggestion> gifts) => string.Join(", ", gifts.Take(4).Select(g => ItemName(g.ItemId)));

    private static string QualityText(int quality, ITranslationHelper i18n) => quality switch
    {
        1 => i18n.Get("quality.silver"),
        2 => i18n.Get("quality.gold"),
        >= 4 => i18n.Get("quality.iridium"),
        _ => ""
    };
}
