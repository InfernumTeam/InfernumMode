using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.Projectiles.Boss;
using InfernumMode.BossIntroScreens;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using DoGHead = CalamityMod.NPCs.DevourerofGods.DevourerofGodsHead;

namespace InfernumMode.BehaviorOverrides.BossAIs.DoG
{
    public class DoGPhase1HeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum Phase2TransitionState
        {
            NotEnteringPhase2,
            NeedsToSummonPortal,
            EnteringPortal
        }

        public static Phase2TransitionState CurrentPhase2TransitionState
        {
            get
            {
                if (CalamityGlobalNPC.DoGHead < 0)
                    return Phase2TransitionState.NotEnteringPhase2;

                NPC npc = Main.npc[CalamityGlobalNPC.DoGHead];
                return (Phase2TransitionState)npc.Infernum().ExtraAI[Phase2TransitionStateAIIndex];
            }
            set
            {
                if (CalamityGlobalNPC.DoGHead < 0)
                    return;

                NPC npc = Main.npc[CalamityGlobalNPC.DoGHead];
                npc.Infernum().ExtraAI[Phase2TransitionStateAIIndex] = (int)value;
                npc.netUpdate = true;
            }
        }

        public override int NPCOverrideType => ModContent.NPCType<DoGHead>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public const float Phase2LifeRatio = 0.8f;

        public const int PassiveMovementTimeP1 = 420;

        public const int AggressiveMovementTimeP1 = 600;

        public const int Phase2TransitionStateAIIndex = 10;

        public const int Phase2PortalProjectileIndexAIIndex = 11;

        public const int BodySegmentFadeTypeAIIndex = 37;

