﻿using System.Collections.Generic;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Noxus;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc
{
    public class Supernova : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public static int Lifetime => 540;

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Supernova");
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 25000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            Projectile.timeLeft = Lifetime;
            Projectile.netImportant = true;
            Projectile.hide = true;

            // This technically screws up the width/height values but that doesn't really matter since the supernova itself isn't meant to do damage.
            Projectile.scale = 0.001f;
        }

        public override void AI()
        {
            // No Xeroc? Die.
            if (XerocBoss.Myself is null)
                Projectile.Kill();

            // Grow over time.
            Projectile.scale += Remap(Projectile.scale, 1f, 28f, 0.45f, 0.08f);
            if (Projectile.scale >= 32f)
                Projectile.scale = 32f;

            // Periodically release light bursts.
            if (Time % 25f == 5f && Time <= 120f)
            {
                SoundEngine.PlaySound(EntropicGod.ExplosionTeleportSound with { MaxInstances = 10 });
                SoundEngine.PlaySound(XerocBoss.SupernovaSound with { Pitch = -0.55f });
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<LightWave>(), 0, 0f);
            }

            // Dissipate at the end.
            Projectile.Opacity = GetLerpValue(8f, 120f, Projectile.timeLeft, true);

            Time++;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.EnterShaderRegion();

            Texture2D pixel = ModContent.Request<Texture2D>("CalamityMod/Projectiles/InvisibleProj").Value;
            var fireballShader = GameShaders.Misc[$"{Mod.Name}:SupernovaShader"];
            fireballShader.SetShaderTexture(ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/GreyscaleTextures/WavyBlotchNoise"));
            fireballShader.SetShaderTexture2(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/Neurons2"));
            fireballShader.UseColor(Color.Orange);
            fireballShader.UseSecondaryColor(Color.Red);
            fireballShader.UseOpacity(Projectile.Opacity);
            fireballShader.Shader.Parameters["scale"].SetValue(Projectile.scale);
            fireballShader.Shader.Parameters["brightness"].SetValue(GetLerpValue(20f, 4f, Projectile.scale, true) * 2f + 1.25f);
            fireballShader.Apply();

            Main.instance.GraphicsDevice.Textures[3] = ModContent.Request<Texture2D>("NoxusBoss/Assets/ExtraTextures/Void").Value;
            Main.spriteBatch.Draw(pixel, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity * 0.42f, Projectile.rotation, pixel.Size() * 0.5f, Projectile.scale * 400f, 0, 0f);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}