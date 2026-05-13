using System.Collections.Generic;
using CraftSharp;

public static class BlockStateHelper
{
    private static readonly HashSet<string> TranslucentBlocks = new()
    {
        "minecraft:air","minecraft:cave_air","minecraft:structure_void",
        "minecraft:barrier",
        // Glass blocks
        "minecraft:glass",
        "minecraft:white_stained_glass", "minecraft:orange_stained_glass",
        "minecraft:magenta_stained_glass", "minecraft:light_blue_stained_glass",
        "minecraft:yellow_stained_glass", "minecraft:lime_stained_glass",
        "minecraft:pink_stained_glass", "minecraft:gray_stained_glass",
        "minecraft:light_gray_stained_glass", "minecraft:cyan_stained_glass",
        "minecraft:purple_stained_glass", "minecraft:blue_stained_glass",
        "minecraft:brown_stained_glass", "minecraft:green_stained_glass",
        "minecraft:red_stained_glass", "minecraft:black_stained_glass",

        // Glass panes
        "minecraft:glass_pane",
        "minecraft:white_stained_glass_pane", "minecraft:orange_stained_glass_pane",
        "minecraft:magenta_stained_glass_pane", "minecraft:light_blue_stained_glass_pane",
        "minecraft:yellow_stained_glass_pane", "minecraft:lime_stained_glass_pane",
        "minecraft:pink_stained_glass_pane", "minecraft:gray_stained_glass_pane",
        "minecraft:light_gray_stained_glass_pane", "minecraft:cyan_stained_glass_pane",
        "minecraft:purple_stained_glass_pane", "minecraft:blue_stained_glass_pane",
        "minecraft:brown_stained_glass_pane", "minecraft:green_stained_glass_pane",
        "minecraft:red_stained_glass_pane", "minecraft:black_stained_glass_pane",

        // Ice
        "minecraft:ice", "minecraft:frosted_ice",

        // Slime / Honey
        "minecraft:slime_block", "minecraft:honey_block",

        // Misc
        "minecraft:tinted_glass",
        "minecraft:end_gateway", // portal effect
        "minecraft:nether_portal",
        "minecraft:end_portal",
        "minecraft:water",
        "minecraft:bubble_column",
        "minecraft:water_cauldron",
    };

