using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumAureus
{
    public class OrangeLaserbeam : BaseLaserbeamProjectile
    {
        public int OwnerIndex => (int)projectile.ai[1];
        public override float Lifetime => 180;
        public override Color LaserOverlayColor => Color.White;
        public override Color LightCastColor => Color.Orange;
        public override Texture2D LaserBeginTexture => Main.projectileTexture[projectile.type];
        public override Texture2D LaserMiddleTexture => ModContent.GetTexture("InfernumMode/ExtraTextures/OrangeLaserbeamMid");
        public override Texture2D LaserEndTexture => ModContent.GetTexture("InfernumMode/ExtraTextures/OrangeLaserbeamEnd");
        public override float MaxLaserLength => 3100f;
        public override float MaxScale => 1f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Astral Deathray");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 12;
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

            int index = (int)projectile.Infernum().ExtraAI[0] - 1;
            if (index == -1)
                index = AstrumAureusBehaviorOverride.LaserbeamSpawnOffsets.Count - 1;
            projectile.Center = Main.npc[OwnerIndex].Center + AstrumAureusBehaviorOverride.LaserbeamSpawnOffsets[index];
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override bool CanDamage() => Time > 35f;

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 300);
    }
}
