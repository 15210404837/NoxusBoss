using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Noxus.SecondPhaseForm;
using NoxusBoss.Content.CustomWorldSeeds;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Configuration;
using NoxusBoss.Core.Graphics.Shaders;
using NoxusBoss.Core.Graphics.SpecificEffectManagers;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Noxus.SpecificEffectManagers
{
    public class NoxusSkyScene : ModSceneEffect
    {
        private static readonly FieldInfo particlesField = typeof(GeneralParticleHandler).GetField("particles", BindingFlags.NonPublic | BindingFlags.Static);

        public override bool IsSceneEffectActive(Player player) => NoxusSky.SkyIntensityOverride > 0f || NPC.AnyNPCs(ModContent.NPCType<EntropicGod>()) || NoxusSky.InProximityOfMidnightMonolith;

        public override void Load()
        {
            Terraria.GameContent.Events.On_MoonlordDeathDrama.DrawWhite += DrawFog;
        }

        private void DrawFog(Terraria.GameContent.Events.On_MoonlordDeathDrama.orig_DrawWhite orig, SpriteBatch spriteBatch)
        {
            orig(spriteBatch);

            var sky = (NoxusSky)SkyManager.Instance["NoxusBoss:NoxusSky"];

            spriteBatch.EnterShaderRegion();
            sky.DrawFog();
            spriteBatch.SetBlendState(BlendState.Additive);

            // Draw twinkles on top of the fog.
            List<Particle> particles = (List<Particle>)particlesField.GetValue(null);
            foreach (Particle p in particles)
            {
                if (p is not TwinkleParticle t)
                    continue;

                t.Opacity *= 0.6f;
                t.CustomDraw(spriteBatch);
                t.Opacity /= 0.6f;
            }
            spriteBatch.ExitShaderRegion();

            SpecialNPCLayeringSystem.EmptyDrawCache(SpecialNPCLayeringSystem.DrawCacheAfterNoxusFog);
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("NoxusBoss:NoxusSky", isActive);
        }
    }

    public class NoxusSky : CustomSky
    {
        public class FloatingRubble
        {
            public int Time;

            public int Lifetime;

            public int Variant;

            public float Depth;

            public Vector2 Position;

            public float Opacity => GetLerpValue(0f, 20f, Time, true) * GetLerpValue(Lifetime, Lifetime - 90f, Time, true);

            public void Update()
            {
                Position += Vector2.UnitY * Sin(TwoPi * Time / 180f) * 1.2f;
                Time++;
            }
        }

        private bool isActive;

        private float fogSpreadDistance;

        private Vector2 fogCenter;

        private readonly List<FloatingRubble> rubble = new();

        internal static float intensity;

        public static float FogIntensity
        {
            get;
            private set;
        }

        public static float FlashIntensity
        {
            get;
            private set;
        }

        public static float SkyIntensityOverride
        {
            get;
            set;
        }

        public static Vector2 FlashNoiseOffset
        {
            get;
            private set;
        }

        public static Vector2 FlashPosition
        {
            get;
            private set;
        }

        public static bool InProximityOfMidnightMonolith
        {
            get;
            set;
        }

        // Ideally it'd be possible to just turn InProximityOfMidnightMonolith back to false if it was already on and its effects were registered, but since NearbyEffects hooks
        // don't run on the same update cycle as the PrepareDimensionTarget method this delay exists.
        public static int TimeSinceCloseToMidnightMonolith
        {
            get;
            set;
        }

        public static Color FogColor => new(49, 40, 70);

        public static readonly SoundStyle ThunderSound = new SoundStyle("NoxusBoss/Assets/Sounds/Custom/ThunderRumble", 3) with { Volume = 0.32f, PitchVariance = 0.35f };

        public override void Update(GameTime gameTime)
        {
            // Keep the effect active when generating a Noxus World.
            float maxIntensity = 1f;
            if (WorldGen.generatingWorld && Main.gameMenu && NoxusWorldManager.Enabled)
            {
                maxIntensity = 0.88f;
                isActive = true;
            }

            // Increase the Midnight monolith proximity timer.
            if (!Main.gamePaused && Main.instance.IsActive)
                TimeSinceCloseToMidnightMonolith++;
            if (TimeSinceCloseToMidnightMonolith >= 10)
                InProximityOfMidnightMonolith = false;

            // Make the intensity go up or down based on whether the sky is in use.
            intensity = Clamp(intensity + isActive.ToDirectionInt() * 0.01f, 0f, maxIntensity);

            // Make the fog intensity go down if the sky is not in use. It does not go up by default, however.
            FogIntensity = Clamp(FogIntensity - (!isActive).ToInt(), 0f, 1f);

            // Disable ambient sky objects like wyverns and eyes appearing in front of the dark cloud of death.
            if (isActive)
                SkyManager.Instance["Ambience"].Deactivate();

            // Make flashes exponentially decay into nothing.
            FlashIntensity *= 0.86f;

            // Randomly create flashes.
            int flashCreationChance = 540;
            int noxusIndex = NPC.FindFirstNPC(ModContent.NPCType<EntropicGod>());
            float flashIntensity = NoxusBossConfig.Instance.VisualOverlayIntensity * 71f;
            if (noxusIndex != -1)
            {
                NPC noxus = Main.npc[noxusIndex];
                flashCreationChance = (int)Lerp(210, 36, 1f - noxus.life / (float)noxus.lifeMax);
                flashIntensity = Lerp(35f, 90f, 1f - noxus.life / (float)noxus.lifeMax);
            }
            if (InProximityOfMidnightMonolith)
            {
                flashCreationChance = 50;
                flashIntensity = 80f;
            }

            if (FlashIntensity <= 2f && FogIntensity < 1f && Main.rand.NextBool(flashCreationChance))
            {
                FlashIntensity = flashIntensity * (1f - FogIntensity);
                FlashNoiseOffset = Main.rand.NextVector2Square(0f, 1f);
                FlashPosition = Main.rand.NextVector2Square(0.2f, 0.8f);
                if (Main.instance.IsActive)
                    SoundEngine.PlaySound(ThunderSound with { Volume = ((1f - FogIntensity) * 0.6f), MaxInstances = 5 });
            }

            // Prepare the fog overlay.
            if (EntropicGod.Myself is not null)
            {
                FogIntensity = EntropicGod.Myself.ModNPC<EntropicGod>().FogIntensity;
                fogSpreadDistance = EntropicGod.Myself.ModNPC<EntropicGod>().FogSpreadDistance;
                fogCenter = EntropicGod.Myself.Center + EntropicGod.Myself.ModNPC<EntropicGod>().HeadOffset;
            }

            // Randomly create rubble around the player.
            if (Main.rand.NextBool(20) && rubble.Count <= 80)
            {
                FloatingRubble r = new()
                {
                    Depth = Main.rand.NextFloat(1.1f, 2.78f),
                    Variant = Main.rand.Next(3),
                    Position = new Vector2(Main.LocalPlayer.Center.X + Main.rand.NextFloatDirection() * 3300f, Main.rand.NextFloat(8000f)),
                    Lifetime = Main.rand.Next(240, 360)
                };
                rubble.Add(r);
            }

            // Update all rubble.
            rubble.RemoveAll(r => r.Time >= r.Lifetime);
            rubble.ForEach(r => r.Update());

            if (InProximityOfMidnightMonolith)
            {
                SkyIntensityOverride = Clamp(SkyIntensityOverride + 0.08f, 0f, 1f);
                intensity = SkyIntensityOverride;
            }
            else
                SkyIntensityOverride = Clamp(SkyIntensityOverride - 0.07f, 0f, 1f);
        }

        public override Color OnTileColor(Color inColor)
        {
            return Color.Lerp(inColor, Color.White, intensity * 0.5f);
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            Main.spriteBatch.EnterShaderRegion();
            DrawBackground(Color.Lerp(Color.Lerp(Color.BlueViolet, Color.Indigo, 0.6f), Color.DarkGray, 0.2f));

            if (Main.gfxQuality >= 0.6f)
            {
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);

                DrawBackground(Color.Lerp(Color.MidnightBlue, Color.Pink, 0.3f), 2f, 1f, 1f, ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/TurbulentNoise"));
                DrawBackground(Color.Lerp(Color.Lerp(Color.BlueViolet, Color.Indigo, 0.3f), Color.DarkGray, 0.2f), 0.6f);

                Main.spriteBatch.ExitShaderRegion();
            }

            DrawRubble(minDepth, maxDepth);
        }

        public static void DrawBackground(Color color, float localIntensity = 1f, float scrollSpeed = 1f, float noiseZoom = 1f, Asset<Texture2D> texture = null)
        {
            // Make the background colors more muted based on how strong the fog is.
            if (EntropicGod.Myself is not null)
            {
                float fogIntensity = EntropicGod.Myself.ModNPC<EntropicGod>().FogIntensity;
                float fogSpreadDistance = EntropicGod.Myself.ModNPC<EntropicGod>().FogSpreadDistance;
                float colorDarknessInterpolant = Clamp(fogSpreadDistance * GetLerpValue(0f, 0.15f, fogIntensity, true), 0f, 1f);
                color = Color.Lerp(color, Color.DarkGray, colorDarknessInterpolant * 0.7f);
            }

            Texture2D pixel = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Pixel").Value;
            Vector2 screenArea = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height);
            Vector2 textureArea = screenArea / pixel.Size();

            var backgroundShader = ShaderManager.GetShader("NoxusBackgroundShader");
            backgroundShader.TrySetParameter("luminanceThreshold", 0.9f);
            backgroundShader.TrySetParameter("intensity", localIntensity * Clamp(intensity, SkyIntensityOverride, 1f));
            backgroundShader.TrySetParameter("scrollSpeed", scrollSpeed);
            backgroundShader.TrySetParameter("noiseZoom", noiseZoom * 0.16f);
            backgroundShader.TrySetParameter("flashCoordsOffset", FlashNoiseOffset);
            backgroundShader.TrySetParameter("flashPosition", FlashPosition);
            backgroundShader.TrySetParameter("flashIntensity", FlashIntensity);
            backgroundShader.TrySetParameter("flashNoiseZoom", 0.02f);
            backgroundShader.TrySetParameter("screenPosition", Main.screenPosition);
            backgroundShader.TrySetParameter("backgroundColor", color.ToVector3());
            backgroundShader.SetTexture(texture ?? ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/Neurons2"), 1);
            backgroundShader.SetTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/WavyBlotchNoise"), 2);
            backgroundShader.Apply();
            Main.spriteBatch.Draw(pixel, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, textureArea, 0, 0f);
        }

        public void DrawFog()
        {
            Texture2D pixel = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Pixel").Value;
            Vector2 screenArea = new(Main.instance.GraphicsDevice.Viewport.Width, Main.instance.GraphicsDevice.Viewport.Height);
            Vector2 textureArea = screenArea / pixel.Size();

            var backgroundShader = ShaderManager.GetShader("DarkFogShader");
            backgroundShader.TrySetParameter("fogCenter", (fogCenter - Main.screenPosition) / screenArea);
            backgroundShader.TrySetParameter("screenResolution", screenArea);
            backgroundShader.TrySetParameter("fogTravelDistance", fogSpreadDistance);
            backgroundShader.SetTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/Smudges"), 1);
            backgroundShader.Apply();
            Main.spriteBatch.Draw(pixel, Vector2.Zero, null, FogColor * FogIntensity, 0f, Vector2.Zero, textureArea, 0, 0f);
        }

        public void DrawRubble(float minDepth, float maxDepth)
        {
            Vector2 screenCenter = Main.screenPosition + new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.5f);
            Rectangle cutoffArea = new(-1000, -1000, 4000, 4000);
            Texture2D texture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Noxus/SpecificEffectManagers/BackgroundRubble").Value;

            for (int i = 0; i < rubble.Count; i++)
            {
                if (rubble[i].Depth > minDepth && rubble[i].Depth < maxDepth)
                {
                    Vector2 rubbleScale = new(1f / rubble[i].Depth, 0.9f / rubble[i].Depth);
                    Vector2 position = (rubble[i].Position - screenCenter) * rubbleScale + screenCenter - Main.screenPosition;
                    if (cutoffArea.Contains((int)position.X, (int)position.Y))
                    {
                        Rectangle frame = texture.Frame(3, 1, rubble[i].Variant, 0);
                        Main.spriteBatch.Draw(texture, position, frame, Color.White * rubble[i].Opacity * intensity * 0.1f, 0f, frame.Size() * 0.5f, rubbleScale.X, SpriteEffects.None, 0f);
                    }
                }
            }
        }

        public override float GetCloudAlpha() => 1f - Clamp(intensity, SkyIntensityOverride, 1f);

        public override void Activate(Vector2 position, params object[] args)
        {
            isActive = true;
        }

        public override void Deactivate(params object[] args)
        {
            isActive = false;
        }

        public override void Reset()
        {
            isActive = false;
        }

        public override bool IsActive()
        {
            return isActive || intensity > 0f;
        }
    }
}
