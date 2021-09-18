using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.BoC
{
    public class BoCBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.BrainofCthulhu;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        #region Enumerations
        internal enum BoCAttackState
		{
            IdlyFloat,
            DiagonalCharge,
            BloodDashSwoop,
            CreeperBloodDripping,
            ConvergingIllusions,
            PsionicBombardment,
            SpinPull
		}
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            NPC.crimsonBoss = npc.whoAmI;

            // Emit a crimson light idly.
            Lighting.AddLight(npc.Center, Color.Crimson.ToVector3());

            // Select a new target if an old one was lost.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
                npc.TargetClosest();

            // Reset damage. Do none by default if somewhat transparent.
            npc.damage = npc.alpha > 30 ? 0 : npc.defDamage;

            // If none was found or it was too far away, despawn.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead ||
                !Main.player[npc.target].active || !npc.WithinRange(Main.player[npc.target].Center, 3400f))
            {
                DoDespawnEffects(npc);
                return false;
            }

            Player target = Main.player[npc.target];

            npc.dontTakeDamage = !target.ZoneCrimson && !target.ZoneCorrupt;

            int creeperCount = 8;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float hasCreatedCreepersFlag = ref npc.localAI[0];

            // Summon creepers.
            if (Main.netMode != NetmodeID.MultiplayerClient && hasCreatedCreepersFlag == 0f)
            {
                for (int i = 0; i < creeperCount; i++)
                {
                    Point spawnPosition = (npc.position + npc.Size * Main.rand.NextVector2Square(0f, 1f)).ToPoint();
                    int creeperAwMan = NPC.NewNPC(spawnPosition.X, spawnPosition.Y, NPCID.Creeper, ai0: i / (float)creeperCount);
                    if (Main.npc.IndexInRange(creeperAwMan))
                        Main.npc[creeperAwMan].velocity = Main.rand.NextVector2Circular(3f, 3f);
                }
                hasCreatedCreepersFlag = 1f;
            }

            switch ((BoCAttackState)(int)attackType)
            {
                case BoCAttackState.IdlyFloat:
                    DoAttack_IdlyFloat(npc, target, ref attackTimer);
                    break;
                case BoCAttackState.DiagonalCharge:
                    DoAttack_DiagonalCharge(npc, target, ref attackTimer);
                    break;
                case BoCAttackState.BloodDashSwoop:
                    DoAttack_BloodDashSwoop(npc, target, ref attackTimer);
                    break;
                case BoCAttackState.CreeperBloodDripping:
                    DoAttack_CreeperBloodDripping(npc, target, ref attackTimer);
                    break;
                case BoCAttackState.ConvergingIllusions:
                    DoAttack_ConvergingIllusions(npc, target, ref attackTimer);
                    break;
                case BoCAttackState.PsionicBombardment:
                    DoAttack_PsionicBombardment(npc, target, ref attackTimer);
                    break;
                case BoCAttackState.SpinPull:
                    DoAttack_SpinPull(npc, target, ref attackTimer);
                    break;
            }
            attackTimer++;
            return false;
        }

        #region Specific Attacks
        internal static void DoDespawnEffects(NPC npc)
		{
            npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 15f, 0.15f);
            npc.alpha = Utils.Clamp(npc.alpha + 20, 0, 255);
            npc.damage = 0;
            if (npc.timeLeft > 60)
                npc.timeLeft = 60;
        }
        
        internal static void DoAttack_IdlyFloat(NPC npc, Player target, ref float attackTimer)
		{
            float lifeRatio = npc.life / (float)npc.lifeMax;

            int teleportFadeTime = 50;
            int floatTime = 320;
            float teleportOffset = MathHelper.Lerp(540f, 400f, 1f - lifeRatio);
            if (!DoTeleportFadeEffect(npc, attackTimer, target.Center + Main.rand.NextVector2CircularEdge(teleportOffset, teleportOffset), teleportFadeTime))
                return;

            float floatSpeed = MathHelper.Lerp(5.3f, 7.5f, 1f - lifeRatio);
            npc.velocity = npc.SafeDirectionTo(target.Center) * floatSpeed;

            // Stick to the target if close to them.
            if (npc.WithinRange(target.Center, 10f))
            {
                npc.velocity = Vector2.Zero;

                // Make the attack go much faster though to prevent annoying telefragging.
                attackTimer += 18f;
            }

            if (attackTimer >= floatTime + teleportFadeTime * 1.5f)
                GotoNextAttackState(npc);
		}

        internal static void DoAttack_DiagonalCharge(NPC npc, Player target, ref float attackTimer)
        {
            int teleportFadeTime = 30;
            float horizontalTeleportDirection = -Math.Sign(target.velocity.X);
            if (horizontalTeleportDirection == 0f)
                horizontalTeleportDirection = Main.rand.NextBool(2).ToDirectionInt();
            ref float canFloatFlag = ref npc.Infernum().ExtraAI[0];
            Vector2 teleportDestination = target.Center + new Vector2(horizontalTeleportDirection * 400f, -210f);
            if (!DoTeleportFadeEffect(npc, attackTimer, teleportDestination, teleportFadeTime))
                return;

            if (canFloatFlag == 1f)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * Utils.InverseLerp(120f, 108f, attackTimer, true) * 4f, 0.125f);
                if (attackTimer >= 120f)
                    GotoNextAttackState(npc);
                return;
            }
            if (attackTimer == teleportFadeTime + 25f)
            {
                Main.PlaySound(SoundID.Roar, target.Center, 0);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center, Vector2.UnitY) * 14f;
                    npc.netUpdate = true;
                }
            }
 
            if (attackTimer > teleportFadeTime + 25f)
            {
                if (attackTimer <= teleportFadeTime + 80f)
                {
                    npc.velocity *= 1.0065f;

                    // Release ichor everywhere.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 5f == 4f)
                    {
                        Vector2 shootVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 7.5f);
                        Vector2 spawnPosition = npc.Center + Main.rand.NextVector2Circular(40f, 40f);
                        int ichor = Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<IchorSpit>(), 50, 0f);
                        if (Main.projectile.IndexInRange(ichor))
                            Main.projectile[ichor].ai[1] = 1f;
                    }
                }
                else
                {
                    npc.velocity *= 0.95f;
                    if (npc.velocity.Length() < 1.25f && canFloatFlag == 0f)
                    {
                        canFloatFlag = 1f;
                        attackTimer = 60f;
                        npc.netUpdate = true;
                    }
                }
            }
        }

        internal static void DoAttack_BloodDashSwoop(NPC npc, Player target, ref float attackTimer)
        {
            int teleportFadeTime = 46;
            Vector2 teleportDestination = target.Center + new Vector2(target.direction * -350f, -280f);
            if (Math.Abs(target.velocity.X) > 0f)
                teleportDestination = target.Center + new Vector2(Math.Sign(target.velocity.X) * -310f, -280f);
            if (!DoTeleportFadeEffect(npc, attackTimer, teleportDestination, teleportFadeTime))
                return;

            if (attackTimer == teleportFadeTime + 10f)
            {
                Main.PlaySound(SoundID.Roar, target.Center, 0);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.velocity = Vector2.UnitY * 14f;
                    npc.direction = (target.Center.X < npc.Center.X).ToDirectionInt();
                    npc.netUpdate = true;

                    for (int i = 0; i < 30; i++)
                    {
                        Vector2 spawnPosition = npc.Center - Vector2.UnitY.RotatedByRandom(0.42f) * 12f;
                        Vector2 bloodVelocity = Utilities.GetProjectilePhysicsFiringVelocity(spawnPosition, target.Center, BloodGeyser2.Gravity, Main.rand.NextFloat(12f, 14f), out _);
                        bloodVelocity = bloodVelocity.RotatedByRandom(0.78f);

                        Utilities.NewProjectileBetter(spawnPosition, bloodVelocity, ModContent.ProjectileType<BloodGeyser2>(), 50, 0f);
                    }
                }
            }

            // Swoop downward.
            if (attackTimer > teleportFadeTime + 10f)
            {
                if (Math.Abs(Vector2.Dot(npc.velocity.SafeNormalize(Vector2.Zero), Vector2.UnitX)) < 0.96f)
                    npc.velocity = npc.velocity.RotatedBy(MathHelper.ToRadians(npc.direction * 2f));
                else
                {
                    npc.velocity.X *= 0.988f;
                    npc.velocity.Y *= 0.96f;
                }

                if (attackTimer > teleportFadeTime + 140f)
                    GotoNextAttackState(npc);
            }
        }

        internal static void DoAttack_CreeperBloodDripping(NPC npc, Player target, ref float attackTimer)
        {
            int teleportFadeTime = 54;
            Vector2 teleportDestination = target.Center + Main.rand.NextVector2CircularEdge(340f, 340f);
            if (!DoTeleportFadeEffect(npc, attackTimer, teleportDestination, teleportFadeTime))
                return;

            // Creepers do most of the interesting stuff with this attack.
            npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * (float)Math.Sin((attackTimer - 54f) / 24f) * 6f, 0.007f);

            if (attackTimer >= 380f)
                GotoNextAttackState(npc);
        }

        internal static void DoAttack_ConvergingIllusions(NPC npc, Player target, ref float attackTimer)
        {
            ref float incorrectHitFlag = ref npc.Infernum().ExtraAI[0];
            ref float reelBackCountdown = ref npc.Infernum().ExtraAI[1];
            ref float spawnOffsetX = ref npc.Infernum().ExtraAI[2];
            ref float spawnOffsetY = ref npc.Infernum().ExtraAI[3];

            npc.dontTakeDamage = attackTimer < 100f || incorrectHitFlag == 1f || npc.Opacity < 0.6f;
            if (reelBackCountdown > 0)
            {
                reelBackCountdown--;
                float idealOpacity = 1f - Utils.InverseLerp(45f, 5f, reelBackCountdown, true);
                npc.velocity *= 0.98f;
                npc.Opacity = MathHelper.Lerp(npc.Opacity, idealOpacity, 0.25f);
                if (reelBackCountdown == 1f)
                    GotoNextAttackState(npc);
                return;
            }

            int teleportFadeTime = 35;
            Vector2 teleportDestination = target.Center + Vector2.UnitY * 435f;
            if (!DoTeleportFadeEffect(npc, attackTimer, teleportDestination, teleportFadeTime))
            {
                if (attackTimer >= teleportFadeTime && attackTimer <= 95f && npc.Opacity > 0.7f)
                    npc.Opacity = 0.7f;
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == teleportFadeTime + 1f)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector2 illusionVelocity = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 8f;
                    int illusion = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<BrainIllusion>());
                    Main.npc[illusion].velocity = illusionVelocity;
                }
            }

            float covergenceStartOutwardness = 640f;
            float baseConvergenceSpeed = covergenceStartOutwardness / 150f;
            if (attackTimer >= 90f && attackTimer <= 110f)
            {
                npc.Opacity = Utils.Clamp(npc.Opacity - 0.05f, 0f, 1f);
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 100f)
                {
                    float baseOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    int offsetIndex = 0;

                    int illusionType = ModContent.NPCType<BrainIllusion>();
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        bool incorrectType = Main.npc[i].type != illusionType && Main.npc[i].type != NPCID.BrainofCthulhu;
                        if (incorrectType || !Main.npc[i].active)
                            continue;
                        Vector2 positionOffset = (offsetIndex / 9f * MathHelper.TwoPi + baseOffsetAngle).ToRotationVector2() * covergenceStartOutwardness;
                        Main.npc[i].Center = target.Center + positionOffset;
                        Main.npc[i].velocity = Main.npc[i].DirectionTo(target.Center) * baseConvergenceSpeed;

                        if (i != npc.whoAmI)
                            Main.npc[i].ai[1] = positionOffset.ToRotation();
                        else
                        {
                            spawnOffsetX = positionOffset.X;
                            spawnOffsetY = positionOffset.Y;
                        }
                        Main.npc[i].netUpdate = true;

                        offsetIndex++;
                    }
                    npc.netUpdate = true;
                }
            }
            else
                npc.Opacity = MathHelper.Lerp(npc.Opacity, 1f, 0.05f);

            if (attackTimer >= 110f)
            {
                // Reel back and fade out if the player hit the correct brain.
                if (npc.justHit && reelBackCountdown == 0f)
                {
                    reelBackCountdown = 45f;
                    npc.velocity = npc.DirectionTo(target.Center) * -8f;
                    Main.PlaySound(SoundID.ForceRoar, (int)target.Center.X, (int)target.Center.Y, -1, 1f, 0f);
                }

                Vector2 newSpawnOffset = new Vector2(spawnOffsetX, spawnOffsetY);
                npc.Center = target.Center + newSpawnOffset;
                newSpawnOffset += npc.velocity;
                spawnOffsetX = newSpawnOffset.X;
                spawnOffsetY = newSpawnOffset.Y;

                if (attackTimer >= 420f || npc.Hitbox.Intersects(target.Hitbox))
                    GotoNextAttackState(npc);
            }
        }

        internal static void DoAttack_PsionicBombardment(NPC npc, Player target, ref float attackTimer)
        {
            int teleportFadeTime = 50;
            Vector2 teleportDestination = target.Center - Vector2.UnitY * 350f;
            if (!DoTeleportFadeEffect(npc, attackTimer, teleportDestination, teleportFadeTime))
                return;

            npc.Opacity = MathHelper.Lerp(npc.Opacity, 1f, 0.1f);
            if (attackTimer >= 70f)
                npc.velocity *= 0.94f;
            ref float cyanAuraStrength = ref npc.localAI[1];
            cyanAuraStrength = Utils.InverseLerp(105f, 125f, attackTimer, true) * Utils.InverseLerp(445f, 425f, attackTimer, true);

            float lifeRatio = npc.life / (float)npc.lifeMax;
            if (attackTimer == 130f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnPosition = npc.Top + Vector2.UnitY * 16f;
                    bool shouldUseUndergroundAI = target.Center.Y / 16f < Main.worldSurface || Collision.SolidCollision(npc.Center - Vector2.One * 24f, 48, 48);
                    if (lifeRatio < 0.2f)
                    {
                        int orb = Utilities.NewProjectileBetter(spawnPosition, Vector2.UnitY.RotatedBy(-0.17f) * -5f, ModContent.ProjectileType<PsionicOrb>(), 56, 0f);
                        if (Main.projectile.IndexInRange(orb))
                            Main.projectile[orb].localAI[0] = shouldUseUndergroundAI.ToInt();
                        orb = Utilities.NewProjectileBetter(spawnPosition, Vector2.UnitY.RotatedBy(0.17f) * -5f, ModContent.ProjectileType<PsionicOrb>(), 56, 0f);
                        if (Main.projectile.IndexInRange(orb))
                            Main.projectile[orb].localAI[0] = shouldUseUndergroundAI.ToInt();
                    }
                    else
                    {
                        int orb = Utilities.NewProjectileBetter(spawnPosition, Vector2.UnitY * -6f, ModContent.ProjectileType<PsionicOrb>(), 56, 0f);
                        if (Main.projectile.IndexInRange(orb))
                            Main.projectile[orb].localAI[0] = shouldUseUndergroundAI.ToInt();
                    }
                }
                Main.PlaySound(SoundID.Item92, target.Center);
            }

            if (attackTimer >= 450f)
                GotoNextAttackState(npc);
        }

        internal static void DoAttack_SpinPull(NPC npc, Player target, ref float attackTimer)
        {
            int teleportFadeTime = 50;
            float spinRadius = 395f;
            float spinTime = 120f;
            ref float spinAngle = ref npc.Infernum().ExtraAI[0];
            if (attackTimer == 1f)
                spinAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            Vector2 teleportDestination = target.Center - Vector2.UnitY.RotatedBy(spinAngle) * spinRadius;
            if (!DoTeleportFadeEffect(npc, attackTimer, teleportDestination, teleportFadeTime))
                return;

            if (attackTimer > teleportFadeTime * 1.5f)
                spinAngle += MathHelper.TwoPi * 2f / spinTime * Utils.InverseLerp(teleportFadeTime * 1.5f + spinTime, teleportFadeTime * 1.5f + spinTime - 30f, attackTimer, true);

            npc.localAI[1] = (float)Math.Sin(Utils.InverseLerp((int)(teleportFadeTime * 1.5f) + spinTime - 20f, (int)(teleportFadeTime * 1.5f) + spinTime + 45f, attackTimer, true) * MathHelper.Pi);
            if (attackTimer == (int)(teleportFadeTime * 1.5f) + spinTime + 15f)
            {
                npc.velocity = npc.SafeDirectionTo(target.Center) * 25f;
                Main.PlaySound(SoundID.ForceRoar, (int)target.Center.X, (int)target.Center.Y, -1, 1f, 0f);
            }

            if (attackTimer < (int)(teleportFadeTime * 1.5f) + spinTime)
                npc.Center = teleportDestination;
            else
                npc.velocity *= 0.98f;

            if (attackTimer >= teleportFadeTime * 1.5f + spinTime + 70f)
            {
                npc.velocity *= 0.9f;
                npc.Opacity -= 0.05f;
                if (npc.Opacity <= 0.5f)
                    GotoNextAttackState(npc);
            }
        }
        #endregion Specific Attacks

        #region AI Utility Methods

        internal const float Subphase2LifeRatio = 0.75f;
        internal const float Subphase3LifeRatio = 0.4f;
        internal static void GotoNextAttackState(NPC npc)
        {
            npc.Opacity = 0f;
            float lifeRatio = npc.life / (float)npc.lifeMax;

            BoCAttackState oldAttackType = (BoCAttackState)(int)npc.ai[0];
            BoCAttackState newAttackType = BoCAttackState.IdlyFloat;
            switch (oldAttackType)
            {
                case BoCAttackState.IdlyFloat:
                    newAttackType = lifeRatio < Subphase2LifeRatio ? BoCAttackState.DiagonalCharge : BoCAttackState.BloodDashSwoop;
                    break;
                case BoCAttackState.DiagonalCharge:
                    newAttackType = BoCAttackState.BloodDashSwoop;
                    break;
                case BoCAttackState.BloodDashSwoop:
                    newAttackType = BoCAttackState.CreeperBloodDripping;
                    break;
                case BoCAttackState.CreeperBloodDripping:
                    newAttackType = lifeRatio < Subphase3LifeRatio ? (Main.rand.NextBool() ? BoCAttackState.PsionicBombardment : BoCAttackState.ConvergingIllusions) : BoCAttackState.IdlyFloat;
                    break;
                case BoCAttackState.ConvergingIllusions:
                    newAttackType = BoCAttackState.PsionicBombardment;
                    break;
                case BoCAttackState.PsionicBombardment:
                    newAttackType = BoCAttackState.SpinPull;
                    break;
                case BoCAttackState.SpinPull:
                    newAttackType = Main.rand.NextBool(2) ? BoCAttackState.DiagonalCharge : BoCAttackState.BloodDashSwoop;
                    break;
            }

            npc.ai[0] = (int)newAttackType;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        internal static bool DoTeleportFadeEffect(NPC npc, float time, Vector2 teleportDestination, int teleportFadeTime)
        {
            // Fade out and teleport after a bit.
            if (time <= teleportFadeTime)
            {
                npc.Opacity = MathHelper.Lerp(1f, 0f, time / teleportFadeTime);

                // Teleport when completely transparent.
                if (Main.netMode != NetmodeID.MultiplayerClient && time == teleportFadeTime)
                {
                    npc.Center = teleportDestination;

                    // And bring creepers along with because their re-adjustment motion in the base game is unpredictable and unpleasant.
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].type != NPCID.Creeper || !Main.npc[i].active)
                            continue;

                        Main.npc[i].Center = npc.Center + Main.rand.NextVector2CircularEdge(3f, 3f);
                        Main.npc[i].netUpdate = true;
                    }
                    npc.netUpdate = true;
                }
                npc.velocity *= 0.94f;
                return false;
            }

            // Fade back in after teleporting.
            if (time > teleportFadeTime && time <= teleportFadeTime * 1.5f)
                npc.Opacity = MathHelper.Lerp(0f, 1f, Utils.InverseLerp(teleportFadeTime, teleportFadeTime * 1.5f, time, true));
            return true;
        }
        #endregion AI Utility Methods

        #endregion AI

        #region Drawing

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.npcTexture[npc.type];
            Rectangle frame = npc.frame;
            frame.Y += texture.Height / Main.npcFrameCount[npc.type] * 4;

            void drawInstance(Vector2 drawPosition, Color color, float scale)
            {
                drawPosition -= Main.screenPosition;
                spriteBatch.Draw(texture, drawPosition, frame, color, npc.rotation, frame.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }

            float cyanAuraStrength = npc.localAI[1];
            if (cyanAuraStrength > 0f)
            {
                float scale = npc.scale * MathHelper.Lerp(0.9f, 1.06f, cyanAuraStrength);
                Color auraColor = Color.Lerp(Color.Transparent, Color.Cyan, cyanAuraStrength) * npc.Opacity * 0.3f;
                auraColor.A = 0;

                for (int i = 0; i < 7; i++)
                {
                    Vector2 drawPosition = npc.Center + (MathHelper.TwoPi * i / 7f + Main.GlobalTime * 4.3f).ToRotationVector2() * cyanAuraStrength * 4f;
                    drawInstance(drawPosition, auraColor, scale);
                }
            }
            drawInstance(npc.Center, npc.GetAlpha(lightColor), npc.scale);
            return false;
        }

        #endregion
    }
}
