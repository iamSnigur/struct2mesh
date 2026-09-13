using System.Collections.Generic;

namespace Logic.Utils
{
    public static class BlockEntityHelper
    {
        private static readonly HashSet<string> BlockEntityBlocks = new()
        {
            // Chests
            "chest", "trapped_chest", "ender_chest",
            "copper_chest", "exposed_copper_chest",
            "weathered_copper_chest", "oxidized_copper_chest",
            "waxed_copper_chest", "waxed_exposed_copper_chest",
            "waxed_weathered_copper_chest", "waxed_oxidized_copper_chest",

            // Beds
            "white_bed", "orange_bed", "magenta_bed", "light_blue_bed",
            "yellow_bed", "lime_bed", "pink_bed", "gray_bed",
            "light_gray_bed", "cyan_bed", "purple_bed", "blue_bed",
            "brown_bed", "green_bed", "red_bed", "black_bed",

            // Banners
            "white_banner", "orange_banner", "magenta_banner", "light_blue_banner",
            "yellow_banner", "lime_banner", "pink_banner", "gray_banner",
            "light_gray_banner", "cyan_banner", "purple_banner", "blue_banner",
            "brown_banner", "green_banner", "red_banner", "black_banner",
            "white_wall_banner", "orange_wall_banner", "magenta_wall_banner", "light_blue_wall_banner",
            "yellow_wall_banner", "lime_wall_banner", "pink_wall_banner", "gray_wall_banner",
            "light_gray_wall_banner", "cyan_wall_banner", "purple_wall_banner", "blue_wall_banner",
            "brown_wall_banner", "green_wall_banner", "red_wall_banner", "black_wall_banner",

            // Shulker boxes
            "shulker_box",
            "white_shulker_box", "orange_shulker_box", "magenta_shulker_box", "light_blue_shulker_box",
            "yellow_shulker_box", "lime_shulker_box", "pink_shulker_box", "gray_shulker_box",
            "light_gray_shulker_box", "cyan_shulker_box", "purple_shulker_box", "blue_shulker_box",
            "brown_shulker_box", "green_shulker_box", "red_shulker_box", "black_shulker_box",

            // Signs
            "oak_sign", "oak_wall_sign", "oak_hanging_sign", "oak_wall_hanging_sign",
            "spruce_sign", "spruce_wall_sign", "spruce_hanging_sign", "spruce_wall_hanging_sign",
            "birch_sign", "birch_wall_sign", "birch_hanging_sign", "birch_wall_hanging_sign",
            "jungle_sign", "jungle_wall_sign", "jungle_hanging_sign", "jungle_wall_hanging_sign",
            "acacia_sign", "acacia_wall_sign", "acacia_hanging_sign", "acacia_wall_hanging_sign",
            "dark_oak_sign", "dark_oak_wall_sign", "dark_oak_hanging_sign", "dark_oak_wall_hanging_sign",
            "mangrove_sign", "mangrove_wall_sign", "mangrove_hanging_sign", "mangrove_wall_hanging_sign",
            "cherry_sign", "cherry_wall_sign", "cherry_hanging_sign", "cherry_wall_hanging_sign",
            "pale_oak_sign", "pale_oak_wall_sign", "pale_oak_hanging_sign", "pale_oak_wall_hanging_sign",
            "bamboo_sign", "bamboo_wall_sign", "bamboo_hanging_sign", "bamboo_wall_hanging_sign",
            "crimson_sign", "crimson_wall_sign", "crimson_hanging_sign", "crimson_wall_hanging_sign",
            "warped_sign", "warped_wall_sign", "warped_hanging_sign", "warped_wall_hanging_sign",

            // Heads and skulls
            "skeleton_skull", "skeleton_wall_skull",
            "wither_skeleton_skull", "wither_skeleton_wall_skull",
            "zombie_head", "zombie_wall_head",
            "player_head", "player_wall_head",
            "creeper_head", "creeper_wall_head",
            "dragon_head", "dragon_wall_head",
            "piglin_head", "piglin_wall_head",

            // Copper golem statues
            "copper_golem_statue", "exposed_copper_golem_statue",
            "weathered_copper_golem_statue", "oxidized_copper_golem_statue",
            "waxed_copper_golem_statue", "waxed_exposed_copper_golem_statue",
            "waxed_weathered_copper_golem_statue", "waxed_oxidized_copper_golem_statue",

            // Misc
            "conduit",
            "decorated_pot",
            "end_portal",
            "end_gateway",
        };
        
        public static bool IsBlockEntity(string blockId) => 
            BlockEntityBlocks.Contains(blockId);
    }
}