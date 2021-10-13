using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Leviathan;
using CalamityMod.Projectiles.Boss;
using InfernumMode.BehaviorOverrides.BossAIs.DukeFishron;
using InfernumMode.Miscellaneous;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;

using LeviathanNPC = CalamityMod.NPCs.Leviathan.Leviathan;

namespace InfernumMode.BehaviorOverrides.BossAIs.Leviathan
{
	public class LeviathanAIClass : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<LeviathanNPC>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public enum LeviathanAttackType
        {
            LazilyHover,
            BubbleBelch,
            CallForHelp,
            MeteorVomiting,
            Charge
        }


        internal static readonly LeviathanAttackType[] Phase1AttackPattern = new LeviathanAttackType[]
        {
            LeviathanAttackType.LazilyHover,
            LeviathanAttackType.BubbleBelch,
            LeviathanAttackType.BubbleBelch,
            LeviathanAttackType.CallForHelp,
            LeviathanAttackType.LazilyHover,
            LeviathanAttackType.MeteorVomiting,
        };

        internal static readonly LeviathanAttackType[] Phase2AttackPattern = new LeviathanAttackType[]
        {
            LeviathanAttackType.LazilyHover,
            LeviathanAttackType.Charge,
            LeviathanAttackType.BubbleBelch,
            LeviathanAttackType.BubbleBelch,
            LeviathanAttackType.CallForHelp,
            LeviathanAttackType.LazilyHover,
            LeviathanAttackType.Charge,
            LeviathanAttackType.MeteorVomiting,
        };

        public override bool PreAI(NPC npc)
        {
            Player target = Main.player[npc.target];
            npc.damage = npc.defDamage;

            CalamityGlobalNPC.leviathan = npc.whoAmI;

            Vector2 mouthPosition = npc.Center + new Vector2(300f * npc.spriteDirection, -45f);

            if (!target.active || target.dead || !npc.WithinRange(target.Center, 5600f))
            {
                npc.TargetClosest(false);
                target = Main.player[npc.target];
                if (!target.active || target.dead || !npc.WithinRange(target.Center, 5600f))
                {
                    // Descend back into the ocean.
                    npc.velocity.X *= 0.97f;
                    npc.velocity.Y = MathHelper.Clamp(npc.velocity.Y + 0.2f, -3f, 16f);

                    if (npc.position.Y > Main.worldSurface * 16.0)
                    {
                        for (int x = 0; x < Main.maxNPCs; x++)
                        {
                            if (Main.npc[x].type == ModContent.NPCType<Siren>())
                            {
                                Main.npc[x].active = false;
                                Main.npc[x].netUpdate = true;
                            }
                        }
                        npc.active = false;
                        npc.netUpdate = true;
                    }

                    return false;
                }
            }

            ref float attackTimer = ref npc.ai[2];
            ref float spawnAnimationTime = ref npc.Infernum().ExtraAI[6];

            // Adjust Calamity's version of the spawn animation timer, for sky darkening purposes.
            npc.Calamity().newAI[3] = spawnAnimationTime;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool anahitaFightingToo = lifeRatio < 0.6f;
            bool sirenAlive = Main.npc.IndexInRange(CalamityGlobalNPC.siren) && Main.npc[CalamityGlobalNPC.siren].active;
            bool outOfOcean = target.position.X > 9400f && target.position.X < (Main.maxTilesX * 16 - 9400);

            npc.dontTakeDamage = outOfOcean;

            if (spawnAnimationTime < 180f)
            {
                npc.damage = 0;
                float minSpawnVelocity = 0.4f;
                float maxSpawnVelocity = 4f;
                float velocityY = maxSpawnVelocity - MathHelper.Lerp(minSpawnVelocity, maxSpawnVelocity, spawnAnimationTime / 180f);
                npc.velocity = Vector2.UnitY * -velocityY;

                npc.Opacity = MathHelper.Clamp(spawnAnimationTime / 180f, 0f, 1f);
                spawnAnimationTime++;
                return false;
            }

            // Play idle sounds.
            int soundChoiceRage = 92;
            int soundChoice = Utils.SelectRandom(Main.rand, new int[]
            {
                38,
                39,
                40
            });

            if (Main.rand.NextBool(600))
                Main.PlaySound(SoundID.Zombie, (int)npc.Center.X, (int)npc.Center.Y, sirenAlive ? soundChoice : soundChoiceRage);

            void goToNextAIState()
            {
                // You cannot use ref locals inside of a delegate context.
                // You should be able to find most important, universal locals above, anyway.
                // Any others that don't have an explicit reference above are exclusively for
                // AI state manipulation.

                npc.ai[3]++;

                LeviathanAttackType[] patternToUse = (anahitaFightingToo || outOfOcean) ? Phase2AttackPattern : Phase1AttackPattern;
                LeviathanAttackType nextAttackType = patternToUse[(int)(npc.ai[3] % patternToUse.Length)];

                // Going to the next AI state.
                npc.ai[1] = (int)nextAttackType;

                // Resetting the attack timer.
                npc.ai[2] = 0f;

                // And the misc ai slots.
                for (int i = 0; i < 5; i++)
                {
                    npc.Infernum().ExtraAI[i] = 0f;
                }
            }

            if (outOfOcean && (LeviathanAttackType)(int)npc.ai[1] != LeviathanAttackType.Charge)
            {
                goToNextAIState();
                return false;
            }

            bool usingBelchFrames = npc.frame.X > 0 && npc.frame.Y <= 0;

            switch ((LeviathanAttackType)(int)npc.ai[1])
            {
                case LeviathanAttackType.LazilyHover:
                    npc.TargetClosest();
                    npc.spriteDirection = npc.direction;

                    Vector2 destination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * 900f;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 8f, 0.16f);

                    if (attackTimer >= (sirenAlive ? 100f : 240f))
                        goToNextAIState();
                    break;
                case LeviathanAttackType.BubbleBelch:
                    npc.TargetClosest();
                    npc.spriteDirection = npc.direction;

                    int shootDelay = sirenAlive ? 60 : 35;

                    destination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * 960f;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 10f, 0.2f);

