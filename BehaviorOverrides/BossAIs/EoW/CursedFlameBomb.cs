using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EoW
{
    public class CursedFlameBomb : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cursed Flame Bomb");
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 150;
            projectile.Opacity = 0f;
        }

        public override void AI()
        {
            projectile.tileCollide = projectile.timeLeft < 120;
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.05f, 0f, 1f);
            projectile.rotation += (projectile.velocity.X > 0f).ToDirectionInt() * 0.3f;

            Lighting.AddLight(projectile.Center, Vector3.One * 0.7f);
        }

        // Explode into smaller flames on death.
        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item74, projectile.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int burstCount = NPC.CountNPCS(NPCID.EaterofWorldsHead) >= 4 ? 4 : 5;
            float burstSpeed = projectile.velocity.Length();
            float initialAngleOffset = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < burstCount; i++)
            {
                Vector2 shootVelocity = (initialAngleOffset + MathHelper.TwoPi * i / burstCount).ToRotationVector2() * burstSpeed;
                Utilities.NewProjectileBetter(projectile.Center + shootVelocity, shootVelocity, ModContent.ProjectileType<CursedBullet>(), 80, 0f);
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.Lerp(Color.White, Color.MediumPurple, Utils.InverseLerp(45f, 0f, projectile.timeLeft, true)) * projectile.Opacity;

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, Color.White, ProjectileID.Sets.TrailingMode[projectile.type], 3);
            return false;
        }

        public override bool CanDamage() => projectile.Opacity >= 1f;

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.AddBuff(BuffID.CursedInferno, 120);
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
