using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class HolySpear3 : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Holy Spear");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            Projectile.ai[1] += 1f;

            float slowGateValue = 90f;
            float fastGateValue = 30f;
            float minVelocity = 3f;
            float maxVelocity = 12f;
            float deceleration = 0.95f;
            float acceleration = 1.2f;

            if (Projectile.ai[1] <= slowGateValue)
            {
                if (Projectile.velocity.Length() > minVelocity)
                    Projectile.velocity *= deceleration;
            }
            else if (Projectile.ai[1] < slowGateValue + fastGateValue)
            {
                if (Projectile.velocity.Length() < maxVelocity)
                    Projectile.velocity *= acceleration;
            }
            else
                Projectile.ai[1] = 0f;

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D spearTexture = Utilities.ProjTexture(Projectile.type);
            Color baseColor = Color.OrangeRed;
            if (!Main.dayTime)
                baseColor = CalamityUtils.MulticolorLerp(Projectile.identity / 6f % 0.65f, ProvidenceBehaviorOverride.NightPalette);

            baseColor.A = 128;

            float fadeFactor = Utils.GetLerpValue(15f, 30f, Projectile.timeLeft, true) * Utils.GetLerpValue(360f, 340f, Projectile.timeLeft, true) * (1f + 0.2f * (float)Math.Cos(Main.GlobalTimeWrappedHourly % 80f / 0.5f * MathHelper.TwoPi * 3f)) * 0.8f;
            Color fadedBrightColor = baseColor * 0.5f;
            fadedBrightColor.A = 0;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Vector2 origin5 = spearTexture.Size() / 2f;
            Color brightColor = fadedBrightColor * fadeFactor * 1.2f;
            Color dimColor = fadedBrightColor * fadeFactor * 0.6f;
            Vector2 largeScale = new Vector2(1f, 1.5f) * fadeFactor;
            Vector2 smallScale = new Vector2(0.5f, 1f) * fadeFactor;

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 0; i < Projectile.oldPos.Length; i++)
                {
                    Vector2 drawPos = Projectile.oldPos[i] + drawPosition;
                    Color color = Projectile.GetAlpha(brightColor) * ((Projectile.oldPos.Length - i) / Projectile.oldPos.Length);
                    Main.spriteBatch.Draw(spearTexture, drawPos, null, color, Projectile.rotation, origin5, largeScale, spriteEffects, 0f);
                    Main.spriteBatch.Draw(spearTexture, drawPos, null, color, Projectile.rotation, origin5, smallScale, spriteEffects, 0f);

                    color = Projectile.GetAlpha(dimColor) * ((Projectile.oldPos.Length - i) / Projectile.oldPos.Length);
                    Main.spriteBatch.Draw(spearTexture, drawPos, null, color, Projectile.rotation, origin5, largeScale * 0.6f, spriteEffects, 0f);
                    Main.spriteBatch.Draw(spearTexture, drawPos, null, color, Projectile.rotation, origin5, smallScale * 0.6f, spriteEffects, 0f);
                }
            }

            Main.spriteBatch.Draw(spearTexture, drawPosition, null, brightColor, Projectile.rotation, origin5, largeScale, spriteEffects, 0);
            Main.spriteBatch.Draw(spearTexture, drawPosition, null, brightColor, Projectile.rotation, origin5, smallScale, spriteEffects, 0);
            Main.spriteBatch.Draw(spearTexture, drawPosition, null, dimColor, Projectile.rotation, origin5, largeScale * 0.6f, spriteEffects, 0);
            Main.spriteBatch.Draw(spearTexture, drawPosition, null, dimColor, Projectile.rotation, origin5, smallScale * 0.6f, spriteEffects, 0);

            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int buffType = Main.dayTime ? ModContent.BuffType<HolyFlames>() : ModContent.BuffType<Nightwither>();
            target.AddBuff(buffType, 120);
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
