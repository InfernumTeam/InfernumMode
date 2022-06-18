using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumAureus
{
    public class AstralMeteor : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Astral Meteor");
            Main.projFrames[projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            projectile.width = 180;
            projectile.height = 180;
            projectile.hostile = true;
            projectile.penetrate = 1;
            projectile.timeLeft = 85;
            cooldownSlot = 1;

            projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            projectile.tileCollide = projectile.timeLeft < 40;
            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 7 % Main.projFrames[projectile.type];

            if (Math.Abs(projectile.velocity.X) > 0.2)
                projectile.spriteDirection = -projectile.direction;

            projectile.rotation = projectile.velocity.ToRotation();
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(184, 184, 184, projectile.alpha);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D tex = Main.projectileTexture[projectile.type];
            int height = Main.projectileTexture[projectile.type].Height / Main.projFrames[projectile.type];
            int y = height * projectile.frame;
            Main.spriteBatch.Draw(tex, projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY), new Rectangle(0, y, tex.Width, height), projectile.GetAlpha(lightColor), projectile.rotation, new Vector2(tex.Width / 2f, height / 2f), projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ProvidenceHolyBlastImpact"), projectile.Center);

            int dustType = Main.rand.NextBool(6) ? ModContent.DustType<AstralBlue>() : ModContent.DustType<AstralOrange>();
            for (int i = 0; i < 35; i++)
            {
                Dust astralFire = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, dustType, 0f, 0f, 100, default, 2f);
                astralFire.velocity *= 3f;
                if (Main.rand.NextBool(2))
                {
                    astralFire.scale = 0.5f;
                    astralFire.fadeIn = Main.rand.NextFloat(1f, 2f);
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float offsetIncrement = Main.rand.NextBool().ToInt() * 0.5f;
                for (int i = 0; i < 12; i++)
                {
                    Vector2 shootVelocity = (MathHelper.TwoPi * (i + offsetIncrement) / 12f).ToRotationVector2() * 13f;
                    Utilities.NewProjectileBetter(projectile.Center, shootVelocity, ModContent.ProjectileType<AstralBlueComet>(), 180, 0f);
                }
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 360);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
