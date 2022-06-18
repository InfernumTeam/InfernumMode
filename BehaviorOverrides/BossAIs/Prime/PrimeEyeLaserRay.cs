using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeEyeLaserRay : BaseLaserbeamProjectile
    {
        public ref float AngularVelocity => ref projectile.ai[0];
        public int OwnerIndex => (int)projectile.ai[1];
        public override float Lifetime => 120;
        public override Color LaserOverlayColor => Color.White;
        public override Color LightCastColor => Color.White;
        public override Texture2D LaserBeginTexture => ModContent.GetTexture("InfernumMode/ExtraTextures/PrimeBeamBegin");
        public override Texture2D LaserMiddleTexture => ModContent.GetTexture("InfernumMode/ExtraTextures/PrimeBeamMid");
        public override Texture2D LaserEndTexture => ModContent.GetTexture("InfernumMode/ExtraTextures/PrimeBeamEnd");
        public override float MaxLaserLength => 2400f;
        public override float MaxScale => 1f;
        public override string Texture => "InfernumMode/ExtraTextures/PrimeBeamBegin";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Deathray");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 14;
            projectile.hostile = true;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.timeLeft = (int)Lifetime;
            projectile.Calamity().canBreakPlayerDefense = true;
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
            if (!Main.npc.IndexInRange(OwnerIndex))
            {
                projectile.Kill();
                return;
            }

            projectile.Center = Main.npc[OwnerIndex].Center + new Vector2((AngularVelocity > 0f).ToDirectionInt() * 16f, -7f).RotatedBy(Main.npc[OwnerIndex].rotation) + projectile.velocity * 2f;
            projectile.velocity = projectile.velocity.RotatedBy(AngularVelocity).SafeNormalize(Vector2.UnitY);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(BuffID.OnFire, 240);
    }
}
