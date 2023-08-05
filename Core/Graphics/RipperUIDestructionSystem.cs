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
using NoxusBoss.Content.Particles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
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

        public static float MoveToCenterOfScreenInterpolant => Pow(FistOpacity, 2f);

        public static Vector2 RageScreenPosition
        {
            get
            {
                Vector2 originalPosition = new Vector2(CalamityConfig.Instance.RageMeterPosX * Main.screenWidth * 0.01f, CalamityConfig.Instance.RageMeterPosY * Main.screenHeight * 0.01f).Floor();
                if (IsUIDestroyed)
                    return originalPosition;

                return Vector2.Lerp(originalPosition, new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f + new Vector2(100f, -80f) * Main.UIScale, MoveToCenterOfScreenInterpolant);
            }
        }

        public static Vector2 AdrenalineScreenPosition
        {
            get
            {
                Vector2 originalPosition = new Vector2(CalamityConfig.Instance.AdrenalineMeterPosX * Main.screenWidth * 0.01f, CalamityConfig.Instance.AdrenalineMeterPosY * Main.screenHeight * 0.01f).Floor();
                if (IsUIDestroyed)
                    return originalPosition;

                return Vector2.Lerp(originalPosition, new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f + new Vector2(-100f, -80f) * Main.UIScale, MoveToCenterOfScreenInterpolant);
            }
        }

        public static readonly SoundStyle RipperDestructionSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/RipperBarDestruction") with { Volume = 1.4f };

        public delegate void orig_RipperDrawMethod(SpriteBatch spriteBatch, Player player);

        public delegate void hook_RipperDrawMethod(orig_RipperDrawMethod orig, SpriteBatch spriteBatch, Player player);

        public override void Load()
        {
            MethodInfo ripperUIDrawMethod = typeof(RipperUI).GetMethod("Draw", BindingFlags.Public | BindingFlags.Static);
            ripperUIDrawHook = new(ripperUIDrawMethod, (hook_RipperDrawMethod)DisableRipperUI);
        }

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

            // Move the bar positions temporarily in accordance with the draw position override. Afterwards they are reset, to prevent the changes from altering the config file.
            Vector2 oldRagePosition = new(CalamityConfig.Instance.RageMeterPosX, CalamityConfig.Instance.RageMeterPosY);
            Vector2 oldAdrenalinePosition = new(CalamityConfig.Instance.AdrenalineMeterPosX, CalamityConfig.Instance.AdrenalineMeterPosY);
            CalamityConfig.Instance.RageMeterPosX = RageScreenPosition.X / Main.screenWidth * 100f;
            CalamityConfig.Instance.RageMeterPosY = RageScreenPosition.Y / Main.screenHeight * 100f;
            CalamityConfig.Instance.AdrenalineMeterPosX = AdrenalineScreenPosition.X / Main.screenWidth * 100f;
            CalamityConfig.Instance.AdrenalineMeterPosY = AdrenalineScreenPosition.Y / Main.screenHeight * 100f;

            orig(spriteBatch, player);

            CalamityConfig.Instance.RageMeterPosX = oldRagePosition.X;
            CalamityConfig.Instance.RageMeterPosY = oldRagePosition.Y;
            CalamityConfig.Instance.AdrenalineMeterPosX = oldAdrenalinePosition.X;
            CalamityConfig.Instance.AdrenalineMeterPosY = oldAdrenalinePosition.Y;

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
            if (Main.netMode == NetmodeID.Server)
                return;

            if (!Main.LocalPlayer.Calamity().RageEnabled || !Main.LocalPlayer.Calamity().AdrenalineEnabled)
                return;

            SoundEngine.PlaySound(RipperDestructionSound);
            SoundEngine.PlaySound(XerocBoss.ScreamSound);

            Vector2 rageBarPositionWorld = RageScreenPosition + Main.screenPosition + new Vector2(20f, 4f) * Main.UIScale;
            Vector2 adrenalineBarPositionWorld = AdrenalineScreenPosition + Main.screenPosition + new Vector2(-20f, 4f) * Main.UIScale;
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

            // Create destruction gores.
            for (int i = 1; i <= 4; i++)
                Gore.NewGore(new EntitySource_WorldEvent(), rageBarPositionWorld + Main.rand.NextVector2Circular(50f, 20f), Main.rand.NextVector2CircularEdge(4f, 4f), ModContent.Find<ModGore>("NoxusBoss", $"RageBar{i}").Type, Main.UIScale * 0.75f);
            for (int i = 1; i <= 3; i++)
                Gore.NewGore(new EntitySource_WorldEvent(), adrenalineBarPositionWorld + Main.rand.NextVector2Circular(50f, 20f), Main.rand.NextVector2CircularEdge(4f, 4f), ModContent.Find<ModGore>("NoxusBoss", $"AdrenalineBar{i}").Type, Main.UIScale * 0.75f);

            // Create some screen imapct effects to add to the intensity.
            Vector2 barCenter = (rageBarPositionWorld + adrenalineBarPositionWorld) * 0.5f;

            Main.LocalPlayer.Calamity().GeneralScreenShakePower = 15f;
            ScreenEffectSystem.SetChromaticAberrationEffect(barCenter, 1.6f, 45);
            ScreenEffectSystem.SetFlashEffect(barCenter, 3f, 60);

            ExpandingChromaticBurstParticle burst = new(barCenter, Vector2.Zero, Color.Wheat, 20, 0.1f);
            GeneralParticleHandler.SpawnParticle(burst);

            ExpandingChromaticBurstParticle burst2 = new(barCenter, Vector2.Zero, Color.Wheat, 16, 0.1f);
            GeneralParticleHandler.SpawnParticle(burst2);
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
