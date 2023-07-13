using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.NPCs
{
    public class XerocCultist : ModNPC
    {
        #region Custom Types and Enumerations
        public enum XerocCultistAIType
        {
            Wait,
            WalkUpToPlayer,
            Disappear
        }

        #endregion Custom Types and Enumerations

        #region Fields and Properties

        public Player PlayerToFollow => Main.player[NPC.target];

        public XerocCultistAIType CurrentState
        {
            get => (XerocCultistAIType)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        public ref float AITimer => ref NPC.ai[1];

        public ref float CurrentFrame => ref NPC.localAI[0];

        #endregion Fields and Properties

        #region Initialization
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("???");
            Main.npcFrameCount[Type] = 9;
            this.HideFromBestiary();
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5f;
            NPC.damage = 0;
            NPC.width = 40;
            NPC.height = 54;
            NPC.defense = 0;
            NPC.lifeMax = 500;
            NPC.aiStyle = -1;
            AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.dontTakeDamage = true;
            NPC.HitSound = null;
            NPC.DeathSound = null;
            NPC.friendly = true;
            NPC.value = 0;
            NPC.netAlways = true;
        }

        #endregion Initialization

        #region AI
        public override void AI()
        {
            switch (CurrentState)
            {
                case XerocCultistAIType.Wait:
                    DoBehavior_Wait();
                    break;
            }

            NPC.timeLeft = 99999;
            NPC.Opacity = 0f;

            // Emit a faint light.
            Lighting.AddLight(NPC.Center, Color.LightCoral.ToVector3() * 0.55f);

            NPC.ShowNameOnHover = NPC.Opacity >= 0.7f;
            AITimer++;
        }

        public void DoBehavior_Wait()
        {
            // Wait in place.
            NPC.velocity.X *= 0.95f;
            CurrentFrame = 0f;
        }

        #endregion AI

        #region Drawing

        public override void FindFrame(int frameHeight) => NPC.frame.Y = (int)(frameHeight * CurrentFrame);

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            return true;
        }

        public override bool CanChat() => CurrentState == XerocCultistAIType.WalkUpToPlayer;
        #endregion Drawing
    }
}
