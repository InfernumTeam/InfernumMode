using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeShield : ModProjectile
    {
        public NPC Owner => Main.npc[(int)OwnerIndex];

        public ref float OwnerIndex => ref Projectile.ai[0];

        public ref float Radius => ref Projectile.ai[1];

        public const float MaxRadius = 100f;

        public const int HealTime = 180;

        public const int Lifetime = HealTime + HealTime / 3;

        public override string Texture => "InfernumMode/ExtraTextures/GreyscaleObjects/Gleam";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shield");
        }

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.timeLeft = Lifetime;
            Projectile.scale = 0.001f;
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange((int)OwnerIndex) || !Main.npc[(int)OwnerIndex].active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = Owner.Center;

            Radius = (float)Math.Sin(Projectile.timeLeft / (float)Lifetime * MathHelper.Pi) * MaxRadius * 4f;
            if (Radius > MaxRadius)
                Radius = MaxRadius;
            Projectile.scale = 2f;

            if (PrimeHeadBehaviorOverride.AnyArms && Projectile.timeLeft < HealTime)
                Projectile.timeLeft = HealTime;

            Projectile.ExpandHitboxBy((int)(Radius * Projectile.scale), (int)(Radius * Projectile.scale));
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (!Main.npc.IndexInRange((int)OwnerIndex) || !Main.npc[(int)OwnerIndex].active)
                return false;

            Main.spriteBatch.EnterShaderRegion();

            Vector2 scale = new(1.5f, 1f);
            DrawData drawData = new(InfernumTextureRegistry.CultistRayMap.Value,
                Projectile.Center - Main.screenPosition + Projectile.Size * scale * 0.5f,
                new Rectangle(0, 0, Projectile.width, Projectile.height),
                new Color(new Vector4(1f)) * 0.7f * Projectile.Opacity,
                Projectile.rotation,
                Projectile.Size,
                scale,
                SpriteEffects.None, 0);

            GameShaders.Misc["ForceField"].UseColor(Color.Lerp(Color.Orange, Color.Red, 0.84f));
            GameShaders.Misc["ForceField"].Apply(drawData);
            drawData.Draw(Main.spriteBatch);

            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
