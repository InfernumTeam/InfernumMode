using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Prime
{
    public class PrimeHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.SkeletronPrime;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        #region Enumerations
        public enum PrimeAttackType
        {
            SpawnEffects,
            MetalBurst,
            RocketRelease,
            HoverCharge,
            LaserRay,
            LightningSupercharge
        }

        public enum PrimeFrameType
        {
            ClosedMouth,
            OpenMouth,
            Spikes
        }
        #endregion

        #region AI
        public static bool AnyArms => NPC.AnyNPCs(NPCID.PrimeCannon) || NPC.AnyNPCs(NPCID.PrimeLaser) || NPC.AnyNPCs(NPCID.PrimeVice) || NPC.AnyNPCs(NPCID.PrimeSaw);
        public static int RemainingArms => NPC.AnyNPCs(NPCID.PrimeCannon).ToInt() + NPC.AnyNPCs(NPCID.PrimeLaser).ToInt() + NPC.AnyNPCs(NPCID.PrimeVice).ToInt() + NPC.AnyNPCs(NPCID.PrimeSaw).ToInt();
        public static bool ShouldBeInactive(int armType, float armCycleTimer)
        {
            if (RemainingArms <= 2)
                return false;

            armCycleTimer %= 1800f;
            if (armCycleTimer < 450f)
                return armType == NPCID.PrimeSaw || armType == NPCID.PrimeVice;
            if (armCycleTimer < 900f)
                return armType == NPCID.PrimeVice || armType == NPCID.PrimeCannon;
            if (armCycleTimer < 1350f)
                return armType == NPCID.PrimeCannon || armType == NPCID.PrimeLaser;
            return armType == NPCID.PrimeLaser || armType == NPCID.PrimeSaw;
        }

        public static void ArmHoverAI(NPC npc)
        {
            ref float angularVelocity = ref npc.localAI[0];

            // Have angular velocity rely on exponential momentum.
            angularVelocity = angularVelocity * 0.8f + npc.velocity.X * 0.04f * 0.2f;
            npc.rotation = npc.rotation.AngleLerp(angularVelocity, 0.15f);
        }

        public override bool PreAI(NPC npc)
        {
            npc.frame = new Rectangle(100000, 100000, 94, 94);
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float armCycleTimer = ref npc.ai[2];
            ref float frameType = ref npc.localAI[0];

            npc.TargetClosest();

            float lifeRatio = npc.life / (float)npc.lifeMax;
            Player target = Main.player[npc.target];

            // Continuously reset defense.
            npc.defense = npc.defDefense;

            // Don't allow further damage to happen when below 55% life if any arms remain.
            npc.dontTakeDamage = lifeRatio < 0.55f && AnyArms;

            switch ((PrimeAttackType)(int)attackType)
            {
                case PrimeAttackType.SpawnEffects:
                    DoAttack_SpawnEffects(npc, target, attackTimer, ref frameType);
                    break;
                case PrimeAttackType.MetalBurst:
                    DoAttack_MetalBurst(npc, target, attackTimer, ref frameType);
                    break;
                case PrimeAttackType.RocketRelease:
                    DoAttack_RocketRelease(npc, target, attackTimer, ref frameType);
                    break;
                case PrimeAttackType.HoverCharge:
                    DoAttack_HoverCharge(npc, target, attackTimer, ref frameType);
                    break;
                case PrimeAttackType.LaserRay:
                    DoAttack_LaserRay(npc, target, attackTimer, ref frameType);
                    break;
                case PrimeAttackType.LightningSupercharge:
                    DoAttack_LightningSupercharge(npc, target, ref attackTimer, ref frameType);
                    break;
            }

            if (npc.position.Y < 900f)
                npc.position.Y = 900f;

            armCycleTimer++;
            attackTimer++;
            return false;
        }

        #region Specific Attacks
        public static void DoAttack_SpawnEffects(NPC npc, Player target, float attackTimer, ref float frameType)
        {
            bool canHover = attackTimer < 90f;

            if (canHover)
            {
                Vector2 hoverDestination = target.Center - Vector2.UnitY * 400f;
                hoverDestination.Y += MathHelper.Lerp(-600f, 600f, attackTimer / 90f);
                hoverDestination.X += MathHelper.Lerp(-700f, 700f, attackTimer / 90f);

                npc.velocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), 32f);
                npc.rotation = npc.rotation.AngleLerp(npc.velocity.X * 0.04f, 0.1f);

                if (npc.WithinRange(target.Center, 90f))
                    npc.Center = target.Center - npc.SafeDirectionTo(target.Center, Vector2.UnitY) * 90f;

                frameType = (int)PrimeFrameType.ClosedMouth;
            }
            else
            {
                if (attackTimer >= 195f)
                    frameType = (int)PrimeFrameType.OpenMouth;

                npc.velocity *= 0.85f;
                npc.rotation = npc.rotation.AngleLerp(0f, 0.2f);
                if (attackTimer > 210f)
                {
                    Main.PlaySound(SoundID.Roar, target.Center, 0);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        npc.TargetClosest();
                        int arm = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.PrimeCannon, npc.whoAmI);
                        Main.npc[arm].ai[0] = -1f;
                        Main.npc[arm].ai[1] = npc.whoAmI;
                        Main.npc[arm].target = npc.target;
                        Main.npc[arm].netUpdate = true;

                        arm = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.PrimeLaser, npc.whoAmI);
                        Main.npc[arm].ai[0] = 1f;
                        Main.npc[arm].ai[1] = npc.whoAmI;
                        Main.npc[arm].target = npc.target;
                        Main.npc[arm].netUpdate = true;

                        arm = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.PrimeSaw, npc.whoAmI);
                        Main.npc[arm].ai[0] = 1f;
                        Main.npc[arm].ai[1] = npc.whoAmI;
                        Main.npc[arm].target = npc.target;
                        Main.npc[arm].netUpdate = true;

                        arm = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.PrimeVice, npc.whoAmI);
                        Main.npc[arm].ai[0] = -1f;
                        Main.npc[arm].ai[1] = npc.whoAmI;
                        Main.npc[arm].target = npc.target;
                        Main.npc[arm].netUpdate = true;
                    }

                    GotoNextAttackState(npc);
                }
            }
        }
        
        public static void DoAttack_MetalBurst(NPC npc, Player target, float attackTimer, ref float frameType)
        {
            int shootRate = AnyArms ? 95 : 45;
            int shootCount = AnyArms ? 4 : 5;
            int spikesPerBurst = AnyArms ? 7 : 16;
            float hoverSpeed = AnyArms ? 15f : 36f;
            float wrappedTime = attackTimer % shootRate;

            Vector2 destination = target.Center - Vector2.UnitY * (AnyArms ? 510f : 380f);
            npc.SimpleFlyMovement(npc.SafeDirectionTo(destination) * hoverSpeed, 0.4f);
            npc.rotation = npc.velocity.X * 0.04f;

            // Open the mouth a little bit before shooting.
            frameType = wrappedTime >= shootRate * 0.7f ? (int)PrimeFrameType.OpenMouth : (int)PrimeFrameType.ClosedMouth;

            if (wrappedTime == shootRate - 1f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < spikesPerBurst; i++)
                    {
                        Vector2 spikeVelocity = (MathHelper.TwoPi * i / spikesPerBurst).ToRotationVector2() * 5.5f;
                        if (AnyArms)
                            spikeVelocity *= 0.56f;

                        Utilities.NewProjectileBetter(npc.Center + spikeVelocity * 12f, spikeVelocity, ModContent.ProjectileType<MetallicSpike>(), 115, 0f);
                    }
                }
                Main.PlaySound(SoundID.Item101, target.Center);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= shootRate * (shootCount + 0.65f))
                GotoNextAttackState(npc);
        }

        public static void DoAttack_RocketRelease(NPC npc, Player target, float attackTimer, ref float frameType)
        {
            int cycleTime = 36;
            int rocketCountPerCycle = 7;
            int shootCycleCount = AnyArms ? 4 : 6;
            float wrappedTime = attackTimer % cycleTime;

            npc.rotation = npc.velocity.X * 0.04f;

            frameType = (int)PrimeFrameType.ClosedMouth;
            if (wrappedTime > cycleTime - rocketCountPerCycle * 2f)
            {
                frameType = (int)PrimeFrameType.OpenMouth;
                npc.velocity *= 0.87f;

                Main.PlaySound(SoundID.Item42, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient && wrappedTime % 3f == 2f)
                {
                    float rocketSpeed = Main.rand.NextFloat(10.5f, 12f) * (AnyArms ? 0.52f : 1f);
                    Vector2 rocketVelocity = Main.rand.NextVector2CircularEdge(rocketSpeed, rocketSpeed);
                    if (rocketVelocity.Y < -1f)
                        rocketVelocity.Y = -1f;
                    rocketVelocity = Vector2.Lerp(rocketVelocity, npc.SafeDirectionTo(target.Center).RotatedByRandom(0.4f) * rocketVelocity.Length(), 0.6f);
                    rocketVelocity = rocketVelocity.SafeNormalize(-Vector2.UnitY) * rocketSpeed;
                    Utilities.NewProjectileBetter(npc.Center + Vector2.UnitY * 33f, rocketVelocity, ProjectileID.SaucerMissile, 115, 0f);
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= cycleTime * (shootCycleCount + 0.4f))
                GotoNextAttackState(npc);
        }

        public static void DoAttack_HoverCharge(NPC npc, Player target, float attackTimer, ref float frameType)
        {
            int chargeCount = 7;
            int hoverTime = AnyArms ? 120 : 60;
            int chargeTime = AnyArms ? 72 : 45;
            float hoverSpeed = AnyArms ? 14f : 33f;
            float chargeSpeed = AnyArms ? 15f : 22.5f;
            float wrappedTime = attackTimer % (hoverTime + chargeTime);

            if (wrappedTime < hoverTime - 15f)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 365f, -300f);
                npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), hoverSpeed)) / 8f;

                if (npc.WithinRange(target.Center, 150f))
                {
                    npc.Center = target.Center - npc.SafeDirectionTo(target.Center, Vector2.UnitY) * 150f;
                    npc.velocity = Vector2.Zero;
                }
                npc.rotation = npc.velocity.X * 0.04f;
                frameType = (int)PrimeFrameType.ClosedMouth;
            }
            else if (wrappedTime < hoverTime)
            {
                npc.velocity *= 0.94f;
                npc.rotation = npc.velocity.X * 0.04f;
                frameType = (int)PrimeFrameType.OpenMouth;
            }
            else
            {
                if (wrappedTime == hoverTime + 1f)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    npc.netUpdate = true;

                    if (!AnyArms)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                Vector2 shootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.Lerp(-0.56f, 0.56f, i / 3f)) * 8f;
                                Utilities.NewProjectileBetter(npc.Center + shootVelocity * 7f, shootVelocity, ModContent.ProjectileType<MetallicSpike>(), 115, 0f);
                            }
                        }
                        Main.PlaySound(SoundID.Item101, target.Center);
                    }

                    Main.PlaySound(SoundID.Roar, target.Center, 0);
                }

                frameType = (int)PrimeFrameType.Spikes;
                npc.rotation += npc.velocity.Length() * 0.018f;
            }

            if (attackTimer >= (hoverTime + chargeTime) * chargeCount + 20)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_LaserRay(NPC npc, Player target, float attackTimer, ref float frameType)
        {
            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 320f, -270f) - npc.velocity * 4f;
            float movementSpeed = MathHelper.Lerp(33f, 4.5f, Utils.InverseLerp(45f, 90f, attackTimer, true));
            npc.velocity = (npc.velocity * 7f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), movementSpeed)) / 8f;

            if (npc.WithinRange(target.Center, 150f))
            {
                npc.Center = target.Center - npc.SafeDirectionTo(target.Center, Vector2.UnitY) * 150f;
                npc.velocity = Vector2.Zero;
            }
            npc.rotation = npc.velocity.X * 0.04f;

            if (attackTimer == 95f)
                Main.PlaySound(SoundID.Roar, target.Center, 0);

            if (attackTimer == 125f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float angularOffset = MathHelper.ToRadians(36f);
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 beamSpawnPosition = npc.Center + new Vector2(-i * 16f, -7f);
                        Vector2 beamDirection = (target.Center - beamSpawnPosition).SafeNormalize(-Vector2.UnitY).RotatedBy(angularOffset * -i);

                        int beam = Utilities.NewProjectileBetter(beamSpawnPosition, beamDirection, ModContent.ProjectileType<LaserRay>(), 140, 0f);
                        if (Main.projectile.IndexInRange(beam))
                        {
                            Main.projectile[beam].ai[0] = i * angularOffset / 120f * 0.385f;
                            Main.projectile[beam].ai[1] = npc.whoAmI;
                            Main.projectile[beam].netUpdate = true;
                        }
                    }
                }
            }

            frameType = attackTimer < 110f ? (int)PrimeFrameType.ClosedMouth : (int)PrimeFrameType.OpenMouth;

            // Release a few rockets after creating the laser to create pressure.
            if (attackTimer > 125f && attackTimer % 20f == 19f)
            {
                Main.PlaySound(SoundID.Item42, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float rocketAngularOffset = Utils.InverseLerp(125f, 225f, attackTimer, true) * MathHelper.TwoPi;
                    Vector2 rocketVelocity = rocketAngularOffset.ToRotationVector2() * (Main.rand.NextFloat(5.5f, 6.2f) + npc.Distance(target.Center) * 0.00267f);
                    Utilities.NewProjectileBetter(npc.Center + Vector2.UnitY * 33f + rocketVelocity * 2.5f, rocketVelocity, ProjectileID.SaucerMissile, 115, 0f);
                }
            }

            if (attackTimer >= 255f)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_LightningSupercharge(NPC npc, Player target, ref float attackTimer, ref float frameType)
        {
            ref float struckByLightningFlag = ref npc.Infernum().ExtraAI[0];
            ref float superchargeTimer = ref npc.Infernum().ExtraAI[1];

            if (attackTimer < 35f)
            {
                npc.velocity *= 0.84f;
                npc.rotation = npc.velocity.X * 0.04f;
            }

            if (attackTimer == 35f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/LightningStrike"), target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 lightningSpawnPosition = npc.Center - Vector2.UnitY * 1300f + Main.rand.NextVector2Circular(30f, 30f);
                        if (lightningSpawnPosition.Y < 600f)
                            lightningSpawnPosition.Y = 600f;
                        int lightning = Utilities.NewProjectileBetter(lightningSpawnPosition, Vector2.UnitY * Main.rand.NextFloat(1.7f, 2f), ModContent.ProjectileType<LightningStrike>(), 0, 0f);
                        if (Main.projectile.IndexInRange(lightning))
                        {
                            Main.projectile[lightning].ai[0] = MathHelper.PiOver2;
                            Main.projectile[lightning].ai[1] = Main.rand.Next(100);
                        }
                    }
                }
            }

            frameType = (int)PrimeFrameType.ClosedMouth;

            if (attackTimer > 36f && struckByLightningFlag == 0f)
                attackTimer = 36f;
            else if (struckByLightningFlag == 1f)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 320f, -270f) - npc.velocity * 4f;
                float movementSpeed = MathHelper.Lerp(6f, 4.5f, Utils.InverseLerp(45f, 90f, attackTimer, true));
                npc.velocity = (npc.velocity * 6f + npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), movementSpeed)) / 7f;
                npc.rotation = npc.velocity.X * 0.04f;

                if (npc.WithinRange(target.Center, 150f))
                {
                    npc.Center = target.Center - npc.SafeDirectionTo(target.Center, Vector2.UnitY) * 150f;
                    npc.velocity = Vector2.Zero;
                }

                superchargeTimer++;

                // Roar as a telegraph.
                if (attackTimer == 140f)
                    Main.PlaySound(SoundID.Roar, target.Center, 0);
                if (attackTimer > 95f)
                    frameType = (int)PrimeFrameType.OpenMouth;

                float shootSpeedAdditive = npc.Distance(target.Center) * 0.0084f;

                // Fire 9 lasers outward. They intentionally avoid intersecting the player's position and do not rotate.
                // Their purpose is to act as a "border".
                if (attackTimer == 165f)
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 9; i++)
                        {
                            Vector2 laserFirePosition = npc.Center - Vector2.UnitY * 16f;
                            Vector2 laserDirection = (target.Center - laserFirePosition).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.TwoPi * (i + 0.5f) / 9f);

                            int beam = Utilities.NewProjectileBetter(laserFirePosition, laserDirection, ModContent.ProjectileType<LaserRayIdle>(), 140, 0f);
                            if (Main.projectile.IndexInRange(beam))
                            {
                                Main.projectile[beam].ai[0] = 0f;
                                Main.projectile[beam].ai[1] = npc.whoAmI;
                                Main.projectile[beam].netUpdate = true;
                            }
                        }
                    }
                }

                if (attackTimer > 165f)
                    frameType = (int)PrimeFrameType.Spikes;

                // Release electric sparks periodically, along with missiles.
                Vector2 mouthPosition = npc.Center + Vector2.UnitY * 33f;
                if (attackTimer > 180f && attackTimer < 435f && attackTimer % 44f == 43f)
                {
                    Main.PlaySound(SoundID.Item12, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 12; i++)
                        {
                            Vector2 electricityVelocity = (target.Center - mouthPosition).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.TwoPi * i / 12f) * (shootSpeedAdditive + 9f);
                            Utilities.NewProjectileBetter(mouthPosition, electricityVelocity, ProjectileID.MartianTurretBolt, 110, 0f);
                        }
                    }
                }
                if (attackTimer > 180f && attackTimer < 435f && attackTimer % 30f == 29f)
                {
                    Main.PlaySound(SoundID.Item42, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 rocketVelocity = (target.Center - mouthPosition).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.47f) * (shootSpeedAdditive + 6.75f);
                        Utilities.NewProjectileBetter(mouthPosition, rocketVelocity, ProjectileID.SaucerMissile, 115, 0f);
                    }
                }
            }

            if (attackTimer > 435f)
                superchargeTimer = Utils.InverseLerp(465f, 435f, attackTimer, true) * 30f;

            if (attackTimer > 465f)
                GotoNextAttackState(npc);
        }
        #endregion Specific Attacks

        #region General Helper Functions
        public static void GotoNextAttackState(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            PrimeAttackType currentAttack = (PrimeAttackType)(int)npc.ai[0];
            PrimeAttackType nextAttack;
            if (!AnyArms)
            {
                if (lifeRatio < 0.5f && Main.rand.NextBool(4) && currentAttack != PrimeAttackType.LightningSupercharge)
                    nextAttack = PrimeAttackType.LightningSupercharge;
                else
                {
                    do
                        nextAttack = Utils.SelectRandom(Main.rand, PrimeAttackType.MetalBurst, PrimeAttackType.RocketRelease, PrimeAttackType.HoverCharge, PrimeAttackType.LaserRay);
                    while (nextAttack == currentAttack);
                }
            }
            else
            {
                do
                    nextAttack = Utils.SelectRandom(Main.rand, PrimeAttackType.MetalBurst, PrimeAttackType.RocketRelease);
                while (nextAttack == currentAttack);
            }

            npc.ai[0] = (int)nextAttack;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }
        #endregion General Helper Function

        #endregion AI

        #region Frames and Drawcode
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.npcTexture[npc.type];
            Texture2D eyeGlowTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/PrimeEyes");
            Rectangle frame = texture.Frame(1, Main.npcFrameCount[npc.type], 0, (int)npc.localAI[0]);
            Vector2 baseDrawPosition = npc.Center - Main.screenPosition;
            for (int i = 9; i >= 0; i -= 2)
            {
                Vector2 drawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                Color afterimageColor = npc.GetAlpha(lightColor);
                afterimageColor.R = (byte)(afterimageColor.R * (10 - i) / 20);
                afterimageColor.G = (byte)(afterimageColor.G * (10 - i) / 20);
                afterimageColor.B = (byte)(afterimageColor.B * (10 - i) / 20);
                afterimageColor.A = (byte)(afterimageColor.A * (10 - i) / 20);
                spriteBatch.Draw(Main.npcTexture[npc.type], drawPosition, frame, afterimageColor, npc.rotation, frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            }

            float superchargePower = Utils.InverseLerp(0f, 30f, npc.Infernum().ExtraAI[1], true);
            if (superchargePower > 0f)
            {
                float outwardness = superchargePower * 6f + (float)Math.Cos(Main.GlobalTime * 2f) * 0.5f;
                for (int i = 0; i < 8; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 8f + Main.GlobalTime * 2.9f).ToRotationVector2() * outwardness;
                    Color drawColor = Color.Red * 0.42f;
                    drawColor.A = 0;

                    spriteBatch.Draw(texture, baseDrawPosition + drawOffset, frame, npc.GetAlpha(drawColor), npc.rotation, frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
                }
            }

            spriteBatch.Draw(texture, baseDrawPosition, frame, npc.GetAlpha(lightColor), npc.rotation, frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(eyeGlowTexture, baseDrawPosition, frame, new Color(200, 200, 200, 255), npc.rotation, frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            return false;
        }
        #endregion Frames and Drawcode
    }
}