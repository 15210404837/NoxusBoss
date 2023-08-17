﻿using NoxusBoss.Content.Subworlds;
using SubworldLibrary;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Biomes
{
    public class EternalGardenBiome : ModBiome
    {
        public override ModWaterStyle WaterStyle => ModContent.Find<ModWaterStyle>("CalamityMod/SunkenSeaWater");

        public override ModSurfaceBackgroundStyle SurfaceBackgroundStyle => ModContent.Find<ModSurfaceBackgroundStyle>("NoxusBoss/LostColosseumSurfaceBGStyle");

        public override ModUndergroundBackgroundStyle UndergroundBackgroundStyle => ModContent.Find<ModUndergroundBackgroundStyle>("NoxusBoss/LostColosseumBGStyle");

        public override SceneEffectPriority Priority => SceneEffectPriority.BossMedium;

        public override string BestiaryIcon => "NoxusBoss/Content/Biomes/EternalGardenIcon";

        public override string BackgroundPath => "NoxusBoss/Content/Backgrounds/EternalGardenBG";

        public override string MapBackground => "NoxusBoss/Content/Backgrounds/EternalGardenBG";

        // TODO -- Implement this.
        public override int Music => 0;

        public override bool IsBiomeActive(Player player) => SubworldSystem.IsActive<EternalGarden>();

        public override float GetWeight(Player player) => 0.96f;
    }
}
