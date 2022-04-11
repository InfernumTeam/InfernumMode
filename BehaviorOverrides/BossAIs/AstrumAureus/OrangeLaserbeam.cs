using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Projectiles.BaseProjectiles;
using InfernumMode.GlobalInstances;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumAureus
{
    public class OrangeLaserbeam : BaseLaserbeamProjectile
    {
        public int OwnerIndex => (int)Projectile.ai[1];
        public const int LaserLifetime = 132;
        public const float FullCircleRotationFactor = 0.84f;
        public override float Lifetime => LaserLifetime;
        public override Color LaserOverlayColor => Color.White;
        public override Color LightCastColor => Color.Orange;
        public override Texture2D LaserBeginTexture => Main.projectileTexture[Projectile.type];
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/OrangeLaserbeamMid").Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/OrangeLaserbeamEnd").Value;
        public override float MaxLaserLength => 3100f;
        public override float MaxScale => 1f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Astral Deathray");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
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
            if (!Main.npc.IndexInRange(GlobalNPCOverrides.AstrumAureus))
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = Main.npc[GlobalNPCOverrides.AstrumAureus].Center - Vector2.UnitY * 12f;
            Projectile.Opacity = 1f;
            RotationalSpeed = MathHelper.Pi / Lifetime * FullCircleRotationFactor;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = Projectile;

        public override bool CanDamage() => Time > 35f;

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 300);
    }
}
