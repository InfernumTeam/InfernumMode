using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class DoGMine : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Cosmic Mine");

        public override void SetDefaults()
        {
            projectile.width = 34;
            projectile.height = 36;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 90;
            projectile.alpha = 100;
            cooldownSlot = 1;
        }

        public override void AI() => projectile.alpha = Utils.Clamp(projectile.alpha - 10, 0, 255);

        public override Color? GetAlpha(Color drawColor) => new Color(200, Main.DiscoG, 255, projectile.alpha);

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(InfernumMode.CalamityMod.BuffType("GodSlayerInferno"), 300);
            target.AddBuff(BuffID.Darkness, 300, true);
        }

        public override void Kill(int timeLeft)
        {
            projectile.Damage();
            Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 14);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // Cardinal Directions
            for (float i = 0; i < 4f; i += 1f)
            {
                float angle = MathHelper.TwoPi / 4f * i;
                Projectile.NewProjectile(projectile.Center, (angle + MathHelper.PiOver2).ToRotationVector2() * 6f, InfernumMode.CalamityMod.ProjectileType("CosmicFlameBurst"), 80, 0f);
            }
        }
    }
}
