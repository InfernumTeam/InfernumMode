using CalamityMod.Events;
using CalamityMod.NPCs.NormalNPCs;
using InfernumMode.Miscellaneous;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using Terraria.Audio;
using Terraria.GameContent;
using CalamityMod.Particles;

namespace InfernumMode.BehaviorOverrides.BossAIs.Deerclops
{
    public class DeerclopsBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Deerclops;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        #region Enumerations
        public enum DeerclopsAttackType
        {
            GroundLaser
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            npc.TargetClosestIfTargetIsInvalid();

            Player target = Main.player[npc.target];
            bool shouldUseShadowFade = false;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float frame = ref npc.localAI[0];
            ref float shadowFadeTimer = ref npc.localAI[3];

            switch ((DeerclopsAttackType)attackType)
            {
                case DeerclopsAttackType.GroundLaser:
                    DoBehavior_GroundLaser(npc, target, ref shouldUseShadowFade, ref frame, ref attackTimer);
                    break;
            }

            shadowFadeTimer = Utils.Clamp(shadowFadeTimer + shouldUseShadowFade.ToDirectionInt(), 0f, 36f);
            attackTimer++;
            return false;
        }

        public static void DoBehavior_GroundLaser(NPC npc, Player target, ref bool shouldUseShadowFade, ref float frame, ref float attackTimer)
        {
            int laserChargeupTime = 84;
            ref float laserRayInterpolant = ref npc.Infernum().ExtraAI[0];
            ref float laserRayDirection = ref npc.Infernum().ExtraAI[1];

            // Slow down and use the shadow fade effect.
            DefaultMovement(npc, target, 0f, true);
            shouldUseShadowFade = true;

            // Look at the target.
            if (attackTimer < laserChargeupTime - 30)
			{
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                laserRayInterpolant = Utils.GetLerpValue(0f, 32f, attackTimer, true);
                laserRayDirection = -MathHelper.PiOver2 + npc.spriteDirection * 0.32f;
			}

            // Create charge particles.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer < laserChargeupTime)
            {
                float particleSpawnRate = (float)Math.Pow(Utils.GetLerpValue(0f, laserChargeupTime, attackTimer, true), 0.33D);
                if (particleSpawnRate < 0.25f)
                    particleSpawnRate = 0.25f;

                if (Main.rand.NextFloat() < particleSpawnRate)
                {
                    float particleScale = Main.rand.NextFloat(0.55f, 0.8f);
                    Vector2 particleSpawnPosition = GetEyePosition(npc) + Main.rand.NextVector2Unit() * Main.rand.NextFloat(60f, 125f);
                    Vector2 particleVelocity = (GetEyePosition(npc) - particleSpawnPosition) * 0.04f;
                    GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(particleSpawnPosition, particleVelocity, particleScale, Color.Red, 40));
                }
            }

