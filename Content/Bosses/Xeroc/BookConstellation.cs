using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Core.ShapeCurves;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class BookConstellation : ModProjectile
    {
        private ShapeCurve bookShape
        {
            get
            {
                ShapeCurveManager.TryFind("Book", out ShapeCurve curve);
                return curve.Upscale(Projectile.width * Projectile.scale * 0.4f);
            }
        }

        private Texture2D bloomFlare;

        private Texture2D bloomCircle;

        private Texture2D starTexture;

        // This stores the bookShape property in a field for performance reasons every frame, since the underlying getter method used there can be straining when done
        // many times per frame, due to looping.
        public ShapeCurve BookShape;

        public float StarScaleFactor => Remap(Time, 150f, 300f, 1f, 2.6f);

        public float CircleOpacity => GetLerpValue(45f, 105f, Time - ConvergeTime, true);

        public static int ConvergeTime => 150;

        public ref float Time => ref Projectile.localAI[1];

        public override string Texture => $"Terraria/Images/Extra_89";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Grand Wisdom");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 15;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 840;
            Projectile.height = 840;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = ConvergeTime + SuperCosmicBeam.LaserLifetime + 175;
        }

        public override void AI()
        {
            // Fade in.
            Projectile.scale = GetLerpValue(0f, 45f, Projectile.timeLeft, true);
            Projectile.Opacity = GetLerpValue(0f, 45f, Time, true) * Projectile.scale;

            // Die if Xeroc is not present.
            if (XerocBoss.Myself is null)
            {
                Projectile.Kill();
                return;
            }

            // Stick to Xeroc and inherit the current direction from him.
            Projectile.velocity = XerocBoss.Myself.ai[2].ToRotationVector2();
            Projectile.Center = XerocBoss.Myself.Center + Projectile.velocity * 200f;
            Projectile.rotation = XerocBoss.Myself.ai[2];

            // Store the book shape.
            BookShape = bookShape;

            // Create charge particles.
            if (CircleOpacity >= 1f && Time <= ConvergeTime + 150f)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 lightAimPosition = Projectile.Center + Projectile.velocity.RotatedBy(PiOver2) * Main.rand.NextFloatDirection() * Projectile.scale * 400f + Main.rand.NextVector2Circular(10f, 10f);
                    Vector2 lightSpawnPosition = Projectile.Center + Projectile.velocity * 75f + Projectile.velocity.RotatedByRandom(1.53f) * Main.rand.NextFloat(330f, 960f);
                    Vector2 lightVelocity = (lightAimPosition - lightSpawnPosition) * 0.06f;
                    SquishyLightParticle light = new(lightSpawnPosition, lightVelocity, 0.33f, Color.Pink, 19, 0.04f, 3f, 8f);
                    GeneralParticleHandler.SpawnParticle(light);
                }
            }

            Time++;
        }

        public float GetStarMovementInterpolant(int index)
        {
            int starPrepareStartTime = (int)(index * ConvergeTime / 400f) + 10;
            return Pow(GetLerpValue(starPrepareStartTime, starPrepareStartTime + 75f, Time, true), 0.68f);
        }

        public Vector2 GetStarPosition(int index)
        {
            // Calculate the seed for the starting spots of the book's stars. This is randomized based on both projectile index and star index, so it should be
            // pretty unique across the fight.
            ulong starSeed = (ulong)Projectile.identity * 113uL + (ulong)index * 602uL + 54uL;

            // Orient the stars in such a way that they come from the background in random spots.
            Vector2 starDirectionFromCenter = (BookShape.ShapePoints[index] - BookShape.Center).SafeNormalize(Vector2.UnitY);
            Vector2 randomOffset = new(Lerp(-1350f, 1350f, RandomFloat(ref starSeed)), Lerp(-920f, 920f, RandomFloat(ref starSeed)));
            Vector2 startingSpot = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f + starDirectionFromCenter * 500f + randomOffset;
            Vector2 bookPosition = BookShape.ShapePoints[index] + Projectile.Center - Main.screenPosition;

            // Apply a tiny, random offset to the book position.
            bookPosition += Lerp(-TwoPi, TwoPi, RandomFloat(ref starSeed)).ToRotationVector2() * Lerp(1.5f, 5.3f, RandomFloat(ref starSeed));

            return Vector2.Lerp(startingSpot, bookPosition, GetStarMovementInterpolant(index));
        }

        public void DrawBloom()
        {
            Color bloomCircleColor = Projectile.GetAlpha(Color.Orange) * 0.4f;
            Vector2 bloomDrawPosition = Projectile.Center - Main.screenPosition;

            // Draw the bloom circle.
            Main.spriteBatch.Draw(bloomCircle, bloomDrawPosition, null, bloomCircleColor, 0f, bloomCircle.Size() * 0.5f, 5f, 0, 0f);

            // Draw bloom flares that go in opposite rotations.
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * -0.4f;
            Color bloomFlareColor = Projectile.GetAlpha(Color.LightCoral) * 0.75f;
            Main.spriteBatch.Draw(bloomFlare, bloomDrawPosition, null, bloomFlareColor, bloomFlareRotation, bloomFlare.Size() * 0.5f, 2f, 0, 0f);
            Main.spriteBatch.Draw(bloomFlare, bloomDrawPosition, null, bloomFlareColor, bloomFlareRotation * -0.7f, bloomFlare.Size() * 0.5f, 2f, 0, 0f);
        }

        public void DrawBloomFlare(Vector2 drawPosition, float colorInterpolant, float scale, int index)
        {
            float bloomFlareRotation = Main.GlobalTimeWrappedHourly * 1.1f + Projectile.identity;
            Color bloomFlareColor1 = Color.Lerp(Color.Red, Color.Yellow, Pow(colorInterpolant, 2f));
            Color bloomFlareColor2 = Color.Lerp(Color.Orange, Color.White, colorInterpolant);

            bloomFlareColor1 *= Remap(GetStarMovementInterpolant(index), 0f, 1f, 0.5f, 1f);
            bloomFlareColor2 *= Remap(GetStarMovementInterpolant(index), 0f, 1f, 0.5f, 1f);

            // Make the stars individually twinkle.
            float scaleFactorPhaseShift = index * 5.853567f * (index % 2 == 0).ToDirectionInt();
            float scaleFactor = Lerp(0.75f, 1.25f, Cos(Main.GlobalTimeWrappedHourly * 6f + scaleFactorPhaseShift) * 0.5f + 0.5f);
            scale *= scaleFactor;

            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor1 with { A = 0 } * Projectile.Opacity * 0.33f, bloomFlareRotation, bloomFlare.Size() * 0.5f, scale * 0.11f, 0, 0f);
            Main.spriteBatch.Draw(bloomFlare, drawPosition, null, bloomFlareColor2 with { A = 0 } * Projectile.Opacity * 0.41f, -bloomFlareRotation, bloomFlare.Size() * 0.5f, scale * 0.08f, 0, 0f);
        }

        public void DrawStar(Vector2 drawPosition, float colorInterpolant, float scale, int index)
        {
            // Draw a bloom flare behind the star.
            DrawBloomFlare(drawPosition, colorInterpolant, scale * XerocBoss.Myself.scale, index);

            // Draw the star.
            Rectangle frame = starTexture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Color color = Projectile.GetAlpha(Color.Wheat) with { A = 0 } * Remap(GetStarMovementInterpolant(index), 0f, 1f, 0.3f, 1f);

            Main.spriteBatch.Draw(starTexture, drawPosition, frame, color, Projectile.rotation, frame.Size() * 0.5f, scale * 0.5f, 0, 0f);
            Main.spriteBatch.Draw(starTexture, drawPosition, frame, color, Projectile.rotation - Pi / 3f, frame.Size() * 0.5f, scale * 0.3f, 0, 0f);
            Main.spriteBatch.Draw(starTexture, drawPosition, frame, color, Projectile.rotation + Pi / 3f, frame.Size() * 0.5f, scale * 0.3f, 0, 0f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Store textures for efficiency.
            bloomCircle ??= ModContent.Request<Texture2D>("CalamityMod/Skies/XerocLight", AssetRequestMode.ImmediateLoad).Value;
            bloomFlare ??= ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/BloomFlare", AssetRequestMode.ImmediateLoad).Value;
            starTexture ??= ModContent.Request<Texture2D>(Texture, AssetRequestMode.ImmediateLoad).Value;

            ulong starSeed = (ulong)Projectile.identity * 674uL + 25uL;

            // Draw bloom behind everything.
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            DrawBloom();
            Main.spriteBatch.ResetBlendState();

            // Draw the stars that compose the book's outline.
            for (int i = 0; i < BookShape.ShapePoints.Count; i++)
            {
                float colorInterpolant = Sqrt(RandomFloat(ref starSeed));
                float scale = StarScaleFactor * Lerp(0.3f, 0.95f, RandomFloat(ref starSeed)) * Projectile.scale;

                // Make the scale more uniform as the star scale factor gets larger.
                scale = Remap(StarScaleFactor * 0.75f, scale, StarScaleFactor, 1f, 2.5f) * 0.7f;

                Vector2 shapeDrawPosition = GetStarPosition(i);
                DrawStar(shapeDrawPosition, colorInterpolant, scale * 0.4f, i);
            }

            // Draw the magic circle.
            Main.spriteBatch.EnterShaderRegion();
            DrawMagicCircle();
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }

        public void DrawMagicCircle()
        {
            // Acquire textures.
            Texture2D magicCircle = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/XerocLightCircle").Value;
            Texture2D magicCircleCenter = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/XerocLightCircleCenter").Value;

            // Determine draw values.
            Vector2 circleDrawPosition = Projectile.Center + Projectile.velocity * 200f - Main.screenPosition;
            Vector2 circleScale = Vector2.One * Projectile.scale * Projectile.Opacity * 1.5f;
            Color circleColor = Projectile.GetAlpha(Color.Coral) * CircleOpacity;

            // Apply the shader.
            var magicCircleShader = GameShaders.Misc[$"{Mod.Name}:MagicCircleShader"];
            CalamityUtils.CalculatePerspectiveMatricies(out Matrix viewMatrix, out Matrix projectionMatrix);
            magicCircleShader.UseSaturation(Projectile.rotation);
            magicCircleShader.Shader.Parameters["uDirection"].SetValue((float)Projectile.direction);
            magicCircleShader.Shader.Parameters["uCircularRotation"].SetValue(-Main.GlobalTimeWrappedHourly * 3.87f);
            magicCircleShader.Shader.Parameters["uImageSize0"].SetValue(magicCircle.Size());
            magicCircleShader.Shader.Parameters["overallImageSize"].SetValue(magicCircle.Size());
            magicCircleShader.Shader.Parameters["uWorldViewProjection"].SetValue(viewMatrix * projectionMatrix);
            magicCircleShader.Apply();

            // Draw the circle.
            Main.EntitySpriteDraw(magicCircle, circleDrawPosition, null, circleColor with { A = 0 }, 0f, magicCircle.Size() * 0.5f, circleScale, 0, 0);

            // Draw the eye on top of the circle.
            magicCircleShader.Shader.Parameters["uImageSize0"].SetValue(magicCircleCenter.Size());
            magicCircleShader.Shader.Parameters["overallImageSize"].SetValue(magicCircleCenter.Size());
            magicCircleShader.Shader.Parameters["uCircularRotation"].SetValue(0f);
            magicCircleShader.Apply();
            Main.EntitySpriteDraw(magicCircleCenter, circleDrawPosition, null, Color.Lerp(circleColor, Color.White * CircleOpacity, 0.5f) with { A = 0 }, 0f, magicCircleCenter.Size() * 0.5f, circleScale, 0, 0);
        }
    }
}