    private static readonly HashSet<string> CutoutBlocks = new()
    {
        // Leaves
        "minecraft:oak_leaves", "minecraft:spruce_leaves",
        "minecraft:birch_leaves", "minecraft:jungle_leaves",
        "minecraft:acacia_leaves", "minecraft:dark_oak_leaves",
        "minecraft:mangrove_leaves", "minecraft:cherry_leaves",
        "minecraft:azalea_leaves", "minecraft:flowering_azalea_leaves",

        // Saplings
        "minecraft:oak_sapling", "minecraft:spruce_sapling",
        "minecraft:birch_sapling", "minecraft:jungle_sapling",
        "minecraft:acacia_sapling", "minecraft:dark_oak_sapling",
        "minecraft:mangrove_propagule", "minecraft:cherry_sapling",
        "minecraft:bamboo_sapling",

        // Flowers & Plants
        "minecraft:dandelion", "minecraft:golden_dandelion", "minecraft:poppy",
        "minecraft:blue_orchid", "minecraft:allium",
        "minecraft:azure_bluet", "minecraft:red_tulip",
        "minecraft:orange_tulip", "minecraft:white_tulip",
        "minecraft:pink_tulip", "minecraft:oxeye_daisy",
        "minecraft:cornflower", "minecraft:lily_of_the_valley",
        "minecraft:wither_rose",
        "minecraft:sunflower", "minecraft:lilac",
        "minecraft:rose_bush", "minecraft:peony",
        "minecraft:pitcher_plant", "minecraft:torchflower",
        "minecraft:spore_blossom",
        "minecraft:open_eyeblossom", "minecraft:closed_eyeblossom",

        // Tall grass / ferns
        "minecraft:grass", 
        "minecraft:bush",
        "minecraft:fern", "minecraft:large_fern",
        "minecraft:short_grass","minecraft:tall_grass",
        "minecraft:dead_bush",
        "minecraft:seagrass", "minecraft:tall_seagrass",
        "minecraft:short_dry_grass", "minecraft:tall_dry_grass",

        // Crops & Food Plants
        "minecraft:wheat", "minecraft:carrots",
        "minecraft:potatoes", "minecraft:beetroots",
        "minecraft:melon_stem", "minecraft:pumpkin_stem",
        "minecraft:attached_melon_stem", "minecraft:attached_pumpkin_stem",
        "minecraft:nether_wart", "minecraft:cocoa",
        "minecraft:sweet_berry_bush", "minecraft:cave_vines",
        "minecraft:cave_vines_plant", "minecraft:torchflower_crop",
        "minecraft:pitcher_crop",

        // Vines & Hanging
        "minecraft:vine", "minecraft:glow_lichen",
        "minecraft:weeping_vines", "minecraft:weeping_vines_plant",
        "minecraft:twisting_vines", "minecraft:twisting_vines_plant",

        // Mushrooms
        "minecraft:brown_mushroom", "minecraft:red_mushroom",
        "minecraft:crimson_fungus", "minecraft:warped_fungus",
        "minecraft:crimson_roots", "minecraft:warped_roots",
        "minecraft:nether_sprouts",

        // Coral (non-block)
        "minecraft:tube_coral", "minecraft:brain_coral",
        "minecraft:bubble_coral", "minecraft:fire_coral",
        "minecraft:horn_coral",
        "minecraft:dead_tube_coral", "minecraft:dead_brain_coral",
        "minecraft:dead_bubble_coral", "minecraft:dead_fire_coral",
        "minecraft:dead_horn_coral",
        "minecraft:tube_coral_fan", "minecraft:brain_coral_fan",
        "minecraft:bubble_coral_fan", "minecraft:fire_coral_fan",
        "minecraft:horn_coral_fan",
        "minecraft:dead_tube_coral_fan", "minecraft:dead_brain_coral_fan",
        "minecraft:dead_bubble_coral_fan", "minecraft:dead_fire_coral_fan",
        "minecraft:dead_horn_coral_fan",
        "minecraft:tube_coral_wall_fan", "minecraft:brain_coral_wall_fan",
        "minecraft:bubble_coral_wall_fan", "minecraft:fire_coral_wall_fan",
        "minecraft:horn_coral_wall_fan",
        "minecraft:dead_tube_coral_wall_fan", "minecraft:dead_brain_coral_wall_fan",
        "minecraft:dead_bubble_coral_wall_fan", "minecraft:dead_fire_coral_wall_fan",
        "minecraft:dead_horn_coral_wall_fan",

        // Bamboo
        "minecraft:bamboo",

        // Rails
        "minecraft:rail", "minecraft:powered_rail",
        "minecraft:detector_rail", "minecraft:activator_rail",

        "minecraft:stonecutter",
        
        // Redstone & Mechanisms
        "minecraft:redstone_wire", "minecraft:repeater",
        "minecraft:comparator", "minecraft:lever",
        "minecraft:tripwire", "minecraft:tripwire_hook",
        "minecraft:torch", "minecraft:wall_torch",
        "minecraft:redstone_torch", "minecraft:redstone_wall_torch",
        "minecraft:soul_torch", "minecraft:soul_wall_torch",

        // Pressure plates
        "minecraft:oak_pressure_plate", "minecraft:spruce_pressure_plate",
        "minecraft:birch_pressure_plate", "minecraft:jungle_pressure_plate",
        "minecraft:acacia_pressure_plate", "minecraft:dark_oak_pressure_plate",
        "minecraft:mangrove_pressure_plate", "minecraft:cherry_pressure_plate",
        "minecraft:bamboo_pressure_plate", "minecraft:crimson_pressure_plate",
        "minecraft:warped_pressure_plate", "minecraft:stone_pressure_plate",
        "minecraft:polished_blackstone_pressure_plate",
        "minecraft:light_weighted_pressure_plate",
        "minecraft:heavy_weighted_pressure_plate",

        // Buttons
        "minecraft:oak_button", "minecraft:spruce_button",
        "minecraft:birch_button", "minecraft:jungle_button",
        "minecraft:acacia_button", "minecraft:dark_oak_button",
        "minecraft:mangrove_button", "minecraft:cherry_button",
        "minecraft:bamboo_button", "minecraft:crimson_button",
        "minecraft:warped_button", "minecraft:stone_button",
        "minecraft:polished_blackstone_button",

        // Iron bars / Chain
        "minecraft:iron_bars", "minecraft:iron_chain",

        // Ladder & Scaffolding
        "minecraft:ladder", "minecraft:scaffolding",

        // Snow
        "minecraft:snow",

        // Cactus, Sugar Cane, Kelp
        "minecraft:cactus", "minecraft:sugar_cane",
        "minecraft:kelp", "minecraft:kelp_plant",
        "minecraft:cactus_flower",

        // Misc cross/cutout models
        "minecraft:beacon",
        "minecraft:lily_pad",
        "minecraft:flower_pot",
        "minecraft:cobweb",
        "minecraft:end_rod",
        "minecraft:chorus_plant", "minecraft:chorus_flower",
        "minecraft:pointed_dripstone",
        "minecraft:moss_carpet",
        "minecraft:hanging_roots",
        "minecraft:small_dripleaf", "minecraft:big_dripleaf", "minecraft:big_dripleaf_stem",
        "minecraft:sculk_vein",
        "minecraft:brewing_stand",
        "minecraft:copper_grate",

        // Candles
        "minecraft:candle",
        "minecraft:white_candle", "minecraft:orange_candle",
        "minecraft:magenta_candle", "minecraft:light_blue_candle",
        "minecraft:yellow_candle", "minecraft:lime_candle",
        "minecraft:pink_candle", "minecraft:gray_candle",
        "minecraft:light_gray_candle", "minecraft:cyan_candle",
        "minecraft:purple_candle", "minecraft:blue_candle",
        "minecraft:brown_candle", "minecraft:green_candle",
        "minecraft:red_candle", "minecraft:black_candle",

        "minecraft:fire", "minecraft:soul_fire",
        "minecraft:campfire", "minecraft:soul_campfire",
        
        // Signs (text cutout)
        "minecraft:oak_sign", "minecraft:spruce_sign",
        "minecraft:birch_sign", "minecraft:jungle_sign",
        "minecraft:acacia_sign", "minecraft:dark_oak_sign",
        "minecraft:mangrove_sign", "minecraft:cherry_sign",
        "minecraft:bamboo_sign", "minecraft:crimson_sign",
        "minecraft:warped_sign",
        "minecraft:oak_wall_sign", "minecraft:spruce_wall_sign",
        "minecraft:birch_wall_sign", "minecraft:jungle_wall_sign",
        "minecraft:acacia_wall_sign", "minecraft:dark_oak_wall_sign",
        "minecraft:mangrove_wall_sign", "minecraft:cherry_wall_sign",
        "minecraft:bamboo_wall_sign", "minecraft:crimson_wall_sign",
        "minecraft:warped_wall_sign",

        // Hanging signs
        "minecraft:oak_hanging_sign", "minecraft:spruce_hanging_sign",
        "minecraft:birch_hanging_sign", "minecraft:jungle_hanging_sign",
        "minecraft:acacia_hanging_sign", "minecraft:dark_oak_hanging_sign",
        "minecraft:mangrove_hanging_sign", "minecraft:cherry_hanging_sign",
        "minecraft:bamboo_hanging_sign", "minecraft:crimson_hanging_sign",
        "minecraft:warped_hanging_sign",
        "minecraft:oak_wall_hanging_sign", "minecraft:spruce_wall_hanging_sign",
        "minecraft:birch_wall_hanging_sign", "minecraft:jungle_wall_hanging_sign",
        "minecraft:acacia_wall_hanging_sign", "minecraft:dark_oak_wall_hanging_sign",
        "minecraft:mangrove_wall_hanging_sign", "minecraft:cherry_wall_hanging_sign",
        "minecraft:bamboo_wall_hanging_sign", "minecraft:crimson_wall_hanging_sign",
        "minecraft:warped_wall_hanging_sign",
        
        // doors & trapdoors
        "minecraft:oak_trapdoor", "minecraft:spruce_trapdoor", "minecraft:birch_trapdoor",
        "minecraft:jungle_trapdoor", "minecraft:acacia_trapdoor", "minecraft:dark_oak_trapdoor",
        "minecraft:mangrove_trapdoor", "minecraft:bamboo_trapdoor", "minecraft:iron_trapdoor",

        "minecraft:oak_door", "minecraft:spruce_door", "minecraft:birch_door",
        "minecraft:jungle_door", "minecraft:acacia_door", "minecraft:dark_oak_door",
        "minecraft:mangrove_door", "minecraft:bamboo_door", "minecraft:iron_door",
        
        "minecraft:soul_lantern",
        "minecraft:lantern",
        "minecraft:copper_lantern",
        "minecraft:waxed_copper_lantern",
        "minecraft:exposed_copper_lantern",
        "minecraft:waxed_exposed_copper_lantern",
        "minecraft:weathered_copper_lantern",
        "minecraft:waxed_weathered_copper_lantern",
        "minecraft:oxidized_copper_lantern",
        "minecraft:waxed_oxidized_copper_lantern",
        
        "minecraft:copper_chain",
        "minecraft:waxed_copper_chain",
        "minecraft:exposed_copper_chain",
        "minecraft:waxed_exposed_copper_chain",
        "minecraft:weathered_copper_chain",
        "minecraft:waxed_weathered_copper_chain",
        "minecraft:oxidized_copper_chain",
        "minecraft:waxed_oxidized_copper_chain",
        
        "minecraft:copper_bars",
        "minecraft:waxed_copper_bars",
        // add
        
        "minecraft:copper_trapdoor",
        "minecraft:waxed_copper_trapdoor",
        // add
        
        "minecraft:firefly_bush",
        "minecraft:azalea",
        // TODO raname "minecraft:azalea",
        "minecraft:pink_petals",
        "minecraft:wildflowers",
        "minecraft:leaf_litter",
        "minecraft:pale_hanging_moss",
        "minecraft:mangrove_roots",
        "minecraft:potted_mangrove_propagule",
        "minecraft:potted_spruce_sapling",
        "minecraft:potted_oak_sapling",
        "minecraft:potted_dark_oak_sapling",
        "minecraft:potted_acacia_sapling",
        "minecraft:potted_jungle_sapling",
        "minecraft:potted_birch_sapling",
        "minecraft:potted_brown_mushroom",
        "minecraft:potted_red_mushroom",
        "minecraft:potted_crimson_fungus",
        "minecraft:potted_closed_eyeblossom",
        "minecraft:potted_open_eyeblossom",
        "minecraft:potted_warped_fungus",
        "minecraft:potted_oxeye_daisy",
        "minecraft:potted_pink_tulip",
        "minecraft:potted_allium",
        "minecraft:potted_torchflower",
        "minecraft:potted_wither_rose",
        "minecraft:potted_cactus",
        
        "minecraft:grass_block",
        
        "minecraft:sculk_sensor",
        "minecraft:calibrated_sculk_sensor",
        "minecraft:sculk_shrieker",
        
        "minecraft:small_amethyst_bud",
        "minecraft:medium_amethyst_bud",
        "minecraft:large_amethyst_bud",
        "minecraft:amethyst_cluster",
        
        "minecraft:spawner",
        "minecraft:trial_spawner",
        "minecraft:vault",
    };

