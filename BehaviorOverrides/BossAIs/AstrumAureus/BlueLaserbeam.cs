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
    public class BlueLaserbeam : BaseLaserbeamProjectile
    {
        public int OwnerIndex => (int)projectile.ai[1];
        public override float Lifetime => OrangeLaserbeam.LaserLifetime;
        public override Color LaserOverlayColor => Color.White;
        public override Color LightCastColor => Color.Cyan;
        public override Texture2D LaserBeginTexture => Main.projectileTexture[projectile.type];
        public override Texture2D LaserMiddleTexture => ModContent.GetTexture("InfernumMode/ExtraTextures/BlueLaserbeamMid");
        public override Texture2D LaserEndTexture => ModContent.GetTexture("InfernumMode/ExtraTextures/BlueLaserbeamEnd");
        public override float MaxLaserLength => 3100f;
        public override float MaxScale => 1f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Astral Deathray");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 12;
            projectile.hostile = true;
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
            if (!Main.npc.IndexInRange(GlobalNPCOverrides.AstrumAureus))
            {
                projectile.Kill();
                return;
            }

            projectile.Center = Main.npc[GlobalNPCOverrides.AstrumAureus].Center - Vector2.UnitY * 12f;
            projectile.Opacity = 1f;
            RotationalSpeed = MathHelper.Pi / Lifetime * -OrangeLaserbeam.FullCircleRotationFactor;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override bool CanDamage() => Time > 35f;

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 300);
    }
}
