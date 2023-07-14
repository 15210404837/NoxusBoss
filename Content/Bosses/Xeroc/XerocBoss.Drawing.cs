using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Particles;
using NoxusBoss.Core.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using static CalamityMod.CalamityUtils;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public partial class XerocBoss : ModNPC
    {
        private static readonly FieldInfo particlesField = typeof(GeneralParticleHandler).GetField("particles", BindingFlags.NonPublic | BindingFlags.Static);

        public override void DrawBehind(int index)
        {
            bool canDraw = CurrentAttack == XerocAttackType.OpenScreenTear || CurrentAttack == XerocAttackType.Awaken || CurrentAttack == XerocAttackType.DeathAnimation || NPC.Opacity >= 0.02f;
            if (NPC.hide && canDraw)
            {
                if ((DrawCongratulatoryText || UniversalBlackOverlayInterpolant >= 0.02f) && ZPosition >= -0.5f)
                    ScreenOverlaysSystem.DrawCacheAfterNoxusFog.Add(index);
                else if (ZPosition < -0.1f)
                    ScreenOverlaysSystem.DrawCacheAfterNoxusFog.Add(index);
                else if (ShouldDrawBehindTiles)
                    ScreenOverlaysSystem.DrawCacheBeforeBlack.Add(index);
                else
                    Main.instance.DrawCacheNPCProjectiles.Add(index);
            }
        }

        public override void BossHeadSlot(ref int index)
        {
            // Make the head icon disappear if Xeroc is invisible.
            if (NPC.Opacity <= 0.45f)
                index = -1;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Draw wings.
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = 0; i < Wings.Length; i++)
            {
                Vector2 bottom = NPC.Center + Vector2.UnitY.RotatedBy(NPC.rotation) * TeleportVisualsAdjustedScale.Y * 220f - screenPos;
                DrawWing(bottom + Vector2.UnitY * (i * -192f + 30f) * TeleportVisualsAdjustedScale.Y, Wings[i].WingRotation, Wings[i].WingRotationDifferenceMovingAverage, NPC.rotation, NPC.Opacity);
            }
            Main.spriteBatch.ResetBlendState();

            // Draw the pitch-black censor.
            DrawProtectiveCensor(screenPos);

            // Draw all hands.
            DrawHands(screenPos);

            //DrawTeeth(screenPos);

            // Draw the eye.
            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            if (UniversalBlackOverlayInterpolant <= 0.99f)
                DrawEye(screenPos);

            // Draw fire particles manually if they'd be obscured.
            if (UniversalBlackOverlayInterpolant >= 0.02f)
            {
                List<Particle> particles = (List<Particle>)particlesField.GetValue(null);
                foreach (Particle p in particles)
                {
                    if (p is not HeavySmokeParticle t)
                        continue;

                    t.CustomDraw(spriteBatch);
                }
            }
            Main.spriteBatch.ResetBlendState();

            return false;
        }

        public void DrawWing(Vector2 drawPosition, float wingRotation, float rotationDifferenceMovingAverage, float generalRotation, float fadeInterpolant)
        {
            Texture2D wingsTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/XerocWing").Value;
            Vector2 leftWingOrigin = wingsTexture.Size() * new Vector2(1f, 0.86f);
            Vector2 rightWingOrigin = leftWingOrigin;
            rightWingOrigin.X = wingsTexture.Width - rightWingOrigin.X;
            Color wingsDrawColor = Color.Lerp(Color.Transparent, Color.Wheat, fadeInterpolant);
            Color wingsDrawColorWeak = Color.Lerp(Color.Transparent, Color.Red * 0.4f, fadeInterpolant);

            // Wings become squished the faster they're moving, to give an illusion of 3D motion.
            float squishOffset = MathF.Min(0.7f, Math.Abs(rotationDifferenceMovingAverage) * 3.5f);

            // Draw multiple instances of the wings. This includes afterimages based on how quickly they're flapping.
            Vector2 scale = MathF.Min(TeleportVisualsAdjustedScale.X, TeleportVisualsAdjustedScale.Y) * new Vector2(1f, 1f - squishOffset) * fadeInterpolant * 1.5f;
            for (int i = 4; i >= 0; i--)
            {
                // Make wings slightly brighter when they're moving at a fast angular pace.
                Color wingColor = Color.Lerp(wingsDrawColor, wingsDrawColorWeak, i / 4f) * Remap(rotationDifferenceMovingAverage, 0f, 0.04f, 0.66f, 0.75f) * ZPositionOpacity;

                float rotationOffset = i * MathF.Min(rotationDifferenceMovingAverage, 0.16f) * (1f - squishOffset) * 0.5f;
                float currentWingRotation = wingRotation + rotationOffset;

                Main.spriteBatch.Draw(wingsTexture, drawPosition - Vector2.UnitX * TeleportVisualsAdjustedScale * 104f, null, wingColor, generalRotation + currentWingRotation, leftWingOrigin, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(wingsTexture, drawPosition + Vector2.UnitX * TeleportVisualsAdjustedScale * 104f, null, wingColor, generalRotation - currentWingRotation, rightWingOrigin, scale, SpriteEffects.FlipHorizontally, 0f);
            }
        }

        public void DrawHands(Vector2 screenPos)
        {
            // TODO -- Add sigil drawing to this.
            Texture2D palmTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/XerocPalm").Value;
            foreach (XerocHand hand in Hands)
            {
                Texture2D handTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/XerocHand").Value;
                Vector2 drawPosition = hand.Center - screenPos;
                Rectangle frame = handTexture.Frame(1, 3, 0, hand.Frame);
                Vector2 handScale = TeleportVisualsAdjustedScale * hand.ScaleFactor * 0.75f;

                if (hand.UsePalmForm)
                {
                    handTexture = palmTexture;
                    frame = handTexture.Frame();
                    handScale *= 2f;
                }

                Color handColor = Color.Coral * hand.Opacity * (CurrentAttack == XerocAttackType.DeathAnimation ? 1f : NPC.Opacity) * ZPositionOpacity;
                if (CurrentAttack == XerocAttackType.OpenScreenTear || CurrentAttack == XerocAttackType.Awaken || TeleportVisualsAdjustedScale.Length() >= 10f)
                    handColor = Color.White;

                SpriteEffects direction = hand.Center.X < NPC.Center.X ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                // Draw hand trails if enabled.
                if (hand.TrailOpacity >= 0.01f)
                {
                    // Draw a flame trail.
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
                    hand.HandTrailDrawer.Draw(hand.OldCenters.Take(25), -Main.screenPosition, 45);
                }

                // Draw hand afterimages.
                for (int i = 8; i >= 0; i--)
                {
                    float afterimageOpacity = 1f - i / 7f;
                    Vector2 afterimageDrawOffset = hand.Velocity * i * -0.15f;
                    Main.spriteBatch.Draw(handTexture, drawPosition + afterimageDrawOffset, frame, handColor * afterimageOpacity, hand.Rotation, frame.Size() * 0.5f, handScale, direction, 0f);
                }

                // Draw the hands in their true position.
                Main.spriteBatch.Draw(handTexture, drawPosition, frame, handColor, hand.Rotation, frame.Size() * 0.5f, handScale, direction, 0f);
                Main.spriteBatch.Draw(handTexture, drawPosition, frame, handColor with { A = 0 } * 0.5f, hand.Rotation, frame.Size() * 0.5f, handScale, direction, 0f);
            }
        }

        public void DrawProtectiveCensor(Vector2 screenPos)
        {
            // TODO -- This is larger than usual for placeholder reasons.
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 censorDrawPosition = EyePosition - screenPos + Vector2.UnitY * NPC.scale * 240f;
            Vector2 censorScale = Vector2.One * MathF.Max(TeleportVisualsAdjustedScale.X, TeleportVisualsAdjustedScale.Y) * new Vector2(300f, 760f) / pixel.Size();
            Main.spriteBatch.Draw(pixel, censorDrawPosition, null, Color.Black * Pow(NPC.Opacity, 0.4f), 0f, pixel.Size() * 0.5f, censorScale, 0, 0f);

            // Draaw the universal overlay if necessary.
            Texture2D overlayTexture = ModContent.Request<Texture2D>("CalamityMod/Skies/XerocLight").Value;
            Vector2 overlayScale = Vector2.One * Lerp(0.1f, 15f, UniversalBlackOverlayInterpolant);
            for (int i = 0; i < 3; i++)
                Main.spriteBatch.Draw(overlayTexture, new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f, null, (ZPosition <= -0.7f ? Color.Transparent : Color.Black) * Sqrt(UniversalBlackOverlayInterpolant), 0f, overlayTexture.Size() * 0.5f, overlayScale, 0, 0f);

            // Draw congratulatory text if necessary.
            if (DrawCongratulatoryText)
            {
                string text = "You have passed the test.";
                DynamicSpriteFont font = FontAssets.DeathText.Value;
                float scale = 0.8f;
                float maxHeight = 225f;
                Vector2 textSize = font.MeasureString(text);
                if (textSize.Y > maxHeight)
                    scale = maxHeight / textSize.Y;
                Vector2 vector2 = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f - textSize * scale * 0.5f;

                ChatManager.DrawColorCodedStringWithShadow(Main.spriteBatch, font, text, vector2, Color.White, 0f, Vector2.Zero, new(scale), -1f, 2f);
            }
        }

        public void DrawTeeth(Vector2 screenPos)
        {
            Texture2D teethTexture1 = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Parts/Teeth1").Value;
            Texture2D teethTexture2 = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Parts/Teeth2").Value;

            Vector2 teethScale = TeleportVisualsAdjustedScale * 0.2f;
            Vector2 generalBottomTeethOffset = new Vector2(-20f, -52f).RotatedBy(NPC.rotation) * teethScale;
            Vector2 teethDrawPositionTop = NPC.Center + Vector2.UnitY.RotatedBy(NPC.rotation) * teethScale * 302f - screenPos;
            Vector2 teethDrawPositionBottom = teethDrawPositionTop;
            Color teethColor = Color.White * NPC.Opacity * 0.9f;

            Main.spriteBatch.Draw(teethTexture1, teethDrawPositionTop, null, teethColor, 0f, teethTexture1.Size() * new Vector2(0.5f, 1f), teethScale, 0, 0f);
            Main.spriteBatch.Draw(teethTexture2, teethDrawPositionBottom + generalBottomTeethOffset, null, teethColor, 0f, teethTexture2.Size() * new Vector2(0.5f, 0f), teethScale, 0, 0f);
        }

        public void DrawEye(Vector2 screenPos)
        {
            // Draw a glowing orb before the eye.
            float universalOpacity = Pow(ZPositionOpacity, 0.7f) * NPC.Opacity * (1f - UniversalBlackOverlayInterpolant);
            float glowDissipateFactor = Remap(NPC.Opacity, 0.2f, 1f, 1f, 0.74f);
            Texture2D backglowTexture = ModContent.Request<Texture2D>("CalamityMod/Skies/XerocLight").Value;
            Texture2D bloomFlareTexture = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/BloomFlare").Value;
            Texture2D spiresTexture = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/XerocSpires").Value;
            Vector2 origin = backglowTexture.Size() * 0.5f;
            Vector2 baseScale = Vector2.One * NPC.Opacity * Lerp(1.9f, 2f, Cos(Main.GlobalTimeWrappedHourly * 4f) * 0.5f + 0.5f) * 0.6f;
            baseScale *= MathF.Min(TeleportVisualsAdjustedScale.X, TeleportVisualsAdjustedScale.Y);

            Main.spriteBatch.Draw(backglowTexture, EyePosition - screenPos, null, Color.White * glowDissipateFactor * universalOpacity, 0f, origin, baseScale * 0.7f, 0, 0f);
            Main.spriteBatch.Draw(backglowTexture, EyePosition - screenPos, null, Color.IndianRed * glowDissipateFactor * universalOpacity * 0.4f, 0f, origin, baseScale * 1.2f, 0, 0f);
            Main.spriteBatch.Draw(backglowTexture, EyePosition - screenPos, null, Color.Coral * glowDissipateFactor * universalOpacity * 0.3f, 0f, origin, baseScale * 1.7f, 0, 0f);

            // Draw a bloom flare over the orb.
            Main.spriteBatch.Draw(bloomFlareTexture, EyePosition - screenPos, null, Color.LightCoral * glowDissipateFactor * universalOpacity * 0.6f, Main.GlobalTimeWrappedHourly * 0.4f, bloomFlareTexture.Size() * 0.5f, baseScale * 0.7f, 0, 0f);
            Main.spriteBatch.Draw(bloomFlareTexture, EyePosition - screenPos, null, Color.Coral * glowDissipateFactor * universalOpacity * 0.6f, Main.GlobalTimeWrappedHourly * -0.26f, bloomFlareTexture.Size() * 0.5f, baseScale * 0.7f, 0, 0f);

            // Draw the spires over the bloom flare.
            Main.spriteBatch.Draw(spiresTexture, EyePosition - screenPos, null, Color.White * glowDissipateFactor * universalOpacity, 0f, spiresTexture.Size() * 0.5f, baseScale * 0.8f, 0, 0f);

            // Draw the eye.
            Texture2D eyeTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/XerocEye").Value;
            Texture2D pupilTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/XerocPupil").Value;
            Vector2 eyeScale = baseScale * 0.4f;
            Main.spriteBatch.Draw(eyeTexture, EyePosition - screenPos, null, Color.White * universalOpacity, 0f, eyeTexture.Size() * 0.5f, eyeScale, 0, 0f);
            Main.spriteBatch.Draw(pupilTexture, EyePosition - screenPos + PupilOffset * TeleportVisualsAdjustedScale, null, Color.White * universalOpacity, 0f, pupilTexture.Size() * 0.5f, eyeScale * PupilScale, 0, 0f);

            // Draw a telegraph over the pupil if it's activated.
            if (PupilTelegraphOpacity >= 0.01f)
            {
                string shader = CurrentAttack == XerocAttackType.SwordConstellation2 ? "NoxusBoss:SpreadTelegraphInverted" : "CalamityMod:SpreadTelegraph";

                Effect telegraphShader = Filters.Scene[shader].GetShader().Shader;
                telegraphShader.Parameters["centerOpacity"].SetValue(1.7f);
                telegraphShader.Parameters["mainOpacity"].SetValue(Sqrt(PupilTelegraphOpacity));
                telegraphShader.Parameters["halfSpreadAngle"].SetValue(PupilTelegraphArc * Sqrt(PupilTelegraphOpacity) * 0.5f);
                telegraphShader.Parameters["edgeColor"].SetValue(Color.Red.ToVector3());
                telegraphShader.Parameters["centerColor"].SetValue(Color.Coral.ToVector3());
                telegraphShader.Parameters["edgeBlendLenght"].SetValue(0.07f);
                telegraphShader.Parameters["edgeBlendStrength"].SetValue(16f);
                telegraphShader.Parameters["spreadOutPower"]?.SetValue(2.32f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, telegraphShader, Main.GameViewMatrix.TransformationMatrix);

                Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj").Value;
                Main.EntitySpriteDraw(texture, PupilPosition - screenPos, null, Color.White, PupilOffset.ToRotation(), texture.Size() * 0.5f, 5000f, 0, 0);

                if (CurrentAttack == XerocAttackType.SwordConstellation2)
                {
                    telegraphShader = Filters.Scene["CalamityMod:SpreadTelegraph"].GetShader().Shader;
                    telegraphShader.Parameters["centerOpacity"].SetValue(1f);
                    telegraphShader.Parameters["mainOpacity"].SetValue(Sqrt(PupilTelegraphOpacity));
                    telegraphShader.Parameters["halfSpreadAngle"].SetValue(PupilTelegraphArc * Sqrt(PupilTelegraphOpacity) * 0.5f);
                    telegraphShader.Parameters["edgeColor"].SetValue(Color.Transparent.ToVector3());
                    telegraphShader.Parameters["centerColor"].SetValue(Color.SpringGreen.ToVector3());
                    telegraphShader.Parameters["edgeBlendLenght"].SetValue(0.011f);
                    telegraphShader.Parameters["edgeBlendStrength"].SetValue(3f);

                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, telegraphShader, Main.GameViewMatrix.TransformationMatrix);

                    Main.EntitySpriteDraw(texture, PupilPosition - screenPos, null, Color.White, PupilOffset.ToRotation(), texture.Size() * 0.5f, 2000f, 0, 0);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }

        public void DrawCrown()
        {
            if (UniversalBlackOverlayInterpolant >= 0.02f)
                return;

            Texture2D crownTexture1 = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Parts/SupremeCrown").Value;

            Vector2 crownScale = TeleportVisualsAdjustedScale * 0.75f;
            Vector2 leftCrownDrawPosition = NPC.Center + new Vector2(-110f, -300f).RotatedBy(NPC.rotation) * TeleportVisualsAdjustedScale - Main.screenPosition;
            Vector2 rightCrownDrawPosition = NPC.Center + new Vector2(110f, -300f).RotatedBy(NPC.rotation) * TeleportVisualsAdjustedScale - Main.screenPosition;

            Main.spriteBatch.Draw(crownTexture1, leftCrownDrawPosition, null, Color.White * ZPositionOpacity * NPC.Opacity, NPC.rotation, crownTexture1.Size() * 0.5f, crownScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(crownTexture1, rightCrownDrawPosition, null, Color.White * ZPositionOpacity * NPC.Opacity, NPC.rotation, crownTexture1.Size() * 0.5f, crownScale, SpriteEffects.FlipHorizontally, 0f);
        }
    }
}
