using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class GreatStar : ModProjectile
    {
        public bool DarknessVariant
        {
            get => projectile.ai[0] == 1f;
            set => projectile.ai[0] = value.ToInt();
        }
        public bool CanSplit
        {
            get => projectile.ai[1] == 0f;
            set => projectile.ai[1] = 1 - value.ToInt();
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Star");
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 70;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.alpha = 255;
            projectile.scale = 1.1f;
            projectile.penetrate = -1;
            projectile.timeLeft = 120;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            if (CanSplit)
                projectile.velocity *= 0.965f;
            else if (projectile.velocity.Length() < 22f)
                projectile.velocity *= 1.019f;

            projectile.Opacity = MathHelper.Lerp(projectile.Opacity, 1f, 0.56f);

            if (projectile.timeLeft != 2)
                return;

            if (CanSplit && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Main.PlaySound(SoundID.Item20, projectile.position);
                Player target = Main.player[Player.FindClosest(projectile.Center, 1, 1)];
                for (int i = 0; i < 5; i++)
                {
                    float shootSpeed = MathHelper.Lerp(2f, 12f, i / 4f);
                    shootSpeed = MathHelper.Lerp(shootSpeed, 31f, Utils.InverseLerp(700f, 1900f, projectile.Distance(target.Center), true));

                    int star = Projectile.NewProjectile(projectile.Center, projectile.SafeDirectionTo(target.Center + target.velocity * 32f) * shootSpeed, projectile.type, projectile.damage, projectile.knockBack);
                    Main.projectile[star].Size /= 1.3f;
                    Main.projectile[star].scale /= 1.3f;
                    Main.projectile[star].netUpdate = true;
                    Main.projectile[star].ai[1] = 1f;
                }
            }
        }


        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            float lerpMult = (1f + 0.22f * (float)Math.Cos(Main.GlobalTime % 30f * MathHelper.TwoPi * 3f + projectile.identity % 10f)) * 0.8f;

            Texture2D texture = Main.projectileTexture[projectile.type];
            Vector2 drawPos = projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
            Color baseColor = new Color(255, 200, 100, 255);
            if (!Main.dayTime)
                baseColor = CalamityUtils.MulticolorLerp(projectile.identity / 6f % 0.6f, ProvidenceBehaviorOverride.NightPalette);

            baseColor *= projectile.Opacity * 0.5f;
            baseColor.A = 0;
            Color colorA = baseColor;
            Color colorB = baseColor * 0.5f;
            colorA *= lerpMult;
            colorB *= lerpMult;
            Vector2 origin = texture.Size() / 2f;
            Vector2 scale = new Vector2(projectile.scale * projectile.Opacity * lerpMult);

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver2, origin, scale, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorA, 0f, origin, scale, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver2, origin, scale * 0.8f, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorB, 0f, origin, scale * 0.8f, spriteEffects, 0);

            spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver2 + Main.GlobalTime * 0.35f, origin, scale, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorA, Main.GlobalTime * 0.35f, origin, scale, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver2 + Main.GlobalTime * 0.625f, origin, scale * 0.8f, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorB, Main.GlobalTime * 0.625f, origin, scale * 0.8f, spriteEffects, 0);

            spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver4, origin, scale * 0.6f, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver4 * 3f, origin, scale * 0.6f, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver4, origin, scale * 0.4f, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver4 * 3f, origin, scale * 0.4f, spriteEffects, 0);

            spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver4 + Main.GlobalTime * 1.1f * 0.75f, origin, scale * 0.6f, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorA, MathHelper.PiOver4 * 3f + Main.GlobalTime * 1.1f * 0.75f, origin, scale * 0.6f, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver4 + Main.GlobalTime * 1.1f, origin, scale * 0.4f, spriteEffects, 0);
            spriteBatch.Draw(texture, drawPos, null, colorB, MathHelper.PiOver4 * 3f + Main.GlobalTime * 1.1f, origin, scale * 0.4f, spriteEffects, 0);

            return false;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
