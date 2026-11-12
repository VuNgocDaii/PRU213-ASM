# Main gameplay integration

Open `Assets/_Game/Scenes/Main.unity` and press Play. Main remains the enabled build scene.

- Move with WASD / arrow keys; approach a station and press E.
- Feed cows, collect raw milk, process it into bottles, then deliver orders using the delivery station or its HUD button.
- CowShop, MedicineShop and MilkShop are south of the central crossroads. They buy cows, buy medicine and sell bottled milk using the same inventory.
- CowShop uses `Art/Features/Economy/shop-hoang/cowShop.png`; the feed shop remains a separate station. Approach a sick cow and press F to use medicine.
- Approach building slots and press E for the building panel.
- Select a tower in the bottom shop, then click an unlocked tower slot. Click an occupied tower slot for upgrade/sell actions. Castle level gates the remaining tower types.
- Tower ranks I/II/III have separate base/weapon art, damage and attack radius. The selected tower shows its range circle and the menu compares current/next-level stats. Weapon animations run on actual shots and return to idle without a valid target. See [tower stats and tests](TOWER_LEVELS.md).
- Escape opens the pause menu.
- Start with 300 Credits and 10 Feed. The first wave starts after 90 seconds; later waves follow their data-defined preparation timers. Kill enemies for Credits and castle EXP, unlock higher castle zones/towers, and defeat the final boss in wave 10 to win. If the castle is destroyed, the defeat panel appears. Restart reloads Main.

To repeat the setup, exit Play Mode, save open scenes, then use **Tools > PolishCow > Integrate All Features Into Main**. The setup reuses existing objects, copies the tower UI from TowersWorkspace, and leaves the source workspaces and module prefabs intact.

`WaveSpawner` from Thang_fly is installed under the enemy module. Main has six routes, ten wave definitions and five enemy prefabs. Debug keys from EnemiesWorkspace are excluded from Main. Initial barn and machine occupants are bound to their corresponding building slots so their upgrades use the shared building panel. New barn cows register for fly targeting and unregister when removed.

Validation performed in Unity 6000.6.2f1:

- Main modules connect to one context and one inventory; all feature references receive that inventory.
- Four tower definitions are wired; purchasing a tower creates it on a Main slot and charges its price once. Its action menu opens and closes.
- Medicine purchases, bottled milk sales and cow purchases work against the shared inventory.
- Cow disease targets register in the context.
- The existing Main gameplay smoke test passes: feed purchases, cow milk cycles, machine processing/power pause, inventory HUD updates, drone delivery, duplicate-delivery rejection and five-order completion.
- No compilation failures or current console errors after the checks.

Run **Tools > PolishCow > Run Main Combat Smoke Test** to verify cow purchases/art, fly infection and healing, barn construction/upgrade/sale, tower construction/upgrade/sale/rebuild, real projectile damage, pause, ten real spawning cycles, final boss victory and restart. It supplies transient resources and damage to advance the waves quickly; this verifies integration, not difficulty balance.

Local reports: `Logs/MainAllFeaturesVerify.txt`, `Logs/MainGameplaySmokeTest.txt`, `Logs/MainCombatSmokeTest.txt`. Preview: `output/main-complete.png`. Verification resources are created only in Play Mode and are not saved into Main.
