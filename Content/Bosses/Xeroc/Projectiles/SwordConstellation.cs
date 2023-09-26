using System.Collections.Generic;
using System.IO;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.ShapeCurves;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class SwordConstellation : BaseXerocConstellationProjectile
    {
        public Matrix SquishTransformation
        {
            get
            {
                float angularVelocity = Abs(WrapAngle(Projectile.rotation - Projectile.oldRot[1]));
                float squishFactorX = Remap(angularVelocity, 0.06f, 0.21f, 1f, 0.67f);
                float squishFactorY = Remap(angularVelocity, 0.06f, 0.21f, 1f, 1.1f);
                return Matrix.CreateScale(squishFactorX, squishFactorY, 1f);
            }
        }

        public override int ConvergeTime => ConvergeTimeConst;

        public override int StarDrawIncrement => 1;

        public override float StarConvergenceSpeed => 0.00185f;

        public override float StarRandomOffsetFactor => 0f;

        protected override ShapeCurve constellationShape
        {
            get
            {
                ShapeCurveManager.TryFind("Sword", out ShapeCurve curve);
                return curve.Upscale(Projectile.width * Projectile.scale * 1.414f).LinearlyTransform(SquishTransformation).Rotate(Projectile.rotation);
            }
        }

        public override Color DecidePrimaryBloomFlareColor(float colorVariantInterpolant)
        {
            return Color.Lerp(Color.SkyBlue, Color.Orange, colorVariantInterpolant) * 0.33f;
        }

        public override Color DecideSecondaryBloomFlareColor(float colorVariantInterpolant)
        {
            return Color.Lerp(Color.Cyan, Color.White, colorVariantInterpolant) * 0.42f;
        }

        public float ZPosition;

        public PrimitiveTrail SlashDrawer
        {
            get;
            private set;
        }

        public int SwordSideFactor = 1;

        public bool UsePositionCacheForTrail => Projectile.ai[0] == 1f;

        public bool SlashIsAttached => Projectile.ai[2] == 0f;

        public ref float SwordSide => ref Projectile.ai[1];

        public ref float SlashOpacity => ref Projectile.localAI[0];

        public static int ConvergeTimeConst => 120;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 50;
        }

        public override void SetDefaults()
        {
            Projectile.width = 850;
            Projectile.height = 850;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = 60000;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(ZPosition);

        public override void ReceiveExtraAI(BinaryReader reader) => ZPosition = reader.ReadSingle();

        // This is done via PreAI instead of PostAI to ensure that the AI method, which defines the constellation shape, has all the correct information, rather than having a one-frame discrepancy.
        public override bool PreAI()
        {
            // Appear from the background at first.
            if (Time <= ConvergeTime)
            {
                float zPositionInterpolant = Pow(GetLerpValue(15f, ConvergeTime, Time, true), 0.8f);
                float zPositionVariance = Projectile.identity * 18557.34173f % 12f;
                ZPosition = Lerp(zPositionVariance + 7f, 1.3f, zPositionInterpolant);
                Projectile.rotation = PiOver4;
            }

            // Determine the scale of the sword based on its Z position.
            Projectile.scale = XerocBoss.Myself.scale / (ZPosition + 1f) * 2f;

            // Fade in based on how long the sword has existed.
            // Also fade out based on how close the stars are to the background.
            Projectile.Opacity = GetLerpValue(0f, 30f, Time, true) * Remap(ZPosition, 0.2f, 9f, 3.3f, 0.45f);

            // Inherit the sword rotation and slash opacity from Xeroc.
            Projectile.rotation = XerocBoss.Myself.ai[2] * (UsePositionCacheForTrail ? 1f : SwordSide);
            if (SwordSideFactor == -1f)
                Projectile.rotation = Vector2.Reflect(Projectile.rotation.ToRotationVector2(), Vector2.UnitX).ToRotation() * SwordSide;

            if (SlashIsAttached)
                SlashOpacity = XerocBoss.Myself.ai[3];
            else
                SlashOpacity = 0f;

            return true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Time <= ConvergeTime || SlashOpacity <= 0.1f)
                return false;

            float _ = 0f;
            Vector2 direction = (Projectile.rotation + SwordSide * PiOver2).ToRotationVector2();
            Vector2 start = Projectile.Center - direction * Projectile.width * Projectile.scale * 0.45f;
            Vector2 end = Projectile.Center + direction * Projectile.width * Projectile.scale * 0.45f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, Projectile.width * Projectile.scale * (UsePositionCacheForTrail ? 1.2f : 0.8f), ref _);
        }

        public float SlashWidthFunction(float completionRatio) => Projectile.scale * Projectile.width * 0.7f;

        public Color SlashColorFunction(float completionRatio) => Color.Orange * GetLerpValue(0.9f, 0.7f, completionRatio, true) * Projectile.Opacity * SlashOpacity;

        public static void DrawAfterimageTrail(PrimitiveTrail slashDrawer, Projectile projectile, Vector2[] oldPos, float slashOpacity, float swordSide, bool usePositionCacheForTrail)
        {
            if (slashOpacity <= 0f)
                return;

            // Generate slash points.
            List<Vector2> slashPoints = new();
            Vector2 direction = (projectile.rotation - PiOver2).ToRotationVector2();
            Vector2 perpendicularDirection = projectile.rotation.ToRotationVector2();
            Vector2 generalOffset = direction * projectile.width * projectile.scale * slashOpacity * -0.5f - Main.screenPosition;

            // Calculate the arc the sword has taken in the past ten frames.
            float maxAngularOffset = WrapAngle(projectile.oldRot[10] - projectile.rotation);
            if (maxAngularOffset < 0f)
                maxAngularOffset += TwoPi;
            if (maxAngularOffset >= 3f)
                maxAngularOffset = 3f;

            // Calculate a bunch of orientation stuff. This took a while to get right.
            float startingAngularOffset = Sign(Sin(maxAngularOffset)) * -0.04f;
            bool flipped = Sin(startingAngularOffset) < 0f;
            if (flipped)
            {
                maxAngularOffset *= -1f;
                startingAngularOffset *= -1f;
            }

            // Calculate slash points based on the aformentioned arc.
            for (int i = 0; i < 16; i++)
            {
                float pointInterpolant = i / 16f;
                Vector2 angularOffset = (startingAngularOffset.AngleLerp(maxAngularOffset, pointInterpolant) + projectile.rotation - PiOver2).ToRotationVector2() * projectile.width * projectile.scale * slashOpacity * 0.5f;
                Vector2 tangentOffset = (projectile.rotation + startingAngularOffset).ToRotationVector2() * projectile.scale * (i * -60f + 20f) * slashOpacity;
                Vector2 slashOffset = tangentOffset + angularOffset;
                if (swordSide == -1f)
                    slashOffset = Vector2.Reflect(slashOffset, perpendicularDirection);

                if (usePositionCacheForTrail)
                {
                    // Correction bullshit.
                    Vector2 slashPoint = oldPos[i];
                    if (i == 2 && slashPoint != Vector2.Zero)
                        slashPoint -= perpendicularDirection * swordSide * 200f;
                    if (i == 0)
                        slashPoint += perpendicularDirection * swordSide * 50f;

                    slashPoints.Add(slashPoint);

                    generalOffset = projectile.Size * 0.5f - Main.screenPosition;
                }
                else
                    slashPoints.Add(projectile.Center + slashOffset);
            }

            var slashShader = GameShaders.Misc["CalamityMod:ExobladeSlash"];
            slashShader.SetShaderTexture(ModContent.Request<Texture2D>($"{GreyscaleTexturesPath}/CrackedNoise"));
            slashShader.UseColor(Color.DeepSkyBlue);
            slashShader.UseSecondaryColor(Color.Transparent);
            slashShader.Shader.Parameters["fireColor"].SetValue(Color.White.ToVector3());
            slashShader.Shader.Parameters["flipped"].SetValue(swordSide == 1f);

            slashDrawer.DegreeOfBezierCurveCornerSmoothening = 8;
            for (int i = 0; i < 2; i++)
                slashDrawer.Draw(slashPoints, generalOffset, 70);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw the slash.
            Main.spriteBatch.EnterShaderRegion();

            var slashShader = GameShaders.Misc["CalamityMod:ExobladeSlash"];
            SlashDrawer ??= new(SlashWidthFunction, SlashColorFunction, null, slashShader);
            DrawAfterimageTrail(SlashDrawer, Projectile, Projectile.oldPos, SlashOpacity, SwordSide, UsePositionCacheForTrail);

            // Draw the bloom behind the blade.
            Effect telegraphShader = Terraria.Graphics.Effects.Filters.Scene["CalamityMod:SpreadTelegraph"].GetShader().Shader;
            telegraphShader.Parameters["centerOpacity"].SetValue(1.5f);
            telegraphShader.Parameters["mainOpacity"].SetValue(Sqrt(SlashOpacity));
            telegraphShader.Parameters["halfSpreadAngle"].SetValue(Pi * 4f);
            telegraphShader.Parameters["edgeColor"].SetValue(Color.Wheat.ToVector3());
            telegraphShader.Parameters["centerColor"].SetValue(Color.Cyan.ToVector3());
            telegraphShader.Parameters["edgeBlendLength"].SetValue(0.4f);
            telegraphShader.Parameters["edgeBlendStrength"].SetValue(3f);
            telegraphShader.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(InvisiblePixel, Projectile.Center - Main.screenPosition, null, Color.White, 0f, InvisiblePixel.Size() * 0.5f, 800f, 0, 0);
            Main.spriteBatch.ExitShaderRegion();

            // Draw the stars that compose the blade.
            base.PreDraw(ref lightColor);

            return false;
        }
    }
}