                    if (attackTimer >= shootDelay && attackTimer <= shootDelay + 35f && usingBelchFrames)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                Vector2 bubbleVelocity = Vector2.UnitX.RotatedByRandom(0.3f) * npc.spriteDirection * Main.rand.NextFloat(7f, 11f);
                                if (!sirenAlive || !anahitaFightingToo)
                                    bubbleVelocity *= 1.33f;

                                int bubble = NPC.NewNPC((int)mouthPosition.X, (int)mouthPosition.Y, Main.rand.NextBool(2) ? ModContent.NPCType<RedirectingBubble>() : NPCID.DetonatingBubble);
                                if (Main.npc.IndexInRange(bubble))
                                {
                                    Main.npc[bubble].velocity = bubbleVelocity;
                                    if (Main.npc[bubble].type == ModContent.NPCType<RedirectingBubble>())
                                        Main.npc[bubble].target = npc.target;
                                }
                            }
                        }

                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/LeviathanRoarMeteor"), npc.Center);
                        attackTimer = shootDelay + 35f;
                    }

                    if (attackTimer >= shootDelay + 35f && attackTimer <= shootDelay + 55f)
                        npc.frameCounter--;

                    if (attackTimer >= 130f)
                        goToNextAIState();
                    break;
                case LeviathanAttackType.CallForHelp:
                    int countedMinions = NPC.CountNPCS(ModContent.NPCType<Parasea>()) + NPC.CountNPCS(ModContent.NPCType<AquaticAberration>()) * 2;
                    int hoverTime = sirenAlive ? 90 : 45;
                    int slowdownTime = sirenAlive ? 60 : 30;
                    if (!anahitaFightingToo)
                    {
                        hoverTime -= 8;
                        slowdownTime -= 8;
                    }

                    if (attackTimer < hoverTime)
                    {
                        if (countedMinions >= 6)
                            goToNextAIState();

                        destination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * 950f;
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 11f, 0.15f);
                    }
                    else if (attackTimer < hoverTime + slowdownTime)
                        npc.velocity *= 0.97f;
                    else if (attackTimer >= hoverTime + slowdownTime && attackTimer <= hoverTime + slowdownTime + 35f && usingBelchFrames)
                    {
                        npc.velocity = Vector2.Zero;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            List<Vector2> decidedSpawnPositions = new List<Vector2>();
                            for (int i = 0; i < 3; i++)
                            {
                                bool shouldSpawnImmediately = true;
                                Vector2 spawnPosition = npc.Center + Main.rand.NextVector2CircularEdge(300f, 300f);
                                for (int tries = 0; tries < 300; tries++)
                                {
                                    float xOffset = Main.rand.NextFloat(450f, 950f) * Main.rand.NextBool(2).ToDirectionInt();

                                    WorldUtils.Find(new Vector2(npc.Center.X + xOffset, npc.Center.Y - 1000f).ToTileCoordinates(), Searches.Chain(new Searches.Down(250), new CustomTileConditions.IsWater()), out Point waterTop);
                                    if (Math.Abs(waterTop.X) > 1000000)
                                        continue;
                                    if (decidedSpawnPositions.Any(decided => Math.Abs(waterTop.X - decided.X) < 400f / 16f))
                                        continue;

                                    decidedSpawnPositions.Add(waterTop.ToVector2());
                                    spawnPosition = waterTop.ToWorldCoordinates() + Vector2.UnitY * 16f;
                                    shouldSpawnImmediately = false;
                                    break;
                                }

                                int typeToSummon = Main.rand.NextBool(2) ? ModContent.NPCType<Parasea>() : ModContent.NPCType<AquaticAberration>();
                                if (countedMinions >= 5)
                                    typeToSummon = ModContent.NPCType<Parasea>();

                                int spawner = Projectile.NewProjectile(spawnPosition, Vector2.Zero, ModContent.ProjectileType<LeviathanMinionSpawner>(), 0, 0f);
                                if (Main.projectile.IndexInRange(spawner))
                                {
                                    Main.projectile[spawner].ai[0] = typeToSummon;
                                    if (shouldSpawnImmediately)
                                        Main.projectile[spawner].timeLeft = 1;
                                }
                            }
                        }

                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/LeviathanRoarMeteor"), npc.Center);
                        attackTimer = hoverTime + slowdownTime + 35f;
                    }

                    if (attackTimer >= hoverTime + slowdownTime + 120f)
                        goToNextAIState();
                    break;
                case LeviathanAttackType.MeteorVomiting:
                    npc.TargetClosest();
                    npc.spriteDirection = npc.direction;

                    destination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * (anahitaFightingToo ? 1360f : 1110f);
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * 19f, 0.3f);

                    int vomitCount = 9;
                    int vomitTime = anahitaFightingToo ? 65 : 75;
                    if (!sirenAlive)
                        vomitTime = 52;

                    if (attackTimer % vomitTime >= 15f && attackTimer % vomitTime <= 50f && usingBelchFrames)
                    {
                        npc.frameCounter += 2;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                Vector2 shootVelocity = (target.Center - mouthPosition).SafeNormalize(Vector2.UnitX * npc.direction).RotatedBy(MathHelper.Lerp(-0.4f, 0.4f, i / 3f)) * 9f;
                                if (!anahitaFightingToo)
                                    shootVelocity *= 1.3f;
                                int meteor = Utilities.NewProjectileBetter(mouthPosition, shootVelocity, ModContent.ProjectileType<LeviathanBomb>(), 150, 0f);
                                if (Main.projectile.IndexInRange(meteor))
                                    Main.projectile[meteor].timeLeft += 180;
                            }
                        }

                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/LeviathanRoarMeteor"), npc.Center);
                        attackTimer += 50 - vomitTime % 50;
                    }

                    if (attackTimer % vomitTime >= 50f && attackTimer % vomitTime <= 75f)
                        npc.frameCounter--;

                    if (attackTimer >= vomitTime * vomitCount)
                        goToNextAIState();
                    break;
                case LeviathanAttackType.Charge:
                    npc.TargetClosest();

                    int redirectTime = sirenAlive ? 60 : 45;
                    int chargeTime = sirenAlive ? 60 : 50;
                    ref float hoverOffsetAngle = ref npc.Infernum().ExtraAI[1];
                    if (outOfOcean)
                    {
                        redirectTime = 50;
                        chargeTime = 24;
                        if (hoverOffsetAngle == 0f)
                            hoverOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    }

                    if (attackTimer < redirectTime)
                    {
                        destination = target.Center - Vector2.UnitX.RotatedBy(hoverOffsetAngle) * Math.Sign(target.Center.X - npc.Center.X) * 1000f;
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * (outOfOcean ? 23f : 12f), (outOfOcean || !sirenAlive) ? 0.55f : 0.25f);
                        npc.spriteDirection = npc.direction;
                    }

                    if (attackTimer == redirectTime)
                    {
                        float chargeSpeed = sirenAlive ? 23.5f : 32f;
                        if (outOfOcean)
                            chargeSpeed = 37f;
                        npc.velocity = Vector2.UnitX * npc.direction * chargeSpeed;
                        if (outOfOcean)
                            npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/LeviathanRoarCharge"), target.Center);
                    }

                    if (attackTimer >= redirectTime + chargeTime)
                        goToNextAIState();
                    break;
            }

            attackTimer++;
            return false;
        }
    }
}