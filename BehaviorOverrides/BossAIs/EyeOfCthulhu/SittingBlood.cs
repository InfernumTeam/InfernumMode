using CalamityMod;
using CalamityMod.Projectiles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EyeOfCthulhu
{
    public class SittingBlood : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tooth Ball");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 330;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            if (Projectile.velocity.Y < 14f)
                Projectile.velocity.Y += 0.25f;
            Projectile.alpha = Utils.Clamp(Projectile.alpha - 36, 0, 255);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Projectile.timeLeft < 60)
            {
                Projectile.scale *= 0.992f;
                CalamityGlobalProjectile.ExpandHitboxBy(Projectile, (int)Math.Ceiling(24 * Projectile.scale));
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity.X *= 0.94f;
            return false;
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough)
        {
            fallThrough = false;
            return base.TileCollideStyle(ref width, ref height, ref fallThrough);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }

        public override void Kill(int timeLeft)
        {
            Player closetstPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            if (Main.netMode == NetmodeID.MultiplayerClient || MathHelper.Distance(closetstPlayer.Center.X, Projectile.Center.X) < 240f)
                return;

            for (int i = 0; i < 2; i++)
            {
                Utilities.NewProjectileBetter(Projectile.Center, -Vector2.UnitY.RotatedByRandom(0.92f) * Main.rand.NextFloat(21f, 31f), ModContent.ProjectileType<EoCTooth2>(), 75, 0f);
            }
        }
    }
}