    public static RenderType GetRenderTypeFor(string blockId)
    {
        blockId = "minecraft:" + blockId;

        if (TranslucentBlocks.Contains(blockId))
            return RenderType.TRANSLUCENT;

        if (CutoutBlocks.Contains(blockId))
            return RenderType.CUTOUT;

        return RenderType.SOLID;
    }

    private static readonly HashSet<string> XZOffsetBlocks = new()
    {
        "minecraft:grass",
        "minecraft:fern",
        "minecraft:dead_bush",
        "minecraft:dandelion",
        "minecraft:golden_dandelion",
        "minecraft:poppy",
        "minecraft:blue_orchid",
        "minecraft:allium",
        "minecraft:azure_bluet",
        "minecraft:red_tulip",
        "minecraft:orange_tulip",
        "minecraft:white_tulip",
        "minecraft:pink_tulip",
        "minecraft:oxeye_daisy",
        "minecraft:cornflower",
        "minecraft:lily_of_the_valley",
        "minecraft:wither_rose",
        "minecraft:tall_seagrass",
        "minecraft:seagrass",
    };

    private static readonly HashSet<string> XYZOffsetBlocks = new()
    {
        "minecraft:brown_mushroom",
        "minecraft:red_mushroom"
    };

    public static OffsetType GetOffsetType(string blockId)
    {
        blockId = "minecraft:" + blockId;

        if (XYZOffsetBlocks.Contains(blockId))
            return OffsetType.XYZ;

        if (XZOffsetBlocks.Contains(blockId))
            return OffsetType.XZ;

        return OffsetType.NONE;
    }

