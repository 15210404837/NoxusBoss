using System.Collections.Generic;
using System.Linq;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NoxusBoss.Content.Bosses.Xeroc;
using NoxusBoss.Content.Bosses.Xeroc.SpecificEffectManagers;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Core.Graphics
{
    public class PitchBlackMetaball2 : Metaball
    {
        public class GasParticle
        {
            public float Size;

            public Vector2 Velocity;

            public Vector2 Center;
        }

        public static readonly List<GasParticle> GasParticles = new();

        public override MetaballDrawLayerType DrawContext => MetaballDrawLayerType.BeforeBlack;

        public override Color EdgeColor
        {
            get
            {
                Color c = Color.Lerp(Color.LightCoral, Color.MediumPurple, XerocSky.DifferentStarsInterpolant);
                if (XerocBoss.Myself is not null)
                    c = Color.Lerp(c, Color.Black, GetLerpValue(1f, 4f, XerocBoss.Myself.ModNPC<XerocBoss>().ZPosition, true) * 0.5f);

                return c;
            }
        }

        public override bool AnythingToDraw => GasParticles.Any() || (XerocBoss.Myself is not null && XerocBoss.Myself.Opacity >= 0.5f);

        public override List<Texture2D> Layers => new()
        {
            ModContent.Request<Texture2D>("NoxusBoss/Core/Graphics/Metaballs/PitchBlackLayer").Value
        };

        public static void CreateParticle(Vector2 spawnPosition, Vector2 velocity, float size)
        {
            GasParticles.Add(new()
            {
                Center = spawnPosition,
                Velocity = velocity,
                Size = size
            });
        }

        public override void Update()
        {
            foreach (GasParticle particle in GasParticles)
            {
                particle.Velocity *= 0.99f;
                particle.Size *= 0.93f;
                particle.Center += particle.Velocity;
            }
            GasParticles.RemoveAll(p => p.Size <= 2f);
        }

        public override void DrawInstances()
        {
            Texture2D circle = ModContent.Request<Texture2D>("NoxusBoss/Core/Graphics/Metaballs/BasicCircle").Value;
            foreach (GasParticle particle in GasParticles)
                Main.spriteBatch.Draw(circle, particle.Center - Main.screenPosition, null, Color.White, 0f, circle.Size() * 0.5f, new Vector2(particle.Size) / circle.Size(), 0, 0f);

            foreach (NPC xeroc in Main.npc.Where(n => n.active && n.type == ModContent.NPCType<XerocBoss>()))
            {
                Vector2 drawPosition = xeroc.Center - Main.screenPosition;
                if (xeroc.Opacity >= 0.5f)
                    xeroc.ModNPC<XerocBoss>().DrawBottom();
            }
        }
    }
}
