using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;

namespace InfernumMode.FuckYouModeAIs.DoG
{
    public class DoGDeathray : BaseLaserbeamProjectile
    {
        public int OwnerIndex => (int)projectile.ai[1];
        public override float Lifetime => 90;
        public override Color LaserOverlayColor => new Color(153, 84, 176, 0) * 0.945f;
        public override Color LightCastColor => Color.Fuchsia;
        public override Texture2D LaserBeginTexture => Main.projectileTexture[projectile.type];
        public override Texture2D LaserMiddleTexture => Main.extraTexture[21];
        public override Texture2D LaserEndTexture => Main.extraTexture[22];
        public override float MaxLaserLength => 2400f;
        public override float MaxScale => 1f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Deathray");

        public override void SetDefaults()
        {
            projectile.width = 48;
            projectile.height = 48;
            projectile.hostile = true;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.timeLeft = (int)Lifetime;
            cooldownSlot = 1;
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
        public override void AttachToSomething()
        {
            if (!Main.projectile.IndexInRange(OwnerIndex))
            {
                projectile.Kill();
                return;
            }

            projectile.Center = Main.projectile[OwnerIndex].Center;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(InfernumMode.CalamityMod.BuffType("GodSlayerInferno"), 300);
        }
    }
}
