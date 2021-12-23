using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.Projectiles.Boss;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using DraedonNPC = CalamityMod.NPCs.ExoMechs.Draedon;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.AresBodyBehaviorOverride;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ThanatosHeadBehaviorOverride;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ExoMechManagement;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using System.Linq;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public static partial class ExoMechComboAttackContent
    {
        // The Nuke's AI is not changed by this system because there's literally nothing you can do with it beyond its
        // normal behavior of shooting and reloading gauss nukes.
        public static Dictionary<ExoMechComboAttackType, int[]> AffectedAresArms => new Dictionary<ExoMechComboAttackType, int[]>()
        {
            [ExoMechComboAttackType.ThanatosAres_ExplosionCircle] = new int[] { ModContent.NPCType<AresTeslaCannon>(), 
                                                                                ModContent.NPCType<AresPlasmaFlamethrower>() },
        };

        public static bool ArmCurrentlyBeingUsed(NPC npc)
        {
            // Return false Ares is not present.
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
                return false;

            // Locate Ares' body as an NPC.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            if (AffectedAresArms.TryGetValue((ExoMechComboAttackType)aresBody.ai[0], out int[] activeArms))
                return activeArms.Contains(npc.type);
            return false;
        }

        public static bool UseThanatosAresComboAttack(NPC npc, ref float attackTimer, ref float frame)
        {
            NPC initialMech = FindInitialMech();
            if (initialMech is null)
                return false;

            Player target = Main.player[initialMech.target];
            switch ((ExoMechComboAttackType)initialMech.ai[0])
            {
                case ExoMechComboAttackType.ThanatosAres_ExplosionCircle:
                    return DoBehavior_ThanatosAres_ExplosionCircle(npc, target, ref attackTimer, ref frame);
            }
            return false;
        }

        public static bool DoBehavior_ThanatosAres_ExplosionCircle(NPC npc, Player target, ref float attackTimer, ref float frame)
        {
            int attackDelay = 180;
            int plasmaBurstShootRate = 140;
            int totalPlasmaPerBurst = 3;
            float plasmaBurstMaxSpread = 0.66f;
            float plasmaShootSpeed = 15f;
            int lightningShootRate = 225;
            int totalLightningShotsPerBurst = 3;
            int lightningBurstTime = 18;

            // Thanatos spins around the target with its head always open.
            if (npc.type == ModContent.NPCType<ThanatosHead>())
            {
                Vector2 spinDestination = target.Center + (attackTimer * MathHelper.TwoPi / 150f).ToRotationVector2() * 3600f;
                npc.velocity = npc.SafeDirectionTo(spinDestination) * MathHelper.Min(npc.Distance(spinDestination), 34f);
                npc.Center = npc.Center.MoveTowards(spinDestination, target.velocity.Length() * 1.2f);
                if (npc.WithinRange(spinDestination, 40f))
                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                else
                    npc.rotation = npc.rotation.AngleTowards((attackTimer + 8f) * MathHelper.TwoPi / 150f + MathHelper.PiOver2, 0.25f);

                frame = (int)ThanatosFrameType.Open;
            }

            // Ares' body hovers above the player, slowly moving back and forth horizontally.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 425f;
                if (attackTimer > attackDelay)
                    hoverDestination.X += (float)Math.Sin((attackTimer - attackDelay) * MathHelper.TwoPi / 180f) * 90f;

                DoHoverMovement(npc, hoverDestination, 24f, 75f);
            }

            // Ares' plasma arm releases bursts of plasma that slow down and explode.
            // If hit by lightning the plasma explodes early.
            if (npc.type == ModContent.NPCType<AresPlasmaFlamethrower>())
            {
                // Choose a direction and rotation.
                // Rotation is relative to predictiveness.
                float aimPredictiveness = 15f;
                Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);
                Vector2 endOfCannon = npc.Center + aimDirection.SafeNormalize(Vector2.Zero) * 66f + Vector2.UnitY * 16f;
                float idealRotation = aimDirection.ToRotation();

                if (npc.spriteDirection == 1)
                    idealRotation += MathHelper.Pi;
                if (idealRotation < 0f)
                    idealRotation += MathHelper.TwoPi;
                if (idealRotation > MathHelper.TwoPi)
                    idealRotation -= MathHelper.TwoPi;
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.065f);

                int direction = Math.Sign(target.Center.X - npc.Center.X);
                if (direction != 0)
                {
                    npc.direction = direction;

                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.spriteDirection = -npc.direction;
                }

                // Release dust at the end of the cannon as a telegraph.
                if (attackTimer >= attackDelay && attackTimer % plasmaBurstShootRate > plasmaBurstShootRate * 0.7f)
                {
                    Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
                    Dust plasma = Dust.NewDustPerfect(dustSpawnPosition, 107);
                    plasma.velocity = (endOfCannon - plasma.position) * 0.04f;
                    plasma.scale = 1.25f;
                    plasma.noGravity = true;
                }

                // Periodically release bursts of plasma bombs.
                if (attackTimer >= attackDelay && attackTimer % plasmaBurstShootRate == plasmaBurstShootRate - 1f)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaCasterFire"), npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < totalPlasmaPerBurst; i++)
                        {
                            Vector2 plasmaShootVelocity = npc.SafeDirectionTo(target.Center).RotatedByRandom(plasmaBurstMaxSpread) * Main.rand.NextFloat(0.85f, 1f) * plasmaShootSpeed;
                            Utilities.NewProjectileBetter(endOfCannon, plasmaShootVelocity, ModContent.ProjectileType<PlasmaBomb>(), 580, 0f);
                        }
                    }
                }
            }

            // Ares' tesla cannon releases streams of lightning rapid-fire.
            if (npc.type == ModContent.NPCType<AresTeslaCannon>())
            {
                // Choose a direction and rotation.
                // Rotation is relative to predictiveness.
                float aimPredictiveness = 38f;
                Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * aimPredictiveness);
                Vector2 endOfCannon = npc.Center + aimDirection.SafeNormalize(Vector2.Zero) * 84f + Vector2.UnitY * 8f;
                float idealRotation = aimDirection.ToRotation();

                if (npc.spriteDirection == 1)
                    idealRotation += MathHelper.Pi;
                if (idealRotation < 0f)
                    idealRotation += MathHelper.TwoPi;
                if (idealRotation > MathHelper.TwoPi)
                    idealRotation -= MathHelper.TwoPi;
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.065f);

                int direction = Math.Sign(target.Center.X - npc.Center.X);
                if (direction != 0)
                {
                    npc.direction = direction;

                    if (npc.spriteDirection != -npc.direction)
                        npc.rotation += MathHelper.Pi;

                    npc.spriteDirection = -npc.direction;
                }

                // Release dust at the end of the cannon as a telegraph.
                if (attackTimer >= attackDelay && attackTimer % lightningShootRate > lightningShootRate * 0.6f)
                {
                    Vector2 dustSpawnPosition = endOfCannon + Main.rand.NextVector2Circular(45f, 45f);
                    Dust plasma = Dust.NewDustPerfect(dustSpawnPosition, 229);
                    plasma.velocity = (endOfCannon - plasma.position) * 0.04f;
                    plasma.scale = 1.25f;
                    plasma.noGravity = true;
                }

                // Release lightning rapidfire.
                int timeBetweenLightningBurst = lightningBurstTime / totalLightningShotsPerBurst;
                bool canFireLightning = attackTimer % lightningShootRate >= lightningShootRate - lightningBurstTime && attackTimer % timeBetweenLightningBurst == timeBetweenLightningBurst - 1f;
                if (attackTimer >= attackDelay && canFireLightning)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), npc.Center);

                    // Release sparks everywhere.
                    for (int i = 0; i < 50; i++)
                    {
                        float sparkPower = Main.rand.NextFloat();
                        Dust spark = Dust.NewDustPerfect(endOfCannon + Main.rand.NextVector2Circular(25f, 25f), 261);
                        spark.velocity = (spark.position - endOfCannon) * MathHelper.Lerp(0.08f, 0.35f, sparkPower);
                        spark.scale = MathHelper.Lerp(1f, 1.5f, sparkPower);
                        spark.fadeIn = sparkPower;
                        spark.noGravity = true;
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 lightningShootVelocity = npc.SafeDirectionTo(endOfCannon) * 8f;
                        int lightning = Utilities.NewProjectileBetter(endOfCannon, lightningShootVelocity, ModContent.ProjectileType<TerateslaLightningBlast>(), 800, 0f);
                        Utilities.NewProjectileBetter(endOfCannon + lightningShootVelocity * 15f, Vector2.Zero, ModContent.ProjectileType<TeslaExplosion>(), 0, 0f);
                        if (Main.projectile.IndexInRange(lightning))
                        {
                            Main.projectile[lightning].ai[0] = lightningShootVelocity.ToRotation();
                            Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                        }
                    }
                }
            }

            return false;
        }
    }
}
