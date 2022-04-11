using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class GreatStar : ModProjectile
    {
        public bool DarknessVariant
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value.ToInt();
        }
        public bool CanSplit
        {
            get => Projectile.ai[1] == 0f;
            set => Projectile.ai[1] = 1 - value.ToInt();
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Star");
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 70;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.scale = 1.1f;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            if (CanSplit)
                Projectile.velocity *= 0.965f;
            else if (Projectile.velocity.Length() < 22f)
                Projectile.velocity *= 1.019f;

            Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, 1f, 0.56f);

            if (Projectile.timeLeft != 2)
                return;

            if (CanSplit && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SoundEngine.PlaySound(SoundID.Item20, Projectile.position);
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                for (int i = 0; i < 5; i++)
                {
                    float shootSpeed = MathHelper.Lerp(2f, 12f, i / 4f);
                    shootSpeed = MathHelper.Lerp(shootSpeed, 31f, Utils.GetLerpValue(700f, 1900f, Projectile.Distance(target.Center), true));

                    int star = Projectile.NewProjectile(Projectile.Center, Projectile.SafeDirectionTo(target.Center + target.velocity * 32f) * shootSpeed, Projectile.type, Projectile.damage, Projectile.knockBack);
                    Main.projectile[star].Size /= 1.3f;
                    Main.projectile[star].scale /= 1.3f;
                    Main.projectile[star].netUpdate = true;
                    Main.projectile[star].ai[1] = 1f;
                }
            }
        }


        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            float lerpMult = (1f + 0.22f * (float)Math.Cos(Main.GlobalTimeWrappedHourly % 30f * MathHelper.TwoPi * 3f + Projectile.identity % 10f)) * 0.8f;

            Texture2D texture = Main.projectileTexture[Projectile.type];
            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY);
            Color baseColor = new(255, 200, 100, 255);
            if (!Main.dayTime)
                baseColor = CalamityUtils.MulticolorLerp(Projectile.identity / 6f % 0.6f, ProvidenceBehaviorOverride.NightPalette);

            baseColor *= Projectile.Opacity * 0.5f;
            baseColor.A = 0;
            Color colorA = baseColor;
            Color colorB = baseColor * 0.5f;
            colorA *= lerpMult;
            colorB *= lerpMult;
            Vector2 origin = texture.Size() / 2f;
            Vector2 scale = new(Projectile.scale * Projectile.Opacity * lerpMult);

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver2, origin, scale, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorA, 0f, origin, scale, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver2, origin, scale * 0.8f, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorB, 0f, origin, scale * 0.8f, spriteEffects, 0);

            spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver2 + Main.GlobalTimeWrappedHourly * 0.35f, origin, scale, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorA, Main.GlobalTimeWrappedHourly * 0.35f, origin, scale, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver2 + Main.GlobalTimeWrappedHourly * 0.625f, origin, scale * 0.8f, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorB, Main.GlobalTimeWrappedHourly * 0.625f, origin, scale * 0.8f, spriteEffects, 0);

            spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver4, origin, scale * 0.6f, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver4 * 3f, origin, scale * 0.6f, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver4, origin, scale * 0.4f, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver4 * 3f, origin, scale * 0.4f, spriteEffects, 0);

            spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver4 + Main.GlobalTimeWrappedHourly * 1.1f * 0.75f, origin, scale * 0.6f, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver4 * 3f + Main.GlobalTimeWrappedHourly * 1.1f * 0.75f, origin, scale * 0.6f, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver4 + Main.GlobalTimeWrappedHourly * 1.1f, origin, scale * 0.4f, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver4 * 3f + Main.GlobalTimeWrappedHourly * 1.1f, origin, scale * 0.4f, spriteEffects, 0);

            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
