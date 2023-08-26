using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Shaders;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Dyes
{
    public class EntropicDye : ModItem
    {
        public static int DyeID
        {
            get;
            private set;
        }

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 3;

            if (!Main.dedServ)
            {
                Effect shader = ModContent.Request<Effect>("NoxusBoss/Assets/Effects/EntropicDyeShader", AssetRequestMode.ImmediateLoad).Value;
                Asset<Texture2D> dyeTexture = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/EntropicDyeTexture");
                GameShaders.Armor.BindShader(Type, new ArmorShaderData(new Ref<Effect>(shader), ManagedShader.DefaultPassName)).UseImage(dyeTexture);
            }
        }

        public override void SetDefaults()
        {
            // Cache and restore the dye ID.
            // This is necessary because CloneDefaults will automatically reset the dye ID in accordance with whatever it's copied, when in reality the BindShader
            // call already determined what ID this dye should use.
            DyeID = Item.dye;
            Item.CloneDefaults(ItemID.AcidDye);
            Item.dye = DyeID;
            Item.rare = ModContent.RarityType<Violet>();
            Item.value = Item.sellPrice(0, 12, 0, 0);
        }
    }
}
