================================================================
  FARM & RANCH LIFE  -  16x16 Top-Down Asset Pack  v1.0
  Part of the free 16x16 pixel series (packs 1-6)
================================================================

The deep farming & ranching systems for a cozy farming game:
29 new crops with full growth stages, 10 fruit trees, 16 animal
breeds with full animation sets, ranch buildings with upgrade
levels, 14 artisan machines, farm tool actions for both player
characters, 10 farm NPCs, 187 item icons, farm UI and effects.

Everything here is NEW compared to pack 1 "Cozy Village & Farm"
(which already has 16 basic crops, tools in basic/copper/gold/
crystal, simple animals, a barn and a coop) - pack 6 extends it.

HONEST COUNT: 3,730 unique sprites & animation frames.
  Every frame is counted once across the whole pack (pixel-identical
  frames, e.g. the same sprout in two crops, count once). Empty
  tiles, preview images, example scenes and combined sheets are not
  counted. Included in the total and listed honestly:
   - 637 frames are horizontal mirrors (left-facing rows)
   - 474 frames are tool-tier RECOLORS of the action sheets
     (copper / gold / crystal versions of hoe, water, scythe, chop,
     mine - see 06_Characters_Tools/Actions/*/tool_tier_recolors)
   - 172 frames are silver/gold quality-star variants of item
     icons (icon + star overlay)
  4-frame walk loops repeat their neutral pose (frame 1 = frame 3);
  the repeat is counted once.

  Per category:  Farm terrain 492 | Crops 188 |
  Fruit trees 121 | Ranch animals 872 |
  Buildings 32 | Ranch props 34 | Fishing 28 |
  Machines & sprinklers 99 | Player characters 1070 |
  Tool icons 26 | NPCs 272 | Items 187 (+172 quality
  variants) | UI 47 | Effects 90

TILE SIZE 16x16. Humanoids 16x32, tool actions 32x32, big animals
32x32, small animals 16x16. One shared 55-color palette (/Palette):
the same palette as packs 3-5, so all six packs match. No new
colors were added in this pack.
Import with "Point / Nearest" filtering and no compression
(Unity: Filter Mode = Point, Compression = None, Pixels Per Unit = 16).

----------------------------------------------------------------
FOLDERS
----------------------------------------------------------------
01_Farm_Terrain     path auto-tiles (gravel, wood chips, flagstone,
                    straw, lush pasture) for 4 seasons, fences 4-bit
                    (log, ranch-red, wire) + 4f gates, hedges 4-bit
                    (boxwood, flowering, autumn), irrigation ditch
                    4-bit (4f flowing water), soil overlays
                    (4 fertilizers, retaining soil, seeded rows, dry
                    cracks, weed sprouts), 4 scarecrows (2f sway)
02_Crops            29 crops by season (spring / summer / autumn /
                    greenhouse), giant crops, ready sparkle
03_Fruit_Trees      10 fruit trees: saplings, young, mature, blossom,
                    unripe, ripe, autumn, bare winter, harvest shake,
                    fallen fruit
04_Ranch_Animals    9 big animals + 2 rabbits + 3 chickens + 2 ducks,
                    each with babies; sheared sheep; pig truffle
                    sniffing; horse riding (both players)
05_Buildings_Machines  barn & coop (3 upgrade levels, door open/
                    closed), stable, artisan shed, 2 silos, winter
                    snow versions, shipping bin (4f lid), 14 artisan
                    machines, sprinklers (3 tiers + spray), ranch
                    props, market stalls
06_Characters_Tools 8 farm outfits for the pack 1 player + player_girl,
                    16 farm actions for both players, tool icons
07_NPCs             10 farm NPCs (+ action sheets for 8 of them)
08_Items            187 icons in 13 categories + quality variants
09_UI               toolbar, inventory, crafting, shipping summary,
                    energy bar, animal panel, crop tooltip, money,
                    day/time HUD, fishing tension bar, catch popup...
10_Effects          22 farm effects
11_Fishing          bobber, bite, fishing spot, crab pots, fish ponds
Example_Scenes      13 example scenes (1x, 4x PNG, 2x GIF)
Previews            content overview pages, GIFs, seam tests
Palette             .png / .gpl (GIMP, Aseprite) / .hex

----------------------------------------------------------------
LAYOUTS
----------------------------------------------------------------
Strips "name_..._Nf.png" = N frames side by side, left -> right.

PATH AUTO-TILES  01_Farm_Terrain/Path_Autotiles/<season>/*_autotile.png
  64x64 = 16 tiles, CORNER layout (see _corner_autotile_layout_guide.png;
  same layout as packs 1-5). Bits = TL TR BL BR corners, 1 = the path,
  0 = grass. Tiled: Wang set "Corner". Godot 4: Terrain "Match Corners".
  full_tiles/*_full_Nv.png = plain fill variants.

FENCES / HEDGES (4-bit)  *_4bit.png = 64x64, 16 tiles.
  tile index = N*8 + E*4 + S*2 + W*1 (1 = a neighbour in that
  direction); tile i sits at column i % 4, row i // 4.
  Vertical runs: posts stretch to the tile edge so they join.
GATES  gate_<style>_open_4f.png: 16x32 frames (closed, 30, 60, 90 deg).
  Put the top 16px in the fence row; the bottom tile is swing room.
IRRIGATION  irrigation_ditch_4bit_frame0..3.png: same 4-bit layout,
  4 frames of flowing water (6-8 fps).
SOIL OVERLAYS  transparent 16x16 (2 variants) to lay on tilled soil.

CROPS  02_Crops/<season>/<crop>_stages_8f.png
  frames: 1 seeds, 2 sprout, 3 seedling, 4 young, 5 growing,
  6 mature (flowering / unripe), 7 READY, 8 withered.
  16x16 crops sit on their soil tile. Tall crops (okra, amaranth,
  artichoke, cotton) and trellis crops (peas, hops, green beans,
  grapes) are 16x32: bottom 16px = the soil tile. Trellis crops
  include the trellis in every frame.
  Crops are transparent: use them on dry OR watered soil (pack 1).
  giant_crops/giant_<crop>_3sizes.png: 32x32 (2x2 tiles).
  ready_sparkle_overlay_4f.png: 16x16 overlay for ready crops.

FRUIT TREES  03_Fruit_Trees/<tree>/
  <tree>_saplings_16x32_2f.png       sapling 1, sapling 2
  <tree>_tree_states_32x48_7f.png    young, mature, blossom, unripe,
                                     ripe, autumn, bare winter
                                     (citrus keep fruit in autumn)
  <tree>_harvest_shake_3f.png        32x48: crown sways, a fruit drops
  <tree>_fallen_fruit.png            16x16
  Trunk base is bottom-centre (x 12-19, y 45-46): put that on the
  tree's tile.

ANIMALS  04_Ranch_Animals/<family>/<breed>/<breed>_<row>_<N>f.png
  + <breed>_sheet.png (4 columns x 12 rows, same rows top->bottom):
  walk_down, walk_up, walk_right, walk_left (4f)
  idle_down, idle_right, idle_left (2f: blink / tail / breathing)
  eat_down, eat_right, eat_left (2f chewing / pecking / nibbling)
  sleep_right, sleep_left (2f breathing; birds & rabbits 1f)
  Frame sizes: cows, goat, sheep, pig, horses, alpaca 32x32;
  chickens, ducks, rabbits 16x16 (babies use the same frame size).
  *_baby / *_chick / *_duckling / *_kit = young animals.
  sheep_*_sheared = shorn sheep (full set).
  pig_spotted_truffle_sniff_right/left_4f = snout digging + truffle.
  horse/riding/<horse>_ridden_by_<player>_4dir_4f.png: 32x48 frames,
  rows down, up, right, left (walk with rider).
  Left-facing rows are mirrors of the right-facing rows.

BUILDINGS  05_Buildings_Machines/Buildings/
  barn_level1-3 (64x64, 80x72, 96x80), coop_level1-3 (48x48, 64x48,
  64x56): 2 frames = door closed, door open. stable 72x56,
  artisan_shed 48x48 (closed/open), silo_wood 32x64, silo_metal 32x80.
  winter_snow/: snowy roof version of every building (door closed).
  shipping_bin_lid_open_4f.png: 32x24 (closed -> open).
  The bottom edge of each building is its ground line.

ARTISAN MACHINES  Artisan_Machines/<machine>_idle_work4_done_6f.png
  16x32 frames: 1 idle, 2-5 working loop (6-8 fps), 6 done (product
  bubble). Machines: keg, preserves_jar, cheese_press, loom, mayonnaise_machine, oil_maker, bee_house, furnace, recycling_machine, seed_maker, gem_maker, tapper, incubator, butter_churn.
SPRINKLERS  sprinkler_copper_iron_gold_3f.png (16x16 objects) +
  spray_tier1_48x48, spray_tier2_48x48, spray_tier3_80x80 (4f each):
  centre the spray on the sprinkler tile (tier 1 waters the 4
  neighbours, tier 2 the 3x3 area, tier 3 the 5x5 area).
RANCH PROPS  feed trough (empty/full), water trough 4f, hay rack
  (empty/full), nesting box (0/1/2 eggs), pet bowl (empty/food/
  water), straw bed, milk pail, feed sacks, egg basket, hay bales,
  silage bale, salt lick, compost bin 4f, beehive supers, tool rack,
  market stalls (veg, fruit, dairy; 48x48).

CHARACTERS  06_Characters_Tools/Outfits/<player>/<outfit>_walk_idle_sheet.png
  16x32 frames, 6 columns x 4 rows (down, up, right, left;
  walk 1-4, idle 1-2) - same layout as packs 1-5.
  Outfits: player_overalls, player_rancher, player_beekeeper, player_raingear, player_girl_overalls, player_girl_rancher, player_girl_beekeeper, player_girl_raingear.
ACTIONS  06_Characters_Tools/Actions/<player>/<player>_<action>_<dirs>_<N>f.png
  32x32 frames, the 16x32 body is drawn at x+8. Rows:
   4dir    = down, up, right, left
   side_RL = right, left (milk, shear)
   3dir_FRL = down, right, left (pet)
  Actions: hoe, water, scythe, chop, mine, harvest, plant_seeds, fish_cast, fish_reel, fish_catch, milk, carry_produce_basket, carry_hay_bale, carry_milk_pail, shear, pet.
  Players: player_overalls (pack 1 player) and player_girl_overalls.
  Tool actions use the IRON tier; tool_tier_recolors/ has copper,
  gold and crystal versions of hoe, water, scythe, chop, mine.
TOOL ICONS  Tool_Icons/: NEW tools (shears, milk pail, pitchfork,
  trowel, brush) in copper / iron / gold / crystal + the IRON tier
  for pack 1's tools (pack 1 had basic/copper/gold/crystal).

NPCs  07_NPCs/<npc>_walk_idle_sheet.png (layout as characters) +
  07_NPCs/actions/ (fisher cast, kid harvest, seed seller planting,
  rancher milking, shepherd & vet petting, carpenter chopping,
  beekeeper harvest).
  NPCs: rancher, beekeeper, vet, seed_seller, fisher, carpenter, farmhand_kid, grandpa_farmer, shepherd, travelling_merchant.

ITEMS  08_Items/<category>/<item>.png (16x16) and
  <category>/quality/<item>_silver_gold_2f.png (crops, fruit, animal
  products, artisan goods). _sheets/ has one sheet per category
  (10 per row, order in sheet_order.json).

UI  09_UI: toolbar (10 slots, tier pips), slot states, inventory
  window, crafting window, shipping summary, energy bar (5 levels),
  animal info panel, hearts, mood faces, animal status bubbles,
  crop tooltip, quality stars, money counter, day/time HUD,
  fishing tension bar (6f), catch popup, 4 extra weather icons
  (pack 5 has the season calendar + 9 weather icons).

EFFECTS  10_Effects: 16x16 4f (fish jump 32x16, level up 32x32 6f).
FISHING  11_Fishing: bobber float 4f, bobber bite 4f, fishing spot
  bubbles 4f, crab pot baited/full 4f, fish pond (48x48) 2 levels 4f.

----------------------------------------------------------------
ENGINE TIPS
----------------------------------------------------------------
Unity:  Sprite Mode Multiple, Slice by Cell Size (16x16 / 16x32 /
  32x32 ...), Pivot Bottom-Center for characters, animals and trees.
  Use a Rule Tile for 4-bit fences and corner auto-tiles.
Godot 4: TileSet atlas 16x16; Terrain sets "Match Corners" for the
  path auto-tiles, "Match Sides" for fences/hedges/irrigation.
  AnimatedSprite2D: add the strip with hframes = N.
Tiled:  Wang sets (Corner for paths, Edge for fences).
GameMaker: import strips with "Import Strip Image" (frames = N).
Speeds: walk 6-8 fps, idle 2-3 fps, actions 8-10 fps, machines 6 fps,
  water / spray 6-8 fps.

----------------------------------------------------------------
RECOLORS (listed honestly)
----------------------------------------------------------------
  player_girl_overalls_chop_copper_4dir_4f.png
  player_girl_overalls_chop_crystal_4dir_4f.png
  player_girl_overalls_chop_gold_4dir_4f.png
  player_girl_overalls_hoe_copper_4dir_4f.png
  player_girl_overalls_hoe_crystal_4dir_4f.png
  player_girl_overalls_hoe_gold_4dir_4f.png
  player_girl_overalls_mine_copper_4dir_4f.png
  player_girl_overalls_mine_crystal_4dir_4f.png
  player_girl_overalls_mine_gold_4dir_4f.png
  player_girl_overalls_scythe_copper_4dir_4f.png
  player_girl_overalls_scythe_crystal_4dir_4f.png
  player_girl_overalls_scythe_gold_4dir_4f.png
  player_girl_overalls_water_copper_4dir_4f.png
  player_girl_overalls_water_crystal_4dir_4f.png
  player_girl_overalls_water_gold_4dir_4f.png
  player_overalls_chop_copper_4dir_4f.png
  player_overalls_chop_crystal_4dir_4f.png
  player_overalls_chop_gold_4dir_4f.png
  player_overalls_hoe_copper_4dir_4f.png
  player_overalls_hoe_crystal_4dir_4f.png
  player_overalls_hoe_gold_4dir_4f.png
  player_overalls_mine_copper_4dir_4f.png
  player_overalls_mine_crystal_4dir_4f.png
  player_overalls_mine_gold_4dir_4f.png
  player_overalls_scythe_copper_4dir_4f.png
  player_overalls_scythe_crystal_4dir_4f.png
  player_overalls_scythe_gold_4dir_4f.png
  player_overalls_water_copper_4dir_4f.png
  player_overalls_water_crystal_4dir_4f.png
  player_overalls_water_gold_4dir_4f.png
Animal colour breeds (e.g. two cows, two horses) are separate
designs with their own shapes/markings where noted, NOT palette
swaps: jersey vs highland (shaggy fringe, long horns), bay (blaze,
socks) vs white horse, black vs brown sheep (different wool/face).
Chicken breeds share one body template (speckled has a pattern):
these 3 are close to recolors.

----------------------------------------------------------------
PART OF A FREE SERIES
----------------------------------------------------------------
1 Cozy Village & Farm  2 Fantasy Dungeon & Adventure
3 Wild Lands: Biomes & Adventure Towns  4 Cozy Interiors & Town Life
5 Seasons & Festivals  6 Farm & Ranch Life (this pack)
Same 16x16 grid, style and palette - mix them freely.

Made with AI assistance - programmatically generated pixel art,
reviewed and curated.