            // Decide frames.
            if (attackTimer < laserChargeupTime / 2)
                frame = Utils.Remap(attackTimer % 60f, 0f, 60f, 0f, 11f);
            else
                frame = Utils.Remap(attackTimer - laserChargeupTime / 2f, 0f, 32f, 12f, 14f);
        }

        public static Vector2 GetEyePosition(NPC npc)
        {
            return npc.Center + new Vector2(npc.spriteDirection * 26f, -72f);
        }

        public static void DefaultMovement(NPC npc, Player target, float walkSpeed, bool haltMovement)
        {
            Rectangle hitbox = target.Hitbox;
            float verticalAcceleration = 0.4f;
            bool closeToTarget = MathHelper.Distance(npc.Center.X, target.Center.X) < 80f;
            bool shouldSlowdown = closeToTarget || haltMovement;
            if (shouldSlowdown)
                npc.velocity.X *= 0.9f;
            else
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, Math.Sign(target.Center.X - npc.Center.X) * walkSpeed, 0.25f);

            int checkWidth = 40;
            int checkHeight = 20;
            Vector2 checkTopLeft = new(npc.Center.X - checkWidth / 2, npc.position.Y + npc.height - checkHeight);
            bool acceptTopSurfaces = npc.Bottom.Y >= hitbox.Top;
            bool shouldRiseUpward = Collision.SolidCollision(checkTopLeft, checkWidth, checkHeight, acceptTopSurfaces);
            bool tileAhead = !Collision.SolidCollision(checkTopLeft + Vector2.UnitX * npc.direction * checkWidth, 16, 80, acceptTopSurfaces);
            if ((checkTopLeft.X < hitbox.X && checkTopLeft.X + npc.width > hitbox.X + hitbox.Width || closeToTarget) && 
                checkTopLeft.Y + checkHeight < hitbox.Y + hitbox.Height - 16)
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + verticalAcceleration * 2f, 0.001f, 16f);
                return;
            }
            if (shouldRiseUpward && !Collision.SolidCollision(checkTopLeft, checkWidth, checkHeight - 4, acceptTopSurfaces))
            {
                npc.velocity.Y = 0f;
                return;
            }
            if (shouldRiseUpward)
            {
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - verticalAcceleration, -8f, 0f);
                return;
            }
            if (npc.velocity.Y == 0f && tileAhead)
            {
                npc.velocity.Y = -8f;
                return;
            }
            npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + verticalAcceleration, -8f, 16f);
        }

        public static void SelectNextAttack(NPC npc)
        {

        }

        #endregion AI

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Width = 180;
            npc.frame.Y = (int)npc.localAI[0];
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 drawPos = npc.Bottom - Main.screenPosition;
            Rectangle frame = texture.Frame(5, 5, npc.frame.Y / 5, npc.frame.Y % 5, 2, 2);
            Vector2 origin = frame.Size() * new Vector2(0.5f, 1f);
            origin.Y -= 4f;
            int horizontalOriginOffset = 106;
            SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (npc.spriteDirection == 1)
                origin.X = horizontalOriginOffset;
            else
                origin.X = frame.Width - horizontalOriginOffset;

            Color shadowColor = Color.White;
            float shadowFade = 0f;
            int backShadowAfterimageCount = 0;
            float shadowFadeInterpolant = npc.localAI[3] / 36f;
            float shadowOffset = 0f;
            Color color = lightColor;
            if (npc.localAI[3] > 0f)
            {
                backShadowAfterimageCount = 2;
                shadowOffset = shadowFadeInterpolant * shadowFadeInterpolant * 20f;
                shadowColor = new Color(80, 0, 0, 255) * 0.5f;
                shadowFade = 1f;
                color = Color.Lerp(Color.Transparent, color, 1f - shadowFadeInterpolant * shadowFadeInterpolant);
            }
            for (int i = 0; i < backShadowAfterimageCount; i++)
            {
                Color afterimageColor = Color.Lerp(npc.GetAlpha(lightColor), shadowColor, shadowFade);
                afterimageColor *= 1f - shadowFadeInterpolant * 0.5f;
                Vector2 afterimagePos = drawPos + Vector2.UnitY.RotatedBy(i * MathHelper.TwoPi / backShadowAfterimageCount + Main.GlobalTimeWrappedHourly * 10f) * shadowOffset;
                spriteBatch.Draw(texture, afterimagePos, frame, afterimageColor, npc.rotation, origin, npc.scale, direction, 0f);
            }
            Color baseColor = npc.GetAlpha(color);
            if (npc.localAI[3] > 0f)
                baseColor = Color.Lerp(baseColor, new(50, 0, 160), Utils.Remap(npc.localAI[3], 0f, 20f, 0f, 1f));

            spriteBatch.Draw(texture, drawPos, frame, baseColor, npc.rotation, origin, npc.scale, direction, 0f);
            if (npc.localAI[3] > 0f)
            {
                Texture2D shadowTexture = TextureAssets.Extra[245].Value;
                float scale = Utils.Remap(npc.localAI[3], 0f, 20f, 0f, 1f);
                Color color4 = new Color(255, 30, 30, 66) * npc.Opacity * scale * 0.4f;
                for (int j = 0; j < backShadowAfterimageCount; j++)
                {
                    Vector2 afterimagePos = drawPos + Vector2.UnitY.RotatedBy(j * MathHelper.TwoPi / backShadowAfterimageCount + Main.GlobalTimeWrappedHourly * 10f) * 2f;
                    spriteBatch.Draw(shadowTexture, afterimagePos, frame, color4, npc.rotation, origin, npc.scale, direction, 0f);
                }
            }
            return false;
        }
        #endregion Frames and Drawcode
    }
}