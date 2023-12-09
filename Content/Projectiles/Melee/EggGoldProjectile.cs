using CalamityMod.Buffs.DamageOverTime;
using InfernumMode.Content.Dusts;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Melee
{
    public class EggGoldProjectile : ModProjectile
    {
        private readonly List<int> PossibleDebuffs = new()
        {
            ModContent.BuffType<Nightwither>(),
            ModContent.BuffType<HolyFlames>(),
            ModContent.BuffType<GodSlayerInferno>(),
            BuffID.CursedInferno
        };

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Golden Egg");
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.TrailCacheLength[Type] = 4;
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Melee;
            Projectile.width = 42;
            Projectile.height = 78;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 180;
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 7)
            {
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
                Projectile.frameCounter = 0;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;

            if (Main.rand.NextBool(2))
                Dust.NewDust(Projectile.Center, 4, 4, ModContent.DustType<EggDust>(), Projectile.velocity.X * 0.3f, Projectile.velocity.Y * 0.3f, Scale: 1f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(PossibleDebuffs[Main.rand.Next(0, PossibleDebuffs.Count)], hit.Crit ? 60 : 30);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            for (int i = 0; i < 10;  i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1f, 3f);
                Dust.NewDust(Projectile.Center, 4, 4, ModContent.DustType<EggDust>(), velocity.X, velocity.Y * 0.3f, Scale: 1f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(Projectile, lightColor, 0);
            return false;
        }
    }
}
