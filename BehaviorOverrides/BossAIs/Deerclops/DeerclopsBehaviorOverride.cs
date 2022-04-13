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
using System.Linq;

namespace InfernumMode.BehaviorOverrides.BossAIs.Deerclops
{
    public class DeerclopsBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Deerclops;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        #region Enumerations
        public enum DeerclopsAttackType
        {
            ShatteredIceRain,
            GroundLaser,
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            npc.TargetClosestIfTargetIsInvalid();

            Player target = Main.player[npc.target];

            // Naturally use the shadow effect if the target is too far away.
            // This may be overridden in specific behaviors below, however.
            bool shouldUseShadowFade = !target.WithinRange(npc.Center, 450f);
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float frame = ref npc.localAI[0];
            ref float shadowFadeTimer = ref npc.localAI[3];
            
            switch ((DeerclopsAttackType)attackType)
            {
                case DeerclopsAttackType.ShatteredIceRain:
                    DoBehavior_ShatteredIceRain(npc, target, ref frame, ref attackTimer);
                    break;
                case DeerclopsAttackType.GroundLaser:
                    DoBehavior_GroundLaser(npc, target, ref shouldUseShadowFade, ref frame, ref attackTimer);
                    break;
            }

            shadowFadeTimer = Utils.Clamp(shadowFadeTimer + shouldUseShadowFade.ToDirectionInt(), 0f, 36f);
            attackTimer++;
            return false;
        }

