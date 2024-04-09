using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Deerclops
{
    public class DeathAnimationShadowHand : ModProjectile
    {
        public float HoverSide => (Projectile.ai[0] == 1f).ToDirectionInt();

        public ref float ThumbRotation => ref Projectile.localAI[0];

        // public override void SetStaticDefaults() => DisplayName.SetDefault("Shadow Hand");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3600;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Disappear if Deerclops is not present.
            int deerclopsIndex = NPC.FindFirstNPC(NPCID.Deerclops);
            if (deerclopsIndex == -1)
            {
                Projectile.Kill();
                return;
            }

            // Fade in, but only to a point that the hand is as opaque as Deerclops is.
            NPC deerclops = Main.npc[deerclopsIndex];
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.04f, 0f, deerclops.Opacity + 0.01f);

            // Choose a location to stick to.
            float attackTimer = deerclops.ai[1];
            Vector2 hoverOffset;

            // Fade in and spin above deerclops.
            if (attackTimer <= DeerclopsBehaviorOverride.ShadowHandSpinTime)
            {
                float repositionInterolant = Utils.GetLerpValue(DeerclopsBehaviorOverride.ShadowHandSpinTime - 84f, DeerclopsBehaviorOverride.ShadowHandSpinTime - 1f, attackTimer, true);
                float horizontalHoverOffset = Lerp(0f, HoverSide * -90f, repositionInterolant);
                float verticalHoverOffset = Lerp(-192f, -356f, repositionInterolant);
                hoverOffset = new Vector2(horizontalHoverOffset, verticalHoverOffset);
                hoverOffset += (Pi * attackTimer / DeerclopsBehaviorOverride.ShadowHandSpinTime * 4f + HoverSide * PiOver2).ToRotationVector2() * new Vector2(146f, 15f);

                // Look at Deerclops.
                LookAtDeerclops(deerclops);
            }

            // Reel back from deerclops, with a minor upward bias so that the hands move in an arc.
            else if (attackTimer <= DeerclopsBehaviorOverride.ShadowHandSpinTime + DeerclopsBehaviorOverride.ShadowHandReelbackTime)
            {
                hoverOffset = CalculateReelbackHoverOffset(deerclops, Utils.GetLerpValue(0f, DeerclopsBehaviorOverride.ShadowHandReelbackTime, attackTimer - DeerclopsBehaviorOverride.ShadowHandSpinTime, true));

                // Look at Deerclops.
                LookAtDeerclops(deerclops);
            }

            // Make the shadow hands grab deerclops' arms.
            else
            {
                float grabInterpolant = Utils.GetLerpValue(0f, DeerclopsBehaviorOverride.ShadowHandGrabTime, attackTimer - DeerclopsBehaviorOverride.ShadowHandSpinTime - DeerclopsBehaviorOverride.ShadowHandReelbackTime, true);
                hoverOffset = Vector2.Lerp(CalculateReelbackHoverOffset(deerclops, 1f), new(HoverSide * -32f - deerclops.spriteDirection * 20f, HoverSide * 82f - 36f), Pow(grabInterpolant, 1.58f));

                // Make the thumbs do a grabbing motion, as though it's locking Deerclops' hands in place.
                ThumbRotation = grabInterpolant * -0.4f;

                if (grabInterpolant >= 1f)
                    Projectile.rotation = Projectile.rotation.AngleLerp(Pi, 0.1f);
            }

            // Move to the hover destination.
            MoveToDestination(deerclops.Center + hoverOffset + deerclops.position - deerclops.oldPosition);
        }

        public Vector2 CalculateReelbackHoverOffset(NPC deerclops, float reelBackInterpolant)
        {
            float reelBackDistance = Lerp(0f, 500f, reelBackInterpolant);
            Vector2 hoverOffset = new(HoverSide * deerclops.spriteDirection * 90f, -156f);
            hoverOffset -= hoverOffset.SafeNormalize(Vector2.Zero) * new Vector2(2f, -0.3f) * reelBackDistance;
            hoverOffset.Y -= reelBackDistance * 0.3f;
            return hoverOffset;
        }

        public void MoveToDestination(Vector2 destination)
        {
            float flySpeed = 36f;
            float distanceFromDestination = Projectile.Distance(destination);
            float fastMovementInterpolant = Utils.GetLerpValue(240f, 376f, distanceFromDestination, true) + Utils.GetLerpValue(55f, 32f, distanceFromDestination, true);
            Vector2 jitterMovement = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(destination) * flySpeed, 3f / flySpeed);
            Vector2 fastMovement = (destination - Projectile.Center) * 0.04f;
            Projectile.velocity = Vector2.Lerp(jitterMovement, fastMovement, fastMovementInterpolant);
        }

        public void LookAtDeerclops(NPC deerclops)
        {
            Projectile.rotation = Projectile.AngleTo(deerclops.Center);
            Projectile.spriteDirection = (Math.Cos(Projectile.rotation) > 0f).ToDirectionInt();
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += Pi;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            float rotation = Projectile.rotation;
            Color backglowColor = Color.Lerp(Color.Red, Color.White, 0.5f) * Projectile.Opacity * 0.5f;
            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            for (int j = 0; j < 4; j++)
            {
                Vector2 offsetDirection = rotation.ToRotationVector2();
                double spin = Main.GlobalTimeWrappedHourly * TwoPi / 24f + TwoPi * j / 4f;
                Main.EntitySpriteDraw(texture, drawPosition + offsetDirection.RotatedBy(spin) * 6f, null, backglowColor, rotation, origin, Projectile.scale, direction, 0);
            }
            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.Black), rotation, origin, Projectile.scale, direction, 0);
            return false;
        }
    }
}
