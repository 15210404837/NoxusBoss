using System;
using System.Collections.Generic;
using System.Linq;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Xeroc.SpecificEffectManagers;
using NoxusBoss.Core.Graphics.Automators;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class Quasar : ModProjectile, IDrawsWithShader
    {
        public class EnergySuckParticle
        {
            public int Time;

            public int Lifetime;

            public float Opacity;

            public Color DrawColor;

            public Vector2 Center;

            public Vector2 Velocity;

            public void Update(Vector2 destination)
            {
                Center += Velocity;
                Velocity = Vector2.Lerp(Velocity, (destination - Center) * 0.1f, 0.04f);
                Time++;
                Opacity = GetLerpValue(Center.Distance(destination), 90f, 205f, true) * GetLerpValue(0f, 16f, Time, true) * 0.56f;
            }
        }

        public SlotId LoopSound;

        public List<EnergySuckParticle> Particles = new();

        public ref float Time => ref Projectile.ai[0];

        public ref float ReboundCountdown => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 810;
            Projectile.height = 500;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = Supernova.Lifetime;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            // No Xeroc? Die.
            if (XerocBoss.Myself is null)
            {
                Projectile.Kill();
                return;
            }

            // Grow over time.
            Projectile.scale = CalamityUtils.ExpInEasing(GetLerpValue(0f, 35f, Time, true), 0) * 10f;

            // Accelerate towards the target.
            Player target = Main.player[XerocBoss.Myself.target];

            if (ReboundCountdown > 0f)
            {
                Projectile.velocity *= 0.99f;
                ReboundCountdown--;
            }
            else if (!Projectile.WithinRange(target.Center, 380f) && Time >= 56f)
            {
                Vector2 force = Projectile.SafeDirectionTo(target.Center) * Projectile.scale * 0.09f;
                if (AllProjectilesByID(Projectile.type).Count() >= 2)
                    force *= 0.56f;

                Projectile.velocity += force;

                // Make the black hole go faster if it's moving away from the target.
                if (Vector2.Dot(Projectile.SafeDirectionTo(target.Center), Projectile.velocity) < 0f)
                    Projectile.velocity += force * 1.3f;

                // Zip towards the target if they're not moving much.
                if (target.velocity.Length() <= 4f)
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 22f, 0.08f);
            }

            // Enforce a hard limit on the velocity.
            Projectile.velocity = Projectile.velocity.ClampMagnitude(0f, 30f);

            // Create suck energy particles.
            Vector2 energySpawnPosition = Projectile.Center + Main.rand.NextVector2Unit() * Projectile.width * Main.rand.NextFloat(0.6f, 1.3f);
            Vector2 energyVelocity = (Projectile.Center - energySpawnPosition).RotatedBy(PiOver2) * 0.037f;
            Particles.Add(new()
            {
                Center = energySpawnPosition,
                Velocity = energyVelocity,
                Opacity = 1f,
                DrawColor = Color.Lerp(Color.Lerp(Color.Yellow, Color.OrangeRed, Main.rand.NextFloat()), Color.White, XerocSky.KaleidoscopeInterpolant * 0.9f),
                Lifetime = 30
            });

            // Update all particles.
            Particles.RemoveAll(p => p.Time >= p.Lifetime);
            for (int i = 0; i < Particles.Count; i++)
            {
                var p = Particles[i];
                p.Update(Projectile.Center);
            }

            // Dissipate at the end.
            Projectile.Opacity = GetLerpValue(8f, 60f, Projectile.timeLeft, true);

            // Start the loop sound on the first frame.
            if (Projectile.localAI[0] == 0f || !SoundEngine.TryGetActiveSound(LoopSound, out _))
            {
                LoopSound = SoundEngine.PlaySound(XerocBoss.QuasarLoopSound, Projectile.Center);
                Projectile.localAI[0] = 1f;
            }

            // Update the loop sound.
            if (SoundEngine.TryGetActiveSound(LoopSound, out ActiveSound s))
            {
                s.Position = Projectile.Center;
                s.Volume = Projectile.Opacity;
            }

            // Avoid other quasars.
            float pushForce = 2f;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile otherProj = Main.projectile[i];

                // Short circuits to make the loop as fast as possible
                if (!otherProj.active || i == Projectile.whoAmI)
                    continue;

                // If the other projectile is indeed the same owned by the same player and they're too close, nudge them away.
                bool sameProjType = otherProj.type == Projectile.type;
                float taxicabDist = Math.Abs(Projectile.position.X - otherProj.position.X) + Math.Abs(Projectile.position.Y - otherProj.position.Y);
                if (sameProjType && taxicabDist < Projectile.width * 0.9f)
                {
                    if (Projectile.position.X < otherProj.position.X)
                        Projectile.velocity.X -= pushForce;
                    else
                        Projectile.velocity.X += pushForce;

                    // Prevent the momentum gained from the pushed quasars serving as a basis for it to slam so quickly into the player that it basically telefrags them.
                    ReboundCountdown = 27f;
                    otherProj.ModProjectile<Quasar>().ReboundCountdown = 27f;

                    if (RadialScreenShoveSystem.DistortionTimer <= 0)
                        RadialScreenShoveSystem.Start((Projectile.Center + otherProj.Center) * 0.5f, 30);
                }
            }

            Time++;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            Main.spriteBatch.EnterShaderRegion();

            Vector2 scale = Vector2.One * Projectile.width * Projectile.scale * 0.2f;
            Texture2D pixel = ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj").Value;

            var blackHoleShader = ShaderManager.GetShader("BlackHoleShader");
            blackHoleShader.TrySetParameter("spriteSize", scale);
            blackHoleShader.TrySetParameter("blackHoleRadius", scale.X * 0.5f);
            blackHoleShader.TrySetParameter("spiralFadeSharpness", 6.6f);
            blackHoleShader.TrySetParameter("spiralSpinSpeed", 7f);
            blackHoleShader.TrySetParameter("generalOpacity", Projectile.Opacity);
            blackHoleShader.TrySetParameter("transformation", Matrix.CreateScale(1.1f, Projectile.width / (float)Projectile.height, 1f));
            blackHoleShader.SetTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/Neurons2"), 1);
            blackHoleShader.Apply();

            Color quasarColor = Color.Lerp(Color.Orange, Color.Wheat, XerocSky.KaleidoscopeInterpolant);
            Main.spriteBatch.Draw(pixel, Projectile.Center - Main.screenPosition, null, quasarColor * Projectile.Opacity, Projectile.rotation, pixel.Size() * 0.5f, scale, 0, 0f);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // Create a sucking effect over the black hole.
            Texture2D suckTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Particles/ChromaticBurst").Value;
            float suckPulse = 1f - Main.GlobalTimeWrappedHourly * 4f % 1f;
            float suckRotation = Main.GlobalTimeWrappedHourly * -3f;
            Color suckColor = Color.Wheat * GetLerpValue(0.05f, 0.25f, suckPulse, true) * GetLerpValue(1f, 0.67f, suckPulse, true) * Projectile.Opacity;
            Main.spriteBatch.Draw(suckTexture, Projectile.Center - Main.screenPosition, null, suckColor, suckRotation, suckTexture.Size() * 0.5f, Vector2.One * suckPulse * 2.6f, 0, 0f);

            // Draw particles.
            DrawParticles();

            Main.spriteBatch.ExitShaderRegion();
        }

        public void DrawParticles()
        {
            float baseScale = 1f;

            foreach (EnergySuckParticle particle in Particles)
            {
                Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Particles/Light").Value;
                Texture2D bloomTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

                float squish = 0.21f;
                float rotation = particle.Velocity.ToRotation();
                Vector2 origin = tex.Size() * 0.5f;
                Vector2 scale = new(baseScale - baseScale * squish * 0.3f, baseScale * squish);
                float properBloomSize = tex.Height / (float)bloomTex.Height;

                Vector2 drawPosition = particle.Center - Main.screenPosition;

                Main.spriteBatch.Draw(bloomTex, drawPosition, null, particle.DrawColor * particle.Opacity * Projectile.Opacity * 0.8f, rotation, bloomTex.Size() * 0.5f, scale * properBloomSize * 2f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(tex, drawPosition, null, particle.DrawColor * particle.Opacity * Projectile.Opacity, rotation, origin, scale * 1.1f, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(tex, drawPosition, null, Color.White * particle.Opacity * Projectile.Opacity * 0.9f, rotation, origin, scale, SpriteEffects.None, 0f);
            }
        }

        // Prevent cheap hits if the quasar happens to spawn near a player at first.
        public override bool? CanDamage() => Time >= 48f && Projectile.Opacity >= 0.8f;

        public override void Kill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(LoopSound, out ActiveSound s))
                s.Stop();
        }
    }
}