        public static void DoBehavior_ShatteredIceRain(NPC npc, Player target, ref float frame, ref float attackTimer)
        {
            int screamTime = 30;
            int walkDelay = 64;
            int walkTime = 120;
            bool shouldWalk = attackTimer >= walkDelay;

            // Scream and release ice crystals into the sky once ready.
            // The crystals are shot in such a way that they do not fall near Deerclops, encouraging the player to stay near him.
            if (attackTimer == screamTime)
            {
                SoundEngine.PlaySound(SoundID.DeerclopsScream, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (float dx = -1100f; dx < 1100f; dx += Main.rand.NextFloat(90f, 110f))
                    {
                        if (Math.Abs(dx) < 200f)
                            continue;

                        Vector2 icicleSpawnPosition = Utilities.GetGroundPositionFrom(new Vector2(npc.Center.X, target.Center.Y) + Vector2.UnitX * dx);
                        Vector2 icicleVelocity = -Vector2.UnitY.RotatedByRandom(0.11f) * Main.rand.NextFloat(20f, 32f);
                        Utilities.NewProjectileBetter(icicleSpawnPosition, icicleVelocity, ModContent.ProjectileType<DeerclopsIcicle>(), 95, 0f);
                    }
                }
            }

            // Handle movement.
            DefaultMovement(npc, target, 7.5f, !shouldWalk);

            // Decide frames.
            if (shouldWalk)
            {
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.frameCounter += Math.Abs(npc.velocity.X) * 0.2f + 0.4f;
                if (npc.frameCounter >= 6f)
                {
                    frame++;
                    npc.frameCounter = 0;
                }

                if (frame > 11f)
                {
                    frame = 0f;
                    npc.frameCounter = 0;
                }
            }
            else
            {
                npc.spriteDirection = 1;
                if (frame is < 19 or > 24)
                    npc.frameCounter = 0.0;
                npc.frameCounter++;
                frame = ScreamFrames[Math.Min((int)npc.frameCounter / 4, ScreamFrames.Length - 1)];
            }

            if (attackTimer >= screamTime + walkDelay + walkTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_GroundLaser(NPC npc, Player target, ref bool shouldUseShadowFade, ref float frame, ref float attackTimer)
        {
            int laserChargeupTime = 84;
            int laserShootTime = DeerclopsEyeLaserbeam.LaserLifetime;
            int attackTransitionDelay = 40;
            ref float laserRayInterpolant = ref npc.Infernum().ExtraAI[0];
            ref float laserRayDirection = ref npc.Infernum().ExtraAI[1];

            // Slow down and use the shadow fade effect.
            DefaultMovement(npc, target, 0f, true);
            shouldUseShadowFade = true;

            // Look at the target.
            if (attackTimer < laserChargeupTime - 30)
            {
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                laserRayInterpolant = Utils.GetLerpValue(0f, 32f, attackTimer, true);
                laserRayDirection = MathHelper.PiOver2 - npc.spriteDirection * 0.45f;
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

            // Create the funny laser.
            if (attackTimer == laserChargeupTime)
            {
                SoundEngine.PlaySound(SoundID.DeerclopsScream, npc.Center);
                SoundEngine.PlaySound(SoundID.DD2_BetsyFireballShot, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    laserRayInterpolant = 0f;
                    int laser = Utilities.NewProjectileBetter(GetEyePosition(npc), Vector2.UnitY, ModContent.ProjectileType<DeerclopsEyeLaserbeam>(), 160, 0f);
                    if (Main.projectile.IndexInRange(laser))
                        Main.projectile[laser].ai[0] = npc.whoAmI;
                    npc.netUpdate = true;
                }
            }

            // Create burst particles and move the laser.
            if (attackTimer > laserChargeupTime)
            {
                laserRayDirection += npc.spriteDirection * -0.0172f;
                float particleScale = Main.rand.NextFloat(0.55f, 0.8f);
                Vector2 particleSpawnPosition = GetEyePosition(npc) + Main.rand.NextVector2Circular(15f, 15f);
                Vector2 particleVelocity = laserRayDirection.ToRotationVector2().RotatedByRandom(0.45f) * Main.rand.NextFloat(2f, 8f);
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(particleSpawnPosition, particleVelocity, particleScale, Color.Red, 40));
            }

            // Decide frames.
            if (attackTimer < laserChargeupTime / 2)
                frame = Utils.Remap(attackTimer % 60f, 0f, 60f, 0f, 11f);
            else
                frame = Utils.Remap(attackTimer - laserChargeupTime / 2f, 0f, 32f, 12f, 14f);

            if (attackTimer >= laserChargeupTime + laserShootTime + attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static Vector2 GetEyePosition(NPC npc)
        {
            Vector2 offset = new(npc.spriteDirection * 46f, -72f);
            switch (npc.frame.Y)
            {
                case 4:
                    offset.X += npc.spriteDirection * 4f;
                    offset.Y += 26f;
                    break;
                case 6:
                    offset.Y += 10f;
                    break;
                case 12:
                    offset.X -= npc.spriteDirection * 14f;
                    offset.Y += 14f;
                    break;
                case 13:
                    offset.X -= npc.spriteDirection * 28f;
                    offset.Y -= 6f;
                    break;
                case 14:
                    offset.X -= npc.spriteDirection * 28f;
                    offset.Y -= 6f;
                    break;
            }

            return npc.Center + offset;
        }

        public static void DefaultMovement(NPC npc, Player target, float walkSpeed, bool haltMovement)
        {
            Rectangle hitbox = target.Hitbox;
            float verticalAcceleration = 0.32f;
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
            bool shouldJump = Collision.CanHit(npc, target) && target.Center.Y - npc.Center.Y < -320f;
            if (npc.velocity.Y == 0f && shouldJump && !haltMovement)
            {
                SoundEngine.PlaySound(SoundID.Item38, npc.Center);
                npc.velocity.Y = -20f;
                return;
            }
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
                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y - verticalAcceleration, -16f, 0f);
                return;
            }
            if (npc.velocity.Y == 0f && (tileAhead || shouldJump))
            {
                npc.velocity.Y = -8f;
                return;
            }
            npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + verticalAcceleration, -16f, 16f);
        }

        public static void SelectNextAttack(NPC npc)
        {
            switch ((DeerclopsAttackType)npc.ai[0])
			{
                case DeerclopsAttackType.ShatteredIceRain:
                    npc.ai[0] = (int)DeerclopsAttackType.GroundLaser;
                    break;
                case DeerclopsAttackType.GroundLaser:
                    npc.ai[0] = (int)DeerclopsAttackType.ShatteredIceRain;
                    break;
            }
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        #endregion AI

        #region Frames and Drawcode

        public static readonly int[] ScreamFrames = new[]
        {
            19,
            20,
            21,
            22,
            21,
            22,
            21,
            22,
            23,
            24,
            23,
            24,
            23,
            24,
            20,
            19
        };

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

            // Draw a laser telegraph from the eye when preparing the laser.
            if (npc.ai[0] == (int)DeerclopsAttackType.GroundLaser)
            {
                float laserRayInterpolant = npc.Infernum().ExtraAI[0];
                float laserRayDirection = npc.Infernum().ExtraAI[1];

                if (laserRayInterpolant > 0f)
                {
                    float[] samples = new float[8];
                    Vector2 start = GetEyePosition(npc);
                    Collision.LaserScan(start, laserRayDirection.ToRotationVector2(), 8f, DeerclopsEyeLaserbeam.MaxLaserLength, samples);

                    Vector2 end = start + laserRayDirection.ToRotationVector2() * samples.Average();
                    spriteBatch.DrawLineBetter(start, end, Color.Red * (float)Math.Pow(laserRayInterpolant, 0.36f) * 0.6f, 3f);
                }
            }
            return false;
        }
        #endregion Frames and Drawcode
    }
}