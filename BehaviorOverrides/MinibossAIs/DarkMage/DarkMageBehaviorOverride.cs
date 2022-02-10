using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.DarkMage
{
    public class DarkMageBehaviorOverride : NPCBehaviorOverride
    {
        public enum DarkMageAttackType
        {
            DarkMagicShots,
            SkeletonSummoning,
            RedirectingFlames,
            DarkMagicCircles
        }

        public override int NPCOverrideType => NPCID.DD2DarkMageT1;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        public static void TargetClosestDarkMage(NPC searcher, bool faceTarget = true)
        {
            NPCUtils.TargetSearchFlag targetFlags = NPCUtils.TargetSearchFlag.All;

            // If a player exists and is nearby, only attack players.
            if (Main.player[Player.FindClosest(searcher.Center, 1, 1)].WithinRange(searcher.Center, 1600f))
                targetFlags = NPCUtils.TargetSearchFlag.Players;

            var playerFilter = NPCUtils.SearchFilters.OnlyPlayersInCertainDistance(searcher.Center, 1600f);
            var npcFilter = new NPCUtils.SearchFilter<NPC>(NPCUtils.SearchFilters.OnlyCrystal);

            NPCUtils.TargetSearchResults searchResults = NPCUtils.SearchForTarget(searcher, targetFlags, playerFilter, npcFilter);
            if (searchResults.FoundTarget)
            {
                searcher.target = searchResults.NearestTargetIndex;
                searcher.targetRect = searchResults.NearestTargetHitbox;
                if (searcher.ShouldFaceTarget(ref searchResults, null) && faceTarget)
                {
                    searcher.FaceTarget();
                }
            }
        }

        public static void ClearPickoffNPCs()
        {
            int[] pickOffNPCs = new int[]
            {
                NPCID.DD2GoblinT1,
                NPCID.DD2GoblinT2,
                NPCID.DD2GoblinT3,
                NPCID.DD2GoblinBomberT1,
                NPCID.DD2GoblinBomberT2,
                NPCID.DD2GoblinBomberT3,
                NPCID.DD2WyvernT1,
                NPCID.DD2WyvernT2,
                NPCID.DD2WyvernT3,
                NPCID.DD2JavelinstT1,
                NPCID.DD2JavelinstT2,
                NPCID.DD2JavelinstT3,
                NPCID.DD2WitherBeastT2,
                NPCID.DD2WitherBeastT3,
                NPCID.DD2KoboldWalkerT2,
                NPCID.DD2KoboldWalkerT3,
                NPCID.DD2KoboldFlyerT2,
                NPCID.DD2KoboldFlyerT3,
                NPCID.DD2LightningBugT3,
            };
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && pickOffNPCs.Contains(Main.npc[i].type))
                {
                    if (Main.npc[i].Opacity > 0.8f)
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            Dust magic = Dust.NewDustDirect(Main.npc[i].position, Main.npc[i].width, Main.npc[i].height, 27);
                            magic.velocity.Y -= 3f;
                            magic.velocity *= Main.rand.NextFloat(1f, 1.25f);
                            magic.alpha = 128;
                            magic.noGravity = true;
                        }
                    }
                    Main.npc[i].active = false;
                }
            }
        }

        public override bool PreAI(NPC npc)
        {
            // Select a target.
            TargetClosestDarkMage(npc);
            NPCAimedTarget target = npc.GetTargetData();

            // Clear pickoff enemies.
            ClearPickoffNPCs();

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float currentFrame = ref npc.localAI[0];
            ref float fadeInTimer = ref npc.localAI[3];

            // Reset things.
            npc.dontTakeDamage = false;
            npc.noTileCollide = true;

            // Fade in after appearing from the portal.
            if (fadeInTimer < 60f)
            {
                npc.Opacity = Utils.InverseLerp(0f, 48f, fadeInTimer, true);
                npc.velocity = -Vector2.UnitY * npc.Opacity * 2f;
                npc.dontTakeDamage = npc.Opacity < 0.7f;

                // Create magic dust while fading.
                int dustCount = (int)MathHelper.Lerp(7f, 1f, npc.Opacity);
                for (int i = 0; i < dustCount; i++)
                {
                    if (!Main.rand.NextBool(3))
                        continue;

                    Dust magic = Dust.NewDustDirect(npc.position, npc.width, npc.height, 27, npc.velocity.X * 1f, 0f, 100, default, 1f);
                    magic.scale = 0.55f;
                    magic.fadeIn = 0.7f;
                    magic.velocity *= npc.Size.Length() / 400f;
                    magic.velocity += npc.velocity;
                }
                fadeInTimer++;
                return false;
            }

            // Rise if ground is close.
            float distanceToGround = MathHelper.Distance(Utilities.GetGroundPositionFrom(npc.Center).Y, npc.Center.Y);
            if (distanceToGround < 64f)
                npc.directionY = -1;

            // Descend if ground is far.
            if (distanceToGround > 176f)
                npc.directionY = 1;

            float verticalSpeed = npc.directionY == 1 ? 0.05f : -0.12f;
            npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + verticalSpeed, -2f, 2f);

            switch ((DarkMageAttackType)(int)attackState)
            {
                case DarkMageAttackType.DarkMagicShots:
                    DoBehavior_DarkMagicShots(npc, target, ref attackTimer, ref currentFrame);
                    break;
                case DarkMageAttackType.SkeletonSummoning:
                    DoBehavior_SkeletonSummoning(npc, target, ref attackTimer, ref currentFrame);
                    break;
                case DarkMageAttackType.RedirectingFlames:
                    DoBehavior_RedirectingFlames(npc, target, ref attackTimer, ref currentFrame);
                    break;
                case DarkMageAttackType.DarkMagicCircles:
                    DoBehavior_DarkMagicCircles(npc, target, ref attackTimer, ref currentFrame);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_DarkMagicShots(NPC npc, NPCAimedTarget target, ref float attackTimer, ref float currentFrame)
        {
            int moveTime = 90;
            int chargeShootTime = 42;
            int totalShots = 5;
            int shootRate = 4;
            int shootTime = totalShots * shootRate;
            int shootCount = 3;
            ref float shootCounter = ref npc.Infernum().ExtraAI[0];

            if (shootCounter > 0f)
                moveTime /= 2;

            if (attackTimer < moveTime)
            {
                currentFrame = MathHelper.Lerp(0f, 4f, attackTimer / 32f % 1f);

                // Attempt to move horizontally towards the target.
                if (MathHelper.Distance(npc.Center.X, target.Center.X) < 150f)
                    npc.velocity.X *= 0.98f;
                else
                    npc.velocity.X = MathHelper.Lerp(npc.velocity.X, npc.SafeDirectionTo(target.Center).X * 7f, 0.05f);

                // Decide the direction.
                if (Math.Abs(npc.velocity.X) > 0.2f)
                    npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
            }
            else if (attackTimer < moveTime + chargeShootTime)
            {
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                currentFrame = MathHelper.Lerp(5f, 7f, (attackTimer - moveTime) / (chargeShootTime / 2f) % 1f);
                npc.velocity *= 0.97f;
            }

            // Release energy bolts.
            else
            {
                npc.velocity *= 0.5f;
                currentFrame = MathHelper.Lerp(8f, 12f, Utils.InverseLerp(moveTime + chargeShootTime, moveTime + chargeShootTime + shootTime, attackTimer, true));
                if (Main.netMode != NetmodeID.MultiplayerClient && (attackTimer - moveTime - chargeShootTime) % shootRate == shootRate - 1f)
                {
                    float offsetAngle = MathHelper.Lerp(-0.48f, 0.48f, Utils.InverseLerp(0f, shootTime, attackTimer - moveTime - chargeShootTime, true));
                    Vector2 spawnPosition = npc.Center + new Vector2(npc.direction * 10f, -16f);
                    Vector2 shootVelocity = (target.Center - spawnPosition + target.Velocity * 10f).SafeNormalize(Vector2.UnitY).RotatedBy(offsetAngle) * 16f;
                    Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ProjectileID.DD2DarkMageBolt, 130, 0f, Main.myPlayer, 0f, 0f);

                    // Decide the direction.
                    npc.spriteDirection = (shootVelocity.X > 0f).ToDirectionInt();
                    npc.netUpdate = true;
                }
            }

            if (attackTimer >= moveTime + chargeShootTime + shootTime)
            {
                if (shootCounter >= shootCount)
                {
                    shootCounter = 0f;
                    switch (Main.rand.Next(2))
                    {
                        case 0:
                            npc.ai[0] = (int)DarkMageAttackType.DarkMagicCircles;
                            break;
                        case 1:
                            npc.ai[0] = (int)DarkMageAttackType.RedirectingFlames;
                            break;
                        case 2:
                            npc.ai[0] = (int)DarkMageAttackType.SkeletonSummoning;
                            break;
                    }
                    npc.netUpdate = true;
                }
                else
                    shootCounter++;
                attackTimer = 0f;
            }

            npc.rotation = npc.velocity.X * 0.04f;
        }

        public static void DoBehavior_SkeletonSummoning(NPC npc, NPCAimedTarget target, ref float attackTimer, ref float currentFrame)
        {
            int castTime = 32;
            int summonTime = 85;

            npc.velocity *= 0.96f;
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            if (attackTimer < castTime)
                currentFrame = MathHelper.Lerp(29f, 27f, (float)Math.Sin(attackTimer / castTime * MathHelper.TwoPi) * 0.5f + 0.5f);
            else if (attackTimer < castTime + summonTime)
                currentFrame = MathHelper.Lerp(30f, 40f, (attackTimer - castTime) / summonTime);

            // Summon skeletons.
            if (attackTimer == castTime + 4f)
            {
                Projectile.NewProjectile(npc.Center + new Vector2(npc.direction * 24f, -40f), Vector2.Zero, ProjectileID.DD2DarkMageRaise, 0, 0f);
                DD2Event.RaiseGoblins(npc.Center);
            }

            if (attackTimer >= castTime + summonTime)
            {
                npc.ai[0] = (int)DarkMageAttackType.DarkMagicShots;
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_RedirectingFlames(NPC npc, NPCAimedTarget target, ref float attackTimer, ref float currentFrame)
        {
            int castTime = 32;
            int shootTime = 85;
            int shootRate = 10;
            int sitTime = 120;

            npc.velocity *= 0.96f;
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            if (attackTimer < castTime)
                currentFrame = MathHelper.Lerp(29f, 27f, (float)Math.Sin(attackTimer / castTime * MathHelper.TwoPi) * 0.5f + 0.5f);
            else if (attackTimer < castTime + shootTime)
                currentFrame = MathHelper.Lerp(30f, 40f, (attackTimer - castTime) / shootTime);
            else
                currentFrame = MathHelper.Lerp(0f, 4f, attackTimer / 32f % 1f);

            // Release flames upwards. They will redirect and accelerate towards targets after a short period of time.
            if (attackTimer >= castTime && attackTimer < castTime + shootTime && attackTimer % shootRate == shootRate - 1f)
            {
                Vector2 spawnPosition = npc.Center + new Vector2(npc.direction * 10f, -16f);
                Main.PlaySound(SoundID.Item73, target.Center);
                for (int i = 0; i < 4; i++)
                {
                    Dust fire = Dust.NewDustPerfect(spawnPosition + Main.rand.NextVector2Circular(5f, 5f), 267);
                    fire.velocity -= Vector2.UnitY.RotatedByRandom(0.23f) * Main.rand.NextFloat(1f, 2.5f);
                    fire.color = Color.Lerp(Color.OrangeRed, Color.Purple, Main.rand.NextFloat(0.7f));
                    fire.noGravity = true;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 flameShootVelocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitX * npc.spriteDirection);
                    flameShootVelocity = Vector2.Lerp(flameShootVelocity, -Vector2.UnitY.RotatedByRandom(0.92f), 0.7f) * 13f;
                    Utilities.NewProjectileBetter(spawnPosition, flameShootVelocity, ModContent.ProjectileType<RedirectingWeakDarkMagicFlame>(), 120, 0f);
                }
            }

            if (attackTimer >= castTime + shootTime + sitTime)
            {
                npc.ai[0] = (int)DarkMageAttackType.DarkMagicShots;
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_DarkMagicCircles(NPC npc, NPCAimedTarget target, ref float attackTimer, ref float currentFrame)
        {
            int castTime = 32;
            int shootTime = 180;
            int shootRate = 30;
            int sitTime = 60;

            npc.velocity *= 0.96f;
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            // Play a cast sound.
            if (attackTimer == castTime)
                Main.PlaySound(SoundID.DD2_DarkMageCastHeal, npc.Center);

            if (attackTimer < castTime)
                currentFrame = MathHelper.Lerp(29f, 27f, (float)Math.Sin(attackTimer / castTime * MathHelper.TwoPi) * 0.5f + 0.5f);
            else if (attackTimer < castTime + shootTime)
                currentFrame = MathHelper.Lerp(30f, 40f, (attackTimer - castTime) / (shootTime / 3f) % 1f);
            else
                currentFrame = MathHelper.Lerp(0f, 4f, attackTimer / 32f % 1f);

            // Release dark magic circles towards the target.
            if (attackTimer >= castTime && attackTimer < castTime + shootTime && attackTimer % shootRate == shootRate - 1f)
            {
                Vector2 spawnPosition = npc.Center + new Vector2(npc.direction * 10f, -16f);

                if (Main.netMode != NetmodeID.Server)
                {
                    var sound = Main.PlaySound(SoundID.DD2_DarkMageHealImpact, target.Center);
                    sound.Volume = MathHelper.Clamp(sound.Volume * 1.6f, 0f, 1f);
                }
                for (int i = 0; i < 20; i++)
                {
                    Dust fire = Dust.NewDustPerfect(spawnPosition + Main.rand.NextVector2Circular(5f, 5f), 267);
                    fire.velocity -= Vector2.UnitY.RotatedByRandom(0.23f) * Main.rand.NextFloat(1f, 2.5f);
                    fire.color = Color.Lerp(Color.OrangeRed, Color.Purple, Main.rand.NextFloat(0.7f));
                    fire.noGravity = true;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 flameShootVelocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitX * npc.spriteDirection) * 15f;
                    Utilities.NewProjectileBetter(spawnPosition, flameShootVelocity, ModContent.ProjectileType<DarkMagicCircle>(), 175, 0f);
                }
            }

            if (attackTimer >= castTime + shootTime + sitTime)
            {
                npc.ai[0] = (int)DarkMageAttackType.DarkMagicShots;
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Width = 80;
            npc.frame.Height = 80;
            npc.frame.Y = (int)Math.Round(npc.localAI[0]);
        }
    }
}
