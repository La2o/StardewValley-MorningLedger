# Morning Ledger: SMAPI mod plan

## Context
The idea is one in-game "morning ledger" that pulls together info players now get from several separate mods. It covers four things: crops ready to harvest, upcoming villager birthdays, Community Center bundle items in your inventory, and crops that will die when the season ends. The project folder `D:\Claude\StardewValley-MorningLedger` is empty, so this is a new project. I didn't find the .NET SDK or a Stardew install in the usual places. The code can still be written now; building and testing in game need both.

Assumed defaults (the questions went unanswered, so tell me if any are wrong):
- Target is Stardew Valley 1.6 with SMAPI 4.x, written in C# for .NET 6.
- A ledger panel opens automatically each morning, and a hotkey (default `L`) reopens it any time.
- v1 has the four core sections, an optional Generic Mod Config Menu (GMCM) hookup, and a console dump command for testing.

## Prep (while the game downloads)
1. Install the .NET 6 SDK: `winget install Microsoft.DotNet.SDK.6`. Version 8 also works for building net6.0.
2. Scaffold the project and write all source files listed below. Run `dotnet restore`, then `dotnet build` with `EnableModDeploy=false` and `EnableGameDebugging=false`. That build only succeeds once the game DLLs are reachable, so it stays as a pending step until the install finishes.
3. After the game is installed: install SMAPI 4.x (the user runs the installer), point `GamePath` at the install, and run the full build with deploy.

## Project layout
```
MorningLedger/
  MorningLedger.csproj        net6.0, Pathoschild.Stardew.ModBuildConfig (auto-deploys to Mods/, zips release)
  manifest.json               UniqueID YourName.MorningLedger, MinimumApiVersion 4.0.0
  ModEntry.cs                 events, hotkey, console command, GMCM registration
  ModConfig.cs                hotkey, auto-open, section toggles, birthday lookahead days (7), sort options
  Ledger/LedgerSnapshot.cs    plain records: HarvestEntry, BirthdayEntry, BundleEntry, SeasonEndEntry
  Ledger/LedgerBuilder.cs     runs the collectors and returns a snapshot (rebuilt when opened and each morning)
  Collectors/HarvestCollector.cs
  Collectors/BirthdayCollector.cs
  Collectors/BundleCollector.cs
  Collectors/SeasonEndCollector.cs
  UI/LedgerMenu.cs            IClickableMenu: parchment box, section headers, item icons, scrolling, collapse sections
  Integrations/IGenericModConfigMenuApi.cs
  i18n/default.json           every user-facing string
  README.md
```

## How each collector gets its data (1.6 APIs)
- **Ready to harvest.** Walk `Utility.ForEachLocation`. Look at `HoeDirt` terrain features with a `crop` that is `!dead` and fully grown (`currentPhase >= phaseDays.Count - 1`, with regrowth crops handled through `fullyGrown`/`dayOfCurrentPhase`). Also check `IndoorPot` objects and fruit trees with `fruit.Count > 0`. Group results by location and crop, like "Farm: 24 Parsnip, Greenhouse: 6 Ancient Fruit". Machines with `readyForHarvest` are behind a config toggle that defaults to off.
- **Birthdays.** Loop over `Utility.getAllVillagers()` where `CanSocialize` is true. Compare `Birthday_Season`/`Birthday_Day` against the next N days, with wraparound to the next season. Mark "today" entries, and show whether a gift was already given from `player.friendshipData[name].GiftsToday`. Show which items in inventory that villager loves or likes, using `npc.getGiftTasteForThisItem`.
- **Bundles you're carrying.** Skip this section if the Community Center is finished or the player went with Joja (`Game1.MasterPlayer.mailReceived` "ccIsComplete" / "JojaMember"). Parse `Game1.netWorldState.Value.BundleData` (fields: name/reward/items/color/required) and check completion with `CommunityCenter.bundlesDict()` or `netWorldState.Bundles`. For every unfilled slot, match the item ID and minimum quality against `Game1.player.Items`. Output looks like "Spring Crops (Pantry): Cauliflower x1 ✓ in bag".
- **Dies at season end.** Look at outdoor crops only. Exclude locations where `IsGreenhouse` is true, Ginger Island (`IsGreenhouse`/`SeasonOverride` logic via `location.GetSeason()`), and garden pots indoors. A crop is flagged when its `GetData().Seasons` does not include the next season. Each entry gets one of three labels: "harvest before day 28" (mature in time), "won't mature in time" (days to maturity > days left), or "dies tonight" (day 28). Days left = `28 - Game1.dayOfMonth`.

## UI and flow
- `GameLoop.DayStarted` marks the snapshot dirty. On the first `UpdateTicked` where `Context.IsPlayerFree`, the mod opens `LedgerMenu` if `AutoOpen` is on and at least one section has entries. A config option skips empty days.
- On `Input.ButtonsChanged`, the hotkey toggles the menu when `Context.IsPlayerFree`. The panel closes with Esc, the menu key, or the hotkey.
- `LedgerMenu` has a dialogue-box background (`drawTextureBox`), uses sprite icons through `ItemRegistry.GetDataOrErrorItem(id).GetTexture/GetSourceRect`, and supports mouse-wheel scrolling. Section headers show counts and collapse when clicked. Sections with no entries show one dim line like "Nothing ready today".
- In split-screen or multiplayer, everything is read from `Game1.player` and the per-screen state (`PerScreen<T>`), so each player gets their own ledger.

## Config / extras
- `ModConfig` holds `ToggleKey`, `AutoOpen`, `SkipIfEmpty`, `BirthdayLookaheadDays`, `ShowMachines`, and a bool for each section. If GMCM is installed, the mod registers with it on `GameLaunched`. GMCM is optional, not a hard dependency.
- The console command `ledger` prints the current snapshot to the SMAPI console so collectors can be tested without the UI.

## Verification
1. Install the .NET 6+ SDK and SMAPI 4.x. Set `<GamePath>` in the csproj (or in `%USERPROFILE%\stardewvalley.targets`) if the game is not in a default path.
2. Run `dotnet build`. ModBuildConfig copies the mod into `Stardew Valley/Mods/MorningLedger`.
3. Launch through SMAPI, load a save, and confirm the panel opens on waking and the `L` key toggles it.
4. Use SMAPI console `debug` commands to set up test cases, then check `ledger` output and the panel each time:
   - `debug growcrops 10` makes crops harvestable.
   - Setting the date to day 27 or 28 (`debug day 27`, `debug season`) exercises the season-end section.
   - Placing a bundle item in inventory with `debug item`/`player_add` exercises the bundle section.
   - Picking a day just before a known villager birthday exercises the birthday section.
5. Check the SMAPI log for errors and test once in split-screen.
