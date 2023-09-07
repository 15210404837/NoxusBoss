using NoxusBoss.Content.Items;
using Terraria.ID;
using Terraria.ModLoader;
using static NoxusBoss.Core.CrossCompatibility.ModReferences;

namespace NoxusBoss.Core.CrossCompatibility
{
    public class CalamityHuntCompatibilitySystem : ModSystem
    {
        public override void PostSetupContent()
        {
            // Don't load anything if the calamity hunt mod is not enabled.
            if (CalamityHunt is null)
                return;

            // Make the good and bad apple interchangeable in shimmer.
            int badAppleID = CalamityHunt.Find<ModItem>("BadApple").Type;
            int goodAppleID = ModContent.ItemType<GoodApple>();
            ItemID.Sets.ShimmerTransformToItem[badAppleID] = goodAppleID;
            ItemID.Sets.ShimmerTransformToItem[goodAppleID] = badAppleID;
        }
    }
}
