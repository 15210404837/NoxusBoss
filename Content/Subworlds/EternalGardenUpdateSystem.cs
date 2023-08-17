using Terraria.ModLoader;
using static NoxusBoss.Content.Subworlds.EternalGarden;

namespace NoxusBoss.Content.Subworlds
{
    public class EternalGardenUpdateSystem : ModSystem
    {
        public override void PostUpdateEverything()
        {
            // Reset the text opacity when the game is being played. It will increase up to full opacity during subworld transition drawing.
            TextOpacity = 0f;
        }
    }
}
