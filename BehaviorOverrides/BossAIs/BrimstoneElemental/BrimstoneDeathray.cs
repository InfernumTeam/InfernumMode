using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Events;
using CalamityMod.Projectiles.BaseProjectiles;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.BrimstoneElemental
{
    public class BrimstoneDeathray : BaseLaserbeamProjectile
    {
        public int OwnerIndex => (int)Projectile.ai[1];
        public override float Lifetime => 85;
        public override Color LaserOverlayColor => Color.White;
        public override Color LightCastColor => Color.Red;
        public override Texture2D LaserBeginTexture => TextureAssets.Projectile[Projectile.type].Value;
        public override Texture2D LaserMiddleTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/BrimstoneRayMid", AssetRequestMode.ImmediateLoad).Value;
        public override Texture2D LaserEndTexture => ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/BrimstoneRayEnd", AssetRequestMode.ImmediateLoad).Value;
        public override float MaxLaserLength => 3100f;
        public override float MaxScale => 1f;
        public Vector2 OwnerEyePosition => Main.npc[OwnerIndex].Center + new Vector2(Main.npc[OwnerIndex].spriteDirection * 20f, -70f);
        public override void SetStaticDefaults() => DisplayName.SetDefault("Brimstone Deathray");

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
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

            Projectile.Center = OwnerEyePosition;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = Projectile;

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int petalDamage = (DownedBossSystem.downedProvidence || BossRushEvent.BossRushActive) && BrimstoneElementalBehaviorOverride.ReadyToUseBuffedAI ? 310 : 140;
            for (float petalOffset = 20f; petalOffset < LaserLength; petalOffset += 165f)
            {
                Vector2 petalSpawnPosition = OwnerEyePosition + Projectile.velocity * petalOffset;
                for (int i = -1; i <= 1; i++)
                {
                    Vector2 petalVelocity = Projectile.velocity.RotatedBy(MathHelper.PiOver2 * i) * 8f;
                    if (BossRushEvent.BossRushActive)
                        petalVelocity *= 1.85f;
                    Utilities.NewProjectileBetter(petalSpawnPosition, petalVelocity, ModContent.ProjectileType<BrimstonePetal2>(), petalDamage, 0f);
                }
            }
        }

        public override bool? CanDamage() => Time > 10f ? null : false;

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Time <= 50f)
                return;

            if ((DownedBossSystem.downedProvidence || BossRushEvent.BossRushActive) && BrimstoneElementalBehaviorOverride.ReadyToUseBuffedAI)
                target.AddBuff(ModContent.BuffType<VulnerabilityHex>(), 300);
            else
                target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 300);
        }
    }
}
