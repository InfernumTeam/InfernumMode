using CalamityMod;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class LunarAsteroid : ModProjectile
    {
        public ref float Owner => ref Projectile.ai[0];
        public ref float Time => ref Projectile.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Lunar Flame");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            // Die if the owner is not present or is dead.
            if (!Main.npc.IndexInRange((int)Owner) || !Main.npc[(int)Owner].active || Main.npc[(int)Owner].ai[0] == 2f)
            {
                Projectile.Kill();
                return;
            }

            NPC core = Main.npc[(int)Owner];

            float distanceToCore = Projectile.Distance(core.Center);
            Projectile.scale = Utils.GetLerpValue(0f, 240f, distanceToCore, true);
            Projectile.rotation += (Projectile.velocity.X > 0f).ToDirectionInt() * 0.007f;

            if (distanceToCore < 360f)
                Projectile.velocity = (Projectile.velocity * 29f + Projectile.SafeDirectionTo(core.Center) * 12f) / 30f;

            if (distanceToCore < Main.rand.NextFloat(64f, 90f))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MoonLordExplosion>(), 0, 0f);
                Projectile.Kill();
            }

            Time++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, Color.White, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.scale * 17f, targetHitbox);
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item20, Projectile.Center);
            for (int dust = 0; dust < 4; dust++)
                Dust.NewDust(Projectile.position + Projectile.velocity, Projectile.width, Projectile.height, (int)CalamityDusts.Nightwither, 0f, 0f);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
