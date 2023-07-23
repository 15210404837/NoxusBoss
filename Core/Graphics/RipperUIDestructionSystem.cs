using System;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.Particles;
using CalamityMod.UI.Rippers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using NoxusBoss.Content.Bosses.Xeroc;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics
{
    public class RipperUIDestructionSystem : ModSystem
    {
        private static Hook ripperUIDrawHook;

        public static float FistOffset
        {
            get;
            set;
        }

        public static float FistOpacity
        {
            get;
            set;
        }

        public static bool IsUIDestroyed
        {
            get;
            set;
        }

        public static Vector2 RageScreenPosition => new Vector2(CalamityConfig.Instance.RageMeterPosX * Main.screenWidth * 0.01f, CalamityConfig.Instance.RageMeterPosY * Main.screenHeight * 0.01f).Floor();

        public static Vector2 AdrenalineScreenPosition => new Vector2(CalamityConfig.Instance.AdrenalineMeterPosX * Main.screenWidth * 0.01f, CalamityConfig.Instance.AdrenalineMeterPosY * Main.screenHeight * 0.01f).Floor();

        public static readonly SoundStyle RipperDestructionSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/RipperBarDestruction") with { Volume = 1.4f };

        public delegate void orig_RipperDrawMethod(SpriteBatch spriteBatch, Player player);

        public delegate void hook_RipperDrawMethod(orig_RipperDrawMethod orig, SpriteBatch spriteBatch, Player player);

        public override void Load()
        {
            // Technically HookEndpointManager should be used in 1.4.3 but that standard is changing in 1.4.4 so whatever.
            MonoModHooks.RequestNativeAccess();

            MethodInfo ripperUIDrawMethod = typeof(RipperUI).GetMethod("Draw", BindingFlags.Public | BindingFlags.Static);
            ripperUIDrawHook = new(ripperUIDrawMethod, (hook_RipperDrawMethod)DisableRipperUI);
        }

        public override void Unload() => ripperUIDrawHook?.Undo();

        public override void OnWorldLoad()
        {
            IsUIDestroyed = false;
            FistOpacity = 0f;
            FistOffset = 0f;
        }

        public override void PostUpdatePlayers()
        {
            // Disable rage and adrenaline effects if the UI is destroyed.
            if (IsUIDestroyed)
            {
                Main.LocalPlayer.Calamity().rage = 0f;
                Main.LocalPlayer.Calamity().adrenaline = 0f;
            }

            if (Main.LocalPlayer.dead || XerocBoss.Myself is null)
            {
                IsUIDestroyed = false;
                FistOpacity = 0f;
                FistOffset = 0f;
            }
        }

        public static void DisableRipperUI(orig_RipperDrawMethod orig, SpriteBatch spriteBatch, Player player)
        {
            if (IsUIDestroyed)
                return;

            orig(spriteBatch, player);

            // Don't bother wasting resources drawing if the fists are invisible.
            if (FistOpacity <= 0f)
                return;

            // Calculate bar positions on the screen.
            bool barsAreLevel = Distance(RageScreenPosition.Y, AdrenalineScreenPosition.Y) <= 4f;
            float rageBarWidth = ModContent.Request<Texture2D>("CalamityMod/UI/Rippers/RageBarBorder").Value.Width * Main.UIScale;
            float adrenalineBarWidth = ModContent.Request<Texture2D>("CalamityMod/UI/Rippers/AdrenalineBarBorder").Value.Width * Main.UIScale;

            // Draw a single pair of fists if the bars are level.
            if (barsAreLevel && player.Calamity().RageEnabled && player.Calamity().AdrenalineEnabled)
            {
                float left = MathF.Min(RageScreenPosition.X - rageBarWidth * 0.5f, AdrenalineScreenPosition.X - adrenalineBarWidth * 0.5f);
                float right = MathF.Max(RageScreenPosition.X + rageBarWidth * 0.5f, AdrenalineScreenPosition.X + adrenalineBarWidth * 0.5f);
                Vector2 center = Vector2.Lerp(RageScreenPosition, AdrenalineScreenPosition, 0.5f);
                center.X = (left + right) * 0.5f;

                DrawFists(center, left, right);
            }

            // Draw two distinct pairs of fists if they're not level.
            else
            {
                if (player.Calamity().RageEnabled)
                    DrawFists(RageScreenPosition, RageScreenPosition.X - rageBarWidth * 0.5f, RageScreenPosition.X + rageBarWidth * 0.5f);

                if (player.Calamity().AdrenalineEnabled)
                    DrawFists(AdrenalineScreenPosition, AdrenalineScreenPosition.X - adrenalineBarWidth * 0.5f, AdrenalineScreenPosition.X + adrenalineBarWidth * 0.5f);
            }
        }

        public static void CreateBarDestructionEffects()
        {
            if (!Main.LocalPlayer.Calamity().RageEnabled || !Main.LocalPlayer.Calamity().AdrenalineEnabled)
                return;

            SoundEngine.PlaySound(RipperDestructionSound);

            Vector2 rageBarPositionWorld = RageScreenPosition + Main.screenPosition + new Vector2(85f, 132f) * Main.UIScale;
            Vector2 adrenalineBarPositionWorld = AdrenalineScreenPosition + Main.screenPosition + new Vector2(85f, 112f) * Main.UIScale;
            List<Vector2> barPositions = new()
            {
                rageBarPositionWorld,
                adrenalineBarPositionWorld
            };

            foreach (Vector2 barPosition in barPositions)
            {
                // Create small glass shards.
                for (int i = 0; i < 145; i++)
                {
                    int dustID = Main.rand.NextBool() ? DustID.t_SteampunkMetal : DustID.BlueCrystalShard;

                    Vector2 shardVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3.6f, 13.6f);
                    Dust shard = Dust.NewDustPerfect(barPosition + Main.rand.NextVector2Circular(50f, 18f), dustID, shardVelocity);
                    shard.noGravity = Main.rand.NextBool();
                    shard.scale = Main.rand.NextFloat(1f, 1.425f);
                    shard.color = Color.Wheat;
                    shard.velocity.Y -= 5f;
                }
            }

            // Create orange and green smoke particles.
            for (int i = 0; i < 15; i++)
            {
                Vector2 smokeVelocity = -Vector2.UnitY.RotatedByRandom(0.93f) * Main.rand.NextFloat(3f, 19f);
                Color rageSmokeColor = Color.Lerp(Color.OrangeRed, Color.DarkRed, Main.rand.NextFloat(0.7f));
                MediumMistParticle smoke = new(rageBarPositionWorld + Main.rand.NextVector2Circular(50f, 18f), smokeVelocity, rageSmokeColor, Color.Gray, 1.8f, 255f, 0.004f);
                GeneralParticleHandler.SpawnParticle(smoke);

                smokeVelocity = -Vector2.UnitY.RotatedByRandom(0.93f) * Main.rand.NextFloat(3f, 19f);
                Color adrenalineSmokeColor = Color.Lerp(Color.Lime, Color.Cyan, Main.rand.NextFloat(0.25f, 0.8f));
                smoke = new(adrenalineBarPositionWorld + Main.rand.NextVector2Circular(50f, 18f), smokeVelocity, adrenalineSmokeColor, Color.LightGray, 1.8f, 255f, 0.004f);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            Main.LocalPlayer.Calamity().GeneralScreenShakePower = 15f;
            ScreenEffectSystem.SetChromaticAberrationEffect((rageBarPositionWorld + adrenalineBarPositionWorld) * 0.5f, 1.6f, 45);
        }

        public static void DrawFists(Vector2 center, float left, float right)
        {
            Texture2D fistTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/XerocHandFist").Value;

            Color fistColor = Color.White * FistOpacity;
            float scale = Main.UIScale * 0.67f;
            Vector2 origin = fistTexture.Size() * 0.5f;
            Vector2 leftFistDrawPosition = new Vector2(left, center.Y) - Vector2.UnitX * FistOffset * scale;
            Vector2 rightFistDrawPosition = new Vector2(right, center.Y) + Vector2.UnitX * FistOffset * scale;
            Main.spriteBatch.Draw(fistTexture, leftFistDrawPosition, null, fistColor, 0f, origin, scale, 0, 0f);
            Main.spriteBatch.Draw(fistTexture, rightFistDrawPosition, null, fistColor, 0f, origin, scale, SpriteEffects.FlipHorizontally, 0f);
        }
    }
}
