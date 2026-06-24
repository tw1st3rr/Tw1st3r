# Tw1st3r

A class-aware consumable manager plugin for **AdventureQuest Worlds** via the [Skua](https://github.com/auqw/Skua) bot.

It auto-uses your Tonics, Elixirs, Potions and Scrolls during combat, keeps your buffs refreshed, and recommends the right potions + enhancements for **96+ classes** — pulled from the community classes guide.

**Made for daily use.** Set it once, leave it running — it handles your buffs and potions for you through farming, grinding and AFK sessions, so you never have to think about consumables again.

**Drop-in plugin.** Just put two files in your Skua plugins folder and load it. Works with **any Skua version** — including custom builds — because it uses Skua's standard plugin interface. No rebuild, no source edits.

![Tw1st3r Plugin](screenshot.png)

---

## Install

1. Download **`Tw1st3rPlugin.dll`** and **`class_recommendations.json`** from the [**Releases**](../../releases) tab.
2. Put **both files** in your Skua plugins folder:
   ```
   %APPDATA%\Skua\plugins\
   ```
   (paste that into the Windows **Run** dialog — Win+R)
3. Open Skua → **Plugins** → **Load** → pick `Tw1st3rPlugin.dll`.
4. The Tw1st3r window opens. Log in, hit **Refresh**, pick your items, press **Start**.

That's it. Works on whatever Skua version you're running.

---

## Features

- **Auto-applies Tonic + Elixir** at start, and re-applies them mid-fight when they expire (~15 min) — without stopping combat.
- **Fires your combat consumable** (Potion or Scroll) on cooldown during combat.
- **Auto re-buffs** on **class change**, **death/respawn**, and buff expiry — no manual restart.
- **Reliable item use** — equips to slot 6, then triggers it the same way the game does when you press the **6** key (with retry logic).
- **Per-class recommendations** for 96+ classes: role, description, **named builds** (Early Game / Solo Forge / Ultra Dage / …), enhancements (class/weapon/helm/cape), recommended potions, and combat tips.
- **Potion Saver:**
  - **Reserve per slot** — keep N of an item, never burn your last ones.
  - **Boss-only mode** — only fire against bosses (target HP ≥ a threshold), so trash mobs don't drain your supply on long farms.
- **Loop modes** — Potion (loop, ignore HP) and Scroll (loop) during combat.
- **Skip if buff already active** — won't waste a consumable on a buff you already have.

---

## How it works

AQW's only quick-use slot is slot 6 (the bottle icon by your skills). Tw1st3r equips the consumable to slot 6, then triggers it via the game's own `useSkill` call — exactly like pressing **6** — retrying if the server reports it's not ready. The bottle slot has no global cooldown, so consumables can be swapped and fired mid-combat without interrupting your rotation. That's how buff refresh and combat firing work without stopping you.

---

## Class data

Recommendations live in `class_recommendations.json` (goes next to the DLL in the plugins folder). It's plain text — edit it and hit **Reload** in the plugin; no Skua restart needed. Data is parsed from the community-maintained Simplified Classes Guide.

---

## Build from source

Requires the **.NET 10 SDK**.
```
dotnet build -c Release
```
Output: `bin/Release/net10.0-windows/Tw1st3rPlugin.dll`. Then run `install.bat` to copy it + the JSON into your plugins folder. (The reference DLLs in `lib/` are only needed to compile; they aren't shipped.)

---

## License

[MIT](LICENSE) — fork, modify, share.

## Credits

- Class data: the AQW community guide maintainers.
- [Skua](https://github.com/auqw/Skua) framework.
- Built for the AQW community by **tw1st3rr**.
