using CalamityMod.Particles;
using InfernumMode.BehaviorOverrides.BossAIs.BoC;
using InfernumMode.BehaviorOverrides.BossAIs.EoW;
using InfernumMode.BehaviorOverrides.BossAIs.QueenSlime;
using InfernumMode.OverridingSystem;
using InfernumMode.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.CorruptionMimic
{
    public class CorruptionMimicBehaviorOverride : NPCBehaviorOverride
    {
        public enum CorruptionMimicAttackState
        {
            Inactive,
            RapidJumps,
            GroundPound,
            CursedFlameWallSlam,
            SpreadOfCursedDarts,
            ChainGuillotineBursts
        }

        public override int NPCOverrideType => NPCID.BigMimicCorruption;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        public override bool PreAI(NPC npc)
        {
            // Pick a target.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Reset things.
            npc.defense = 10;
            npc.npcSlots = 16f;
            npc.knockBackResist = 0f;
            npc.noTileCollide = false;
            npc.noGravity = false;

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float isHostile = ref npc.ai[2];
            ref float currentFrame = ref npc.localAI[0];
            
            if ((npc.justHit || target.WithinRange(npc.Center, 200f)) && isHostile == 0f)
            {
                isHostile = 1f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            switch ((CorruptionMimicAttackState)(int)attackState)
            {
                case CorruptionMimicAttackState.Inactive:
                    if (DoBehavior_Inactive(npc, target, isHostile == 1f, ref attackTimer, ref currentFrame))
                        SelectNextAttack(npc);
                    break;
                case CorruptionMimicAttackState.RapidJumps:
                    if (DoBehavior_RapidJumps(npc, target, false, ref attackTimer, ref currentFrame))
                        SelectNextAttack(npc);
                    break;
                case CorruptionMimicAttackState.GroundPound:
                    if (DoBehavior_GroundPound(npc, target, ref attackTimer, ref currentFrame))
                        SelectNextAttack(npc);
                    break;
                case CorruptionMimicAttackState.CursedFlameWallSlam:
                    if (DoBehavior_RapidJumps(npc, target, true, ref attackTimer, ref currentFrame))
                        SelectNextAttack(npc);
                    break;
                case CorruptionMimicAttackState.SpreadOfCursedDarts:
                    DoBehavior_SpreadOfCursedDarts(npc, target, ref attackTimer, ref currentFrame);
                    break;
                case CorruptionMimicAttackState.ChainGuillotineBursts:
                    DoBehavior_ChainGuillotineBursts(npc, target, ref attackTimer, ref currentFrame);
                    break;
            }
            attackTimer++;

            return false;
        }

        public static bool DoBehavior_Inactive(NPC npc, Player target, bool isHostile, ref float attackTimer, ref float currentFrame)
        {
            npc.noTileCollide = false;
            npc.noGravity = false;
            npc.defense = 9999;
            npc.velocity.X *= 0.8f;
            currentFrame = 0f;
            if (isHostile)
                currentFrame = (int)Math.Round(Utils.Remap(attackTimer, 0f, 54f, 0f, 6f));

            return attackTimer >= 54f && isHostile;
        }

        public static bool DoBehavior_RapidJumps(NPC npc, Player target, bool wallSlams, ref float attackTimer, ref float currentFrame)
        {
            int jumpCount = 4;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float jumpDelay = MathHelper.Lerp(35f, 24f, 1f - lifeRatio);
            if (wallSlams)
            {
                jumpCount--;
                jumpDelay += 30f;
            }

            ref float jumpCounter = ref npc.Infernum().ExtraAI[0];

            if (npc.velocity.Y == 0f)
            {
                currentFrame = (int)(npc.frameCounter / 7 % 3 + 10f);

                // Slow down when touching the floor.
                npc.velocity.X *= 0.8f;

                // Look at the target.
                if (attackTimer >= jumpDelay * 0.5f)
                {
                    npc.TargetClosest();
                    npc.spriteDirection = npc.direction;
                }

                if (attackTimer >= jumpDelay)
                {
                    jumpCounter++;
                    if (jumpCounter >= jumpCount)
                        return true;

                    npc.velocity.X = npc.spriteDirection * 14f;
                    npc.velocity.Y = -3f;
                    if (target.Bottom.Y < npc.Center.Y)
                        npc.velocity.Y -= 1.25f;
                    if (target.Bottom.Y < npc.Center.Y - 40f)
                        npc.velocity.Y -= 1.5f;
                    if (target.Bottom.Y < npc.Center.Y - 80f)
                        npc.velocity.Y -= 1.75f;
                    if (target.Bottom.Y < npc.Center.Y - 120f)
                        npc.velocity.Y -= 2f;
                    if (target.Bottom.Y < npc.Center.Y - 160f)
                        npc.velocity.Y -= 2.25f;
                    if (target.Bottom.Y < npc.Center.Y - 200f)
                        npc.velocity.Y -= 2.5f;
                    if (!Collision.CanHit(npc, target))
                        npc.velocity.Y -= 2f;
                    if (wallSlams)
                    {
                        npc.velocity.Y *= 0.55f;

                        SoundEngine.PlaySound(SoundID.Item100, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Utilities.NewProjectileBetter(target.Center + Vector2.UnitX * 500f, Vector2.UnitX * -5f, ModContent.ProjectileType<CursedFlamePillar>(), 120, 0f);
                            Utilities.NewProjectileBetter(target.Center - Vector2.UnitX * 500f, Vector2.UnitX * 5f, ModContent.ProjectileType<CursedFlamePillar>(), 120, 0f);
                        }
                    }

                    npc.netUpdate = true;
                }
            }
            else
            {
                npc.velocity.X *= 0.99f;
                attackTimer = 0f;
                currentFrame = 13f;
            }
            return false;
        }

        public static bool DoBehavior_GroundPound(NPC npc, Player target, ref float attackTimer, ref float currentFrame)
        {
            // Disable tile collision and natural gravity.
            npc.noTileCollide = true;
            npc.noGravity = true;

            int slamCount = 3;
            int hoverTime = 8;
            int sitTime = 40;
            int projID = ModContent.ProjectileType<CursedBullet>();
            int cinderCount = 7;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float gravity = MathHelper.Lerp(0.8f, 1.12f, 1f - lifeRatio);
            float maxSlamSpeed = MathHelper.Lerp(15.4f, 21f, 1f - lifeRatio);
            Color sparkColor = Color.Lerp(Color.Yellow, Color.Green, 0.5f);
            Color stompColor = Color.Lerp(Color.Yellow, Color.Lime, 0.7f);
            
            if (npc.type == NPCID.BigMimicCrimson)
            {
                projID = ModContent.ProjectileType<BloodGeyser2>();
                sparkColor = Color.Lerp(Color.Yellow, Color.Orange, 0.4f);
                stompColor = Color.Crimson;
            }

            if (npc.type == NPCID.BigMimicHallow)
            {
                projID = ModContent.ProjectileType<CrystalShard>();
                sparkColor = Color.Lerp(Color.Cyan, Color.DeepPink, 0.3f);
                stompColor = Color.HotPink;
            }

            ref float attackState = ref npc.Infernum().ExtraAI[0];
            ref float slamCounter = ref npc.Infernum().ExtraAI[1];

            // Fly above the target in anticipation of the slam.
            if (attackState == 0f)
            {
                float flySpeed = attackTimer / 20f + 18.5f;
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 400f;
                npc.velocity = npc.SafeDirectionTo(hoverDestination) * flySpeed;
                npc.spriteDirection = (hoverDestination.X > npc.Center.X).ToDirectionInt();

                if (npc.WithinRange(hoverDestination, flySpeed * 2f))
                {
                    npc.Center = hoverDestination;
                    npc.velocity = Vector2.Zero;
                    attackState = 1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }

                currentFrame = 13f;
                return false;
            }

            // Slam downward.
            if (attackState == 1f)
            {
                currentFrame = 5f;
                npc.velocity.X *= 0.8f;

                npc.noTileCollide = npc.Bottom.Y < target.Top.Y;
                if (attackTimer < hoverTime)
                    return false;

                // Release cursed cinders at the target.
                if (attackTimer == hoverTime)
                {
                    SoundEngine.PlaySound(SoundID.Item73, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < cinderCount; i++)
                        {
                            float cinderShootOffsetAngle = MathHelper.Lerp(-0.75f, 0.75f, i / (float)(cinderCount - 1f));
                            Vector2 cinderVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(cinderShootOffsetAngle) * 9.6f;
                            Utilities.NewProjectileBetter(npc.Center, cinderVelocity, projID, 115, 0f);
                        }
                    }
                }

                // Slam onto the ground.
                if (!npc.noTileCollide && npc.velocity.Y == 0f)
                {
                    CreateGroundImpactEffects(npc, sparkColor, stompColor);
                    attackState = 2f;
                    attackTimer = 0f;
                    return false;
                }

                npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + gravity, -8f, maxSlamSpeed);
                return false;
            }

            // Sit on the ground briefly.
            if (attackState == 2f)
            {
                currentFrame = (int)Math.Round(Utils.Remap(attackTimer, 0f, sitTime - 12f, 5f, 0f));
                npc.velocity.X *= 0.8f;

                if (attackTimer >= sitTime)
                {
                    attackState = 0f;
                    attackTimer = 0f;
                    slamCounter++;
                    if (slamCounter >= slamCount)
                        return true;

                    npc.netUpdate = true;
                }
            }
            return false;
        }

        public static void DoBehavior_SpreadOfCursedDarts(NPC npc, Player target, ref float attackTimer, ref float currentFrame)
        {
            int dartBurstReleaseRate = 120;
            int dartBurstCount = 3;
            int dartsPerBurst = 3;
            ref float dartBurstCounter = ref npc.Infernum().ExtraAI[0];

            // Wait for the mimic to reach ground before shooting.
            if (npc.velocity.Y != 0f)
            {
                attackTimer = 0f;
                currentFrame = 13f;
                return;
            }

            // Look at the target.
            npc.TargetClosest();
            npc.spriteDirection = npc.direction;

            if (attackTimer >= 10f)
                currentFrame = (int)Math.Round(Utils.Remap(attackTimer, 0f, dartBurstReleaseRate - 30f, 6f, 12f));
            else
                currentFrame = 12f - attackTimer * 0.5f;

            // Release the spread of darts evenly.
            if (attackTimer >= dartBurstReleaseRate)
            {
                dartBurstCounter++;
                if (dartBurstCounter >= dartBurstCount)
                {
                    SelectNextAttack(npc);
                    return;
                }

                SoundEngine.PlaySound(SoundID.Item98, npc.Center);

                // Release some burst flames from the mouth.
                Vector2 mouthPosition = npc.Center + new Vector2(npc.spriteDirection * 8f, -4f);
                for (int i = 0; i < 10; i++)
                {
                    Vector2 fireSpawnPosition = mouthPosition + new Vector2(Main.rand.NextFloat(10f) * npc.spriteDirection, Main.rand.NextFloat(-6f, 0f));
                    Vector2 fireVelocity = npc.SafeDirectionTo(fireSpawnPosition).RotatedByRandom(0.48f) * Main.rand.NextFloat(1.8f, 4.5f);
                    Dust fire = Dust.NewDustPerfect(fireSpawnPosition, 75, fireVelocity);
                    fire.scale = Main.rand.NextFloat(1f, 1.5f);
                    fire.noGravity = true;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float dartSpeed = npc.Distance(target.Center) * 0.0125f + 15.5f;
                    for (int i = 0; i < dartsPerBurst; i++)
                    {
                        float dartShootOffsetAngle = MathHelper.Lerp(0.1f, -0.47f, i / (float)(dartsPerBurst - 1f)) * npc.spriteDirection + Main.rand.NextFloatDirection() * 0.05f;
                        Vector2 dartVelocity = npc.SafeDirectionTo(target.Center - Vector2.UnitY * 600f).RotatedBy(dartShootOffsetAngle) * dartSpeed;
                        Utilities.NewProjectileBetter(mouthPosition, dartVelocity, ModContent.ProjectileType<CursedDart>(), 120, 0f);
                    }
                }

                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_ChainGuillotineBursts(NPC npc, Player target, ref float attackTimer, ref float currentFrame)
        {
            int hideTime = 45;
            int guillotineShootDelay = 90;
            int guillotineCount = 13;
            ref float aimDirection = ref npc.Infernum().ExtraAI[0];

            // Wait for the mimic to reach ground before shooting.
            if (npc.velocity.Y != 0f)
            {
                attackTimer = 0f;
                currentFrame = 13f;
                return;
            }
            currentFrame = (int)Math.Round(Utils.Remap(attackTimer, 0f, hideTime, 6f, 0f));

            // Cast telegraph lines outward to mark where the guillotines will spawn.
            if (attackTimer == hideTime)
            {
                aimDirection = npc.AngleTo(target.Center);
                for (int i = 0; i < guillotineCount; i++)
                {
                    float offsetAngle = MathHelper.Lerp(-1.18f, 1.18f, i / (float)(guillotineCount - 1f));
                    for (int j = 0; j < 80; j++)
                    {
                        Vector2 dustSpawnPosition = npc.Center + (offsetAngle + aimDirection).ToRotationVector2() * j * 24f;
                        Dust telegraph = Dust.NewDustPerfect(dustSpawnPosition, 267, Vector2.Zero);
                        telegraph.color = Color.Lerp(Color.Purple, Color.Gray, 0.45f);
                        telegraph.scale = 1.75f;
                        telegraph.noGravity = true;
                    }
                }

                npc.netUpdate = true;
            }

            // Release the guillotines.
            if (attackTimer == guillotineShootDelay)
            {
                SoundEngine.PlaySound(SoundID.Item101, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < guillotineCount; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-1.18f, 1.18f, i / (float)(guillotineCount - 1f));
                        Vector2 guillotineShootVelocity = (offsetAngle + aimDirection).ToRotationVector2() * 15f;
                        int chain = Utilities.NewProjectileBetter(npc.Center, guillotineShootVelocity, ModContent.ProjectileType<ChainGuillotine>(), 120, 0f);
                        if (Main.projectile.IndexInRange(chain))
                            Main.projectile[chain].ai[1] = npc.whoAmI;
                    }
                }
            }

            if (attackTimer >= guillotineShootDelay + ChainGuillotine.PierceTime + ChainGuillotine.ReturnTime)
                SelectNextAttack(npc);
        }

        public static void CreateGroundImpactEffects(NPC npc, Color sparkColor, Color stompColor)
        {
            // Play a crash sound.
            SoundEngine.PlaySound(SoundID.Item14, npc.Bottom);

            // Create the particles.
            //for (int i = 0; i < 3; i++)
                //GeneralParticleHandler.SpawnParticle(new GroundImpactParticle(npc.Bottom, Vector2.UnitY, stompColor, 32, 0.5f));
            for (int i = 0; i < 15; i++)
            {
                float horizontalOffsetInterpolant = Main.rand.NextFloat();
                Vector2 sparkSpawnPosition = Vector2.Lerp(npc.BottomLeft, npc.BottomRight, MathHelper.Lerp(0.2f, 0.8f, horizontalOffsetInterpolant));
                Vector2 sparkVelocity = -Vector2.UnitY.RotatedBy(MathHelper.Lerp(-0.75f, 0.75f, horizontalOffsetInterpolant)) * Main.rand.NextFloat(7f, 16f);
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(sparkSpawnPosition, sparkVelocity, 1.1f, sparkColor, 40, 1f, 3f));
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            switch ((CorruptionMimicAttackState)npc.ai[0])
            {
                case CorruptionMimicAttackState.Inactive:
                    npc.ai[0] = (int)CorruptionMimicAttackState.RapidJumps;
                    break;
                case CorruptionMimicAttackState.RapidJumps:
                    npc.ai[0] = (int)CorruptionMimicAttackState.GroundPound;
                    break;
                case CorruptionMimicAttackState.GroundPound:
                    npc.ai[0] = (int)CorruptionMimicAttackState.CursedFlameWallSlam;
                    break;
                case CorruptionMimicAttackState.CursedFlameWallSlam:
                    npc.ai[0] = (int)CorruptionMimicAttackState.SpreadOfCursedDarts;
                    break;
                case CorruptionMimicAttackState.SpreadOfCursedDarts:
                    npc.ai[0] = (int)CorruptionMimicAttackState.ChainGuillotineBursts;
                    break;
                case CorruptionMimicAttackState.ChainGuillotineBursts:
                    npc.ai[0] = (int)CorruptionMimicAttackState.RapidJumps;
                    break;
            }

            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            npc.frame.Y = (int)(frameHeight * Math.Round(npc.localAI[0]));
        }
    }
}
