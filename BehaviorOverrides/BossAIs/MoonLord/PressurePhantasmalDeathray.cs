using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class PressurePhantasmalDeathray : BaseLaserbeamProjectile
    {
        public const int LifetimeConstant = 120;

        public ref float AngularVelocity => ref Projectile.ai[0];
        public int OwnerIndex => (int)Projectile.ai[1];
        public override float Lifetime => LifetimeConstant;
        public override Color LaserOverlayColor => new(1f, 1f, 1f, 0f);
        public override Color LightCastColor => Color.White;
        public override Texture2D LaserBeginTexture => ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/PhantasmalBeamBegin", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/PhantasmalBeamMid", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/PhantasmalBeamEnd", AssetRequestMode.ImmediateLoad).Value;
        public override float MaxLaserLength => 3200f;
        public override float MaxScale => 1f;
        public override string Texture => "InfernumMode/ExtraTextures/PhantasmalBeamBegin";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Phantasmal Deathray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = (int)Lifetime;
            Projectile.Calamity().DealsDefenseDamage = true;
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
            if (!Main.npc.IndexInRange(OwnerIndex))
            {
                Projectile.Kill();
                return;
            }

            NPC eye = Main.npc[OwnerIndex];
            Vector2 pupilOffset = TrueEyeOfCthulhuBehaviorOverride.CalculatePupilOffset(eye, -eye.spriteDirection);
            Projectile.Center = Main.npc[OwnerIndex].Center + pupilOffset + Projectile.velocity * 2f;
            Projectile.velocity = Projectile.velocity.RotatedBy(AngularVelocity).SafeNormalize(Vector2.UnitY);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = Projectile;

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<Nightwither>(), 300);
    }
}
