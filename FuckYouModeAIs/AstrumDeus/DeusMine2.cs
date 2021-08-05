using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.AstrumDeus
{
    public class DeusMine2 : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Astral Mine");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 26;
            projectile.hostile = true;
            projectile.alpha = 100;
            projectile.penetrate = -1;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 900;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(projectile.localAI[0]);
            writer.Write(projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            projectile.localAI[0] = reader.ReadSingle();
            projectile.localAI[1] = reader.ReadSingle();
        }

        public override void AI()
        {
            if (projectile.timeLeft < 85)
            {
                projectile.Opacity = projectile.timeLeft / 85f;
                projectile.damage = 0;
            }
            else
                projectile.velocity *= 0.99f;
		}

        public override bool CanHitPlayer(Player target) => projectile.timeLeft < 815 && projectile.timeLeft > 85;

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, projectile.alpha);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 240);
    }
}
