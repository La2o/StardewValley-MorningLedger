# Morning Ledger

A Stardew Valley 1.6 mod (SMAPI 4) that opens a daily planner each morning. It shows:

- **Ready to harvest:** fully grown crops, garden pots, and fruit trees with fruit, grouped by location. Machines with finished output can be added in config.
- **Birthdays:** villagers with birthdays in the next 7 days (configurable), plus any loved or liked gifts you're carrying for them.
- **Bundle items in your bag:** items in your inventory that an unfinished Community Center bundle still needs. This section is hidden once the Community Center is done, on the Joja route, or before you can read the bundles.
- **Season's end:** outdoor crops that won't survive into next season. Crops that can't mature in time are always listed. In the last week, every crop still in the ground is listed.

Press **L** to open or close the ledger at any time. Click a section header to collapse it, and scroll with the mouse wheel, arrow keys, or D-pad.

## Building

1. Install the [.NET SDK](https://dotnet.microsoft.com/download) (version 6 or newer), Stardew Valley, and [SMAPI](https://smapi.io).
2. Run `dotnet build MorningLedger`. The build finds the game automatically when it's in a standard Steam or GOG folder. Otherwise, set the path in `%USERPROFILE%\stardewvalley.targets`:
   ```xml
   <Project>
     <PropertyGroup>
       <GamePath>D:\Games\Stardew Valley</GamePath>
     </PropertyGroup>
   </Project>
   ```
3. The build copies the mod into `Stardew Valley/Mods/MorningLedger` and puts a release zip in `MorningLedger/bin/`.

## Testing

Load a save, then type `ledger` in the SMAPI console to print today's ledger. These SMAPI debug commands set up test cases:

- `debug growcrops 10` grows planted crops so the harvest section fills up.
- `debug day 26` jumps near season's end for the season-end section.
- `player_add name Parsnip` puts a bundle item in your bag.

## Config

Edit `config.json` in the mod folder after the first launch, or use [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) in game.

## Known limits

- Farmhands in multiplayer only see locations the game has synced to them.
- The days-until-ready numbers assume the crops get watered.
