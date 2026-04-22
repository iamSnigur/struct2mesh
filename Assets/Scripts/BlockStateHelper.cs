using System.Collections.Generic;
using CraftSharp;

public static class BlockStateHelper
{
    private static readonly HashSet<string> TranslucentBlocks = new()
    {
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
        "minecraft:beacon",
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
        "minecraft:grass", "minecraft:fern",
        "minecraft:large_fern", "minecraft:tall_grass",
        "minecraft:dead_bush",
        "minecraft:seagrass", "minecraft:tall_seagrass",

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
        "minecraft:iron_bars", "minecraft:chain",

        // Ladder & Scaffolding
        "minecraft:ladder", "minecraft:scaffolding",

        // Snow
        "minecraft:snow",

        // Cactus, Sugar Cane, Kelp
        "minecraft:cactus", "minecraft:sugar_cane",
        "minecraft:kelp", "minecraft:kelp_plant",

        // Misc cross/cutout models
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
        "minecraft:wither_rose"
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
}