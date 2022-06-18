using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.ModLoader;

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

        public override bool PreAI(NPC npc) => DoAI(npc);

        public static bool DoAI(NPC npc)
        {
            // Select a target.
            OldOnesArmyMinibossChanges.TargetClosestMiniboss(npc);
            NPCAimedTarget target = npc.GetTargetData();

            bool isBuffed = npc.type == NPCID.DD2DarkMageT3;
            bool wasSpawnedInValidContext = npc.Infernum().ExtraAI[5] == 1f || !DD2Event.Ongoing;
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float currentFrame = ref npc.localAI[0];
            ref float fadeInTimer = ref npc.localAI[3];

            // Reset things.
            npc.dontTakeDamage = false;
            npc.noTileCollide = true;

            // Despawn if spawned in an incorrect context.
            if (Main.netMode != NetmodeID.MultiplayerClient && !wasSpawnedInValidContext)
                npc.active = false;
            else
            {
                // Clear pickoff enemies.
                OldOnesArmyMinibossChanges.ClearPickoffOOAEnemies();
            }

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
                    DoBehavior_DarkMagicShots(npc, target, isBuffed, ref attackTimer, ref currentFrame);
                    break;
                case DarkMageAttackType.SkeletonSummoning:
                    DoBehavior_SkeletonSummoning(npc, target, isBuffed, ref attackTimer, ref currentFrame);
                    break;
                case DarkMageAttackType.RedirectingFlames:
                    DoBehavior_RedirectingFlames(npc, target, isBuffed, ref attackTimer, ref currentFrame);
                    break;
                case DarkMageAttackType.DarkMagicCircles:
                    DoBehavior_DarkMagicCircles(npc, target, ref attackTimer, ref currentFrame);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_DarkMagicShots(NPC npc, NPCAimedTarget target, bool isBuffed, ref float attackTimer, ref float currentFrame)
        {
            int moveTime = 90;
            int chargeShootTime = 42;
            int totalShots = 5;
            int shootRate = 4;
            int shootCount = 3;
            if (isBuffed)
            {
                moveTime -= 30;
                chargeShootTime -= 16;
                totalShots++;
                shootRate--;
                shootCount--;
            }
            int shootTime = totalShots * shootRate;
            ref float shootCounter = ref npc.Infernum().ExtraAI[0];
            ref float aimDirection = ref npc.Infernum().ExtraAI[1];

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
                if (aimDirection == 0f)
                {
                    aimDirection = npc.AngleTo(target.Center + target.Velocity * 10f);
                    npc.netUpdate = true;
                }

                npc.velocity *= 0.5f;
                currentFrame = MathHelper.Lerp(8f, 12f, Utils.InverseLerp(moveTime + chargeShootTime, moveTime + chargeShootTime + shootTime, attackTimer, true));
                if (Main.netMode != NetmodeID.MultiplayerClient && (attackTimer - moveTime - chargeShootTime) % shootRate == shootRate - 1f)
                {
                    int darkMagicDamage = isBuffed ? 185 : 90;
                    float offsetAngle = MathHelper.Lerp(-0.48f, 0.48f, Utils.InverseLerp(0f, shootTime, attackTimer - moveTime - chargeShootTime, true));
                    Vector2 spawnPosition = npc.Center + new Vector2(npc.direction * 10f, -16f);
                    Vector2 shootVelocity = (aimDirection + offsetAngle).ToRotationVector2() * 16f;
                    Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ProjectileID.DD2DarkMageBolt, darkMagicDamage, 0f, Main.myPlayer, 0f, 0f);

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
                    int attackSelection = Main.rand.Next(isBuffed ? 3 : 2);
                    switch (attackSelection)
                    {
                        case 0:
                            npc.ai[0] = (int)DarkMageAttackType.SkeletonSummoning;
                            break;
                        case 1:
                            npc.ai[0] = (int)DarkMageAttackType.RedirectingFlames;
                            break;
                        case 2:
                            npc.ai[0] = (int)DarkMageAttackType.DarkMagicCircles;
                            break;
                    }
                    npc.netUpdate = true;
                }
                else
                    shootCounter++;
                aimDirection = 0f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            npc.rotation = npc.velocity.X * 0.04f;
        }

        public static void DoBehavior_SkeletonSummoning(NPC npc, NPCAimedTarget target, bool isBuffed, ref float attackTimer, ref float currentFrame)
        {
            int castTime = 32;
            int summonTime = 65;
            if (isBuffed)
                summonTime -= 20;

            npc.velocity *= 0.96f;
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            if (attackTimer < castTime)
                currentFrame = MathHelper.Lerp(29f, 27f, (float)Math.Sin(attackTimer / castTime * MathHelper.TwoPi) * 0.5f + 0.5f);
            else if (attackTimer < castTime + summonTime)
                currentFrame = MathHelper.Lerp(30f, 40f, (attackTimer - castTime) / summonTime);

            // Summon skeletons.
            bool shouldMoveOnToNextAttack = false;
            if (attackTimer == castTime + 4f)
            {
                shouldMoveOnToNextAttack = !DD2Event.CanRaiseGoblinsHere(npc.Center);

                if (!shouldMoveOnToNextAttack)
                {
                    Projectile.NewProjectile(npc.Center + new Vector2(npc.direction * 24f, -40f), Vector2.Zero, ProjectileID.DD2DarkMageRaise, 0, 0f);
                    DD2Event.RaiseGoblins(npc.Center);
                }
            }

            if (attackTimer >= castTime + summonTime || shouldMoveOnToNextAttack)
            {
                npc.ai[0] = (int)DarkMageAttackType.DarkMagicShots;
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_RedirectingFlames(NPC npc, NPCAimedTarget target, bool isBuffed, ref float attackTimer, ref float currentFrame)
        {
            int castTime = 32;
            int shootTime = 85;
            int shootRate = 10;
            int sitTime = 120;
            if (isBuffed)
            {
                shootRate -= 4;
                sitTime -= 40;
            }

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
                    int flameDamage = isBuffed ? 180 : 90;
                    Vector2 flameShootVelocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitX * npc.spriteDirection);
                    flameShootVelocity = Vector2.Lerp(flameShootVelocity, -Vector2.UnitY.RotatedByRandom(0.92f), 0.7f) * 13f;
                    int flame = Utilities.NewProjectileBetter(spawnPosition, flameShootVelocity, ModContent.ProjectileType<RedirectingWeakDarkMagicFlame>(), flameDamage, 0f);
                    if (Main.projectile.IndexInRange(flame))
                        Main.projectile[flame].ai[1] = isBuffed.ToInt();
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
                    Utilities.NewProjectileBetter(spawnPosition, flameShootVelocity, ModContent.ProjectileType<DarkMagicCircle>(), 185, 0f);
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
