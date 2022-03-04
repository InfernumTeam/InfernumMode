using CalamityMod;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class LunarAsteroid : ModProjectile
    {
        public ref float Owner => ref projectile.ai[0];
        public ref float Time => ref projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lunar Flame");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 34;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 360;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            // Die if the owner is not present or is dead.
            if (!Main.npc.IndexInRange((int)Owner) || !Main.npc[(int)Owner].active || Main.npc[(int)Owner].ai[0] == 2f)
            {
                projectile.Kill();
                return;
            }

            NPC core = Main.npc[(int)Owner];

            float distanceToCore = projectile.Distance(core.Center);
            projectile.scale = Utils.InverseLerp(0f, 240f, distanceToCore, true);
            projectile.rotation += (projectile.velocity.X > 0f).ToDirectionInt() * 0.007f;

            if (distanceToCore < 360f)
                projectile.velocity = (projectile.velocity * 29f + projectile.SafeDirectionTo(core.Center) * 12f) / 30f;

            if (distanceToCore < Main.rand.NextFloat(64f, 90f))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(projectile.Center, Vector2.Zero, ModContent.ProjectileType<MoonLordExplosion>(), 0, 0f);
                projectile.Kill();
            }

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, Color.White, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(projectile.Center, projectile.scale * 17f, targetHitbox);
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 20);
            for (int dust = 0; dust < 4; dust++)
                Dust.NewDust(projectile.position + projectile.velocity, projectile.width, projectile.height, (int)CalamityDusts.Nightwither, 0f, 0f);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
