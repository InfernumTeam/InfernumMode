using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares
{
    public class AresCannonLaser : ModProjectile
    {
        public float TelegraphDelay
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public NPC ThingToAttachTo => Main.npc.IndexInRange((int)Projectile.ai[1]) ? Main.npc[(int)Projectile.ai[1]] : null;

        public Vector2 Destination;
        public Vector2 Velocity;
        public const float TelegraphTotalTime = 30f;
        public const float TelegraphFadeTime = 15f;
        public const float TelegraphWidth = 4200f;
        public const float LaserVelocity = 10f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Exo Flame Laser");
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 600;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(Destination);
            writer.WriteVector2(Velocity);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Destination = reader.ReadVector2();
            Velocity = reader.ReadVector2();
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 12)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame > 3)
                Projectile.frame = 0;

            Lighting.AddLight(Projectile.Center, 0.6f, 0f, 0f);

            // Die if the thing to attach to disappears.
            if (ThingToAttachTo is null || !ThingToAttachTo.active)
            {
                Projectile.Kill();
                return;
            }

            // Direction and rotation.
            Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += MathHelper.Pi;

            // Fade in after telegraphs have faded.
            if (TelegraphDelay > TelegraphTotalTime)
            {
                if (Projectile.alpha > 0)
                    Projectile.alpha -= 25;
                if (Projectile.alpha < 0)
                    Projectile.alpha = 0;

                // If a velocity is in reserve, set the true velocity to it and make it as "taken" by setting it to <0,0>
                if (Velocity != Vector2.Zero)
                {
                    Projectile.extraUpdates = 2;
                    Projectile.velocity = Velocity;
                    Velocity = Vector2.Zero;
                    Projectile.netUpdate = true;
                }
                return;
            }

            // Set start of telegraph to the npc center.
            Projectile.Center = ThingToAttachTo.Center + new Vector2(ThingToAttachTo.spriteDirection * -78f, 16f).RotatedBy(ThingToAttachTo.rotation);

            if (Destination == Vector2.Zero)
            {
                // Set destination of the laser, the target's center.
                Destination = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * 1600f;

                // Calculate and store the velocity that will be used for laser telegraph rotation and beam firing.
                Velocity = Projectile.velocity;

                // Set velocity to zero.
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
            }
            else
            {
                // Calculate and store the velocity that will be used for laser telegraph rotation and beam firing.
                if (Projectile.velocity != Vector2.Zero)
                    Velocity = Projectile.velocity;
            }

            TelegraphDelay++;
        }

        public override bool CanHitPlayer(Player target) => TelegraphDelay > TelegraphTotalTime;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(projHitbox.Center(), Projectile.Size.Length() * 0.5f, targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (TelegraphDelay >= TelegraphTotalTime)
            {
                lightColor.R = (byte)(255 * Projectile.Opacity);
                lightColor.G = (byte)(255 * Projectile.Opacity);
                lightColor.B = (byte)(255 * Projectile.Opacity);
                Vector2 drawOffset = Projectile.velocity.SafeNormalize(Vector2.Zero) * -30f;
                Projectile.Center += drawOffset;
                CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);
                Projectile.Center -= drawOffset;
                return false;
            }

            Texture2D laserTelegraph = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/LaserWallTelegraphBeam").Value;

            float yScale = 2f;
            if (TelegraphDelay < TelegraphFadeTime)
                yScale = MathHelper.Lerp(0f, 2f, TelegraphDelay / 15f);
            if (TelegraphDelay > TelegraphTotalTime - TelegraphFadeTime)
                yScale = MathHelper.Lerp(2f, 0f, (TelegraphDelay - (TelegraphTotalTime - TelegraphFadeTime)) / 15f);

            Vector2 scaleInner = new(TelegraphWidth / laserTelegraph.Width, yScale);
            Vector2 origin = laserTelegraph.Size() * new Vector2(0f, 0.5f);
            Vector2 scaleOuter = scaleInner * new Vector2(1f, 2.2f);

            Color colorOuter = Color.Lerp(Color.Red, Color.Crimson, TelegraphDelay / TelegraphTotalTime * 2f % 1f); // Iterate through crimson and red once and then flash.
            Color colorInner = Color.Lerp(colorOuter, Color.White, 0.75f);

            colorOuter *= 0.6f;
            colorInner *= 0.6f;

            Main.spriteBatch.Draw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorInner, Velocity.ToRotation(), origin, scaleInner, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorOuter, Velocity.ToRotation(), origin, scaleOuter, SpriteEffects.None, 0f);
            return false;
        }
    }
}
