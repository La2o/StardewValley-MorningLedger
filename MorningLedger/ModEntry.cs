using MorningLedger.Integrations;
using MorningLedger.Ledger;
using MorningLedger.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace MorningLedger;

internal sealed class ModEntry : Mod
{
    /// <summary>Ticks to wait after the day starts so the ledger doesn't pop up mid fade-in.</summary>
    private const int AutoOpenDelayTicks = 60;

    private ModConfig config = new();
    private readonly PerScreen<int> ticksUntilAutoOpen = new(() => -1);
    private readonly PerScreen<HashSet<string>> collapsedSections = new(() => new HashSet<string>());

    public override void Entry(IModHelper helper)
    {
        config = helper.ReadConfig<ModConfig>();

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Input.ButtonsChanged += OnButtonsChanged;

        helper.ConsoleCommands.Add("ledger", helper.Translation.Get("command.ledger"), (_, _) => DumpLedger());
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (gmcm is null)
            return;

        ITranslationHelper i18n = Helper.Translation;
        gmcm.Register(ModManifest, () => config = new ModConfig(), () => Helper.WriteConfig(config));

        gmcm.AddSectionTitle(ModManifest, () => i18n.Get("config.general"));
        gmcm.AddKeybindList(ModManifest, () => config.ToggleKey, v => config.ToggleKey = v, () => i18n.Get("config.toggle-key"), () => i18n.Get("config.toggle-key.tooltip"));
        gmcm.AddBoolOption(ModManifest, () => config.AutoOpen, v => config.AutoOpen = v, () => i18n.Get("config.auto-open"), () => i18n.Get("config.auto-open.tooltip"));
        gmcm.AddBoolOption(ModManifest, () => config.SkipIfEmpty, v => config.SkipIfEmpty = v, () => i18n.Get("config.skip-if-empty"), () => i18n.Get("config.skip-if-empty.tooltip"));

        gmcm.AddSectionTitle(ModManifest, () => i18n.Get("config.sections"));
        gmcm.AddBoolOption(ModManifest, () => config.ShowHarvest, v => config.ShowHarvest = v, () => i18n.Get("section.harvest"));
        gmcm.AddBoolOption(ModManifest, () => config.IncludeMachines, v => config.IncludeMachines = v, () => i18n.Get("config.include-machines"), () => i18n.Get("config.include-machines.tooltip"));
        gmcm.AddBoolOption(ModManifest, () => config.ShowBirthdays, v => config.ShowBirthdays = v, () => i18n.Get("section.birthdays"));
        gmcm.AddNumberOption(ModManifest, () => config.BirthdayLookaheadDays, v => config.BirthdayLookaheadDays = v, () => i18n.Get("config.birthday-days"), () => i18n.Get("config.birthday-days.tooltip"), min: 0, max: 28);
        gmcm.AddBoolOption(ModManifest, () => config.ShowBundles, v => config.ShowBundles = v, () => i18n.Get("section.bundles"));
        gmcm.AddBoolOption(ModManifest, () => config.ShowSeasonEnd, v => config.ShowSeasonEnd = v, () => i18n.Get("section.season-end"));
        gmcm.AddNumberOption(ModManifest, () => config.SeasonEndWarningDays, v => config.SeasonEndWarningDays = v, () => i18n.Get("config.season-days"), () => i18n.Get("config.season-days.tooltip"), min: 0, max: 27);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        ticksUntilAutoOpen.Value = config.AutoOpen ? AutoOpenDelayTicks : -1;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (ticksUntilAutoOpen.Value < 0 || !Context.IsWorldReady || !Context.IsPlayerFree)
            return;
        if (--ticksUntilAutoOpen.Value > 0)
            return;

        ticksUntilAutoOpen.Value = -1;
        LedgerSnapshot snapshot = LedgerBuilder.Build(config, Monitor);
        if (config.SkipIfEmpty && snapshot.IsEmpty)
            return;
        OpenLedger(snapshot);
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsWorldReady || !config.ToggleKey.JustPressed())
            return;

        if (Game1.activeClickableMenu is LedgerMenu menu)
        {
            Helper.Input.SuppressActiveKeybinds(config.ToggleKey);
            menu.exitThisMenu();
        }
        else if (Context.IsPlayerFree)
        {
            Helper.Input.SuppressActiveKeybinds(config.ToggleKey);
            OpenLedger(LedgerBuilder.Build(config, Monitor));
        }
    }

    private void OpenLedger(LedgerSnapshot snapshot)
    {
        Game1.activeClickableMenu = new LedgerMenu(
            LedgerFormatter.Format(snapshot, Helper.Translation),
            collapsedSections.Value,
            Helper.Translation.Get("menu.title"),
            DateText());
    }

    private string DateText()
    {
        return Helper.Translation.Get("menu.date", new
        {
            season = Utility.getSeasonNameFromNumber(Game1.seasonIndex),
            day = Game1.dayOfMonth,
            year = Game1.year
        });
    }

    private void DumpLedger()
    {
        if (!Context.IsWorldReady)
        {
            Monitor.Log(Helper.Translation.Get("command.not-ready"), LogLevel.Warn);
            return;
        }

        Monitor.Log($"{Helper.Translation.Get("menu.title")}: {DateText()}", LogLevel.Info);
        foreach (LedgerSection section in LedgerFormatter.Format(LedgerBuilder.Build(config, Monitor), Helper.Translation))
        {
            Monitor.Log($"== {section.Title} ({section.EntryCount}) ==", LogLevel.Info);
            if (section.Lines.Count == 0)
                Monitor.Log($"  {section.EmptyText}", LogLevel.Info);
            foreach (LedgerLine line in section.Lines)
                Monitor.Log($"  {line.Text}", LogLevel.Info);
        }
    }
}
