using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Common.Utilities;
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
                    Main.instance.DrawCacheNPCsBehindNonSolidTiles.Add(index);
            }
        }

        public override void ModifyTypeName(ref string typeName)
        {
            typeName = string.Empty;
            for (int i = 0; i < 8; i++)
                typeName += (char)Main.rand.Next(700);
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            scale *= TeleportVisualsAdjustedScale.Length() * 0.707f;
            return null;
        }

        public override void BossHeadSlot(ref int index)
        {
            // Make the head icon disappear if Xeroc is invisible.
            if (NPC.Opacity <= 0.45f)
                index = -1;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Draw wings with afterimages.
            if (!NPC.IsABestiaryIconDummy && CurrentAttack != XerocAttackType.Awaken)
                Main.spriteBatch.Draw(XerocWingDrawer.AfterimageTargetPrevious.Target, Main.screenLastPosition - Main.screenPosition, Color.White);

            // Draw all hands.
            if (UniversalBlackOverlayInterpolant <= 0f)
                DrawHands(screenPos);

            // Draw the pitch-black censor.
            DrawProtectiveCensor(screenPos);
            if (UniversalBlackOverlayInterpolant > 0f)
                DrawHands(screenPos);

            DrawTeeth(screenPos);

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
            Color wingsDrawColor = Color.Lerp(Color.Transparent, Color.White, fadeInterpolant);

            // Wings become squished the faster they're moving, to give an illusion of 3D motion.
            float squishOffset = MathF.Min(0.7f, Math.Abs(rotationDifferenceMovingAverage) * 3.5f);

            Vector2 scale = MathF.Min(TeleportVisualsAdjustedScale.X, TeleportVisualsAdjustedScale.Y) * new Vector2(1f, 1f - squishOffset) * fadeInterpolant * new Vector2(1f, 1.15f);
            Main.spriteBatch.Draw(wingsTexture, drawPosition - Vector2.UnitX * TeleportVisualsAdjustedScale * 64f, null, wingsDrawColor, generalRotation + wingRotation, leftWingOrigin, scale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(wingsTexture, drawPosition + Vector2.UnitX * TeleportVisualsAdjustedScale * 64f, null, wingsDrawColor, generalRotation - wingRotation, rightWingOrigin, scale, SpriteEffects.FlipHorizontally, 0f);
        }

        public void DrawWings()
        {
            // Prepare the wing psychedelic shader.
            var wingShader = GameShaders.Misc["NoxusBoss:XerocPsychedelicWingShader"];
            wingShader.Shader.Parameters["colorShift"].SetValue(new Vector3(Sin(Main.GlobalTimeWrappedHourly * 5.8f) * 0.5f + 1f, 0.5f, 0.67f));
            wingShader.Shader.Parameters["lightDirection"].SetValue(Vector3.Backward);
            wingShader.SetShaderTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/TurbulentNoise"));
            wingShader.SetShaderTexture2(ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/XerocWingNormalMap"));
            wingShader.Apply();

            for (int i = 0; i < Wings.Length; i++)
            {
                Vector2 bottom = NPC.Center + Vector2.UnitY.RotatedBy(NPC.rotation) * TeleportVisualsAdjustedScale.Y * 220f - Main.screenPosition;
                DrawWing(bottom + Vector2.UnitY * (i * -180f - 240f) * TeleportVisualsAdjustedScale.Y, Wings[i].WingRotation, Wings[i].WingRotationDifferenceMovingAverage, NPC.rotation, NPC.Opacity);
            }
        }

        public void DrawHands(Vector2 screenPos)
        {
            if (DrawCongratulatoryText)
                return;

            Main.spriteBatch.ExitShaderRegion();

            // TODO -- Add sigil drawing to this.
            bool canDrawRobeArms = CurrentAttack != XerocAttackType.OpenScreenTear && CurrentAttack != XerocAttackType.Awaken && CurrentAttack != XerocAttackType.DeathAnimation;
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
                    handScale *= 1.3f;
                }

                ulong seed = hand.UniqueID;
                int brightness = (int)Lerp(102f, 186f, PolyInOutEasing(RandomFloat(ref seed), 7));

                Color baseHandColor = new(brightness, brightness, brightness);
                Color handColor = baseHandColor * Pow(hand.Opacity, 3f) * (CurrentAttack == XerocAttackType.DeathAnimation ? 1f : NPC.Opacity) * ZPositionOpacity;
                if (CurrentAttack == XerocAttackType.OpenScreenTear || CurrentAttack == XerocAttackType.Awaken)
                    handColor = Color.White;
                if (TeleportVisualsAdjustedScale.Length() >= 10f)
                    handColor = Color.White with { A = 0 };

                SpriteEffects direction = hand.Center.X > NPC.Center.X ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                if (hand.DirectionOverride != 0)
                    direction = hand.DirectionOverride == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                // Draw the arm robe.
                if (canDrawRobeArms && hand.UseRobe)
                    DrawArmRobe(hand);

                // Draw hand trails if enabled.
                if (hand.TrailOpacity >= 0.01f)
                {
                    // Draw a flame trail.
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
                    hand.HandTrailDrawer.Draw(hand.OldCenters.Take(25), -Main.screenPosition, 45);
                }

                // Draw hand afterimages.
                float handRotation = hand.Rotation;
                for (int i = 8; i >= 0; i--)
                {
                    float afterimageOpacity = 1f - i / 7f;
                    Vector2 afterimageDrawOffset = hand.Velocity * i * -0.15f;
                    Main.spriteBatch.Draw(handTexture, drawPosition + afterimageDrawOffset, frame, handColor * afterimageOpacity, handRotation, frame.Size() * 0.5f, handScale, direction, 0f);
                }

                // Draw the hands in their true position.
                Main.spriteBatch.Draw(handTexture, drawPosition, frame, handColor, handRotation, frame.Size() * 0.5f, handScale, direction, 0f);
                Main.spriteBatch.Draw(handTexture, drawPosition, frame, handColor with { A = 0 } * 0.5f, handRotation, frame.Size() * 0.5f, handScale, direction, 0f);
            }
        }

        public void DrawArmRobe(XerocHand hand)
        {
            // Things get scuffed if this check isn't performed. Specifically, the ellipsoid doesn't play nice with the motion the attack
            // performs and causes the cloth to look unnaturally thin.
            if (CurrentAttack == XerocAttackType.PunchesWithScreenSlices && Distance(NPC.Center.X, hand.Center.X) <= 200f)
                return;

            // Store the robe cloth in a short variable for ease of use.
            var cloth = hand.RobeCloth;

            // Calculate internal arm positions.
            float midpointDistanceFactor = Pow(TeleportVisualsAdjustedScale.Y, 0.5f);
            float incrementFactor = Remap(TeleportVisualsAdjustedScale.Y, 0.2f, 1f, 1.18f, 1.84f);
            Vector2 robeStart = NPC.Center + TeleportVisualsAdjustedScale * new Vector2(hand.RobeDirection * 80f, -140f);
            Vector2 robeEnd = hand.Center + TeleportVisualsAdjustedScale * new Vector2(hand.RobeDirection * -30f, -4f);
            if (hand.Frame <= 1)
                robeEnd.Y -= TeleportVisualsAdjustedScale.Y * 37.5f;

            Vector2 robeMidpoint = IKSolve2(robeStart, robeEnd, midpointDistanceFactor * 118f, midpointDistanceFactor * 120f, hand.RobeDirection == -1) + Vector2.UnitY * Pow(TeleportVisualsAdjustedScale.Y, 0.67f) * 100f;

            // Calculate internal positions for the open hand radius.
            Vector2 ellipsoidCenter = robeEnd + new Vector2(hand.RobeDirection * -20f, 150f) * TeleportVisualsAdjustedScale;
            Vector3 ellipsoidRadius = new(TeleportVisualsAdjustedScale.Length() * 42f);
            ellipsoidRadius.Y *= 2.6f;
            if (CurrentAttack == XerocAttackType.StarConvergenceAndRedirecting)
                ellipsoidRadius.X *= 0.7f;

            // Hold the end of the robe to the hand. This has a mild amount of horizontal wind force.
            int endX = hand.RobeDirection == -1 ? 0 : (cloth.CellCountX - 1);
            int startX = hand.RobeDirection == 1 ? 0 : (cloth.CellCountX - 1);
            for (int i = 0; i < cloth.CellCountY - 2; i++)
            {
                Vector2 windSwayOffset = Vector2.UnitX * Cos(Main.GlobalTimeWrappedHourly * 5.6f + robeEnd.X * 0.04f - i) * TeleportVisualsAdjustedScale.X * i * 0.5f;
                Vector2 verticalStickOffset = Vector2.UnitY * i * Sqrt(TeleportVisualsAdjustedScale.Y) * cloth.CellSizeY * incrementFactor;
                if (CurrentAttack == XerocAttackType.StarConvergenceAndRedirecting)
                    windSwayOffset = Vector2.Zero;

                cloth.SetStickPosition(endX, i, robeEnd + windSwayOffset + verticalStickOffset - Vector2.UnitY * 4f, true);
            }

            // Hold the start of the robe to Xeroc.
            for (int i = 0; i < cloth.CellCountY - 2; i++)
                cloth.SetStickPosition(startX, i, robeStart + Vector2.UnitY * i * Sqrt(TeleportVisualsAdjustedScale.Y) * cloth.CellSizeY * incrementFactor * 1.25f - Vector2.UnitY * TeleportVisualsAdjustedScale * 60f, true);

            // Hold the top of the robe in accordance with the IK arms.
            for (int i = 0; i < cloth.CellCountX; i++)
            {
                float armCompletion = i / (float)(cloth.CellCountX - 1f);
                if (hand.RobeDirection == -1)
                    armCompletion = 1f - armCompletion;

                Vector2 armHoldPosition;
                if (armCompletion < 0.5f)
                    armHoldPosition = Vector2.Lerp(robeStart, robeMidpoint, armCompletion * 2f);
                else
                    armHoldPosition = Vector2.Lerp(robeMidpoint, robeEnd, (armCompletion - 0.5f) * 2f);

                cloth.SetStickPosition(i, 0, armHoldPosition, true);

                // Keep the bottom half of the cloth in place at the point of the arm midpoint. This helps to make the bottom half of the robes seem more like
                // robes with arms than just a parabolic sag.
                if (i == cloth.CellCountX / 2)
                    cloth.SetStickPosition(i, cloth.CellCountY - 1, armHoldPosition + Vector2.UnitY * TeleportVisualsAdjustedScale * 130f, true);
            }

            // Draw the cloth.
            cloth.Simulate(Pow(TeleportVisualsAdjustedScale.X, 1.5f), new(ellipsoidCenter, 0f), ellipsoidRadius);

            // Collect necessary shader and graphics information for the cloth drawing.
            var mesh = cloth.GenerateMesh();
            var gd = Main.instance.GraphicsDevice;
            var clothShader = GameShaders.Misc["NoxusBoss:ClothShader"];
            Texture2D clothTexture = XerocRobePatternGenerator.PatternTarget.Target;
            CalculatePerspectiveMatricies(out Matrix view, out Matrix projection);

            // Apply the cloth shader and draw the cloth.
            Matrix worldToUV = Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0f) * view * projection;
            clothShader.Shader.Parameters["uLightSource"].SetValue(Vector3.UnitZ);
            clothShader.Shader.Parameters["brightnessPower"].SetValue(40f);
            clothShader.Shader.Parameters["pixelationZoom"].SetValue(Vector2.One * 2f / clothTexture.Size());
            clothShader.Shader.Parameters["uWorldViewProjection"].SetValue(worldToUV);
            clothShader.Apply();

            // Disable backface culling from the rasterizer, as that can prevent some of the cloth from rendering.
            gd.RasterizerState = RasterizerState.CullNone;

            // Apply the cloth texture.
            gd.Textures[0] = clothTexture;
            gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, mesh, 0, mesh.Length, cloth.MeshIndexCache, 0, mesh.Length / 2);

            // Turn off the shader. It has completed its job.
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.ExitShaderRegion();
        }

        public void DrawProtectiveCensor(Vector2 screenPos)
        {
            // TODO -- This is larger than usual for placeholder reasons.
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 censorScale = Vector2.One * MathF.Max(TeleportVisualsAdjustedScale.X, TeleportVisualsAdjustedScale.Y) * new Vector2(200f, 340f) / pixel.Size();
            Vector2 censorDrawPosition = EyePosition - screenPos + Vector2.UnitY * censorScale * 80f;
            Main.spriteBatch.Draw(pixel, censorDrawPosition, null, Color.Black * Pow(NPC.Opacity, 0.4f) * (1f - UniversalBlackOverlayInterpolant), 0f, pixel.Size() * 0.5f, censorScale, 0, 0f);

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
            // Collect textures.
            Texture2D outlineTexture1 = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Parts/TeethOutline1").Value;
            Texture2D outlineTexture2 = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Parts/TeethOutline2").Value;
            Texture2D backglowTexture = ModContent.Request<Texture2D>("CalamityMod/Skies/XerocLight").Value;

            // Calculate draw values.
            Vector2 teethScale = TeleportVisualsAdjustedScale * 0.85f;
            Vector2 outlineDrawPosition = NPC.Center + Vector2.UnitY.RotatedBy(NPC.rotation) * teethScale * 110f - screenPos;
            Color teethColor = Color.White * NPC.Opacity * (1f - UniversalBlackOverlayInterpolant) * 0.9f;

            // Draw a black circle behind the teeth so that background isn't revealed behind them.
            Main.spriteBatch.Draw(backglowTexture, outlineDrawPosition, null, (Color.Black * NPC.Opacity * (1f - UniversalBlackOverlayInterpolant)), 0f, backglowTexture.Size() * 0.5f, new Vector2(0.54f, 0.5f) * TeleportVisualsAdjustedScale, 0, 0f);

            // Draw the teeth outlines.
            Main.spriteBatch.Draw(outlineTexture1, outlineDrawPosition + Vector2.UnitY * TopTeethOffset * teethScale, null, teethColor, 0f, outlineTexture1.Size() * new Vector2(0.5f, 1f), teethScale, 0, 0f);
            Main.spriteBatch.Draw(outlineTexture2, outlineDrawPosition, null, teethColor, 0f, outlineTexture2.Size() * new Vector2(0.5f, 0f), teethScale, 0, 0f);

            // Draw individual teeth.
            Vector2[] upperTeethOffset = new[]
            {
                new Vector2(21f, -12f),
                new Vector2(32f, -16f),
                new Vector2(46f, -16f),
                new Vector2(68f, -16f),
                new Vector2(95f, -16f),
                new Vector2(116f, -14f),
                new Vector2(132f, -13f),
            };
            for (int i = 0; i < upperTeethOffset.Length; i++)
            {
                float toothRotation = Sin(Main.GlobalTimeWrappedHourly * 6f + i) * 0.09f;
                Texture2D toothTexture = ModContent.Request<Texture2D>($"NoxusBoss/Content/Bosses/Xeroc/Parts/UpperTooth{i + 1}").Value;
                Vector2 toothOffset = (upperTeethOffset[i] - Vector2.UnitX * outlineTexture1.Width * 0.5f) * teethScale;
                Main.spriteBatch.Draw(toothTexture, (outlineDrawPosition + toothOffset + Vector2.UnitY * TopTeethOffset * teethScale).Floor(), null, teethColor, toothRotation, toothTexture.Size() * 0.5f, teethScale, 0, 0f);
            }
        }

        public void DrawBottom()
        {
            Texture2D bottomTexture = ModContent.Request<Texture2D>("NoxusBoss/Content/Bosses/Xeroc/Parts/Bottom").Value; // HAHAHAHAHA Bottom Text.
            Vector2 bottomScale = TeleportVisualsAdjustedScale * 0.85f;
            Vector2 bottomDrawPosition = NPC.Center - Vector2.UnitY.RotatedBy(NPC.rotation) * bottomScale * 40f - Main.screenPosition;
            Color bottomColor = Color.White * NPC.Opacity * (1f - UniversalBlackOverlayInterpolant);

            // Draw the bottom texture.
            Main.spriteBatch.Draw(bottomTexture, bottomDrawPosition, null, bottomColor, 0f, bottomTexture.Size() * new Vector2(0.5f, 0f), bottomScale, 0, 0f);
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
            Vector2 leftCrownDrawPosition = NPC.Center + new Vector2(-80f, -274f).RotatedBy(NPC.rotation) * TeleportVisualsAdjustedScale - Main.screenPosition;
            Vector2 rightCrownDrawPosition = NPC.Center + new Vector2(80f, -274f).RotatedBy(NPC.rotation) * TeleportVisualsAdjustedScale - Main.screenPosition;

            Main.spriteBatch.Draw(crownTexture1, leftCrownDrawPosition, null, Color.White * ZPositionOpacity * NPC.Opacity, NPC.rotation, crownTexture1.Size() * 0.5f, crownScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(crownTexture1, rightCrownDrawPosition, null, Color.White * ZPositionOpacity * NPC.Opacity, NPC.rotation, crownTexture1.Size() * 0.5f, crownScale, SpriteEffects.FlipHorizontally, 0f);
        }
    }
}
