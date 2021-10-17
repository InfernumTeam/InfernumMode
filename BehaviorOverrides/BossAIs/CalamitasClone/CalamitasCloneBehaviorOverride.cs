using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.Projectiles.Boss;
using CalamityMod.World;
using InfernumMode.Dusts;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.World.Generation;
using CalamitasClownNPC = CalamityMod.NPCs.Calamitas.CalamitasRun3;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class CalamitasCloneBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<CalamitasClownNPC>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public const bool ReadyToUseBuffedAI = true;

        #region Enumerations
        public enum CloneAttackType
        {
            HorizontalDartRelease,
            BrimstoneMeteors,
            BrimstoneVolcano
        }
        #endregion

        #region AI

        public const float Phase2LifeRatio = 0.65f;

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            CalamityGlobalNPC.calamitas = npc.whoAmI;

            if (!target.active || target.dead || !npc.WithinRange(target.Center, 7200f))
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * -28f, 0.08f);
                if (!npc.WithinRange(target.Center, 1450f))
                {
                    npc.life = 0;
                    npc.active = false;
                    npc.netUpdate = true;
                }
                return false;
            }

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool shouldBeBuffed = CalamityWorld.downedProvidence && !BossRushEvent.BossRushActive;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            switch ((CloneAttackType)(int)attackType)
            {
                case CloneAttackType.HorizontalDartRelease:
                    npc.damage = 0;
                    DoBehavior_HorizontalDartRelease(npc, target, lifeRatio, shouldBeBuffed, ref attackTimer);
                    break;
                case CloneAttackType.BrimstoneMeteors:
                    npc.damage = 0;
                    DoBehavior_BrimstoneMeteors(npc, target, lifeRatio, shouldBeBuffed, ref attackTimer);
                    break;
                case CloneAttackType.BrimstoneVolcano:
                    DoBehavior_BrimstoneVolcano(npc, target, lifeRatio, shouldBeBuffed, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoBehavior_HorizontalDartRelease(NPC npc, Player target, float lifeRatio, bool shouldBeBuffed, ref float attackTimer)
        {
            int attackCycleCount = 4;
            int hoverTime = 210;
            float hoverHorizontalOffset = 530f;
            float hoverSpeed = 15f;
            float initialFlameSpeed = 10.5f;
            int flameReleaseRate = 6;
            int flameReleaseTime = 180;
            if (lifeRatio < Phase2LifeRatio)
            {
                attackCycleCount--;
                hoverHorizontalOffset -= 70f;
                initialFlameSpeed += 4.5f;
                flameReleaseRate -= 2;
            }

            if (shouldBeBuffed)
            {
                attackCycleCount--;
                hoverSpeed += 9f;
            }

            ref float attackCycleCounter = ref npc.Infernum().ExtraAI[0];
            ref float attackSubstate = ref npc.Infernum().ExtraAI[1];

            // Attempt to hover to the side of the target.
            Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * hoverHorizontalOffset;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 45f);
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            // Prepare the attack after either enough time has passed or if sufficiently close to the hover destination.
            // This is done to ensure that the attack begins once the boss is close to the target.
            if (attackSubstate == 0f && (attackTimer > hoverTime || npc.WithinRange(hoverDestination, 60f)))
            {
                attackSubstate = 1f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            // Release fireballs.
            if (attackSubstate == 1f)
            {
                if (attackTimer % flameReleaseRate == flameReleaseRate - 1f && attackTimer % 90f > 35f)
                {
                    Main.PlaySound(SoundID.Item73, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int dartDamage = shouldBeBuffed ? 310 : 145;
                        float idealDirection = npc.AngleTo(target.Center);
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY).RotatedByRandom(0.72f) * initialFlameSpeed;

                        int cinder = Utilities.NewProjectileBetter(npc.Center + shootVelocity * 2f, shootVelocity, ModContent.ProjectileType<AdjustingCinder>(), dartDamage, 0f);
                        if (Main.projectile.IndexInRange(cinder))
                            Main.projectile[cinder].ai[0] = idealDirection;
                    }
                }

                if (attackTimer > flameReleaseTime)
                {
                    attackTimer = 0f;
                    attackSubstate = 0f;
                    attackCycleCounter++;

                    if (attackCycleCounter > attackCycleCount)
                        SelectNewAttack(npc);
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_BrimstoneMeteors(NPC npc, Player target, float lifeRatio, bool shouldBeBuffed, ref float attackTimer)
        {
            int attackDelay = 90;
            int attackTime = 480;
            int meteorShootRate = 12;
            float meteorShootSpeed = 9f;
            float hoverSpeed = 15f;
            if (shouldBeBuffed)
            {
                attackTime += 60;
                meteorShootRate -= 4;
                meteorShootSpeed += 4f;
                hoverSpeed += 9f;
            }
            meteorShootSpeed *= MathHelper.Lerp(1f, 1.35f, 1f - lifeRatio);

            ref float meteorAngle = ref npc.Infernum().ExtraAI[0];

            // Attempt to hover above the target.
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 380f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 45f);
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            // Create an explosion sound and decide the meteor angle right before the meteors fall.
            if (attackTimer == attackDelay - 25f)
            {
                Main.PlaySound(SoundID.DD2_KoboldExplosion, target.Center);
                meteorAngle = Main.rand.NextFloatDirection() * MathHelper.Pi / 9f;
                npc.netUpdate = true;
            }

            bool canFire = attackTimer > attackDelay && attackTimer < attackTime + attackDelay;

            // Rain meteors from the sky. This has a delay at the start and end of the attack.
            if (Main.netMode != NetmodeID.MultiplayerClient && canFire && attackTimer % meteorShootRate == meteorShootRate - 1f)
            {
                int meteorDamage = shouldBeBuffed ? 325 : 150;
                Vector2 meteorSpawnPosition = target.Center + new Vector2(Main.rand.NextFloat(-1050, 1050f), -780f);
                Vector2 shootDirection = Vector2.UnitY.RotatedBy(meteorAngle);
                Vector2 shootVelocity = shootDirection * meteorShootSpeed;

                int meteorType = ModContent.ProjectileType<BrimstoneMeteor>();
                Utilities.NewProjectileBetter(meteorSpawnPosition, shootVelocity, meteorType, meteorDamage, 0f);
            }

            if (attackTimer > attackTime + attackDelay * 2f)
                SelectNewAttack(npc);
        }

        public static void DoBehavior_BrimstoneVolcano(NPC npc, Player target, float lifeRatio, bool shouldBeBuffed, ref float attackTimer)
        {
            int attackDelay = 45;
            int attackTime = 300;
            int lavaShootRate = 24;
            float hoverSpeed = 15f;

            if (lifeRatio < Phase2LifeRatio)
            {
                attackTime += 25;
                lavaShootRate -= 4;
            }

            if (shouldBeBuffed)
            {
                attackTime += 45;
                lavaShootRate -= 10;
                hoverSpeed += 9f;
            }

            // Attempt to hover above the target.
            Vector2 hoverDestination = target.Center - Vector2.UnitY * 350f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, hoverSpeed / 45f);
            npc.rotation = npc.AngleTo(target.Center) - MathHelper.PiOver2;

            // Create an flame burst sound and before the lava comes up.
            if (attackTimer == attackDelay - 25f)
            {
                Main.PlaySound(SoundID.DD2_FlameburstTowerShot, target.Center);
                npc.netUpdate = true;
            }

            bool canFire = attackTimer > attackDelay && attackTimer < attackTime + attackDelay;

            // Create lava from the ground. This has a delay at the start and end of the attack.
            if (Main.netMode != NetmodeID.MultiplayerClient && canFire && attackTimer % lavaShootRate == lavaShootRate - 1f)
            {
                int lavaDamage = shouldBeBuffed ? 325 : 150;
                Vector2 lavaSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 350f, 40f);
                if (WorldUtils.Find(lavaSpawnPosition.ToTileCoordinates(), Searches.Chain(new Searches.Down(1500), new Conditions.IsSolid()), out Point result))
                {
                    lavaSpawnPosition = result.ToWorldCoordinates();
                    int lavaType = ModContent.ProjectileType<BrimstoneGeyser>();
                    Utilities.NewProjectileBetter(lavaSpawnPosition, Vector2.Zero, lavaType, lavaDamage, 0f);
                }

                // Use a different attack if a bottom could not be located.
                else
                    SelectNewAttack(npc);
            }

            if (attackTimer > attackTime + attackDelay * 2f)
                SelectNewAttack(npc);
        }

        public static void SelectNewAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            List<CloneAttackType> possibleAttacks = new List<CloneAttackType>
            {
                CloneAttackType.HorizontalDartRelease,
                CloneAttackType.BrimstoneMeteors,
                CloneAttackType.BrimstoneVolcano
            };

            if (possibleAttacks.Count > 1)
                possibleAttacks.Remove((CloneAttackType)(int)npc.ai[0]);

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.ai[0] = (int)Main.rand.Next(possibleAttacks);
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI
    }
}
