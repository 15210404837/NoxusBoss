using CalamityMod.Rarities;
using NoxusBoss.Core;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items
{
    public class Cattail : ModItem
    {
        public static readonly SoundStyle CelebrationSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Cattail") with { SoundLimitBehavior = SoundLimitBehavior.IgnoreNew };

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 50;
            On_Player.PlaceThing_Tiles_PlaceIt += PlayAwesomeSoundForCattail;
        }

        private TileObject PlayAwesomeSoundForCattail(On_Player.orig_PlaceThing_Tiles_PlaceIt orig, Player self, bool newObjectType, TileObject data, int tileToCreate)
        {
            if (tileToCreate == TileID.Cattail && !WorldSaveSystem.HasPlacedCattail)
            {
                SoundEngine.PlaySound(CelebrationSound);
                WorldSaveSystem.HasPlacedCattail = true;
            }
            return orig(self, newObjectType, data, tileToCreate);
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileID.Cattail);
            Item.width = 16;
            Item.height = 10;
            Item.rare = ModContent.RarityType<CalamityRed>();
            Item.value = Item.buyPrice(100, 0, 0, 0);
            Item.consumable = true;
        }
    }
}

