using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CalamityMod.Events;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Content.Items.MiscOPTools;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Primitives;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace NoxusBoss.Content.Projectiles.Typeless
{
    public class ThePurifierProj : ModProjectile, IDrawAdditive, IDrawPixelated
    {
        public class ChargingEnergyStreak
        {
            public float Opacity = 1f;

            public float BaseWidth;

            public float SpeedInterpolant;

            public Color GeneralColor;

            public Vector2 CurrentOffset;

            public Vector2 StartingOffset;

            public PrimitiveTrailCopy EnergyStreakDrawer;

            public ChargingEnergyStreak(float speedInterpolant, float baseWidth, Color generalColor, Vector2 startingOffset)
            {
                // Initialize things.
                SpeedInterpolant = speedInterpolant;
                BaseWidth = baseWidth;
                GeneralColor = generalColor;
                CurrentOffset = startingOffset;
                StartingOffset = startingOffset;

                // Don't attempt to load shaders serverside.
                if (Main.netMode == NetmodeID.Server)
                    return;

                // Initialize the streak drawer.
                var streakShader = ShaderManager.GetShader("GenericTrailStreak");
                EnergyStreakDrawer ??= new(EnergyWidthFunction, EnergyColorFunction, null, true, ShaderManager.GetShader("GenericTrailStreak"));
            }

            public void Update()
            {
                CurrentOffset = Vector2.Lerp(CurrentOffset, Vector2.Zero, SpeedInterpolant);
                CurrentOffset = Utils.MoveTowards(CurrentOffset, Vector2.Zero, SpeedInterpolant * 19f);

                if (CurrentOffset.Length() <= 8f)
                {
                    StartingOffset = Vector2.Lerp(StartingOffset, Vector2.Zero, SpeedInterpolant * 1.4f);
                    Opacity = Clamp(Opacity * 0.94f - 0.09f, 0f, 1f);
                }
            }

            public float EnergyWidthFunction(float completionRatio) => BaseWidth - (1f - completionRatio) * 3f;

            public Color EnergyColorFunction(float completionRatio) => GeneralColor with { A = 0 } * Opacity;
        }

        public List<ChargingEnergyStreak> EnergyStreaks = new();

        public float AnimationCompletion => Time / Lifetime;

        public ref float Time => ref Projectile.ai[0];

        public const int Lifetime = 900;

        public override string Texture => "NoxusBoss/Content/Items/MiscOPTools/ThePurifier";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.Opacity = 0f;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            // Play the buildup sound on the first frame.
            // If in multiplayer, broadcast a warning as well.
            if (Projectile.localAI[0] == 0f)
            {
                if (Main.netMode == NetmodeID.Server)
                    CreateDeploymentAlert();
                SoundEngine.PlaySound(ThePurifier.BuildupSound);
                Projectile.localAI[0] = 1f;
            }

            // Fade in.
            Projectile.Opacity = GetLerpValue(0f, 8f, Time, true);

            // Jitter before exploding.
            float jitter = GetLerpValue(0.6f, 0.9f, AnimationCompletion, true) * 3f;
            Projectile.Center += Main.rand.NextVector2Circular(1f, 1f) * jitter;

            // Shake the screen.
            SetUniversalRumble(AnimationCompletion * 10f);

            // Create chromatic aberration effects.
            if (Time % 20f == 19f)
                ScreenEffectSystem.SetChromaticAberrationEffect(Projectile.Center, AnimationCompletion * 2f, 15);

            // Create pulse rings and bloom periodically.
            if (Time % 15f == 0f)
            {
                // Play pulse sounds.
                float suckFadeIn = GetLerpValue(0.1f, 0.3f, AnimationCompletion, true);
                if (suckFadeIn >= 0.01f)
                    SoundEngine.PlaySound(BossRushEvent.TeleportSound with { PitchVariance = 0.3f, MaxInstances = 10, Volume = AnimationCompletion * suckFadeIn + 0.01f });

                Color energyColor = Color.Lerp(Color.Wheat, Color.Red, Main.rand.NextFloat(0.5f)) * suckFadeIn;
                PulseRing ring = new(Projectile.Center, Vector2.Zero, energyColor, 4.2f, 0f, 32);
                GeneralParticleHandler.SpawnParticle(ring);

                StrongBloom bloom = new(Projectile.Center, Vector2.Zero, energyColor, AnimationCompletion * 4.5f + 0.5f, 30);
                GeneralParticleHandler.SpawnParticle(bloom);

                StrongBloom bloomBright = new(Projectile.Center, Vector2.Zero, Color.White * GetLerpValue(0.45f, 0.75f, AnimationCompletion, true), 4f, 45);
                GeneralParticleHandler.SpawnParticle(bloomBright);
            }

            // Rotate and gradually slow down.
            if (Time <= 10f)
                Projectile.rotation = Projectile.velocity.ToRotation();
            else
                Projectile.rotation += Projectile.velocity.X * 0.02f;
            Projectile.velocity *= 0.978f;

            // Update all streaks.
            EnergyStreaks.RemoveAll(s => s.Opacity <= 0.003f);
            for (int i = 0; i < EnergyStreaks.Count; i++)
                EnergyStreaks[i].Update();

            // Create energy streaks at a rate proportional to the animation completion.
            if (AnimationCompletion >= 0.1f)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (Main.rand.NextFloat() >= AnimationCompletion)
                        continue;

                    float streakSpeedInterpolant = Main.rand.NextFloat(0.15f, 0.19f);
                    float streakWidth = Main.rand.NextFloat(4f, 5.6f);
                    Vector2 streakOffset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(250f, 600f);
                    Color streakColor = Color.Lerp(Color.Wheat, Color.IndianRed, Main.rand.NextFloat(0.74f));
                    EnergyStreaks.Add(new(streakSpeedInterpolant, streakWidth, streakColor * GetLerpValue(0.1f, 0.45f, AnimationCompletion, true), streakOffset));
                }
            }

            // Make everything go bright before the explosion happens.
            TotalWhiteOverlaySystem.WhiteInterpolant = GetLerpValue(0.97f, 0.99f, AnimationCompletion, true);

            Time++;
        }

        public void CreateDeploymentAlert()
        {
            string playerWhoWillBeBlamed = Main.player[Projectile.owner].name;

            // Blame a random player if there are more than three people present.
            List<Player> activePlayers = Main.player.Where(p => p.active && !p.dead).ToList();
            if (Main.rand.NextBool() && activePlayers.Count >= 3)
                playerWhoWillBeBlamed = Main.rand.Next(activePlayers).name;

            string text = Language.GetText($"Mods.NoxusBoss.Dialog.PurifierMultiplayerUseAlertText").Format(playerWhoWillBeBlamed);
            BroadcastText(text, Color.Red);
        }

        public override void Kill(int timeLeft)
        {
            WorldGen.generatingWorld = true;
            SoundEngine.PlaySound(XerocBoss.ScreamSoundLong);

            // Kick clients out.
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                WorldGen.generatingWorld = false;
                Netplay.Disconnect = true;
                Main.netMode = NetmodeID.SinglePlayer;
            }

            GenerationProgress _ = new();
            new Thread(context =>
            {
                WorldGen.worldGenCallback(context);
                TotalWhiteOverlaySystem.TimeSinceWorldgenFinished = 1;
            }).Start(_);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color baseColor = Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.57f % 1f, 0.8f, 0.9f);
            return baseColor * Projectile.Opacity;
        }

        public void DrawAdditive(SpriteBatch spriteBatch)
        {
            // Draw the suck visual.
            Texture2D suckTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Particles/ChromaticBurst").Value;
            float suckPulse = 1f - Main.GlobalTimeWrappedHourly * 3.7f % 1f;
            float suckRotation = Main.GlobalTimeWrappedHourly * -3f;
            float suckFadeIn = GetLerpValue(0.1f, 0.25f, AnimationCompletion, true);
            Color suckColor = Color.Wheat * GetLerpValue(0.05f, 0.25f, suckPulse, true) * GetLerpValue(1f, 0.67f, suckPulse, true) * Projectile.Opacity * suckFadeIn;
            spriteBatch.Draw(suckTexture, Projectile.Center - Main.screenPosition, null, suckColor, suckRotation, suckTexture.Size() * 0.5f, Vector2.One * suckPulse * 2.6f, 0, 0f);
        }

        public void DrawWithPixelation()
        {
            // Configure the streak shader's texture.
            var streakShader = ShaderManager.GetShader("GenericTrailStreak");
            streakShader.SetTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/BasicTrail"), 1);

            // Draw energy streaks as primitives.
            Vector2 drawCenter = Projectile.Center - Vector2.UnitY.RotatedBy(Projectile.rotation) * Projectile.scale * 6f - Main.screenPosition;
            for (int i = 0; i < EnergyStreaks.Count; i++)
            {
                ChargingEnergyStreak streak = EnergyStreaks[i];
                Vector2 start = streak.StartingOffset;
                Vector2 end = streak.CurrentOffset;
                Vector2 midpoint1 = Vector2.Lerp(start, end, 0.33f);
                Vector2 midpoint2 = Vector2.Lerp(start, end, 0.67f);
                streak.EnergyStreakDrawer.Draw(new Vector2[]
                {
                    end, midpoint2, midpoint1, start
                }, drawCenter, 27);
            }
        }
    }
}
