using CalamityMod;
using CalamityMod.NPCs.AdultEidolonWyrm;
using CalamityMod.Sounds;
using InfernumMode.Content.BehaviorOverrides.AbyssAIs;
using InfernumMode.Core.OverridingSystem;
using InfernumMode.Projectiles;
using InfernumMode.Assets.Sounds;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using InfernumMode.Content.WorldGeneration;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AEWHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum AEWAttackType
        {
            // Spawn animation states.
            SnatchTerminus,
            ThreateninglyHoverNearPlayer,

            // Light attacks.
            BurningGaze,

            // Neutral attacks.
            SplitFormCharges,

            // Enrage attack.
            RuthlesslyMurderTarget
        }

        public override int NPCOverrideType => ModContent.NPCType<AdultEidolonWyrmHead>();

        #region AI
        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio,
            Phase4LifeRatio,
            Phase5LifeRatio
        };

        // Projectile damage values.
        public const int NormalShotDamage = 540;

        public const int StrongerNormalShotDamage = 560;

        public const int PowerfulShotDamage = 850;

        public const float Phase2LifeRatio = 0.8f;

        public const float Phase3LifeRatio = 0.6f;

        public const float Phase4LifeRatio = 0.35f;

        public const float Phase5LifeRatio = 0.1f;

        public const int EyeGlowOpacityIndex = 5;

        public const int SuperEnrageFormAestheticInterpolantIndex = 6;

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float initializedFlag = ref npc.ai[2];
            ref float superEnrageTimer = ref npc.ai[3];
            ref float eyeGlowOpacity = ref npc.Infernum().ExtraAI[EyeGlowOpacityIndex];
            ref float superEnrageFormAestheticInterpolant = ref npc.Infernum().ExtraAI[SuperEnrageFormAestheticInterpolantIndex];

            if (Main.netMode != NetmodeID.MultiplayerClient && initializedFlag == 0f)
            {
                CreateSegments(npc, 125, ModContent.NPCType<AdultEidolonWyrmBody>(), ModContent.NPCType<AdultEidolonWyrmBodyAlt>(), ModContent.NPCType<AdultEidolonWyrmTail>());
                initializedFlag = 1f;
                npc.netUpdate = true;
            }
            
            // If there still was no valid target, swim away.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active || !Main.player[npc.target].WithinRange(npc.Center, 18000f))
            {
                DoBehavior_Despawn(npc);
                return false;
            }

            Player target = Main.player[npc.target];
            bool targetNeedsDeath = superEnrageTimer >= 1f || target.chaosState;

            // Disable obnoxious water mechanics so that the player can fight the boss without interruption.
            target.breath = target.breathMax;
            target.ignoreWater = true;
            target.wingTime = target.wingTimeMax;
            AbyssWaterColorSystem.WaterBlacknessInterpolant = 0f;

            // Reset various things every frame.
            npc.dontTakeDamage = false;
            npc.defDamage = 600;
            npc.damage = npc.defDamage;

            // This is necessary to allow the boss effects buff to be shown.
            npc.Calamity().KillTime = 1;

            // Why are you despawning?
            npc.boss = true;
            npc.timeLeft = 7200;

            switch ((AEWAttackType)attackType)
            {
                case AEWAttackType.SnatchTerminus:
                    DoBehavior_SnatchTerminus(npc);
                    break;
                case AEWAttackType.ThreateninglyHoverNearPlayer:
                    DoBehavior_ThreateninglyHoverNearPlayer(npc, target, ref eyeGlowOpacity, ref attackTimer);
                    break;
                case AEWAttackType.BurningGaze:
                    DoBehavior_BurningGaze(npc, target, ref attackTimer);
                    break;
                case AEWAttackType.SplitFormCharges:
                    DoBehavior_SplitFormCharges(npc, target, ref attackTimer);
                    break;
                case AEWAttackType.RuthlesslyMurderTarget:
                    DoBehavior_RuthlesslyMurderTarget(npc, target, ref attackTimer);
                    break;
            }

            // Determine rotation based on the current velocity.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Increment the attack timer.
            attackTimer++;

            // Increment the super-enrage timer if it's activated.
            // If the player is somehow not dead after enough time has passed they're just manually killed.
            if (targetNeedsDeath)
                superEnrageTimer++;
            if (superEnrageTimer >= 1800f)
                target.KillMe(PlayerDeathReason.ByNPC(npc.whoAmI), 1000000D, 0);

            // Transform into a being of light if the super-enrage form is active.
            // This happens gradually under normal circumstances but is instantaneous if enraged with the RoD on the first frame.
            superEnrageFormAestheticInterpolant = Utils.GetLerpValue(0f, 60f, superEnrageTimer, true);
            if (attackType != (int)AEWAttackType.RuthlesslyMurderTarget && targetNeedsDeath)
            {
                if (attackType == (int)AEWAttackType.ThreateninglyHoverNearPlayer && attackTimer <= 10f && target.chaosState)
                {
                    superEnrageTimer = 60f;
                    superEnrageFormAestheticInterpolant = 1f;
                }

                SelectNextAttack(npc);
                attackType = (int)AEWAttackType.RuthlesslyMurderTarget;
                npc.netUpdate = true;
            }

            return false;
        }
        #endregion AI

        #region Specific Behaviors

        public static void DoBehavior_Despawn(NPC npc)
        {
            npc.velocity.X *= 0.985f;
            if (npc.velocity.Y < 72f)
                npc.velocity.Y += 1.3f;

            if (npc.timeLeft > 210)
                npc.timeLeft = 210;
        }

        public static void DoBehavior_SnatchTerminus(NPC npc)
        {
            float chargeSpeed = 41f;
            List<Projectile> terminusInstances = Utilities.AllProjectilesByID(ModContent.ProjectileType<TerminusAnimationProj>()).ToList();

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.08f, 0f, 1f);

            // Transition to the next attack if there are no more Terminus instances.
            if (terminusInstances.Count <= 0)
            {
                SelectNextAttack(npc);
                return;
            }

            Projectile target = terminusInstances.First();

            // Fly very, very quickly towards the Terminus.
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * chargeSpeed, 0.16f);

            // Delete the Terminus instance if it's being touched.
            // On the next frame the AEW will transition to the next attack, assuming there isn't another Terminus instance for some weird reason.
            if (npc.WithinRange(target.Center, 90f))
            {
                SoundEngine.PlaySound(SoundID.DD2_SkyDragonsFuryShot with { Volume = 1.3f }, target.Center);
                target.Kill();
            }
        }

        public static void DoBehavior_ThreateninglyHoverNearPlayer(NPC npc, Player target, ref float eyeGlowOpacity, ref float attackTimer)
        {
            int roarDelay = 60;
            int eyeGlowFadeinTime = 105;
            int attackTransitionDelay = 210;
            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 450f, -360f);
            ref float hasReachedDestination = ref npc.Infernum().ExtraAI[0];

            // Attempt to hover to the top left/right of the target at first.
            if (hasReachedDestination == 0f)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 32f, 0.084f);
                if (npc.WithinRange(hoverDestination, 96f))
                {
                    hasReachedDestination = 1f;
                    npc.netUpdate = true;
                }

                // Don't let the attack timer increment.
                attackTimer = -1f;

                return;
            }

            // Roar after a short delay.
            if (attackTimer == roarDelay)
                SoundEngine.PlaySound(InfernumSoundRegistry.AEWThreatenRoar);

            // Slow down and look at the target threateningly before attacking.
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 3f, 0.071f);

            // Make the eye glowmask gradually fade in.
            eyeGlowOpacity = Utils.GetLerpValue(0f, eyeGlowFadeinTime, attackTimer, true);

            if (attackTimer >= roarDelay + attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_BurningGaze(NPC npc, Player target, ref float attackTimer)
        {
            int boltShootDelay = 36;
            int boltCountPerBurst = 5;
            int telegraphTime = 27;
            float boltShootSpeed = 1.6f;
            ref float hasReachedDestination = ref npc.Infernum().ExtraAI[0];
            ref float horizontalHoverOffsetDirection = ref npc.Infernum().ExtraAI[1];
            ref float generalAttackTimer = ref npc.Infernum().ExtraAI[2];

            // Attempt to hover to the top left/right of the target at first.
            if (hasReachedDestination == 0f)
            {
                // Disable contact damage to prevent cheap hits.
                npc.damage = 0;

                if (horizontalHoverOffsetDirection == 0f)
                {
                    horizontalHoverOffsetDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
                    npc.netUpdate = true;
                }

                Vector2 hoverDestination = target.Center + new Vector2(horizontalHoverOffsetDirection * 600f, -360f);

                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 64f, 0.12f);
                if (npc.WithinRange(hoverDestination, 96f))
                {
                    hasReachedDestination = 1f;
                    npc.netUpdate = true;
                }

                // Don't let the attack timer increment.
                attackTimer = -1f;
                return;
            }

            // Slow down and look at the target threateningly.
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 2f, 0.08f);
            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.18f);

            // Create a buffer before the attack properly begins, to ensure that the player can reasonably react to it.
            generalAttackTimer++;
            if (generalAttackTimer < 90f)
                attackTimer = 0f;

            // Release eye bursts.
            if (attackTimer >= boltShootDelay)
            {
                GetEyePositions(npc, out Vector2 left, out Vector2 right);
                SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int telegraphID = ModContent.ProjectileType<AEWTelegraphLine>();
                    int boltID = ModContent.ProjectileType<DivineLightBolt>();
                    for (int i = 0; i < boltCountPerBurst; i++)
                    {
                        // Be careful when adjusting the range of the offset angle. If it isn't just right then the attack might be invalidated by just literally
                        // standing still and letting the bolts fly away.
                        float shootOffsetAngle = MathHelper.Lerp(-0.51f, 0.51f, i / (float)(boltCountPerBurst - 1f));
                        Vector2 leftVelocity = npc.SafeDirectionTo(left).RotatedBy(shootOffsetAngle) * boltShootSpeed;
                        Vector2 rightVelocity = npc.SafeDirectionTo(right).RotatedBy(shootOffsetAngle) * boltShootSpeed;

                        int telegraph = Utilities.NewProjectileBetter(left, leftVelocity, telegraphID, 0, 0f, -1, 0f, telegraphTime);
                        if (Main.projectile.IndexInRange(telegraph))
                            Main.projectile[telegraph].localAI[1] = 1f;

                        telegraph = Utilities.NewProjectileBetter(right, rightVelocity, telegraphID, 0, 0f, -1, 0f, telegraphTime);
                        if (Main.projectile.IndexInRange(telegraph))
                            Main.projectile[telegraph].localAI[1] = 1f;

                        Utilities.NewProjectileBetter(left, leftVelocity, boltID, NormalShotDamage, 0f);
                        Utilities.NewProjectileBetter(right, rightVelocity, boltID, NormalShotDamage, 0f);
                    }

                    attackTimer = 0f;
                    hasReachedDestination = 0f;
                    horizontalHoverOffsetDirection *= -1f;
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoBehavior_SplitFormCharges(NPC npc, Player target, ref float attackTimer)
        {
            int swimTime = 105;
            int telegraphTime = 42;
            int chargeTime = 45;
            int chargeCount = 5;
            int attackCycleTime = telegraphTime + chargeTime;
            int chargeCounter = (int)(attackTimer - swimTime) / attackCycleTime;
            float attackCycleTimer = (attackTimer - swimTime) % attackCycleTime;
            bool shouldStopAttacking = chargeCounter >= chargeCount && !Utilities.AnyProjectiles(ModContent.ProjectileType<AEWSplitForm>());
            ref float verticalSwimDirection = ref npc.Infernum().ExtraAI[0];

            // Don't let the attack cycle timer increment if still swimming.
            if (attackTimer < swimTime)
                attackCycleTimer = 0f;

            // Swim away from the target. If they're close to the bottom of the abyss, swim up. Otherwise, swim down.
            if (verticalSwimDirection == 0f)
            {
                verticalSwimDirection = 1f;
                if (target.Center.Y >= CustomAbyss.AbyssBottom * 16f - 2400f)
                    verticalSwimDirection = -1f;
                
                npc.netUpdate = true;
            }
            else if (!shouldStopAttacking)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * verticalSwimDirection * 105f, 0.1f);

                // Fade out after enough time has passed, in anticipation of the attack.
                npc.Opacity = Utils.GetLerpValue(swimTime - 1f, swimTime - 35f, attackTimer, true);
            }

            // Stay below the target once completely invisible.
            if (npc.Opacity <= 0f)
            {
                npc.Center = target.Center + Vector2.UnitY * verticalSwimDirection * 1600f;
                npc.velocity = -Vector2.UnitY * verticalSwimDirection * 23f;
            }

            // Fade back in if ready to transition to the next attack.
            if (shouldStopAttacking)
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.01f, 0f, 1f);
                if (npc.Opacity >= 1f)
                {
                    Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<EidolistIce>());
                    SelectNextAttack(npc);
                }
            }

            // Cast telegraph direction lines. Once they dissipate the split forms will appear and charge.
            if (attackCycleTimer == 1f && !shouldStopAttacking)
            {
                SoundEngine.PlaySound(SoundID.Item158, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int crossLineID = ModContent.ProjectileType<AEWTelegraphLine>();
                    bool firstCrossIsDark = Main.rand.NextBool();
                    float crossSpawnOffset = 2080f;
                    bool flipHorizontalDirection = target.Center.X < Main.maxTilesX * 16f - 3000f;
                    float directionX = flipHorizontalDirection.ToDirectionInt();
                    switch (chargeCounter % 2)
                    {
                        // Plus-shaped cross.
                        case 0:
                            Utilities.NewProjectileBetter(target.Center + Vector2.UnitX * directionX * crossSpawnOffset, -Vector2.UnitX * directionX, crossLineID, 0, 0f, -1, firstCrossIsDark.ToInt(), telegraphTime);
                            Utilities.NewProjectileBetter(target.Center + Vector2.UnitY * crossSpawnOffset, -Vector2.UnitY, crossLineID, 0, 0f, -1, 1f - firstCrossIsDark.ToInt(), telegraphTime);
                            break;

                        // X-shaped cross.
                        case 1:
                            Utilities.NewProjectileBetter(target.Center + new Vector2(directionX, -1f) * crossSpawnOffset * 0.707f, new(-directionX, 1f), crossLineID, 0, 0f, -1, firstCrossIsDark.ToInt(), telegraphTime);
                            Utilities.NewProjectileBetter(target.Center + new Vector2(directionX, 1f) * crossSpawnOffset * 0.707f, new(-directionX, -1f), crossLineID, 0, 0f, -1, 1f - firstCrossIsDark.ToInt(), telegraphTime);
                            break;
                    }
                }
            }
        }

        public static void DoBehavior_RuthlesslyMurderTarget(NPC npc, Player target, ref float attackTimer)
        {
            float swimSpeed = Utils.Remap(attackTimer, 0f, 95f, 6f, attackTimer * 0.05f + 50f);

            // Scream very, very loudly on the first frame.
            if (attackTimer == 1f)
                SoundEngine.PlaySound(InfernumSoundRegistry.AEWThreatenRoar with { Volume = 3f });

            // Be fully opaque.
            npc.Opacity = 1f;

            // The target must die.
            npc.damage = 25000;
            npc.dontTakeDamage = true;
            npc.Calamity().ShouldCloseHPBar = true;

            if (!npc.WithinRange(target.Center, 300f))
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * swimSpeed, 0.1f);
            else
                npc.velocity *= 1.1f;
        }

        #endregion Specific Behaviors

        #region AI Utility Methods
        public static void GetEyePositions(NPC npc, out Vector2 left, out Vector2 right)
        {
            float normalizedRotation = npc.rotation;
            Vector2 eyeCenterPoint = npc.Center - Vector2.UnitY.RotatedBy(normalizedRotation) * npc.width * npc.scale * 0.19f;
            left = eyeCenterPoint - normalizedRotation.ToRotationVector2() * npc.scale * npc.width * 0.09f;
            right = eyeCenterPoint + normalizedRotation.ToRotationVector2() * npc.scale * npc.width * 0.09f;
        }

        public static void CreateSegments(NPC npc, int wormLength, int bodyType1, int bodyType2, int tailType)
        {
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < wormLength; i++)
            {
                int nextIndex;
                if (i < wormLength - 1)
                {
                    int bodyID = i % 2 == 0 ? bodyType1 : bodyType2;
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, bodyID, npc.whoAmI + 1);
                }
                else
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI + 1);

                Main.npc[nextIndex].realLife = npc.whoAmI;
                Main.npc[nextIndex].ai[2] = npc.whoAmI;
                Main.npc[nextIndex].ai[1] = previousIndex;

                if (i >= 1)
                    Main.npc[previousIndex].ai[0] = nextIndex;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

        internal static void SelectNextAttack(NPC npc)
        {
            AEWAttackType currentAttack = (AEWAttackType)npc.ai[0];
            AEWAttackType nextAttack = currentAttack;

            if (currentAttack == AEWAttackType.SnatchTerminus)
                nextAttack = AEWAttackType.ThreateninglyHoverNearPlayer;
            else if (currentAttack == AEWAttackType.ThreateninglyHoverNearPlayer)
                nextAttack = AEWAttackType.SplitFormCharges;
            else if (currentAttack == AEWAttackType.SplitFormCharges)
                nextAttack = AEWAttackType.BurningGaze;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.ai[0] = (int)nextAttack;
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        #endregion AI Utility Methods

        #region Draw Effects
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            npc.frame = new(0, 0, 254, 138);

            Texture2D eyeTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AdultEidolonWyrm/AEWEyes").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Color eyeColor = Color.Cyan * npc.Opacity * npc.Infernum().ExtraAI[EyeGlowOpacityIndex];

            DrawSegment(npc, lightColor);
            ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(eyeTexture, drawPosition, null, eyeColor, npc.rotation, eyeTexture.Size() * 0.5f, npc.scale, 0, 0));

            return false;
        }

        public static void DrawSegment(NPC npc, Color lightColor)
        {
            string segmentString = "Head";
            if (npc.type == ModContent.NPCType<AdultEidolonWyrmBody>())
                segmentString = "Body";
            if (npc.type == ModContent.NPCType<AdultEidolonWyrmBodyAlt>())
                segmentString = "BodyAlt";
            if (npc.type == ModContent.NPCType<AdultEidolonWyrmTail>())
                segmentString = "Tail";

            // Find the head segment.
            NPC head = npc;
            if (npc.realLife >= 0 && Main.npc[npc.realLife].active && Main.npc[npc.realLife].type == ModContent.NPCType<AdultEidolonWyrmHead>())
                head = Main.npc[npc.realLife];

            // Calculate variables for the super-enrage aesthetic.
            float superEnrageFormAestheticInterpolant = head.Infernum().ExtraAI[SuperEnrageFormAestheticInterpolantIndex];

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmaskTexture = ModContent.Request<Texture2D>($"CalamityMod/NPCs/AdultEidolonWyrm/AdultEidolonWyrm{segmentString}Glow").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;

            // Reset the texture frame for drawing.
            npc.frame = texture.Frame();
            
            // Draw the segment.
            Main.EntitySpriteDraw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0);
            ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Insert(0, new(glowmaskTexture, drawPosition, npc.frame, npc.GetAlpha(Color.White) * 0.65f, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0));
            if (superEnrageFormAestheticInterpolant > 0f)
            {
                Color wispFormColor = new Color(255, 178, 167, 0) * npc.Opacity * superEnrageFormAestheticInterpolant;
                for (int i = 0; i < 5; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 5f + Main.GlobalTimeWrappedHourly * 2.1f + npc.whoAmI + npc.rotation).ToRotationVector2() * superEnrageFormAestheticInterpolant * new Vector2(20f, 8f);
                    ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(texture, drawPosition + drawOffset, npc.frame, wispFormColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0));
                }
            }

            // Hacky way of ensuring that PostDraw doesn't do anything.
            npc.frame = Rectangle.Empty;
        }
        #endregion Draw Effects

        #region Tips

        #endregion Tips
    }
}
