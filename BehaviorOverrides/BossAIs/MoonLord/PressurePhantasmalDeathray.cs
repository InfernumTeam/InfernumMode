using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class PressurePhantasmalDeathray : BaseLaserbeamProjectile
    {
        public const int LifetimeConstant = 120;

        public ref float AngularVelocity => ref projectile.ai[0];
        public int OwnerIndex => (int)projectile.ai[1];
        public override float Lifetime => LifetimeConstant;
        public override Color LaserOverlayColor => new Color(1f, 1f, 1f, 0f);
        public override Color LightCastColor => Color.White;
        public override Texture2D LaserBeginTexture => ModContent.GetTexture("InfernumMode/ExtraTextures/PhantasmalBeamBegin");
        public override Texture2D LaserMiddleTexture => ModContent.GetTexture("InfernumMode/ExtraTextures/PhantasmalBeamMid");
        public override Texture2D LaserEndTexture => ModContent.GetTexture("InfernumMode/ExtraTextures/PhantasmalBeamEnd");
        public override float MaxLaserLength => 3200f;
        public override float MaxScale => 1f;
        public override string Texture => "InfernumMode/ExtraTextures/PhantasmalBeamBegin";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Phantasmal Deathray");

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

            NPC eye = Main.npc[OwnerIndex];
            Vector2 pupilOffset = TrueEyeOfCthulhuBehaviorOverride.CalculatePupilOffset(eye, -eye.spriteDirection);
            projectile.Center = Main.npc[OwnerIndex].Center + pupilOffset + projectile.velocity * 2f;
            projectile.velocity = projectile.velocity.RotatedBy(AngularVelocity).SafeNormalize(Vector2.UnitY);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<Nightwither>(), 300);
    }
}
