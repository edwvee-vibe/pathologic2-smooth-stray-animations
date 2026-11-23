# Smooth Stray Animations for Pathologic 2

A small [BepInEx](https://github.com/BepInEx/BepInEx) plugin that fixes the rare leftover ~30 FPS animations in
Pathologic 2. Only a few dozen staged characters and cutscene actors are affected, not regular NPCs. They animate
~30 times per second however high your frame rate is; with the mod they animate every frame. Most players will see
these characters only a few times in a playthrough.

> **Testing status.** This mod has been tested much less than the others. The author has personally checked only one
> case in game: the girl playing hopscotch in the prologue. That is also why it is a separate mod and not part of
> Smooth NPC Walking or Smooth Combat: you can install it or leave it out on its own. Bugs are possible. If you notice
> one, press **Insert** to compare with vanilla, and please report it.

Pathologic 2 runs its physics at a fixed step of 0.033 s, about 30 times per second. Almost all characters in the
game animate every frame, but some have their animator set to update together with physics ("Animate Physics") in the
game assets. Their pose changes only on physics steps. These are rare characters that are not regular NPCs:

- the girl playing hopscotch in the prologue;
- soldier and orderly dummies (the prologue, the military base at the station, the cathedral barricades), the Khan
  dummy at the Polyhedron;
- dancing Herb Brides, the Herb Bride with the bull;
- characters of several cutscenes (the doll funeral, the burning of the Herb Bride, a dying boy, a coffin and others).

With the mod these animators are switched to normal per-frame updates. Frozen poses (the pointing Tragedians, the dead
bull) are left as they are, because they would cost CPU time without any visible change. Regular NPCs are not touched
by this mod: walking NPCs are fixed by [Smooth NPC Walking](https://github.com/edwvee-vibe/pathologic2-smooth-npc-walking).
Combat animations are fixed by [Smooth Combat](https://github.com/edwvee-vibe/pathologic2-smooth-combat).

## Affected characters

This table was compiled by Claude, the AI assistant that wrote the mod. It is based on a scan of the game assets:
which animators are set to "Animate Physics", which game scenes or prefabs they are in, and which animation clips they
play. The names are internal asset names, not names from the game texts. Where and when a character appears in the
game is partly a guess; such entries are marked *(guess)*. **Only the first row has been checked in game by the
author.** The mod does not use this list: it switches any animator set to "Animate Physics" (see
[How it works](#how-it-works)), so the table shows what it is expected to affect.

| Character | Where in the game assets | When you can see it | Checked in game |
|---|---|---|---|
| The girl playing hopscotch (`Girl_Teen_Nata_1`) | Prologue, Stvorki: scene `Pathologic_Plague_Intro/Stvorki_Intro_Addons_2` | Prologue | **Yes** |
| 7 soldier and 2 flamethrower dummies | Prologue, Stvorki: scene `Stvorki_Plague_Objects_2` | Prologue | No |
| 10 soldier dummies | Military base at the station: scenes `Military_Base`, `Military_Base_Army_Leave` | While the army is in town *(guess)* | No |
| 8 soldier dummies | Cathedral barricades: scene `Cathedral_Outdoor_MN` | While the barricades stand *(guess)* | No |
| The Khan dummy (`Khan_Dummy_Idle`) | Polyhedron: scene `Polyhedron_Region` | Quest object *(guess)* | No |
| Dancing Herb Brides | Prefabs `Herb_Bride_1_Dance`, `HerbBride_1_Dance` | Steppe rituals *(guess)* | No |
| The Herb Bride with the bull, the bull | Prefabs `Herb_Bride_2_with_Bull`, `Bull_with_Bride` | *(guess)* the bull scene | No |
| A boy dying of seizures (`Boy_Gavroche_1`) | Prefab `Boy_Gavroche_Playground_Dummy` | At a playground *(guess)* | No |
| Boys with shovels | Cutscene `Doll_Funeral_Cutscene_Blueprint` (3 boys), prefab `Boy_with_Shovel_Real` | The doll funeral | No |
| The burning Herb Bride | Cutscene `BrideBurningNewCutscene_Blueprint` | The burning of the Herb Bride | No |
| A coffin that tips over (`Amazing_Coffin`) | Prefab with animations `Coffin_TumbleDown`, `Campanian_Coffin_Final` | An intro cutscene *(guess)* | No |
| A Tragedian pointing at a wardrobe | Cutscene `Tragedian_GriefIndoor_Cutscene` | *(guess)* a cutscene indoors | No |
| The girl playing hopscotch, ending version | Cutscene `Final_1_Cutscene_Blueprint` | An ending | No |
| Reincarnation scenes at the water: Daniil, the Changeling, Capella, Maria, Saburova, Peter, Anna, Oyun | Cutscenes `Water_*_Blueprint` | An ending *(guess)* | No |

Left as they are on purpose, because their animation is a frozen pose (all animation curves are constant), so
switching them would cost CPU time and change nothing on screen: the pointing Tragedians (240 of them in the district
scenes) and the dead bull. The sleeping Haruspex (scene `Zavodi_Addons`) is also a frozen pose; it uses the older `Animation`
component, which the mod does not touch. NPCs that walk around, such as the two Aigul spectators near the railroad,
are left to the game's own NPC code and to Smooth NPC Walking.

## Requirements

- **Pathologic 2** for Windows. Tested on game version 2.1.31648 (Unity 2022.3).
- **BepInEx 5, 64-bit (x64)**. Pathologic 2 is a 64-bit game, so the x86 (32-bit) build of BepInEx will not load.
  BepInEx 6 is not supported. Tested with BepInEx 5.4.23.

## Installation

1. Install BepInEx 5 x64 if you don't have it yet:
   - Download `BepInEx_win_x64_5.4.x.x.zip` from the
     [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases). Pick the latest 5.4 release, not 6.x.
   - Extract it into the game folder, next to `Pathologic.exe`. For Steam that is usually
     `C:\Program Files (x86)\Steam\steamapps\common\Pathologic\`.
   - Start the game once and quit. BepInEx creates its `BepInEx\plugins` and `BepInEx\config` folders on that first run.
2. Download `Pathologic2SmoothStrayAnimations-<version>.zip` from the [Releases](../../releases) page of this repository.
3. Extract it into the game folder. The plugin ends up in
   `BepInEx\plugins\pathologic2-smooth-stray-animations\Pathologic2SmoothStrayAnimations.dll`.
4. Start the game. `BepInEx\LogOutput.log` should contain a line starting with
   `Pathologic 2 Smooth Stray Animations 1.0.0:`.

The mod doesn't modify any game files. To uninstall it, delete the `BepInEx\plugins\pathologic2-smooth-stray-animations`
folder.

## Usage

The fix is on by default. Press **Insert** in game to switch between the mod and the vanilla behaviour. A short
message in the top-left corner shows the current mode and how many animators are switched right now. The key works
while other keys are held.

### Settings

They are stored in `BepInEx\config\pathologic2.smoothstrayanimations.cfg`. The file is created on the first start with
the mod.

| Section | Key | Default | Description |
|---|---|---|---|
| General | `Enabled` | `true` | Master switch. Toggled in game with `ToggleKey`. |
| General | `ToggleKey` | `Insert` | Key that toggles `Enabled`. Modifiers are supported, e.g. `Insert + LeftControl`. |
| General | `ShowMessages` | `true` | Show the on-screen message when toggling. |
| Fixes | `InterpolateBodies` | `true` | Also interpolate the physics body of a switched character, if it has a moving one. |
| Fixes | `SkipControllers` | frozen poses | Animator controller names to leave as they are, separated by commas. |
| Diagnostics | `Log` | `false` | Log every switched animator and the time the mod spends (see below). |

### Checking performance

The mod does no work every frame. It checks animators when a scene is loaded and when the game creates an object from
a prefab for the first time. Switched characters are then animated every frame by the game itself, as all other
characters are. That's a few dozen characters over the whole game, and animators off screen are not updated anyway.

To see the numbers on your machine, set `Log = true`. `BepInEx\LogOutput.log` then gets lines like these:

```
Per-frame: Stvorki_Intro_Addons_2: SpawnPoints/Hopscotch_Nata/Girl_Teen_Nata_1 Root/Girl_Teen_Nata_1 (Girl_Teen_Nata_Animator_Demo_Hopscotch)
Scene pass (Stvorki_Intro_Addons_2): 412 animators, 1 switched, 180 us
Spawn hook, last minute: 230 calls, total 95 us, longest 40 us; ...
```

## How it works

The game sets `AnimatorUpdateMode.AnimatePhysics` on these animators in its assets. The mod sets them to
`AnimatorUpdateMode.Normal`:

- **Scene objects.** On every `SceneManager.sceneLoaded`, the mod goes once over the loaded animators and switches
  those in `AnimatePhysics`. Scene activation is already a loading frame.
- **Objects created at run time.** The game creates entity models and blueprints (cutscenes included) through
  `UnityFactory.Instantiate(GameObject, string)`. A Harmony prefix looks up the prefab in a dictionary. The first time
  a prefab is seen, the mod checks its animators, and if needed switches the prefab asset in memory. All copies are
  then created already switched. Every later spawn of any prefab, passers-by included, costs one dictionary lookup.

Animators on objects with a `NavMeshAgent` are skipped. These are NPCs, and the game's NPC state code changes their
update mode itself. If a switched character has a non-kinematic `Rigidbody`, the mod also turns on its interpolation,
so that the body position moves every frame too (`InterpolateBodies`). Insert reverts everything the mod changed, and
pressing it again applies the fix again.

## Building from source

Requirements: the .NET SDK (any recent version; the project targets .NET Framework 4.7.2 through reference
assemblies) and the game with BepInEx 5 installed. The game folder provides the referenced assemblies.

```powershell
# builds dist\Pathologic2SmoothStrayAnimations.dll and the release zip dist\Pathologic2SmoothStrayAnimations-<version>.zip
powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1 [-GameDir "D:\Games\Pathologic"]

# copies the DLL into <game>\BepInEx\plugins\pathologic2-smooth-stray-animations\ (-Uninstall removes it)
powershell -NoProfile -ExecutionPolicy Bypass -File install.ps1 [-GameDir "D:\Games\Pathologic"] [-Uninstall]
```

The default game path is `C:\Program Files (x86)\Steam\steamapps\common\Pathologic`.

## About this mod

This mod is vibe-coded. The investigation (decompiling the game code, reading the game assets) and the code were done
by an AI assistant ([Claude Code](https://claude.com/claude-code)) under the direction of
[edwvee](https://github.com/edwvee). Bug reports and pull requests are welcome.

## Changelog

- **1.0.0**: First version.

## License

[MIT](LICENSE). Pathologic 2 is © Ice-Pick Lodge. This project is not affiliated with Ice-Pick Lodge or tinyBuild.
