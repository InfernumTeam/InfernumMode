using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using PolterNPC = CalamityMod.NPCs.Polterghast.Polterghast;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Polterghast
{
    public class CirclingEctoplasm : ModProjectile
    {
        public float OrbitRadius;

        public float OrbitOffsetAngle;

        public float OrbitAngularVelocity;

        public Vector2 OrbitCenter;

        public static readonly Color[] ColorSet = new Color[]
        {
            Color.Pink,
            Color.Cyan
        };

        public Color StreakBaseColor => Color.Lerp(CalamityUtils.MulticolorLerp(Projectile.ai[1] % 0.999f, ColorSet), Color.White, 0.2f);

        public override string Texture => "CalamityMod/Projectiles/StarProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ectoplasm Wisp");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 1600;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hostile = true;
            CooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(OrbitRadius);
            writer.Write(OrbitOffsetAngle);
            writer.Write(OrbitAngularVelocity);
            writer.WriteVector2(OrbitCenter);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            OrbitRadius = reader.ReadSingle();
            OrbitOffsetAngle = reader.ReadSingle();
            OrbitAngularVelocity = reader.ReadSingle();
            OrbitCenter = reader.ReadVector2();
        }

        public override void AI()
        {
            // Fade away if Polter is gone or not performing the relevant attack.
            int fadeoutTime = 40;
            int polterghastIndex = NPC.FindFirstNPC(ModContent.NPCType<PolterNPC>());
            if (polterghastIndex == -1 && Projectile.timeLeft > fadeoutTime)
                Projectile.timeLeft = fadeoutTime;

            if (polterghastIndex >= 0 && Main.npc[polterghastIndex].ai[0] != (int)PolterghastBehaviorOverride.PolterghastAttackType.WispCircleCharges && Projectile.timeLeft > fadeoutTime)
                Projectile.timeLeft = fadeoutTime;

            if (Projectile.timeLeft < fadeoutTime)
                Projectile.damage = 0;

            // Spin and orbit in place.
            OrbitOffsetAngle += OrbitAngularVelocity;
            Projectile.Center = OrbitCenter + OrbitOffsetAngle.ToRotationVector2() * OrbitRadius;

            // Initialize the hue.
            if (Projectile.ai[1] == 0f)
                Projectile.ai[1] = Main.rand.NextFloat();

            // Calculate opacity, scale, and rotation.
            Projectile.Opacity = Utils.GetLerpValue(1600, 1555f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, fadeoutTime, Projectile.timeLeft, true);
            Projectile.scale = Projectile.Opacity + 0.01f;
            Projectile.rotation = (Projectile.position - Projectile.oldPosition).ToRotation() + MathHelper.PiOver2;
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
                float scale = Projectile.scale * MathHelper.Lerp(1f, 0.56f, Utils.GetLerpValue(0f, 0.24f, completionRatio, true)) * MathHelper.Lerp(0.9f, 0.56f, Utils.GetLerpValue(0.5f, 0.78f, completionRatio, true));
                Color drawColor = Color.HotPink * (1f - fade) * Projectile.Opacity;
                drawColor.A = 0;

                Vector2 drawPosition = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f - Main.screenPosition;
                Vector2 drawPosition2 = Vector2.Lerp(drawPosition, Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition, 0.5f);
                Main.spriteBatch.Draw(streakTexture, drawPosition, null, drawColor, Projectile.oldRot[i], streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(streakTexture, drawPosition2, null, drawColor, Projectile.oldRot[i], streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override bool? CanDamage() => Projectile.timeLeft < 1480 ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < 3; i++)
            {
                if (targetHitbox.Intersects(Utils.CenteredRectangle(Projectile.oldPos[i] + Projectile.Size * 0.5f, Projectile.Size)))
                    return true;
            }
            return false;
        }
    }
}