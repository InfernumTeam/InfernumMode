using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares;
using InfernumMode.BehaviorOverrides.BossAIs.Draedon.Thanatos;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.World.Generation;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.Ares.AresBodyBehaviorOverride;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.ExoMechManagement;
using static InfernumMode.BehaviorOverrides.BossAIs.Draedon.Thanatos.ThanatosHeadBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.BossAIs.Draedon
{
    public static partial class ExoMechComboAttackContent
    {
        public static Dictionary<ExoMechComboAttackType, int[]> AffectedAresArms => new Dictionary<ExoMechComboAttackType, int[]>()
        {
            [ExoMechComboAttackType.ThanatosAres_ElectricCage] = new int[] { ModContent.NPCType<AresTeslaCannon>(),
                ModContent.NPCType<AresPlasmaFlamethrower>(),
                ModContent.NPCType<AresLaserCannon>(),
                ModContent.NPCType<AresPulseCannon>() },
        };

        public static bool ArmCurrentlyBeingUsed(NPC npc)
        {
            // Return false Ares is not present.
            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
                return false;

            // Return false if the arm is disabled.
            if (ArmIsDisabled(npc))
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

            int thanatosIndex = NPC.FindFirstNPC(ModContent.NPCType<ThanatosHead>());
            int aresIndex = NPC.FindFirstNPC(ModContent.NPCType<AresBody>());
            if (thanatosIndex >= 0 && initialMech.ai[0] >= 100f)
            {
                if (Main.npc[thanatosIndex].Infernum().ExtraAI[13] < 240f)
                {
                    npc.velocity *= 0.9f;
                    npc.rotation *= 0.9f;
                    return true;
                }
            }

            Player target = Main.player[initialMech.target];
            switch ((ExoMechComboAttackType)initialMech.ai[0])
            {
                case ExoMechComboAttackType.ThanatosAres_LaserCircle:
                    return DoBehavior_ThanatosAres_LaserCircle(npc, target, ref attackTimer, ref frame);
                case ExoMechComboAttackType.ThanatosAres_ElectricCage:
                    {
                        bool result = DoBehavior_ThanatosAres_ElectricCage(npc, target, ref attackTimer, ref frame);
                        if (result && aresIndex >= 0)
                        {
                            Main.npc[aresIndex].Infernum().ExtraAI[13] = 0f;
                            Main.npc[aresIndex].netUpdate = true;
                        }
                        return result;
                    }
            }
            return false;
        }

        public static bool DoBehavior_ThanatosAres_LaserCircle(NPC npc, Player target, ref float attackTimer, ref float frame)
        {
            int attackDelay = 95;
            int telegraphTime = 50;
            int attackTime = 780;
            int spinTime = attackTime - attackDelay;
            int totalLasers = 8;
            ref float generalAngularOffset = ref npc.Infernum().ExtraAI[0];
            
            if (CurrentThanatosPhase != 4 || CurrentAresPhase != 4)
            {
                attackDelay -= 24;
                totalLasers += 4;
            }

            // Thanatos spins around the target with its head always open while releasing lasers inward.
            if (npc.type == ModContent.NPCType<ThanatosHead>() && CalamityGlobalNPC.draedonExoMechPrime != -1)
            {
                NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
                Vector2 spinDestination = aresBody.Center + (attackTimer * MathHelper.TwoPi / 150f).ToRotationVector2() * 2000f;

                npc.velocity = npc.SafeDirectionTo(spinDestination) * MathHelper.Min(npc.Distance(spinDestination), 34f);
                npc.Center = npc.Center.MoveTowards(spinDestination, target.velocity.Length() * 1.2f + 35f);
                if (npc.WithinRange(spinDestination, 40f))
                    npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                else
                    npc.rotation = npc.rotation.AngleTowards((attackTimer + 8f) * MathHelper.TwoPi / 150f + MathHelper.PiOver2, 0.25f);

                ref float totalSegmentsToFire = ref npc.Infernum().ExtraAI[0];
                ref float segmentFireTime = ref npc.Infernum().ExtraAI[1];
                ref float segmentFireCountdown = ref npc.Infernum().ExtraAI[2];

                // Select segment shoot attributes.
                int segmentShootDelay = 115;
                if (attackTimer > attackDelay && attackTimer % segmentShootDelay == segmentShootDelay - 1f)
                {
                    totalSegmentsToFire = 24f;
                    segmentFireTime = 92f;

                    segmentFireCountdown = segmentFireTime;
                    npc.netUpdate = true;
                }

                // Disable contact damage before the attack happens, to prevent cheap hits.
                if (attackTimer < attackDelay)
                    npc.damage = 0;

                if (segmentFireCountdown > 0f)
                    segmentFireCountdown--;

                // Decide frames.
                frame = (int)ThanatosFrameType.Open;
            }

            // Ares sits in place, creating five large exo overload laser bursts.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                if (attackTimer == 2f)
                {
                    // Clear away old projectiles.
                    int[] projectilesToDelete = new int[]
                    {
                        ModContent.ProjectileType<SmallPlasmaSpark>(),
                    };
                    for (int i = 0; i < Main.maxProjectiles; i++)
                    {
                        if (projectilesToDelete.Contains(Main.projectile[i].type))
                            Main.projectile[i].active = false;
                    }
                }

                // Decide frames.
                frame = (int)AresBodyFrameType.Normal;

                // Create telegraphs.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == attackDelay - telegraphTime)
                {
                    generalAngularOffset = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < totalLasers; i++)
                    {
                        Vector2 laserDirection = (MathHelper.TwoPi * i / totalLasers + generalAngularOffset).ToRotationVector2();
                        int telegraph = Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<AresDeathBeamTelegraph>(), 0, 0f);
                        if (Main.projectile.IndexInRange(telegraph))
                        {
                            Main.projectile[telegraph].ai[1] = npc.whoAmI;
                            Main.projectile[telegraph].localAI[0] = telegraphTime;
                            Main.projectile[telegraph].netUpdate = true;
                        }
                    }
                    npc.netUpdate = true;
                }

                // Create laser bursts.
                if (attackTimer == attackDelay)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < totalLasers; i++)
                        {
                            Vector2 laserDirection = (MathHelper.TwoPi * i / totalLasers + generalAngularOffset).ToRotationVector2();
                            int deathray = Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<AresSpinningDeathBeam>(), 1050, 0f);
                            if (Main.projectile.IndexInRange(deathray))
                            {
                                Main.projectile[deathray].ai[1] = npc.whoAmI;
                                Main.projectile[deathray].ModProjectile<AresSpinningDeathBeam>().LifetimeThing = spinTime;
                                Main.projectile[deathray].netUpdate = true;
                            }
                        }
                        generalAngularOffset = 0f;
                        npc.netUpdate = true;
                    }
                }

                if (attackTimer > attackDelay)
                {
                    float angularVelocity = Utils.InverseLerp(attackDelay, attackDelay + 60f, attackTimer, true) * MathHelper.Pi / 180f;
                    generalAngularOffset += angularVelocity;
                }

                // Slow down.
                if (!npc.WithinRange(target.Center, 1900f) || attackTimer < attackDelay - 75f)
                    ExoMechAIUtilities.DoSnapHoverMovement(npc, target.Center - Vector2.UnitY * 450f, 24f, 75f);
                else
                    npc.velocity *= 0.9f;
            }

            return attackTimer > attackDelay + attackTime;
        }

        public static bool DoBehavior_ThanatosAres_ElectricCage(NPC npc, Player target, ref float attackTimer, ref float frame)
        {
            int attackDelay = 150;
            int aresShootRate = 90;
            int aresCircularBoltCount = 25;
            int aresShotBoltCount = 7;
            int thanatosShootRate = 70;
            int lasersPerRotor = 9;
            float rotorSpeed = 23f;
            bool aresShouldAttack = attackTimer % 360f > 210f && attackTimer > attackDelay;
            bool thanatosShouldAttack = attackTimer % 360f <= 150f && attackTimer > attackDelay;
            ref float telegraphInterpolant = ref npc.Infernum().ExtraAI[Ares_LineTelegraphInterpolantIndex];
            ref float telegraphRotation = ref npc.Infernum().ExtraAI[Ares_LineTelegraphRotationIndex];

            if (CurrentThanatosPhase != 4 || CurrentAresPhase != 4)
            {
                aresShootRate -= 8;
                aresCircularBoltCount += 12;
                aresShotBoltCount += 2;
                thanatosShootRate -= 10;
            }

            if (CalamityGlobalNPC.draedonExoMechPrime == -1)
                return false;

            // Ares' arms hover in rectangular positions, marking a border.
            NPC aresBody = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            Vector2? hoverOffset = null;
            float borderOffset = 950f;
            Rectangle borderArea = Utils.CenteredRectangle(aresBody.Center, Vector2.One * borderOffset * 2f);
            int armShootType = -1;
            string shootSoundPath = null;
            float armShootSpeed = 10f;
            float perpendicularOffset = 0f;
            bool enraged = aresBody.Infernum().ExtraAI[13] == 1f;

            if (enraged)
            {
                aresShootRate /= 3;
                aresCircularBoltCount += 8;
                aresShotBoltCount += 8;
                thanatosShootRate -= 24;
            }

            if (npc.type == ModContent.NPCType<AresLaserCannon>())
            {
                armShootType = ModContent.ProjectileType<CannonLaser>();
                hoverOffset = new Vector2(-borderOffset, -borderOffset);
                shootSoundPath = "Sounds/Item/LaserCannon";
            }
            if (npc.type == ModContent.NPCType<AresTeslaCannon>())
            {
                armShootType = ModContent.ProjectileType<TeslaSpark>();
                hoverOffset = new Vector2(-borderOffset, borderOffset);
                shootSoundPath = "Sounds/Item/PlasmaBolt";
            }
            if (npc.type == ModContent.NPCType<AresPlasmaFlamethrower>())
            {
                armShootType = ModContent.ProjectileType<SmallPlasmaSpark>();
                hoverOffset = new Vector2(borderOffset, borderOffset);
                shootSoundPath = "Sounds/Item/PlasmaCasterFire";
            }
            if (npc.type == ModContent.NPCType<AresPulseCannon>())
            {
                armShootType = ModContent.ProjectileType<AresPulseBlast>();
                hoverOffset = new Vector2(borderOffset, -borderOffset);
                shootSoundPath = "Sounds/Item/PulseRifleFire";
                armShootSpeed = 2f;
                perpendicularOffset = 15f;
            }

            // Have arms hover and continuously fire things.
            if (hoverOffset.HasValue && attackTimer >= attackDelay)
            {
                // Hover into position.
                ExoMechAIUtilities.DoSnapHoverMovement(npc, aresBody.Center + hoverOffset.Value, 65f, 115f);

                float idealHoverRotation = hoverOffset.Value.ToRotation() + MathHelper.Pi - MathHelper.PiOver4;
                Vector2 aimDirection = idealHoverRotation.ToRotationVector2();

                // Choose a direction and rotation.
                // Rotation is relative to predictiveness.
                float idealRotation = aimDirection.ToRotation();
                if (npc.spriteDirection == 1)
                    idealRotation += MathHelper.Pi;
                if (idealRotation < 0f)
                    idealRotation += MathHelper.TwoPi;
                if (idealRotation > MathHelper.TwoPi)
                    idealRotation -= MathHelper.TwoPi;
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.065f);
                npc.spriteDirection = -1;

                float rotationToEndOfCannon = npc.rotation;
                if (rotationToEndOfCannon < 0f)
                    rotationToEndOfCannon += MathHelper.Pi;
                npc.ai[3] = rotationToEndOfCannon;
                Vector2 endOfCannon = npc.Center + aimDirection * 120f + aimDirection.RotatedBy(npc.spriteDirection * -MathHelper.PiOver2) * perpendicularOffset;

                if (attackTimer % 9f == 8f)
                {
                    if (Main.rand.NextBool(5))
                        Main.PlaySound(InfernumMode.CalamityMod.GetSoundSlot(SoundType.Item, shootSoundPath), npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int laser = Utilities.NewProjectileBetter(endOfCannon, aimDirection * armShootSpeed, armShootType, 500, 0f);
                        if (Main.projectile.IndexInRange(laser) && armShootType == ModContent.ProjectileType<CannonLaser>())
                        {
                            Main.projectile[laser].ai[1] = npc.whoAmI;
                            Main.projectile[laser].ModProjectile<CannonLaser>().Destination = npc.Center + aimDirection * 2000f;
                        }
                    }
                }
            }

            // Ares sits in place and releases bursts of exo sparks.
            if (npc.type == ModContent.NPCType<AresBody>())
            {
                // Prevent arms from swapping.
                npc.Infernum().ExtraAI[14] = 180;
                npc.Infernum().ExtraAI[15] = 0f;

                // Decide frames.
                frame = (int)AresBodyFrameType.Normal;

                Vector2 coreCenter = npc.Center + Vector2.UnitY * 34f;

                if (attackTimer < attackDelay)
                    ExoMechAIUtilities.DoSnapHoverMovement(npc, target.Center - Vector2.UnitY * 450f, 24f, 75f);
                else
                {
                    npc.velocity *= 0.9f;

                    // Descend if there is no ground below in sight, to prevent cases where the player is stuck in the air
                    // with limited flight time.
                    if (!WorldUtils.Find(npc.Center.ToTileCoordinates(), Searches.Chain(new Searches.Down(30), new Conditions.IsSolid()), out _))
                        npc.position.Y += 8f;
                }

                if (aresShouldAttack)
                {
                    Dust electricity = Dust.NewDustPerfect(npc.Center + Vector2.UnitY * 34f, 226);
                    electricity.velocity = Main.rand.NextVector2Circular(7f, 7f);
                    electricity.scale *= 1.3f;
                    electricity.position += electricity.velocity.SafeNormalize(Vector2.Zero) * 20f;
                    electricity.noGravity = true;

                    if (attackTimer % aresShootRate >= aresShootRate - 35f)
                    {
                        telegraphInterpolant = Utils.InverseLerp(aresShootRate - 50f, aresShootRate - 5f, attackTimer % aresShootRate, true);
                        if (attackTimer % aresShootRate < aresShootRate - 5f)
                            telegraphRotation = (target.Center - coreCenter).ToRotation();
                    }
                }

                if (aresShouldAttack && attackTimer % aresShootRate == aresShootRate - 1f)
                {
                    Main.PlaySound(InfernumMode.Instance.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/AresTeslaShot"), npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        // Fire a burst of circular sparks along with sparks that are loosely fired towards the target.
                        float circularSpreadAngularOffset = Main.rand.NextFloat(MathHelper.TwoPi);
                        for (int i = 0; i < aresCircularBoltCount; i++)
                        {
                            Vector2 boltShootVelocity = (MathHelper.TwoPi * i / aresCircularBoltCount + circularSpreadAngularOffset).ToRotationVector2() * 13f;
                            Vector2 boltSpawnPosition = coreCenter + boltShootVelocity.SafeNormalize(Vector2.UnitY) * 20f;
                            Utilities.NewProjectileBetter(boltSpawnPosition, boltShootVelocity, ModContent.ProjectileType<TeslaSpark>(), 500, 0f);
                        }

                        for (int i = 0; i < aresShotBoltCount; i++)
                        {
                            Vector2 boltShootVelocity = telegraphRotation.ToRotationVector2() * 31f + Main.rand.NextVector2Circular(5f, 5f);
                            Vector2 boltSpawnPosition = coreCenter + boltShootVelocity.SafeNormalize(Vector2.UnitY) * 21f;
                            Utilities.NewProjectileBetter(boltSpawnPosition, boltShootVelocity, ModContent.ProjectileType<TeslaSpark>(), 500, 0f);
                        }
                    }
                }

                // Get pissed off if the player attempts to leave the arm borders.
                if (!target.Hitbox.Intersects(borderArea) && !enraged && attackTimer > attackDelay)
                {
                    if (Main.player[Main.myPlayer].active && !Main.player[Main.myPlayer].dead)
                        Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/AresEnraged"), target.Center);

                    // Have Draedon comment on the player's attempts to escape.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        CalamityUtils.DisplayLocalizedText("Mods.CalamityMod.DraedonAresEnrageText", CalamityMod.NPCs.ExoMechs.Draedon.TextColorEdgy);

                    npc.Infernum().ExtraAI[13] = 1f;
                    npc.netUpdate = true;
                }
            }

            // Thanatos moves around and releases refraction rotors.
            if (npc.type == ModContent.NPCType<ThanatosHead>())
            {
                // Decide frames.
                frame = (int)ThanatosFrameType.Open;

                // Don't do contact damage.
                npc.damage = 0;

                // Do slow movement.
                DoProjectileShootInterceptionMovement(npc, target, 1.1f);
                if (npc.WithinRange(target.Center, 270f))
                    npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(target.Center) * -22f, 2f);

                ref float totalSegmentsToFire = ref npc.Infernum().ExtraAI[0];
                ref float segmentFireTime = ref npc.Infernum().ExtraAI[1];
                ref float segmentFireCountdown = ref npc.Infernum().ExtraAI[2];

                // Select segment shoot attributes.
                int segmentShootDelay = 115;
                if (attackTimer > attackDelay && attackTimer % segmentShootDelay == segmentShootDelay - 1f)
                {
                    totalSegmentsToFire = 16f;
                    segmentFireTime = 75f;

                    segmentFireCountdown = segmentFireTime;
                    npc.netUpdate = true;
                }

                if (segmentFireCountdown > 0f)
                    segmentFireCountdown--;

                // Shoot refraction rotors.
                if (thanatosShouldAttack && attackTimer % thanatosShootRate == thanatosShootRate - 1f && npc.WithinRange(target.Center, 1040f))
                {
                    Vector2 rotorShootVelocity = npc.SafeDirectionTo(target.Center).RotatedByRandom(0.4f) * rotorSpeed;
                    for (int i = 0; i < 5; i++)
                    {
                        int rotor = Utilities.NewProjectileBetter(npc.Center, rotorShootVelocity, ModContent.ProjectileType<RefractionRotor>(), 0, 0f);
                        if (Main.projectile.IndexInRange(rotor))
                            Main.projectile[rotor].ai[0] = lasersPerRotor;

                        rotorShootVelocity = rotorShootVelocity.RotatedBy(MathHelper.TwoPi / 5f);
                    }
                }
            }

            return attackTimer > 840f;
        }
    }
}
