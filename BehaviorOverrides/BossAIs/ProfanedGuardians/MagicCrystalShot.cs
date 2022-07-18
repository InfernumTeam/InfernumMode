using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class MagicCrystalShot : ModProjectile
    {
        public static readonly Color[] ColorSet = new Color[]
        {
            // Pale pink crystal.
            new Color(181, 136, 177),

            // Profaned fire.
            new Color(255, 191, 73),

            // Yellow-orange crystal.
            new Color(255, 194, 161),
        };

        public NPC Target => Main.npc[(int)Projectile.ai[0]];
        public Color StreakBaseColor => CalamityUtils.MulticolorLerp(Projectile.ai[1] % 0.999f, ColorSet);
        public ref float HealAmount => ref Projectile.localAI[0];
        public override string Texture => "CalamityMod/Projectiles/StarProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crystalline Light");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 24;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            CooldownSlot = 1;
        }

        public override void AI()
        {
            if (Projectile.timeLeft > 140f)
            {
                float offsetScale = (float)Math.Cos(Projectile.identity % 6f / 6f + Projectile.position.X / 320f + Projectile.position.Y / 160f);

                if (Projectile.velocity.Length() < 43f)
                    Projectile.velocity *= 1.008f;
                Projectile.velocity = Projectile.velocity.RotatedBy(offsetScale * MathHelper.TwoPi / 240f);
            }

            if (Projectile.timeLeft > 30f)
            {
                Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                Vector2 idealVelocity = Projectile.SafeDirectionTo(target.Center) * 16f;
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, idealVelocity, MathHelper.Lerp(0.045f, 0.1f, Utils.GetLerpValue(140f, 30f, Projectile.timeLeft, true)));
            }

            if (Projectile.timeLeft < 15)
                Projectile.damage = 0;

            Projectile.Opacity = Utils.GetLerpValue(240f, 220f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D streakTexture = TextureAssets.Projectile[Projectile.type].Value;
            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i - 1] == Vector2.Zero || Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completionRatio = i / (float)Projectile.oldPos.Length;
                float fade = (float)Math.Pow(completionRatio, 2D);
                float scale = Projectile.scale * MathHelper.Lerp(1.3f, 0.9f, Utils.GetLerpValue(0f, 0.24f, completionRatio, true)) *
                    MathHelper.Lerp(0.9f, 0.56f, Utils.GetLerpValue(0.5f, 0.78f, completionRatio, true));
                Color drawColor = Color.Lerp(StreakBaseColor, new Color(229, 255, 255), fade) * (1f - fade) * Projectile.Opacity;
                drawColor.A = 0;

                Vector2 drawPosition = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f - Main.screenPosition;
                Vector2 drawPosition2 = Vector2.Lerp(drawPosition, Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition, 0.5f);
                Main.spriteBatch.Draw(streakTexture, drawPosition, null, drawColor, Projectile.oldRot[i], streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(streakTexture, drawPosition2, null, drawColor, Projectile.oldRot[i], streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < Projectile.oldPos.Length / 3; i++)
            {
                if (targetHitbox.Intersects(Utils.CenteredRectangle(Projectile.oldPos[i] + Projectile.Size * 0.5f, Projectile.Size)))
                    return true;
            }
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 120);
            if (Projectile.timeLeft > 15)
                Projectile.timeLeft = 15;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}