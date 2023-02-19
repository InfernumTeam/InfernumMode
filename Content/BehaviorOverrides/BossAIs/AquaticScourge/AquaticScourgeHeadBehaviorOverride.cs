using CalamityMod;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.NPCs.DesertScourge;
using InfernumMode.Assets.Sounds;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class AquaticScourgeHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum AquaticScourgeAttackType
        {
            BubbleSpin,
            RadiationPulse,
            WallHitCharges,
        }

        public override int NPCOverrideType => ModContent.NPCType<AquaticScourgeHead>();

        #region AI
        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio,
            Phase4LifeRatio
        };

        public const float Phase2LifeRatio = 0.75f;

        public const float Phase3LifeRatio = 0.45f;

        public const float Phase4LifeRatio = 0.15f;

        public static float PoisonChargeUpSpeedFactor => 0.333f;

        public static float PoisonFadeOutSpeedFactor => 2.5f;

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            // Fade in.
            npc.alpha = Utils.Clamp(npc.alpha - 20, 0, 255);

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float generalTimer = ref npc.ai[1];
            ref float attackType = ref npc.ai[2];
            ref float attackTimer = ref npc.ai[3];
            ref float attackDelay = ref npc.Infernum().ExtraAI[5];
            ref float initializedFlag = ref npc.Infernum().ExtraAI[6];

            if (Main.netMode != NetmodeID.MultiplayerClient && initializedFlag == 0f)
            {
                CreateSegments(npc, 32, ModContent.NPCType<AquaticScourgeBody>(), ModContent.NPCType<AquaticScourgeTail>());
                initializedFlag = 1f;
                npc.netUpdate = true;
            }

            // Determine hostility.
            CalamityMod.CalamityMod.bossKillTimes.TryGetValue(npc.type, out int revKillTime);
            npc.Calamity().KillTime = revKillTime;
            npc.damage = npc.defDamage;
            npc.boss = true;
            npc.netUpdate = true;
            npc.chaseable = true;
            npc.Calamity().newAI[0] = 1f;

            // If there still was no valid target, swim away.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                DoBehavior_Despawn(npc);
                return false;
            }

            Player target = Main.player[npc.target];

            // Disable obnoxious water mechanics so that the player can fight the boss without interruption.
            if (!target.Calamity().ZoneAbyss)
            {
                target.breath = target.breathMax;
                target.ignoreWater = true;
                target.wingTime = target.wingTimeMax;
            }

            switch ((AquaticScourgeAttackType)attackType)
            {
                case AquaticScourgeAttackType.BubbleSpin:
                    DoBehavior_BubbleSpin(npc, target, ref attackTimer);
                    break;
                case AquaticScourgeAttackType.RadiationPulse:
                    DoBehavior_RadiationPulse(npc, target, ref attackTimer);
                    break;
                case AquaticScourgeAttackType.WallHitCharges:
                    DoBehavior_WallHitCharges(npc, target, ref attackTimer);
                    break;
            }

            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            attackTimer++;
            generalTimer++;

            return false;
        }
        #endregion AI

        #region Specific Behaviors

        public static void DoBehavior_Despawn(NPC npc)
        {
            npc.velocity.X *= 0.985f;
            if (npc.velocity.Y < 24f)
                npc.velocity.Y += 0.32f;

            if (npc.timeLeft > 210)
                npc.timeLeft = 210;

            Player closestTarget = Main.player[Player.FindClosest(npc.Center, 1, 1)];
            if (!npc.WithinRange(closestTarget.Center, 3200f))
                npc.active = false;
        }

        public static void DoBehavior_BubbleSpin(NPC npc, Player target, ref float attackTimer)
        {
            int redirectTime = 35;
            int spinTime = 270;
            int bubbleReleaseRate = 32;
            int chargeRedirectTime = 16;
            int chargeTime = 56;
            float spinSpeed = 23f;
            float chargeSpeed = 28.5f;
            float spinArc = MathHelper.Pi / spinTime * 3f;
            bool charging = attackTimer >= redirectTime + spinTime + chargeRedirectTime;
            bool doneCharging = attackTimer >= redirectTime + spinTime + chargeRedirectTime + chargeTime;
            ref float bubbleReleaseCount = ref npc.Infernum().ExtraAI[0];

            // Don't do damage if not charging.
            if (!charging)
                npc.damage = 0;

            // Approach the target before spinning.
            if (attackTimer < redirectTime)
            {
                float flySpeed = Utils.Remap(attackTimer, 16f, redirectTime - 8f, 9f, spinSpeed);
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * flySpeed, 0.1f);
                return;
            }

            // Spin in place.
            if (attackTimer < redirectTime + spinTime)
            {
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedBy(spinArc) * spinSpeed;
                if (!npc.WithinRange(target.Center, 600f))
                    npc.Center = npc.Center.MoveTowards(target.Center, 8f);
            }

            // Release bubbles at the player.
            if (attackTimer % bubbleReleaseRate == bubbleReleaseRate - 1f && attackTimer < redirectTime + spinTime)
            {
                SoundEngine.PlaySound(SoundID.Item95, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int bubbleDamage = 135;
                    int bubbleID = ModContent.ProjectileType<AcidBubble>();
                    Vector2 bubbleShootVelocity = npc.SafeDirectionTo(target.Center) * 13f;
                    if (bubbleReleaseCount % 8f == 3f)
                    {
                        bubbleDamage = 0;
                        bubbleID = ModContent.ProjectileType<WaterClearingBubble>();
                        bubbleShootVelocity *= 0.35f;
                    }

                    Utilities.NewProjectileBetter(npc.Center, bubbleShootVelocity, bubbleID, bubbleDamage, 0f);
                    bubbleReleaseCount++;
                    npc.netUpdate = true;
                }
            }

            // Redirect for a charge towards the target.
            if (attackTimer >= redirectTime + spinTime && !charging)
            {
                // Roar and pop all bubbles before the redirecting begins.
                if (attackTimer == redirectTime + spinTime)
                {
                    SoundEngine.PlaySound(DesertScourgeHead.RoarSound, target.Center);
                    PopAllBubbles();
                }

                Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                npc.velocity = Vector2.Lerp(npc.velocity, chargeVelocity, 0.08f);

                if (attackTimer == redirectTime + spinTime + chargeRedirectTime - 1f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.AquaticScourgeCharge, target.Center);
                    npc.velocity = chargeVelocity;
                    npc.netUpdate = true;
                }
            }
            
            // Emit acid mist while charging.
            if (Main.netMode != NetmodeID.MultiplayerClient && charging && attackTimer % 2f == 0f)
            {
                Vector2 gasVelocity = npc.velocity.SafeNormalize(Main.rand.NextVector2Unit()).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(2f, 6f);
                Utilities.NewProjectileBetter(npc.Center, -gasVelocity.RotatedByRandom(0.3f), ModContent.ProjectileType<SulphuricGas>(), 135, 0f);
                Utilities.NewProjectileBetter(npc.Center, gasVelocity.RotatedByRandom(0.3f), ModContent.ProjectileType<SulphuricGas>(), 135, 0f);
            }

            if (doneCharging)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_RadiationPulse(NPC npc, Player target, ref float attackTimer)
        {
            int shootDelay = 90;
            int pulseReleaseRate = 120;
            int acidReleaseRate = 60;
            int shootTime = 480;
            int goodBubbleReleaseRate = 180;
            int acidShootCount = 4;
            float pulseMaxRadius = 425f;

            // Slowly move towards the target.
            Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * 5f;
            if (npc.WithinRange(target.Center, 200f))
                npc.velocity = (npc.velocity * 1.01f).ClampMagnitude(0f, idealVelocity.Length() * 1.5f);
            else
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.125f);

            if (attackTimer < shootDelay)
                return;

            // Release radiation pulses.
            if (attackTimer % pulseReleaseRate == pulseReleaseRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<RadiationPulse>(), 0, 0f, -1, 0f, pulseMaxRadius);
            }

            // Release acid.
            if (attackTimer % acidReleaseRate == acidReleaseRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath13, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < acidShootCount; i++)
                    {
                        float acidInterpolant = i / (float)(acidShootCount - 1f);
                        float angularVelocity = MathHelper.Lerp(0.016f, -0.016f, acidInterpolant);
                        Vector2 acidShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.Lerp(-1.09f, 1.09f, acidInterpolant)) * 5f;
                        Utilities.NewProjectileBetter(npc.Center + acidShootVelocity * 5f, acidShootVelocity, ModContent.ProjectileType<AcceleratingArcingAcid>(), 135, 0f, -1, 0f, angularVelocity);
                    }
                }
            }

            // Release safe bubbles from below occasionally.
            if (attackTimer % goodBubbleReleaseRate == goodBubbleReleaseRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item95, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(target.Center + Vector2.UnitY * 450f, -Vector2.UnitY * 5f, ModContent.ProjectileType<WaterClearingBubble>(), 0, 0f);
            }

            if (attackTimer >= shootDelay + shootTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_WallHitCharges(NPC npc, Player target, ref float attackTimer)
        {
            int chargeCount = 5;
            int chargeDelay = 30;
            int minChargeTime = 36;
            int maxChargeTime = 67;
            int stunTime = 60;
            int rubbleCount = 9;
            bool insideBlocks = Collision.SolidCollision(npc.TopLeft, npc.width, npc.height);
            float chargeSpeed = 25f;
            float rubbleArc = 1.17f;
            float rubbleShootSpeed = 5f;
            float bubbleSpacing = 384f;
            float bubbleAreaCoverage = 1500f;
            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];
            ref float performingCharge = ref npc.Infernum().ExtraAI[1];
            ref float stunTimer = ref npc.Infernum().ExtraAI[2];
            ref float dontInteractWithBlocksYet = ref npc.Infernum().ExtraAI[3];

            if (chargeCounter <= 0f)
                chargeDelay += 60;

            // Attempt to move towards the target before charging.
            if (attackTimer <= chargeDelay)
            {
                float slowdownInterpolant = (float)Math.Pow(attackTimer / chargeDelay, 2D);
                Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * slowdownInterpolant * chargeSpeed;
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.08f);
            }

            // Do the charge.
            if (attackTimer == chargeDelay)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.AquaticScourgeCharge, target.Center);
                npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                npc.netUpdate = true;

                performingCharge = 1f;
                dontInteractWithBlocksYet = insideBlocks.ToInt();
            }

            // Handle post-stun behaviors.
            if (stunTimer >= 1f)
            {
                stunTimer--;
                if (stunTimer <= 0f)
                {
                    attackTimer = 0f;
                    dontInteractWithBlocksYet = 0f;
                    chargeCounter++;
                    if (chargeCounter >= chargeCount)
                        SelectNextAttack(npc);

                    // Release a single clean bubble on the third charge.
                    if (Main.netMode != NetmodeID.MultiplayerClient && chargeCounter == 3f)
                        Utilities.NewProjectileBetter(target.Center + Vector2.UnitY * 650f, -Vector2.UnitY * 5f, ModContent.ProjectileType<WaterClearingBubble>(), 0, 0f);

                    npc.netUpdate = true;
                }
            }

            if (performingCharge == 1f)
            {
                // If the scourge started in blocks when charging but has now left them, allow it to rebound.
                if (dontInteractWithBlocksYet == 1f && !insideBlocks)
                {
                    dontInteractWithBlocksYet = 0f;
                    npc.netUpdate = true;
                }

                // Perform rebound effects when tiles are hit. This takes a small amount of time before it can happen, so that charges aren't immediate.
                if (attackTimer >= minChargeTime && dontInteractWithBlocksYet == 0f && insideBlocks && npc.WithinRange(target.Center, 1200f))
                {
                    performingCharge = 0f;
                    stunTimer = stunTime;

                    // Create tile hit dust effects.
                    Collision.HitTiles(npc.TopLeft, -npc.velocity, npc.width, npc.height);

                    // Create rubble that aims backwards and some bubbles from below.
                    SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < rubbleCount; i++)
                        {
                            float rubbleOffsetAngle = MathHelper.Lerp(-rubbleArc, rubbleArc, i / (float)(rubbleCount - 1f)) + Main.rand.NextFloatDirection() * 0.05f;
                            Vector2 rubbleVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(rubbleOffsetAngle) * rubbleShootSpeed;
                            Utilities.NewProjectileBetter(npc.Center + rubbleVelocity * 3f, rubbleVelocity, ModContent.ProjectileType<SulphurousRockRubble>(), 135, 0f);
                        }
                        
                        for (float dx = -bubbleAreaCoverage; dx < bubbleAreaCoverage; dx += bubbleSpacing)
                        {
                            float bubbleSpeed = Main.rand.NextFloat(7f, 9f);
                            float verticalOffset = Math.Max(target.velocity.Y * 30f, 0f) + 600f;
                            Utilities.NewProjectileBetter(target.Center + new Vector2(dx, verticalOffset), -Vector2.UnitY * bubbleSpeed, ModContent.ProjectileType<AcidBubble>(), 135, 0f);
                        }
                    }

                    npc.velocity = npc.velocity.RotatedByRandom(0.32f) * -0.24f;
                    npc.netUpdate = true;
                }

                if (attackTimer >= chargeDelay + maxChargeTime)
                {
                    performingCharge = 0f;
                    stunTimer = 2f;
                    npc.velocity *= 0.5f;
                    npc.netUpdate = true;
                }

                // Emit acid mist while charging and not inside of tiles.
                if (Main.netMode != NetmodeID.MultiplayerClient && !insideBlocks && attackTimer % 5f == 0f)
                {
                    Vector2 gasVelocity = npc.velocity.SafeNormalize(Main.rand.NextVector2Unit()).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(0.7f, 3f);
                    Utilities.NewProjectileBetter(npc.Center, -gasVelocity.RotatedByRandom(0.25f), ModContent.ProjectileType<SulphuricGas>(), 135, 0f);
                    Utilities.NewProjectileBetter(npc.Center, gasVelocity.RotatedByRandom(0.25f), ModContent.ProjectileType<SulphuricGas>(), 135, 0f);
                }
            }
        }

        #endregion Specific Behaviors

        #region AI Utility Methods
        public static void CreateSegments(NPC npc, int wormLength, int bodyType, int tailType)
        {
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < wormLength; i++)
            {
                int nextIndex;
                if (i < wormLength - 1)
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, bodyType, npc.whoAmI + 1);
                else
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI + 1);

                Main.npc[nextIndex].realLife = npc.whoAmI;
                Main.npc[nextIndex].ai[2] = npc.whoAmI;
                Main.npc[nextIndex].ai[1] = previousIndex;
                Main.npc[previousIndex].ai[0] = nextIndex;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.Opacity = 1f;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            AquaticScourgeAttackType currentAttack = (AquaticScourgeAttackType)(int)npc.ai[2];
            AquaticScourgeAttackType nextAttack = AquaticScourgeAttackType.WallHitCharges;

            // Get a new target.
            npc.TargetClosest();

            npc.ai[2] = (int)nextAttack;
            npc.ai[3] = 0f;

            // Set an 2 second delay up after the attack.
            npc.Infernum().ExtraAI[5] = 120f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.noTileCollide = true;
            npc.netUpdate = true;
        }

        public static void PopAllBubbles()
        {
            List<int> bubbles = new()
            {
                ModContent.ProjectileType<AcidBubble>(),
                ModContent.ProjectileType<WaterClearingBubble>()
            };

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || !bubbles.Contains(p.type))
                    continue;
                
                for (int j = 0; j < 45; j++)
                {
                    Dust bubble = Dust.NewDustPerfect(p.Center + Main.rand.NextVector2Circular(32f, 32f), 256);
                    bubble.velocity = (MathHelper.TwoPi * i / 45f + Main.rand.NextFloatDirection() * 0.1f).ToRotationVector2() * Main.rand.NextFloat(1f, 16f);
                    bubble.scale = Main.rand.NextFloat(1f, 1.5f);
                    bubble.noGravity = true;
                }
                p.timeLeft = Main.rand.Next(10, 20);
            }
        }

        #endregion AI Utility Methods
    }
}