        #region AI
        public override bool PreAI(NPC npc)
        {
            // Disable secondary teleport effects.
            Main.player[npc.target].Calamity().normalityRelocator = false;
            Main.player[npc.target].Calamity().spectralVeil = false;

            ref float attackTimer = ref npc.Infernum().ExtraAI[5];
            ref float flyAcceleration = ref npc.Infernum().ExtraAI[6];
            ref float jawRotation = ref npc.Infernum().ExtraAI[7];
            ref float chompTime = ref npc.Infernum().ExtraAI[8];
            ref float portalIndex = ref npc.Infernum().ExtraAI[Phase2PortalProjectileIndexAIIndex];
            ref float phaseCycleTimer = ref npc.Infernum().ExtraAI[12];
            ref float passiveAttackDelay = ref npc.Infernum().ExtraAI[13];
            ref float uncoilTimer = ref npc.Infernum().ExtraAI[35];
            ref float segmentFadeType = ref npc.Infernum().ExtraAI[BodySegmentFadeTypeAIIndex];

            // Increment timers.
            attackTimer++;
            phaseCycleTimer++;
            passiveAttackDelay++;

            // Adjust scale.
            npc.scale = 1.2f;

            // Adjust DR and defense.
            npc.defense = 0;
            npc.Calamity().DR = 0f;
            npc.takenDamageMultiplier = 2f;

            // Declare this NPC as the occupant of the DoG whoAmI index.
            CalamityGlobalNPC.DoGHead = npc.whoAmI;

            // Stop rain.
            if (Main.raining)
                Main.raining = false;

            // Prevent the Godslayer Inferno and Whispering Death debuff from being a problem.
            if (Main.player[npc.target].HasBuff(ModContent.BuffType<GodSlayerInferno>()))
                Main.player[npc.target].ClearBuff(ModContent.BuffType<GodSlayerInferno>());
            if (Main.player[npc.target].HasBuff(ModContent.BuffType<WhisperingDeath>()))
                Main.player[npc.target].ClearBuff(ModContent.BuffType<WhisperingDeath>());

            if (DoGPhase2HeadBehaviorOverride.InPhase2)
            {
                npc.Calamity().CanHaveBossHealthBar = true;

                typeof(DoGHead).GetField("phase2Started", Utilities.UniversalBindingFlags)?.SetValue(npc.modNPC, true);
                typeof(DoGHead).GetField("Phase2Started", Utilities.UniversalBindingFlags)?.SetValue(npc.modNPC, true);

                return DoGPhase2HeadBehaviorOverride.Phase2AI(npc, ref phaseCycleTimer, ref passiveAttackDelay, ref portalIndex, ref segmentFadeType);
            }

            // Set music.
            npc.modNPC.music = (InfernumMode.CalamityMod as CalamityMod.CalamityMod).GetMusicFromMusicMod("DevourerOfGodsP1") ?? MusicID.LunarBoss;

            // Do through the portal once ready to enter the second phase.
            if (CurrentPhase2TransitionState != Phase2TransitionState.NotEnteringPhase2)
            {
                HandlePhase2TransitionEffect(npc, ref portalIndex);
                return false;
            }

            // Reset invulnerability and fade in.
            npc.dontTakeDamage = false;
            npc.Opacity = MathHelper.Lerp(npc.Opacity, 1f, 0.25f);

            // Light
            Lighting.AddLight((int)((npc.position.X + npc.width / 2) / 16f), (int)((npc.position.Y + npc.height / 2) / 16f), 0.2f, 0.05f, 0.2f);

            // Worm variable
            if (npc.ai[3] > 0f)
                npc.realLife = (int)npc.ai[3];

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Stay away from the target if the screen is being obstructed by the intro animation.
            if (IntroScreenManager.ScreenIsObstructed)
            {
                npc.dontTakeDamage = true;
                npc.Center = target.Center - Vector2.UnitY * 3000f;
                npc.netUpdate = true;
            }

            npc.damage = npc.dontTakeDamage ? 0 : 2500;

            // Spawn segments
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (npc.Infernum().ExtraAI[2] == 0f && npc.ai[0] == 0f)
                {
                    int previousSegment = npc.whoAmI;
                    for (int segmentSpawn = 0; segmentSpawn < 81; segmentSpawn++)
                    {
                        int segment;
                        if (segmentSpawn >= 0 && segmentSpawn < 80)
                            segment = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), InfernumMode.CalamityMod.NPCType("DevourerofGodsBody"), npc.whoAmI);
                        else
                            segment = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), InfernumMode.CalamityMod.NPCType("DevourerofGodsTail"), npc.whoAmI);

                        Main.npc[segment].realLife = npc.whoAmI;
                        Main.npc[segment].ai[2] = npc.whoAmI;
                        Main.npc[segment].ai[1] = previousSegment;
                        Main.npc[previousSegment].ai[0] = segment;
                        Main.npc[segment].Infernum().ExtraAI[34] = 80f - segmentSpawn;
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, segment, 0f, 0f, 0f, 0);
                        previousSegment = segment;
                    }
                    portalIndex = -1f;
                    npc.Infernum().ExtraAI[2] = 1f;
                }
            }

            // Chomping after attempting to eat the player.
            bool chomping = !npc.dontTakeDamage && DoGPhase2HeadBehaviorOverride.DoChomp(npc, ref chompTime, ref jawRotation);

            // Despawn if no valid target exists.
            if (target.dead || !target.active)
                DoGPhase2HeadBehaviorOverride.Despawn(npc);

            // Initially uncoil.
            else if (uncoilTimer < 45f)
            {
                uncoilTimer++;
                npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY * 27f, 0.125f);
            }
            else if (phaseCycleTimer % (PassiveMovementTimeP1 + AggressiveMovementTimeP1) < PassiveMovementTimeP1)
            {
                DoGPhase2HeadBehaviorOverride.DoPassiveFlyMovement(npc, ref jawRotation, ref chompTime);

                // Idly release laserbeams.
                if (phaseCycleTimer % 150f == 0f && passiveAttackDelay >= 300f)
                {
                    Main.PlaySound(SoundID.Item12, target.position);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            Vector2 spawnOffset = (MathHelper.TwoPi * i / 16f).ToRotationVector2() * 1650f + Main.rand.NextVector2Circular(130f, 130f);
                            Vector2 laserShootVelocity = spawnOffset.SafeNormalize(Vector2.UnitY) * -Main.rand.NextFloat(30f, 36f) + Main.rand.NextVector2Circular(3f, 3f);
                            Utilities.NewProjectileBetter(target.Center + spawnOffset, laserShootVelocity, ModContent.ProjectileType<DoGDeath>(), 415, 0f);
                        }
                    }
                }
            }
            else
            {
                bool dontChompYet = (phaseCycleTimer % (PassiveMovementTimeP1 + AggressiveMovementTimeP1)) - PassiveMovementTimeP1 < 90f;
                DoGPhase2HeadBehaviorOverride.DoAggressiveFlyMovement(npc, target, dontChompYet, chomping, ref jawRotation, ref chompTime, ref attackTimer, ref flyAcceleration);
            }

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            return false;
        }

        public static void HandlePhase2TransitionEffect(NPC npc, ref float portalIndex)
        {
            npc.Calamity().CanHaveBossHealthBar = false;
            npc.velocity = npc.velocity.ClampMagnitude(32f, 60f);
            npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(npc.velocity.Length(), 50f, 0.1f);
            npc.damage = 0;

            // Summon the portal and become fully opaque if the portal hasn't been created yet.
            if (CurrentPhase2TransitionState == Phase2TransitionState.NeedsToSummonPortal)
            {
                // Spawn the portal.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPosition = npc.Center + npc.velocity.SafeNormalize(Vector2.UnitX) * 1600f;
                    portalIndex = Projectile.NewProjectile(spawnPosition, Vector2.Zero, ModContent.ProjectileType<DoGChargeGate>(), 0, 0f);

                    Main.projectile[(int)portalIndex].localAI[0] = 1f;
                    Main.projectile[(int)portalIndex].localAI[1] = DoGPhase2IntroPortalGate.Phase2AnimationTime;
                }

                int headType = ModContent.NPCType<DoGHead>();
                int bodyType = ModContent.NPCType<DevourerofGodsBody>();
                int tailType = ModContent.NPCType<DevourerofGodsTail>();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && (Main.npc[i].type == headType || Main.npc[i].type == bodyType || Main.npc[i].type == tailType))
                    {
                        Main.npc[i].Opacity = 1f;
                        Main.npc[i].netUpdate = true;
                    }
                }

                npc.Opacity = 1f;
                CurrentPhase2TransitionState = Phase2TransitionState.EnteringPortal;
            }

            // Enter the portal if it's being touched.
            if (Main.projectile[(int)portalIndex].Hitbox.Intersects(npc.Hitbox))
                npc.alpha = Utils.Clamp(npc.alpha + 140, 0, 255);

            // Vanish if the target died in the middle of the transition.
            if (Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                npc.TargetClosest();
                if (Main.player[npc.target].dead || !Main.player[npc.target].active)
                    npc.active = false;
            }
        }

        #endregion AI

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            if (DoGPhase2HeadBehaviorOverride.InPhase2)
                return DoGPhase2HeadBehaviorOverride.PreDraw(npc, spriteBatch, lightColor);

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            float jawRotation = npc.Infernum().ExtraAI[7];

            Texture2D headTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1Head");
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 headTextureOrigin = headTexture.Size() * 0.5f;

            Texture2D jawTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1Jaw");
            Vector2 jawOrigin = jawTexture.Size() * 0.5f;

            for (int i = -1; i <= 1; i += 2)
            {
                float jawBaseOffset = 20f;
                SpriteEffects jawSpriteEffect = spriteEffects;
                if (i == 1)
                {
                    jawSpriteEffect |= SpriteEffects.FlipHorizontally;
                    jawBaseOffset *= -1f;
                }
                Vector2 jawPosition = drawPosition;
                jawPosition += Vector2.UnitX.RotatedBy(npc.rotation + jawRotation * i) * (18f + i * (34f + jawBaseOffset + (float)Math.Sin(jawRotation) * 20f));
                jawPosition -= Vector2.UnitY.RotatedBy(npc.rotation) * (16f + (float)Math.Sin(jawRotation) * 20f);
                spriteBatch.Draw(jawTexture, jawPosition, null, lightColor, npc.rotation + jawRotation * i, jawOrigin, npc.scale, jawSpriteEffect, 0f);
            }

            Rectangle headFrame = headTexture.Frame();
            spriteBatch.Draw(headTexture, drawPosition, headFrame, npc.GetAlpha(lightColor), npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);

            Texture2D glowmaskTexture = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/DoG/DoGP1HeadGlow");
            spriteBatch.Draw(glowmaskTexture, drawPosition, headFrame, Color.White, npc.rotation, headTextureOrigin, npc.scale, spriteEffects, 0f);
            return false;
        }
        #endregion Drawing
    }
}
