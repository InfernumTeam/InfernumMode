using System.IO;
using InfernumMode.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        public static readonly Color[] ColorSet =
        [
            Color.Pink,
            Color.Cyan
        ];

        public Color StreakBaseColor => Color.Lerp(LumUtils.MulticolorLerp(Projectile.ai[1] % 0.999f, ColorSet), Color.White, 0.2f);

        public override string Texture => "CalamityMod/Projectiles/StarProj";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ectoplasm Wisp");
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
            CooldownSlot = ImmunityCooldownID.Bosses;
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
            // Don't draw offscreen projectiles.
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 250;

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
            Projectile.rotation = (Projectile.position - Projectile.oldPosition).ToRotation() + PiOver2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Simply draws a soul that is squished to be the size of the hitbox.
            // This looks a bit funny but visual quality isn't the point when this config is enabled.
            if (InfernumConfig.Instance.ReducedGraphicsConfig)
            {
                OptimizedDraw();
                return false;
            }

            // Draws whispy lights. Slightly more performance intensive due to looping, but also more visually interesting.
            DefaultDraw();
            return false;
        }

        public void DefaultDraw()
        {
            Texture2D streakTexture = TextureAssets.Projectile[Projectile.type].Value;
            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i - 1] == Vector2.Zero || Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completionRatio = i / (float)Projectile.oldPos.Length;
                float fade = Pow(completionRatio, 2f);
                float scale = Projectile.scale * Lerp(1f, 0.56f, Utils.GetLerpValue(0f, 0.24f, completionRatio, true)) * Lerp(0.9f, 0.56f, Utils.GetLerpValue(0.5f, 0.78f, completionRatio, true));
                Color drawColor = Color.HotPink * (1f - fade) * Projectile.Opacity;
                drawColor.A = 0;

                Vector2 drawPosition = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f - Main.screenPosition;
                Vector2 drawPosition2 = Vector2.Lerp(drawPosition, Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition, 0.5f);
                Main.spriteBatch.Draw(streakTexture, drawPosition, null, drawColor, Projectile.oldRot[i], streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(streakTexture, drawPosition2, null, drawColor, Projectile.oldRot[i], streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
        }

        public void OptimizedDraw()
        {
            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Polterghast/SoulMedium").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, 4, 0, 0);
            Vector2 scale = Vector2.One * Projectile.scale * 0.4f;
            Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation + Pi, frame.Size() * 0.5f, scale, 0, 0);
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
