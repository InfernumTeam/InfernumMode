using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon.ArtemisAndApollo
{
    public class ApolloTelegraphedPlasmaSpark : ModProjectile
    {
        public ref float TelegraphDelay => ref Projectile.ai[0];
        public ref float PulseFlash => ref Projectile.localAI[0];
        public ref float InitialSpeed => ref Projectile.localAI[1];
        public NPC ThingToAttachTo => Main.npc.IndexInRange((int)Projectile.ai[1]) ? Main.npc[(int)Projectile.ai[1]] : null;

        public Vector2 InitialDestination;
        public Vector2 Destination;
        public Vector2 Velocity;
        public const float TelegraphTotalTime = ArtemisLaser.TelegraphTotalTime;
        public const float TelegraphFadeTime = ArtemisLaser.TelegraphFadeTime;
        public const float TelegraphWidth = 4200f;
        public const float LaserVelocity = ArtemisLaser.LaserVelocity;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Plasma Bolt");
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
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
            writer.Write(InitialSpeed);
            writer.WriteVector2(Destination);
            writer.WriteVector2(Velocity);
            writer.WriteVector2(InitialDestination);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            InitialSpeed = reader.ReadSingle();
            Destination = reader.ReadVector2();
            Velocity = reader.ReadVector2();
            InitialDestination = reader.ReadVector2();
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 12)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= Main.projFrames[Projectile.type])
                Projectile.frame = 0;

            Lighting.AddLight(Projectile.Center, 0.6f, 0f, 0f);

            // Die if the thing to attach to disappears.
            if (ThingToAttachTo is null || !ThingToAttachTo.active)
            {
                Projectile.Kill();
                return;
            }

            if (ThingToAttachTo.Opacity <= 0f)
            {
                Projectile.Kill();
                return;
            }

            if (InitialSpeed == 0f)
                InitialSpeed = Projectile.velocity.Length();

            // Fade in after telegraphs have faded.
            float positionOffset = ExoMechManagement.CurrentTwinsPhase >= 2 ? 90f : 70f;
            if (TelegraphDelay > TelegraphTotalTime)
            {
                if (Projectile.alpha > 0)
                    Projectile.alpha -= 25;
                if (Projectile.alpha < 0)
                    Projectile.alpha = 0;

                // If a velocity is in reserve, set the true velocity to it and make it as "taken" by setting it to <0,0>
                if (Velocity != Vector2.Zero)
                {
                    Projectile.extraUpdates = 3;
                    Projectile.velocity = Velocity;
                    Velocity = Vector2.Zero;
                    Projectile.netUpdate = true;
                }

                // Direction and rotation.
                Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            }
            else if (Destination == Vector2.Zero)
            {
                // Set start of telegraph to the npc center.
                Projectile.Center = ThingToAttachTo.Center + (ThingToAttachTo.rotation - MathHelper.PiOver2).ToRotationVector2() * positionOffset;

                // Set destination of the laser, the target's center.
                Destination = InitialDestination;

                // Calculate and store the velocity that will be used for laser telegraph rotation and beam firing.
                Vector2 projectileDestination = Destination - ThingToAttachTo.Center;
                Velocity = Vector2.Normalize(projectileDestination) * InitialSpeed;

                // Set velocity to zero.
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;

                // Direction and rotation.
                Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            }
            else
            {
                // Set start of telegraph to the npc center.
                Projectile.Center = ThingToAttachTo.Center + (ThingToAttachTo.rotation - MathHelper.PiOver2).ToRotationVector2() * positionOffset;

                // Calculate and store the velocity that will be used for laser telegraph rotation and beam firing.
                Vector2 projectileDestination = Destination - ThingToAttachTo.Center;
                Velocity = Vector2.Normalize(projectileDestination) * InitialSpeed;

                // Direction and rotation.
                Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            }

            TelegraphDelay++;
        }

        public override bool CanHitPlayer(Player target) => TelegraphDelay > TelegraphTotalTime;
        
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Projectile.RotatingHitboxCollision(targetHitbox.TopLeft(), targetHitbox.Size());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (TelegraphDelay >= TelegraphTotalTime)
            {
                lightColor.R = (byte)(70 * Projectile.Opacity);
                lightColor.G = (byte)(192 * Projectile.Opacity);
                lightColor.B = (byte)(70 * Projectile.Opacity);
                lightColor.A = (byte)(255 * (1f - Projectile.Opacity));
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

            Color colorOuter = Color.Lerp(Color.Lime, Color.Green, TelegraphDelay / TelegraphTotalTime * 2f % 1f);
            Color colorInner = Color.Lerp(colorOuter, Color.White with { A = 0 }, 0.67f);

            colorOuter *= 0.7f;
            colorInner *= 0.85f;

            Main.spriteBatch.Draw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorOuter, Velocity.ToRotation(), origin, scaleOuter, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorInner, Velocity.ToRotation(), origin, scaleInner, SpriteEffects.None, 0f);
            return false;
        }
    }
}
