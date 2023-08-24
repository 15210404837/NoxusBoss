using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics.SpecificEffectManagers
{
    public class XerocPlayerDeathVisualsPlayer : ModPlayer
    {
        public bool WasKilledByXeroc
        {
            get;
            private set;
        }

        public ManagedRenderTarget PlayerAtTimeOfDeath
        {
            get;
            private set;
        }

        public int DeathTimerOverride
        {
            get;
            set;
        }

        public bool TakePlayerScreenshot
        {
            get;
            set;
        }

        public static int DeathTimerMax => 360;

        public override void Load()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            Main.OnPreDraw += ManagePlayerTarget;
            On_LegacyPlayerRenderer.DrawPlayerFull += DrawPlayerFull;
        }

        private void DrawPlayerFull(On_LegacyPlayerRenderer.orig_DrawPlayerFull orig, LegacyPlayerRenderer self, Camera camera, Player player)
        {
            // Draw the dissipation effect.
            var modPlayer = player.GetModPlayer<XerocPlayerDeathVisualsPlayer>();
            if (!modPlayer.TakePlayerScreenshot && modPlayer.WasKilledByXeroc && !player.ShouldNotDraw && player.dead)
            {
                // Prepare the sprite batch for shaders.
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

                float deathAnimationCompletion = GetLerpValue(0f, DeathTimerMax * 0.64f, modPlayer.DeathTimerOverride, true);
                Texture2D screenshot = modPlayer.PlayerAtTimeOfDeath.Target;

                // Prepare the psychedelic dissipation shader.
                float fadeOut = GetLerpValue(0.49f, 0.71f, deathAnimationCompletion, true);
                float dissipationIntensity = GetLerpValue(0.15f, 0.3f, deathAnimationCompletion, true) * Remap(deathAnimationCompletion, 0.2f, 0.64f, 0.001f, 0.1577f);
                float scale = Remap(deathAnimationCompletion, 0.3f, 0.6f, 1f, 1.4f);
                Color screenshotColor = Color.Lerp(Color.White, Color.Gold with { A = 0 }, GetLerpValue(0.05f, 0.12f, deathAnimationCompletion, true)) * (1f - fadeOut);

                var afterimageShader = ShaderManager.GetShader("XerocPsychedelicAfterimageShader");
                afterimageShader.TrySetParameter("uScreenResolution", screenshot.Size());
                afterimageShader.TrySetParameter("warpSpeed", dissipationIntensity);
                afterimageShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/TurbulentNoise"), 1);
                afterimageShader.Apply();
                Main.spriteBatch.Draw(screenshot, player.Center - Main.screenPosition, null, screenshotColor, 0f, screenshot.Size() * 0.5f, scale, 0, 0f);

                // Return to regular drawing.
                Main.spriteBatch.End();
                return;
            }

            orig(self, camera, player);
        }

        public override void Unload() => Main.OnPreDraw -= ManagePlayerTarget;

        private static void ManagePlayerTarget(GameTime obj)
        {
            List<XerocPlayerDeathVisualsPlayer> playersInNeedOfScreenshot = new();
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.active)
                    continue;

                var modPlayer = p.GetModPlayer<XerocPlayerDeathVisualsPlayer>();
                if (!modPlayer.TakePlayerScreenshot)
                    continue;

                playersInNeedOfScreenshot.Add(modPlayer);
            }

            // Don't go any further if no players are in need of screenshots.
            if (!playersInNeedOfScreenshot.Any())
                return;

            // Go through all players that need a screenshot and take on, drawing the results to their render target.
            var gd = Main.instance.GraphicsDevice;
            foreach (var player in playersInNeedOfScreenshot)
            {
                // Go to the screenshot target.
                player.PlayerAtTimeOfDeath ??= new(false, (width, height) =>
                {
                    return new(Main.instance.GraphicsDevice, 350, 350);
                });
                var screenshotTarget = player.PlayerAtTimeOfDeath.Target;
                gd.SetRenderTarget(screenshotTarget);
                gd.Clear(Color.Transparent);

                Vector2 oldPlayerPosition = player.Player.position;
                player.Player.Center = player.PlayerAtTimeOfDeath.Target.Size() * 0.5f + Main.Camera.UnscaledPosition;
                Lighting.AddLight(player.Player.Center, Vector3.One);
                typeof(LegacyPlayerRenderer).GetMethod("DrawPlayerFull", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Main.PlayerRenderer, new object[]
                {
                    Main.Camera, player.Player
                });
                player.Player.position = oldPlayerPosition;

                player.TakePlayerScreenshot = false;
            }

            // Return to the backbuffer.
            gd.SetRenderTarget(null);
        }

        public override void UpdateDead()
        {
            // Disable the natural death vfx where the player's body parts fly around if Xeroc killed them.
            // Instead, the player will dissolve into holy light.
            if (WasKilledByXeroc)
            {
                Player.headPosition = Vector2.Zero;
                Player.bodyPosition = Vector2.Zero;
                Player.legPosition = Vector2.Zero;
                Player.headVelocity = Vector2.Zero;
                Player.bodyVelocity = Vector2.Zero;
                Player.legVelocity = Vector2.Zero;
                Player.headRotation = 0f;
                Player.bodyRotation = 0f;
                Player.legRotation = 0f;

                // Release death particles.
                float deathAnimationCompletion = GetLerpValue(0f, DeathTimerMax * 0.45f, DeathTimerOverride, true);
                float particleAppearInterpolant = GetLerpValue(0.02f, 0.1f, deathAnimationCompletion, true);
                float deathFadeOut = GetLerpValue(0.49f, 0.71f, deathAnimationCompletion, true);
                if (Main.rand.NextFloat() > deathFadeOut)
                {
                    for (int i = 0; i < particleAppearInterpolant * (1f - deathFadeOut) * 4f; i++)
                    {
                        Dust light = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Square(-25f, 25f), 264);
                        light.velocity = -Vector2.UnitY * Main.rand.NextFloat(1.2f, 2.5f);
                        light.color = Color.Lerp(Color.Wheat, Color.Gold, Sin01(Main.GlobalTimeWrappedHourly * 10f + i * 0.2f)) * particleAppearInterpolant * (1f - deathFadeOut);
                        light.scale = Main.rand.NextFloat(0.1f, 1.8f) * Lerp(1f, 0.1f, deathAnimationCompletion);
                        light.noGravity = true;
                    }
                }
            }
        }

        public override void PreUpdate()
        {
            if (!Player.dead)
                DeathTimerOverride = 0;
            else if (WasKilledByXeroc)
            {
                DeathTimerOverride = Clamp(DeathTimerOverride + 1, 0, DeathTimerMax);
                if (DeathTimerOverride < DeathTimerMax)
                    Player.respawnTimer = 8;
            }
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            // Determine if the death resulted from Xeroc. Technically a handful of niche cases can cause this where it wasn't actually Xeroc who killed the player, such as
            // if two players enabled PVP while Xeroc is killing both of them, but honestly who cares.
            WasKilledByXeroc = XerocBoss.Myself is not null;

            // Use custom death text and sounds if Xeroc killed the player.
            if (WasKilledByXeroc)
            {
                MagicBurstParticle burst = new(Player.Center, Vector2.Zero, Color.Wheat, 24, 1f, 0.6f);
                GeneralParticleHandler.SpawnParticle(burst);
                RadialScreenShoveSystem.Start(Player.Center - Vector2.UnitY * 400f, 36);

                // Create burst effects.
                ScreenEffectSystem.SetFlashEffect(Player.Center - Vector2.UnitY * 500f, 0.8f, 60);
                ShakeScreen(Player.Center - Vector2.UnitY * 500f, 9f);

                TakePlayerScreenshot = true;
                damageSource = PlayerDeathReason.ByCustomReason(Language.GetText($"Mods.NoxusBoss.PlayerDeathMessages.Xeroc{Main.rand.Next(1, 18)}").Format(Player.name));
                playSound = false;
                genGore = false;

                WorldSaveSystem.XerocDeathCount++;
                CalamityNetcode.SyncWorld();
            }

            return true;
        }
    }
}
