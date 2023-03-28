using CalamityMod;
using CalamityMod.NPCs;
using InfernumMode.Common.Graphics.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Draedon.Thanatos
{
    public class ThanatosAresComboLaser : ModProjectile, IScreenCullDrawer
    {
        public ref float TelegraphDelay => ref Projectile.ai[0];
        public ref float PulseFlash => ref Projectile.localAI[0];
        public ref float InitialSpeed => ref Projectile.localAI[1];
        public NPC ThingToAttachTo => Main.npc.IndexInRange((int)Projectile.ai[1]) ? Main.npc[(int)Projectile.ai[1]] : null;

        public Vector2 InitialDestination;
        public Vector2 Destination;
        public Vector2 Velocity;
        public const float TelegraphTotalTime = 55f;
        public const float TelegraphFadeTime = 15f;
        public const float TelegraphWidth = 1200f;
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
            Projectile.timeLeft = 960;
            Projectile.hide = true;
            Projectile.Calamity().DealsDefenseDamage = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
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
            // Die if Ares is not present.
            if (CalamityGlobalNPC.draedonExoMechPrime == -1 || !Main.npc[CalamityGlobalNPC.draedonExoMechPrime].active)
            {
                Projectile.Kill();
                return;
            }

            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];

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

            if (InitialSpeed == 0f)
                InitialSpeed = Projectile.velocity.Length();

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
                    Projectile.extraUpdates = 3;
                    Projectile.velocity = Velocity;
                    Velocity = Vector2.Zero;
                    Projectile.netUpdate = true;
                }

                // Direction and rotation.
                if (Projectile.velocity.X < 0f)
                {
                    Projectile.spriteDirection = -1;
                    Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;
                }
                else
                {
                    Projectile.spriteDirection = 1;
                    Projectile.rotation = Projectile.velocity.ToRotation();
                }

                if (Projectile.WithinRange(aresBody.Center + Vector2.UnitY * 34f, 60f))
                    Projectile.Kill();
            }
            else if (Destination == Vector2.Zero)
            {
                // Set start of telegraph to the npc center.
                Projectile.Center = ThingToAttachTo.Center;

                // Set destination of the laser, the target's center.
                Destination = InitialDestination;

                // Calculate and store the velocity that will be used for laser telegraph rotation and beam firing.
                Vector2 projectileDestination = Destination - ThingToAttachTo.Center;
                Velocity = Vector2.Normalize(projectileDestination) * InitialSpeed;

                // Set velocity to zero.
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;

                // Direction and rotation.
                if (Projectile.velocity.X < 0f)
                {
                    Projectile.spriteDirection = -1;
                    Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;
                }
                else
                {
                    Projectile.spriteDirection = 1;
                    Projectile.rotation = Projectile.velocity.ToRotation();
                }
            }
            else
            {
                // Calculate and store the velocity that will be used for laser telegraph rotation and beam firing.
                Vector2 projectileDestination = Destination - Projectile.Center;
                Velocity = Vector2.Normalize(projectileDestination) * InitialSpeed;

                // Direction and rotation.
                if (Projectile.velocity.X < 0f)
                {
                    Projectile.spriteDirection = -1;
                    Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;
                }
                else
                {
                    Projectile.spriteDirection = 1;
                    Projectile.rotation = Projectile.velocity.ToRotation();
                }
            }

            TelegraphDelay++;
        }

        public override bool CanHitPlayer(Player target) => TelegraphDelay > TelegraphTotalTime;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(projHitbox.Center(), Projectile.Size.Length() * 0.5f, targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public void CullDraw(SpriteBatch spriteBatch)
        {
            if (TelegraphDelay >= TelegraphTotalTime)
            {
                Vector2 drawOffset = Projectile.velocity.SafeNormalize(Vector2.Zero) * -30f;
                Projectile.Center += drawOffset;
                CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], Color.White * Projectile.Opacity, 1);
                Projectile.Center -= drawOffset;
                return;
            }

            Texture2D laserTelegraph = Assets.ExtraTextures.InfernumTextureRegistry.BloomLineSmall.Value;

            float xScale = 0.75f;
            if (TelegraphDelay < TelegraphFadeTime)
                xScale = MathHelper.Lerp(0f, xScale, TelegraphDelay / 15f);
            if (TelegraphDelay > TelegraphTotalTime - TelegraphFadeTime)
                xScale = MathHelper.Lerp(xScale, 0f, (TelegraphDelay - (TelegraphTotalTime - TelegraphFadeTime)) / 15f);

            Vector2 scaleInner = new(xScale, 1750f / laserTelegraph.Height);
            Vector2 origin = laserTelegraph.Size() * new Vector2(0.5f, 0f);
            Vector2 scaleOuter = scaleInner * new Vector2(1.5f, 1f);

            Color colorOuter = Color.Lerp(Color.Red, Color.White, TelegraphDelay / TelegraphTotalTime * 0.4f);
            Color colorInner = Color.Lerp(colorOuter, Color.White, 0.5f);
            colorOuter.A = 0;
            colorInner.A = 0;
            Main.spriteBatch.Draw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorOuter, Velocity.ToRotation() - MathHelper.PiOver2, origin, scaleOuter, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorInner, Velocity.ToRotation() - MathHelper.PiOver2, origin, scaleInner, SpriteEffects.None, 0f);
        }
    }
}