using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumDeus
{
    public class DeusMine2 : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Astral Mine");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.alpha = 100;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 900;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
        }

        public override void AI()
        {
            if (Projectile.timeLeft < 85)
            {
                Projectile.Opacity = Projectile.timeLeft / 85f;
                Projectile.damage = 0;
            }
            else
                Projectile.velocity *= 0.99f;
        }

        public override bool CanHitPlayer(Player target) => Projectile.timeLeft < 815 && Projectile.timeLeft > 85;

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, Projectile.alpha);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 240);
    }
}
