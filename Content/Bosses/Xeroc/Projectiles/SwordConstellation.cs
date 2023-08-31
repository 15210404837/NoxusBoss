using System.Collections.Generic;
using System.IO;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.Graphics.Primitives;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.ShapeCurves;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class SwordConstellation : ModProjectile
    {
        private ShapeCurve swordShape
        {
            get
            {
                ShapeCurveManager.TryFind("Sword", out ShapeCurve curve);
                return curve.Upscale(Projectile.width * Projectile.scale * 1.414f).LinearlyTransform(SquishTransformation).Rotate(Projectile.rotation);
            }
        }

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

        public float ZPosition;

        // This stores the swordShape property in a field for performance reasons every frame, since the underlying getter method used there can be straining when done
        // many times per frame, due to looping.
        public ShapeCurve SwordShape;

        public PrimitiveTrail SlashDrawer
        {
            get;
            private set;
        }

        public int SwordSideFactor = 1;

        public bool UsePositionCacheForTrail => Projectile.ai[0] == 1f;

        public bool SlashIsAttached => Projectile.ai[2] == 0f;

        public float StarScaleFactor => Remap(Time, 150f, 300f, 1f, 2.6f);

        public static int ConvergeTime => 120;

        public ref float SwordSide => ref Projectile.ai[1];

        public ref float SlashOpacity => ref Projectile.localAI[0];

        public ref float Time => ref Projectile.localAI[1];

        public override string Texture => "Terraria/Images/Extra_89";

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
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(ZPosition);

        public override void ReceiveExtraAI(BinaryReader reader) => ZPosition = reader.ReadSingle();

        public override void AI()
        {
            // Appear from the background at first.
            if (Time <= ConvergeTime)
            {
                float zPositionInterpolant = Pow(GetLerpValue(15f, ConvergeTime, Time, true), 0.8f);
                float zPositionVariance = Projectile.identity * 18557.34173f % 12f;
                ZPosition = Lerp(zPositionVariance + 7f, 1.3f, zPositionInterpolant);
                Projectile.rotation = PiOver4;
            }

            // Die if Xeroc is not present.
            if (XerocBoss.Myself is null)
            {
                Projectile.Kill();
                return;
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

            if (Projectile.FinalExtraUpdate())
                Time++;

            // Store the sword shape.
            SwordShape = swordShape;
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

        public float GetStarMovementInterpolant(int index)
        {
            int starPrepareStartTime = (int)(index * ConvergeTime / 540f) + 10;
            return Pow(GetLerpValue(starPrepareStartTime, starPrepareStartTime + 50f, Time, true), 0.65f);
        }

        public Vector2 GetStarPosition(int index)
        {
            // Calculate the seed for the starting spots of the sword's stars. This is randomized based on both projectile index and star index, so it should be
            // pretty unique across the fight.
            ulong starSeed = (ulong)Projectile.identity * 113uL + (ulong)index * 602uL + 54uL;

            // Orient the stars in such a way that they come from the background in random spots.
            Vector2 starDirectionFromCenter = (SwordShape.ShapePoints[index] - SwordShape.Center).SafeNormalize(Vector2.UnitY);
            Vector2 randomOffset = new(Lerp(-950f, 950f, RandomFloat(ref starSeed)), Lerp(-750f, 750f, RandomFloat(ref starSeed)));
            Vector2 startingSpot = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f + starDirectionFromCenter * 500f + randomOffset;
            Vector2 swordPosition = SwordShape.ShapePoints[index] + Projectile.Center - Main.screenPosition;
            return Vector2.Lerp(startingSpot, swordPosition, GetStarMovementInterpolant(index));
        }

        public void DrawBloomFlare(Vector2 drawPosition, float colorInterpolant, float scale, int index)
        {
            Texture2D bloomFlare = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/BloomFlare").Value;
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 1.1f + Projectile.identity;
            Color bloomFlareColor1 = Color.Lerp(Color.SkyBlue, Color.Orange, colorInterpolant);
            Color bloomFlareColor2 = Color.Lerp(Color.Cyan, Color.White, colorInterpolant);
            bloomFlareColor1 = Color.Lerp(bloomFlareColor1, Color.Cyan, SlashOpacity);

            bloomFlareColor1 *= Remap(GetStarMovementInterpolant(index), 0f, 1f, 0.5f, 1f);
            bloomFlareColor2 *= Remap(GetStarMovementInterpolant(index), 0f, 1f, 0.5f, 1f);

            // Make the stars individually twinkle.
            float scaleFactorPhaseShift = index * 5.853567f * (index % 2 == 0).ToDirectionInt();
            float scaleFactor = Lerp(0.75f, 1.25f, Cos01(Main.GlobalTimeWrappedHourly * 6f + scaleFactorPhaseShift));
            scale *= scaleFactor;

            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor1 with { A = 0 } * Projectile.Opacity * 0.33f, bloomFlareRotation, bloomFlare.Size() * 0.5f, scale * 0.11f, 0, 0f);
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor2 with { A = 0 } * Projectile.Opacity * 0.41f, -bloomFlareRotation, bloomFlare.Size() * 0.5f, scale * 0.08f, 0, 0f);
        }

        public void DrawStar(Vector2 drawPosition, float colorInterpolant, float scale, int index)
        {
            // Draw a bloom flare behind the star.
            DrawBloomFlare(drawPosition, colorInterpolant, scale * XerocBoss.Myself.scale, index);

            // Draw the star.
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Color color = Projectile.GetAlpha(Color.Wheat) with { A = 0 };

            // Make the color interpolant err towards more cyan stars based on the slash.
            color = Color.Lerp(color, Projectile.GetAlpha(Color.DeepSkyBlue) with { A = 0 }, SlashOpacity * 0.7f);
            color *= Remap(GetStarMovementInterpolant(index), 0f, 1f, 0.3f, 1f);

            Main.spriteBatch.Draw(texture, drawPosition, frame, color, Projectile.rotation, frame.Size() * 0.5f, scale * 0.5f, 0, 0f);
            Main.spriteBatch.Draw(texture, drawPosition, frame, color, Projectile.rotation - Pi / 3f, frame.Size() * 0.5f, scale * 0.3f, 0, 0f);
            Main.spriteBatch.Draw(texture, drawPosition, frame, color, Projectile.rotation + Pi / 3f, frame.Size() * 0.5f, scale * 0.3f, 0, 0f);
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
            slashShader.SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/Cracks"));
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
            ulong starSeed = (ulong)Projectile.identity * 674uL + 25uL;

            // Draw the slash.
            Main.spriteBatch.EnterShaderRegion();

            var slashShader = GameShaders.Misc["CalamityMod:ExobladeSlash"];
            SlashDrawer ??= new(SlashWidthFunction, SlashColorFunction, null, slashShader);
            DrawAfterimageTrail(SlashDrawer, Projectile, Projectile.oldPos, SlashOpacity, SwordSide, UsePositionCacheForTrail);

            // Draw the bloom behind the blade.
            Texture2D invisible = ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj").Value;
            Effect telegraphShader = Terraria.Graphics.Effects.Filters.Scene["CalamityMod:SpreadTelegraph"].GetShader().Shader;
            telegraphShader.Parameters["centerOpacity"].SetValue(1.5f);
            telegraphShader.Parameters["mainOpacity"].SetValue(Sqrt(SlashOpacity));
            telegraphShader.Parameters["halfSpreadAngle"].SetValue(Pi * 4f);
            telegraphShader.Parameters["edgeColor"].SetValue(Color.Wheat.ToVector3());
            telegraphShader.Parameters["centerColor"].SetValue(Color.Cyan.ToVector3());
            telegraphShader.Parameters["edgeBlendLength"].SetValue(0.4f);
            telegraphShader.Parameters["edgeBlendStrength"].SetValue(3f);
            telegraphShader.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(invisible, Projectile.Center - Main.screenPosition, null, Color.White, 0f, invisible.Size() * 0.5f, 800f, 0, 0);
            Main.spriteBatch.ExitShaderRegion();

            // Draw the stars that compose the blade.
            for (int i = 0; i < SwordShape.ShapePoints.Count; i++)
            {
                float colorInterpolant = Sqrt(RandomFloat(ref starSeed));
                float scale = StarScaleFactor * Lerp(0.15f, 0.95f, RandomFloat(ref starSeed)) * Projectile.scale;

                // Make the scale more uniform as the star scale factor gets larger.
                scale = Remap(StarScaleFactor * 0.75f + SlashOpacity, scale, StarScaleFactor, 1f, 2.5f);

                Vector2 shapeDrawPosition = GetStarPosition(i);
                DrawStar(shapeDrawPosition, colorInterpolant, scale * 0.4f, i);
            }

            return false;
        }
    }
}
