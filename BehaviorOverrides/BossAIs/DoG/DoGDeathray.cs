using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class DoGDeathray : BaseLaserbeamProjectile
    {
        public int OwnerIndex => (int)Projectile.ai[1];
        public override float Lifetime => 90;
        public override Color LaserOverlayColor => new Color(153, 84, 176, 0) * 0.945f;
        public override Color LightCastColor => Color.Fuchsia;
        public override Texture2D LaserBeginTexture => Utilities.ProjTexture(Projectile.type);
        public override Texture2D LaserMiddleTexture => Main.extraTexture[21];
        public override Texture2D LaserEndTexture => Main.extraTexture[22];
        public override float MaxLaserLength => 2400f;
        public override float MaxScale => 0.5f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Deathray");

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = (int)Lifetime;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
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
        public override void AttachToSomething()
        {
            if (!Main.projectile.IndexInRange(OwnerIndex))
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = Main.projectile[OwnerIndex].Center;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = Projectile;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(InfernumMode.CalamityMod.Find<ModBuff>("GodSlayerInferno").Type, 300);
        }
    }
}
