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

namespace InfernumMode.BehaviorOverrides.BossAIs.BrimstoneElemental
{
    public class BrimstoneDeathray : BaseLaserbeamProjectile
    {
        public int OwnerIndex => (int)projectile.ai[1];
        public override float Lifetime => 85;
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

            int petalDamage = (CalamityWorld.downedProvidence || BossRushEvent.BossRushActive) && BrimstoneElementalBehaviorOverride.ReadyToUseBuffedAI ? 310 : 140;
            for (float petalOffset = 20f; petalOffset < LaserLength; petalOffset += 165f)
            {
                Vector2 petalSpawnPosition = OwnerEyePosition + projectile.velocity * petalOffset;
                for (int i = -1; i <= 1; i++)
                {
                    Vector2 petalVelocity = projectile.velocity.RotatedBy(MathHelper.PiOver2 * i) * 8f;
                    if (BossRushEvent.BossRushActive)
                        petalVelocity *= 1.85f;
                    Utilities.NewProjectileBetter(petalSpawnPosition, petalVelocity, ModContent.ProjectileType<BrimstonePetal2>(), petalDamage, 0f);
                }
            }
        }

        public override bool CanDamage() => Time > 10f;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Time <= 50f)
                return;

            if ((CalamityWorld.downedProvidence || BossRushEvent.BossRushActive) && BrimstoneElementalBehaviorOverride.ReadyToUseBuffedAI)
                target.AddBuff(ModContent.BuffType<AbyssalFlames>(), 300);
            else
                target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 300);
        }
    }
}
