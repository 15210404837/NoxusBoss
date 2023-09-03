using CalamityMod.Rarities;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using NoxusBoss.Core;
using NoxusBoss.Core.CrossCompatibility;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Items.Placeable
{
    public class Cattail : ModItem, IToastyQoLChecklistItemSupport
    {
        public ToastyQoLRequirement Requirement => ToastyQoLRequirementRegistry.PostXeroc;

        public static readonly SoundStyle CelebrationSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/Cattail") with { SoundLimitBehavior = SoundLimitBehavior.IgnoreNew };

        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 50;
            IL_Player.PlaceThing_Tiles_PlaceIt += PlayAwesomeSoundForCattail;
        }

        private void PlayAwesomeSoundForCattail(ILContext il)
        {
            ILCursor cursor = new(il);

            cursor.GotoNext(i => i.MatchCallOrCallvirt<Player>("PlaceThing_Tiles_PlaceIt_KillGrassForSolids"));

            cursor.Emit(OpCodes.Ldarg_3);
            cursor.EmitDelegate((int tileType) =>
            {
                if (tileType == TileID.Cattail && !WorldSaveSystem.HasPlacedCattail)
                {
                    SoundEngine.PlaySound(CelebrationSound);
                    CattailAnimationSystem.StartAnimation();
                    WorldSaveSystem.HasPlacedCattail = true;
                }
            });
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(TileID.Cattail);
            Item.width = 16;
            Item.height = 10;
            Item.rare = ModContent.RarityType<CalamityRed>();
            Item.value = Item.sellPrice(100, 0, 0, 0);
            Item.consumable = true;
        }
    }
}

