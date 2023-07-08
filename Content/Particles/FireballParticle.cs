﻿using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace NoxusBoss.Content.Particles
{
    public class FireballParticle : Particle
    {
        public float Opacity;

        public float Spin;

        public bool IsImportant
        {
            get;
            set;
        }

        public override bool SetLifetime => true;

        public override int FrameVariants => 7;

        public override bool UseCustomDraw => true;

        public override bool UseAdditiveBlend => true;

        public override bool Important => IsImportant;

        public override string Texture => "CalamityMod/Particles/HeavySmoke";

        public FireballParticle(Vector2 position, Vector2 velocity, Color color, int lifetime, float scale, float opacity, bool important, float rotationSpeed = 0f)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
            Scale = scale;
            Variant = Main.rand.Next(7);
            Lifetime = lifetime;
            Opacity = opacity;
            Spin = rotationSpeed;
            IsImportant = important;
        }

        public override void Update()
        {
            if (LifetimeCompletion < 0.1f)
                Scale += 0.01f;
            if (LifetimeCompletion > 0.9f)
                Scale *= 0.975f;

            Color = Main.hslToRgb(Main.rgbToHsl(Color).X % 1f, Main.rgbToHsl(Color).Y, Main.rgbToHsl(Color).Z);
            Opacity *= 0.98f;
            Rotation += Spin * (Velocity.X > 0f).ToDirectionInt();

            float opacity = GetLerpValue(1f, 0.85f, LifetimeCompletion, clamped: true);
            Color *= opacity;
        }

        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            int frame = (int)Math.Floor(Time / (Lifetime / 6f));
            Rectangle rectangle = new(Variant * 80, frame * 80, 80, 80);
            spriteBatch.Draw(texture, Position - Main.screenPosition, rectangle, Color * Opacity, Rotation, rectangle.Size() / 2f, Scale, SpriteEffects.None, 0f);
        }
    }
}
