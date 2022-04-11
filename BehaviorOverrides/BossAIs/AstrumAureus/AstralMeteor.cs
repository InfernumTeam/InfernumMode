using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumAureus
{
    public class AstralMeteor : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Astral Meteor");
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 180;
            Projectile.height = 180;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 85;
            CooldownSlot = 1;

            Projectile.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            Projectile.tileCollide = Projectile.timeLeft < 40;
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 7 % Main.projFrames[Projectile.type];

            if (Math.Abs(Projectile.velocity.X) > 0.2)
                Projectile.spriteDirection = -Projectile.direction;

            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(184, 184, 184, Projectile.alpha);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D tex = Main.projectileTexture[Projectile.type];
            int height = Main.projectileTexture[Projectile.type].Height / Main.projFrames[Projectile.type];
            int y = height * Projectile.frame;
            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), new Rectangle(0, y, tex.Width, height), Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(tex.Width / 2f, height / 2f), Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Custom/ProvidenceHolyBlastImpact"), Projectile.Center);

            int dustType = Main.rand.NextBool(6) ? ModContent.DustType<AstralBlue>() : ModContent.DustType<AstralOrange>();
            for (int i = 0; i < 35; i++)
            {
                Dust astralFire = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, 0f, 100, default, 2f);
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
                    Utilities.NewProjectileBetter(Projectile.Center, shootVelocity, ModContent.ProjectileType<AstralBlueComet>(), 180, 0f);
                }
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 360);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