    private static readonly HashSet<string> SemiSolidBlocks = new()
    {
        // Slabs
        "minecraft:oak_slab",
        "minecraft:spruce_slab",
        "minecraft:birch_slab",
        "minecraft:jungle_slab",
        "minecraft:acacia_slab",
        "minecraft:dark_oak_slab",
        "minecraft:mangrove_slab",
        "minecraft:cherry_slab",
        "minecraft:bamboo_slab",
        "minecraft:bamboo_mosaic_slab",
        "minecraft:crimson_slab",
        "minecraft:warped_slab",
        "minecraft:stone_slab",
        "minecraft:smooth_stone_slab",
        "minecraft:cobblestone_slab",
        "minecraft:mossy_cobblestone_slab",
        "minecraft:stone_brick_slab",
        "minecraft:mossy_stone_brick_slab",
        "minecraft:granite_slab",
        "minecraft:polished_granite_slab",
        "minecraft:diorite_slab",
        "minecraft:polished_diorite_slab",
        "minecraft:andesite_slab",
        "minecraft:polished_andesite_slab",
        "minecraft:cobbled_deepslate_slab",
        "minecraft:polished_deepslate_slab",
        "minecraft:deepslate_brick_slab",
        "minecraft:deepslate_tile_slab",
        "minecraft:tuff_slab",
        "minecraft:tuff_brick_slab",
        "minecraft:polished_tuff_slab",
        "minecraft:brick_slab",
        "minecraft:mud_brick_slab",
        "minecraft:sandstone_slab",
        "minecraft:cut_sandstone_slab",
        "minecraft:smooth_sandstone_slab",
        "minecraft:red_sandstone_slab",
        "minecraft:cut_red_sandstone_slab",
        "minecraft:smooth_red_sandstone_slab",
        "minecraft:prismarine_slab",
        "minecraft:prismarine_brick_slab",
        "minecraft:dark_prismarine_slab",
        "minecraft:nether_brick_slab",
        "minecraft:red_nether_brick_slab",
        "minecraft:blackstone_slab",
        "minecraft:polished_blackstone_slab",
        "minecraft:polished_blackstone_brick_slab",
        "minecraft:end_stone_brick_slab",
        "minecraft:purpur_slab",
        "minecraft:quartz_slab",
        "minecraft:smooth_quartz_slab",
        "minecraft:cut_copper_slab",
        "minecraft:exposed_cut_copper_slab",
        "minecraft:weathered_cut_copper_slab",
        "minecraft:oxidized_cut_copper_slab",
        "minecraft:waxed_cut_copper_slab",
        "minecraft:waxed_exposed_cut_copper_slab",
        "minecraft:waxed_weathered_cut_copper_slab",
        "minecraft:waxed_oxidized_cut_copper_slab",
        "minecraft:petrified_oak_slab",

        // Stairs
        "minecraft:oak_stairs",
        "minecraft:spruce_stairs",
        "minecraft:birch_stairs",
        "minecraft:jungle_stairs",
        "minecraft:acacia_stairs",
        "minecraft:dark_oak_stairs",
        "minecraft:mangrove_stairs",
        "minecraft:cherry_stairs",
        "minecraft:bamboo_stairs",
        "minecraft:bamboo_mosaic_stairs",
        "minecraft:crimson_stairs",
        "minecraft:warped_stairs",
        "minecraft:cobblestone_stairs",
        "minecraft:mossy_cobblestone_stairs",
        "minecraft:stone_stairs",
        "minecraft:stone_brick_stairs",
        "minecraft:mossy_stone_brick_stairs",
        "minecraft:granite_stairs",
        "minecraft:polished_granite_stairs",
        "minecraft:diorite_stairs",
        "minecraft:polished_diorite_stairs",
        "minecraft:andesite_stairs",
        "minecraft:polished_andesite_stairs",
        "minecraft:cobbled_deepslate_stairs",
        "minecraft:polished_deepslate_stairs",
        "minecraft:deepslate_brick_stairs",
        "minecraft:deepslate_tile_stairs",
        "minecraft:tuff_stairs",
        "minecraft:tuff_brick_stairs",
        "minecraft:polished_tuff_stairs",
        "minecraft:brick_stairs",
        "minecraft:mud_brick_stairs",
        "minecraft:sandstone_stairs",
        "minecraft:smooth_sandstone_stairs",
        "minecraft:red_sandstone_stairs",
        "minecraft:smooth_red_sandstone_stairs",
        "minecraft:prismarine_stairs",
        "minecraft:prismarine_brick_stairs",
        "minecraft:dark_prismarine_stairs",
        "minecraft:nether_brick_stairs",
        "minecraft:red_nether_brick_stairs",
        "minecraft:blackstone_stairs",
        "minecraft:polished_blackstone_stairs",
        "minecraft:polished_blackstone_brick_stairs",
        "minecraft:end_stone_brick_stairs",
        "minecraft:purpur_stairs",
        "minecraft:quartz_stairs",
        "minecraft:smooth_quartz_stairs",
        "minecraft:cut_copper_stairs",
        "minecraft:exposed_cut_copper_stairs",
        "minecraft:weathered_cut_copper_stairs",
        "minecraft:oxidized_cut_copper_stairs",
        "minecraft:waxed_cut_copper_stairs",
        "minecraft:waxed_exposed_cut_copper_stairs",
        "minecraft:waxed_weathered_cut_copper_stairs",
        "minecraft:waxed_oxidized_cut_copper_stairs",

        // Fences
        "minecraft:oak_fence",
        "minecraft:spruce_fence",
        "minecraft:birch_fence",
        "minecraft:jungle_fence",
        "minecraft:acacia_fence",
        "minecraft:dark_oak_fence",
        "minecraft:mangrove_fence",
        "minecraft:cherry_fence",
        "minecraft:bamboo_fence",
        "minecraft:crimson_fence",
        "minecraft:warped_fence",
        "minecraft:nether_brick_fence",

        // Fence Gates
        "minecraft:oak_fence_gate",
        "minecraft:spruce_fence_gate",
        "minecraft:birch_fence_gate",
        "minecraft:jungle_fence_gate",
        "minecraft:acacia_fence_gate",
        "minecraft:dark_oak_fence_gate",
        "minecraft:mangrove_fence_gate",
        "minecraft:cherry_fence_gate",
        "minecraft:bamboo_fence_gate",
        "minecraft:crimson_fence_gate",
        "minecraft:warped_fence_gate",

        // Walls
        "minecraft:cobblestone_wall",
        "minecraft:mossy_cobblestone_wall",
        "minecraft:stone_brick_wall",
        "minecraft:mossy_stone_brick_wall",
        "minecraft:granite_wall",
        "minecraft:diorite_wall",
        "minecraft:andesite_wall",
        "minecraft:cobbled_deepslate_wall",
        "minecraft:polished_deepslate_wall",
        "minecraft:deepslate_brick_wall",
        "minecraft:deepslate_tile_wall",
        "minecraft:tuff_wall",
        "minecraft:tuff_brick_wall",
        "minecraft:polished_tuff_wall",
        "minecraft:brick_wall",
        "minecraft:mud_brick_wall",
        "minecraft:sandstone_wall",
        "minecraft:red_sandstone_wall",
        "minecraft:prismarine_wall",
        "minecraft:nether_brick_wall",
        "minecraft:red_nether_brick_wall",
        "minecraft:blackstone_wall",
        "minecraft:polished_blackstone_wall",
        "minecraft:polished_blackstone_brick_wall",
        "minecraft:end_stone_brick_wall",

        // Bars / Panes
        "minecraft:iron_bars",
        "minecraft:glass_pane",
        "minecraft:white_stained_glass_pane",
        "minecraft:orange_stained_glass_pane",
        "minecraft:magenta_stained_glass_pane",
        "minecraft:light_blue_stained_glass_pane",
        "minecraft:yellow_stained_glass_pane",
        "minecraft:lime_stained_glass_pane",
        "minecraft:pink_stained_glass_pane",
        "minecraft:gray_stained_glass_pane",
        "minecraft:light_gray_stained_glass_pane",
        "minecraft:cyan_stained_glass_pane",
        "minecraft:purple_stained_glass_pane",
        "minecraft:blue_stained_glass_pane",
        "minecraft:brown_stained_glass_pane",
        "minecraft:green_stained_glass_pane",
        "minecraft:red_stained_glass_pane",
        "minecraft:black_stained_glass_pane",

        // Doors
        "minecraft:oak_door",
        "minecraft:spruce_door",
        "minecraft:birch_door",
        "minecraft:jungle_door",
        "minecraft:acacia_door",
        "minecraft:dark_oak_door",
        "minecraft:mangrove_door",
        "minecraft:cherry_door",
        "minecraft:bamboo_door",
        "minecraft:crimson_door",
        "minecraft:warped_door",
        "minecraft:iron_door",
        "minecraft:copper_door",
        "minecraft:exposed_copper_door",
        "minecraft:weathered_copper_door",
        "minecraft:oxidized_copper_door",
        "minecraft:waxed_copper_door",
        "minecraft:waxed_exposed_copper_door",
        "minecraft:waxed_weathered_copper_door",
        "minecraft:waxed_oxidized_copper_door",

        // Trapdoors
        "minecraft:oak_trapdoor",
        "minecraft:spruce_trapdoor",
        "minecraft:birch_trapdoor",
        "minecraft:jungle_trapdoor",
        "minecraft:acacia_trapdoor",
        "minecraft:dark_oak_trapdoor",
        "minecraft:mangrove_trapdoor",
        "minecraft:cherry_trapdoor",
        "minecraft:bamboo_trapdoor",
        "minecraft:crimson_trapdoor",
        "minecraft:warped_trapdoor",
        "minecraft:iron_trapdoor",
        "minecraft:copper_trapdoor",
        "minecraft:exposed_copper_trapdoor",
        "minecraft:weathered_copper_trapdoor",
        "minecraft:oxidized_copper_trapdoor",
        "minecraft:waxed_copper_trapdoor",
        "minecraft:waxed_exposed_copper_trapdoor",
        "minecraft:waxed_weathered_copper_trapdoor",
        "minecraft:waxed_oxidized_copper_trapdoor",

        // Lanterns
        "minecraft:lantern",
        "minecraft:soul_lantern",

        // Chains
        "minecraft:iron_chain",

        // Carpets
        "minecraft:white_carpet",
        "minecraft:orange_carpet",
        "minecraft:magenta_carpet",
        "minecraft:light_blue_carpet",
        "minecraft:yellow_carpet",
        "minecraft:lime_carpet",
        "minecraft:pink_carpet",
        "minecraft:gray_carpet",
        "minecraft:light_gray_carpet",
        "minecraft:cyan_carpet",
        "minecraft:purple_carpet",
        "minecraft:blue_carpet",
        "minecraft:brown_carpet",
        "minecraft:green_carpet",
        "minecraft:red_carpet",
        "minecraft:black_carpet",
        "minecraft:moss_carpet",

        // Skulls / Heads
        "minecraft:skeleton_skull",
        "minecraft:wither_skeleton_skull",
        "minecraft:zombie_head",
        "minecraft:player_head",
        "minecraft:creeper_head",
        "minecraft:dragon_head",
        "minecraft:piglin_head",
        "minecraft:skeleton_wall_skull",
        "minecraft:wither_skeleton_wall_skull",
        "minecraft:zombie_wall_head",
        "minecraft:player_wall_head",
        "minecraft:creeper_wall_head",
        "minecraft:dragon_wall_head",
        "minecraft:piglin_wall_head",

        // Pressure Plates
        "minecraft:oak_pressure_plate",
        "minecraft:spruce_pressure_plate",
        "minecraft:birch_pressure_plate",
        "minecraft:jungle_pressure_plate",
        "minecraft:acacia_pressure_plate",
        "minecraft:dark_oak_pressure_plate",
        "minecraft:mangrove_pressure_plate",
        "minecraft:cherry_pressure_plate",
        "minecraft:bamboo_pressure_plate",
        "minecraft:crimson_pressure_plate",
        "minecraft:warped_pressure_plate",
        "minecraft:stone_pressure_plate",
        "minecraft:polished_blackstone_pressure_plate",
        "minecraft:light_weighted_pressure_plate",
        "minecraft:heavy_weighted_pressure_plate",

        // Buttons
        "minecraft:oak_button",
        "minecraft:spruce_button",
        "minecraft:birch_button",
        "minecraft:jungle_button",
        "minecraft:acacia_button",
        "minecraft:dark_oak_button",
        "minecraft:mangrove_button",
        "minecraft:cherry_button",
        "minecraft:bamboo_button",
        "minecraft:crimson_button",
        "minecraft:warped_button",
        "minecraft:stone_button",
        "minecraft:polished_blackstone_button",

        // Torches / Redstone
        "minecraft:torch",
        "minecraft:wall_torch",
        "minecraft:soul_torch",
        "minecraft:soul_wall_torch",
        "minecraft:redstone_torch",
        "minecraft:redstone_wall_torch",
        "minecraft:lever",
        "minecraft:tripwire_hook",
        "minecraft:tripwire",

        // Ladders / Vines
        "minecraft:ladder",
        "minecraft:vine",
        "minecraft:glow_lichen",
        "minecraft:cave_vines",
        "minecraft:cave_vines_plant",
        "minecraft:twisting_vines",
        "minecraft:twisting_vines_plant",
        "minecraft:weeping_vines",
        "minecraft:weeping_vines_plant",

        // Misc Thin / Partial blocks
        "minecraft:snow",                      // 1–7 layer snow
        "minecraft:lily_pad",
        "minecraft:turtle_egg",
        "minecraft:sea_pickle",
        "minecraft:candle",
        "minecraft:white_candle",
        "minecraft:orange_candle",
        "minecraft:magenta_candle",
        "minecraft:light_blue_candle",
        "minecraft:yellow_candle",
        "minecraft:lime_candle",
        "minecraft:pink_candle",
        "minecraft:gray_candle",
        "minecraft:light_gray_candle",
        "minecraft:cyan_candle",
        "minecraft:purple_candle",
        "minecraft:blue_candle",
        "minecraft:brown_candle",
        "minecraft:green_candle",
        "minecraft:red_candle",
        "minecraft:black_candle",
        "minecraft:cake",
        "minecraft:enchanting_table",
        "minecraft:anvil",
        "minecraft:chipped_anvil",
        "minecraft:damaged_anvil",
        "minecraft:grindstone",
        "minecraft:stonecutter",
        "minecraft:bell",
        "minecraft:cauldron",
        "minecraft:water_cauldron",
        "minecraft:lava_cauldron",
        "minecraft:powder_snow_cauldron",
        "minecraft:composter",
        "minecraft:chest",
        "minecraft:trapped_chest",
        "minecraft:ender_chest",
        "minecraft:brewing_stand",
        "minecraft:flower_pot",
        "minecraft:end_rod",
        "minecraft:lightning_rod",
        "minecraft:conduit",
        "minecraft:scaffolding",
        "minecraft:honey_block",
        "minecraft:mud",
        "minecraft:soul_sand",
        "minecraft:pointed_dripstone",
        "minecraft:pink_petals",
        "minecraft:decorated_pot",
        "minecraft:chiseled_bookshelf",
        "minecraft:trial_spawner",
        "minecraft:vault",
        //"minecraft:crafter",
        "minecraft:sculk_sensor",
        "minecraft:calibrated_sculk_sensor",
        "minecraft:sculk_shrieker",
        "minecraft:white_wall_banner", // TODO check all blocks
        
        "minecraft:copper_golem_statue",
        "minecraft:waxed_copper_golem_statue",
        "minecraft:exposed_copper_golem_statue",
        "minecraft:waxed_exposed_copper_golem_statue",
        "minecraft:oxidized_copper_golem_statue",
        "minecraft:waxed_oxidized_copper_golem_statue",
        "minecraft:weathered_copper_golem_statue",
        "minecraft:waxed_weathered_copper_golem_statue",
    };
    
    public static bool IsSemiSolid(string blockId)
    {
        blockId = "minecraft:" + blockId;
        return SemiSolidBlocks.Contains(blockId);
    }
}