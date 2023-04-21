using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Destroyer
{
    public class DestroyerPierceLaserTelegraph : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Telegraph");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 45;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            // Pulse in and out.
            Projectile.scale = (float)Math.Sin(MathHelper.Pi * Projectile.timeLeft / 45f) * 6f;
        }

        public override bool? CanDamage()/* tModPorter Suggestion: Return null instead of false */ => false;

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            // Create an inner and outer telegraph.
            Color outerTelegraphColor = new(255, 70, 53, 0);
            Color innerTelegraphColor = new Color(255, 142, 132, 0) * 1.15f;
            float outerTelegraphScale = Projectile.scale;
            float innerTelegraphScale = outerTelegraphScale * 0.56f;
            Vector2 telegraphStart = Projectile.Center;
            Vector2 telegraphEnd = telegraphStart + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 5000f;

            Main.spriteBatch.DrawLineBetter(telegraphStart, telegraphEnd, outerTelegraphColor, outerTelegraphScale);
            Main.spriteBatch.DrawLineBetter(telegraphStart, telegraphEnd, innerTelegraphColor, innerTelegraphScale);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Utilities.NewProjectileBetter(Projectile.Center, Projectile.velocity.SafeNormalize(-Vector2.UnitY), ModContent.ProjectileType<DestroyerPierceLaser>(), DestroyerHeadBehaviorOverride.PierceLaserbeamDamage, 0f);
        }
    }
}
