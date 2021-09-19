using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Events;
using CalamityMod.Projectiles.BaseProjectiles;
using CalamityMod.Projectiles.Boss;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.BrimstoneElemental
{
    public class BrimstoneDeathray : BaseLaserbeamProjectile
    {
        public int OwnerIndex => (int)projectile.ai[1];
        public override float Lifetime => 90;
        public override Color LaserOverlayColor => Color.White;
        public override Color LightCastColor => Color.Red;
        public override Texture2D LaserBeginTexture => Main.projectileTexture[projectile.type];
        public override Texture2D LaserMiddleTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/BrimstoneRayMid");
        public override Texture2D LaserEndTexture => ModContent.GetTexture("CalamityMod/ExtraTextures/Lasers/BrimstoneRayEnd");
        public override float MaxLaserLength => 3100f;
        public override float MaxScale => 1f;
        public Vector2 OwnerEyePosition => Main.npc[OwnerIndex].Center + new Vector2(Main.npc[OwnerIndex].spriteDirection * 20f, -70f);
        public override void SetStaticDefaults() => DisplayName.SetDefault("Brimstone Deathray");

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

            projectile.Center = OwnerEyePosition;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int dartDamage = CalamityWorld.downedProvidence || BossRushEvent.BossRushActive ? 310 : 140;
            for (float dartOffset = 20f; dartOffset < LaserLength; dartOffset += 200f)
            {
                Vector2 dartSpawnPosition = OwnerEyePosition + projectile.velocity * dartOffset;
                for (int i = -1; i <= 1; i++)
                {
                    Vector2 dartVelocity = projectile.velocity.RotatedBy(MathHelper.PiOver2 * i) * 8f;
                    Utilities.NewProjectileBetter(dartSpawnPosition, dartVelocity, ModContent.ProjectileType<BrimstoneBarrage>(), dartDamage, 0f);
                }
            }
        }

        public override bool CanDamage() => Time > 35f;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (CalamityWorld.downedProvidence || BossRushEvent.BossRushActive)
                target.AddBuff(ModContent.BuffType<AbyssalFlames>(), 300);
            else
                target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 300);
        }
    }
}
