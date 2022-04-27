using CalamityMod.NPCs;
using CalamityMod.NPCs.Perforator;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Perforators
{
    public class PerforatorHiveBehaviorOverride : NPCBehaviorOverride
    {
        public enum PerforatorHiveAttackState
        {
            DiagonalBloodCharge,
            HorizontalCrimeraSpawnCharge,
            IchorBlasts
        }

        public const float Phase2LifeRatio = 0.75f;
        public const float Phase3LifeRatio = 0.4f;
        public const float Phase4LifeRatio = 0.15f;

        public override int NPCOverrideType => ModContent.NPCType<PerforatorHive>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Set a global whoAmI variable.
            CalamityGlobalNPC.perfHive = npc.whoAmI;

            // Set damage.
            npc.defDamage = 74;
            npc.damage = npc.defDamage;

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead || !npc.WithinRange(Main.player[npc.target].Center, 6400f))
            {
                DoDespawnEffects(npc);
                return false;
            }

            Player target = Main.player[npc.target];

            switch ((PerforatorHiveAttackState)attackState)
            {
                case PerforatorHiveAttackState.DiagonalBloodCharge:
                    DoBehavior_DiagonalBloodCharge(npc, target, ref attackTimer);
                    break;
                case PerforatorHiveAttackState.HorizontalCrimeraSpawnCharge:
                    DoBehavior_HorizontalCrimeraSpawnCharge(npc, target, ref attackTimer);
                    break;
                case PerforatorHiveAttackState.IchorBlasts:
                    DoBehavior_IchorBlasts(npc, target, ref attackTimer);
                    break;
            }

            npc.rotation = MathHelper.Clamp(npc.velocity.X * 0.04f, -MathHelper.Pi / 6f, MathHelper.Pi / 6f);

            attackTimer++;
            return false;
        }

        public static void DoDespawnEffects(NPC npc)
        {
            npc.damage = 0;
            npc.velocity = Vector2.Lerp(npc.Center, Vector2.UnitY * 21f, 0.08f);
            if (npc.timeLeft > 225)
                npc.timeLeft = 225;
        }

        public static void DoBehavior_DiagonalBloodCharge(NPC npc, Player target, ref float attackTimer)
        {
            int chargeDelay = 55;
            int burstIchorCount = 5;
            int fallingIchorCount = 8;
            int chargeTime = 45;
            int chargeCount = 3;
            float chargeSpeed = 20f;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[1];

            // Hover into position.
            if (attackSubstate == 0f)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 375f, -270f);
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 20f;

                npc.SimpleFlyMovement(idealVelocity, idealVelocity.Length() / 12f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.08f);

                // Slow down and go to the next attack substate if sufficiently close to the hover destination.
                if (npc.WithinRange(hoverDestination, 75f))
                {
                    attackTimer = 0f;
                    attackSubstate = 1f;
                    npc.velocity *= 0.5f;
                    npc.netUpdate = true;
                }

                // Give up and perform a different attack if unable to reach the hover destination in time.
                if (attackTimer >= 480f)
                    SelectNextAttack(npc);
            }

            // Slow down in anticipation of a charge.
            if (attackSubstate == 1f)
            {
                npc.velocity *= 0.925f;

                // Release ichor into the air that slowly falls and charge at the target.
                if (attackTimer >= chargeDelay)
                {
                    SoundEngine.PlaySound(SoundID.NPCHit20, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < fallingIchorCount; i++)
                        {
                            float projectileOffsetInterpolant = i / (float)(fallingIchorCount - 1f);
                            float horizontalSpeed = MathHelper.Lerp(-16f, 16f, projectileOffsetInterpolant) + Main.rand.NextFloatDirection() / fallingIchorCount * 5f;
                            float verticalSpeed = Main.rand.NextFloat(-8f, -7f);
                            Vector2 ichorVelocity = new(horizontalSpeed, verticalSpeed);
                            Utilities.NewProjectileBetter(npc.Top + Vector2.UnitY * 10f, ichorVelocity, ModContent.ProjectileType<FallingIchor>(), 95, 0f);
                        }

                        for (int i = 0; i < burstIchorCount; i++)
                        {
                            float projectileOffsetInterpolant = i / (float)(burstIchorCount - 1f);
                            float offsetAngle = MathHelper.Lerp(-0.55f, 0.55f, projectileOffsetInterpolant);
                            Vector2 ichorVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(offsetAngle) * 6.5f;
                            Utilities.NewProjectileBetter(npc.Center + ichorVelocity * 3f, ichorVelocity, ModContent.ProjectileType<FlyingIchor>(), 95, 0f);
                        }

                        attackTimer = 0f;
                        attackSubstate = 2f;
                        npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                        npc.netUpdate = true;
                    }
                }
            }

            // Charge.
            if (attackSubstate == 2f)
            {
                if (attackTimer >= chargeTime)
                {
                    chargeCounter++;

                    if (chargeCounter >= chargeCount)
                        SelectNextAttack(npc);
                    attackSubstate = 0f;
                    attackTimer = 0f;
                    npc.velocity *= 0.45f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_HorizontalCrimeraSpawnCharge(NPC npc, Player target, ref float attackTimer)
        {
            int chargeDelay = 20;
            int chargeTime = 60;
            int crimeraSpawnCount = 1;
            int crimeraLimit = 3;
            int crimeraSpawnRate = chargeTime / crimeraSpawnCount;
            float hoverOffset = 500f;
            float chargeSpeed = hoverOffset / chargeTime * 2f;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];

            // Hover into position.
            if (attackSubstate == 0f)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * hoverOffset, -300f);
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 20f;

                npc.SimpleFlyMovement(idealVelocity, idealVelocity.Length() / 20f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.05f);

                // Slow down and go to the next attack substate if sufficiently close to the hover destination.
                if (npc.WithinRange(hoverDestination, 75f))
                {
                    attackTimer = 0f;
                    attackSubstate = 1f;
                    npc.velocity *= 0.5f;
                    npc.netUpdate = true;
                }

                // Give up and perform a different attack if unable to reach the hover destination in time.
                if (attackTimer >= 480f)
                    SelectNextAttack(npc);
            }

            // Slow down in anticipation of a charge.
            if (attackSubstate == 1f)
            {
                npc.velocity *= 0.925f;

                // Release ichor into the air that slowly falls and charge at the target.
                if (attackTimer >= chargeDelay)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        attackTimer = 0f;
                        attackSubstate = 2f;
                        npc.velocity = Vector2.UnitX * (target.Center.X > npc.Center.X).ToDirectionInt() * chargeSpeed;
                        npc.netUpdate = true;
                    }
                }
            }

            // Charge.
            if (attackSubstate == 2f)
            {
                // Summon Crimeras.
                bool enoughCrimerasAreAround = NPC.CountNPCS(NPCID.Crimera) >= crimeraLimit;
                if (attackTimer % crimeraSpawnRate == crimeraSpawnRate / 2 && !enoughCrimerasAreAround)
                {
                    SoundEngine.PlaySound(SoundID.NPCHit20, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        NPC.NewNPC(new InfernumSource(), (int)npc.Center.X, (int)npc.Center.Y, NPCID.Crimera, npc.whoAmI);
                }

                if (attackTimer >= chargeTime)
                {
                    SelectNextAttack(npc);
                    npc.velocity *= 0.45f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_IchorBlasts(NPC npc, Player target, ref float attackTimer)
        {
            int fireDelay = 50;
            int shootRate = 35;
            int blastCount = 12;
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];
            ref float reboundCoundown = ref npc.Infernum().ExtraAI[1];
            ref float universalTimer = ref npc.Infernum().ExtraAI[2];

            universalTimer++;

            float verticalHoverOffset = (float)Math.Sin(universalTimer / 13f) * 100f - 50f;
            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 480f, verticalHoverOffset);
            if (reboundCoundown <= 0f)
            {
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 20f;

                npc.SimpleFlyMovement(idealVelocity, idealVelocity.Length() / 60f);
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.02f);
                if (MathHelper.Distance(npc.Center.X, hoverDestination.X) < 35f)
				{
                    npc.position.X = hoverDestination.X - npc.width * 0.5f;
                    npc.velocity.X = 0f;
				}
            }
			else
            {
                reboundCoundown--;
            }

            // Hover into position.
            if (attackSubstate == 0f)
            {
                // Slow down and go to the next attack substate if sufficiently close to the hover destination.
                if (npc.WithinRange(hoverDestination, 75f))
                {
                    attackTimer = 0f;
                    attackSubstate = 1f;
                    npc.netUpdate = true;
                }

                // Give up and perform a different attack if unable to reach the hover destination in time.
                if (attackTimer >= 480f)
                    SelectNextAttack(npc);
            }
            
            // Slow down in preparation of firing.
            if (attackSubstate == 1f)
            {
                // Slow down.
                reboundCoundown = 1f;
                npc.velocity = (npc.velocity * 0.95f).MoveTowards(Vector2.Zero, 0.75f);

                if (attackTimer >= fireDelay)
                {
                    attackTimer = 0f;
                    attackSubstate = 2f;
                    npc.netUpdate = true;
                }
            }

            // Fire ichor blasts.
            if (attackSubstate == 2f)
            {
                if (attackTimer % shootRate == shootRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.NPCHit20, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            float offsetAngle = MathHelper.Lerp(-0.41f, 0.41f, i / 2f);
                            Vector2 shootVelocity = Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * 3.5f;
                            shootVelocity = shootVelocity.RotatedBy(offsetAngle);
                            Utilities.NewProjectileBetter(npc.Center + shootVelocity * 3f, shootVelocity, ModContent.ProjectileType<IchorBlast>(), 95, 0f);
                        }
                        npc.netUpdate = true;
                    }
                }

                if (attackTimer >= blastCount * shootRate)
                    SelectNextAttack(npc);
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            switch ((PerforatorHiveAttackState)npc.ai[0])
            {
                case PerforatorHiveAttackState.DiagonalBloodCharge:
                    npc.ai[0] = (int)PerforatorHiveAttackState.HorizontalCrimeraSpawnCharge;
                    break;
                case PerforatorHiveAttackState.HorizontalCrimeraSpawnCharge:
                    npc.ai[0] = (int)PerforatorHiveAttackState.IchorBlasts;
                    break;
                case PerforatorHiveAttackState.IchorBlasts:
                    npc.ai[0] = (int)PerforatorHiveAttackState.DiagonalBloodCharge;
                    break;
            }

            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.netUpdate = true;
        }

        #endregion AI

        #region Drawcode

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            return true;
        }
        #endregion
    }
}
