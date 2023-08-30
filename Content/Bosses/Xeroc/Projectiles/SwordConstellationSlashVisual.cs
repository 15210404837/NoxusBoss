using System.IO;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Bosses.Xeroc.Projectiles
{
    public class SwordConstellationSlashVisual : ModProjectile
    {
        public PrimitiveTrail SlashDrawer;

        public Vector2[] TrailCache;

        public bool UsePositionCacheForTrail => Projectile.ai[0] == 1f;

        public ref float SwordSide => ref Projectile.ai[1];

        public ref float Time => ref Projectile.localAI[0];

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
            Projectile.timeLeft = 45;
            Projectile.MaxUpdates = 2;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            // Send position cache data.
            writer.Write(TrailCache.Length);
            for (int i = 0; i < TrailCache.Length; i++)
                writer.WritePackedVector2(TrailCache[i]);

            // Write the scale.
            writer.Write(Projectile.scale);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            int positionCount = reader.ReadInt32();
            TrailCache = new Vector2[positionCount];
            for (int i = 0; i < positionCount; i++)
                TrailCache[i] = reader.ReadPackedVector2();

            Projectile.scale = reader.ReadSingle();
        }

        public override void AI()
        {
            // Die if Xeroc is not present.
            if (XerocBoss.Myself is null)
            {
                Projectile.Kill();
                return;
            }

            // Fade out.
            Projectile.Opacity = Clamp(Projectile.Opacity * 0.98f - 0.026f, 0f, 1f);

            // Slow down.
            Projectile.velocity *= 0.91f;

            // Gradually grow.
            Projectile.scale *= 1.02f;

            for (int i = 0; i < TrailCache.Length; i++)
            {
                if (TrailCache[i] != Vector2.Zero)
                    TrailCache[i] += Projectile.velocity;
            }

            Time++;
        }

        public float SlashWidthFunction(float completionRatio) => Projectile.scale * Projectile.width * 0.7f;

        public Color SlashColorFunction(float completionRatio) => Color.Orange * GetLerpValue(0.9f, 0.7f, completionRatio, true) * GetLerpValue(0f, 0.08f, completionRatio, true) * Projectile.Opacity;

        public override bool PreDraw(ref Color lightColor)
        {
            var slashShader = GameShaders.Misc["CalamityMod:ExobladeSlash"];
            SlashDrawer ??= new(SlashWidthFunction, SlashColorFunction, null, slashShader);
            SwordConstellation.DrawAfterimageTrail(SlashDrawer, Projectile, TrailCache, 1f, SwordSide, UsePositionCacheForTrail);

            return false;
        }
    }
}
