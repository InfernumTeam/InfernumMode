using CalamityMod;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Typeless;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Projectiles.Boss;
using InfernumMode.Assets.Sounds;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

using SCalNPC = CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SupremeCataclysmBehaviorOverride : NPCBehaviorOverride
    {
        public enum SCalBrotherAttackType
        {
            AttackDelay,
            SinusoidalBobbing,
            ProjectileShooting,
            Hyperdashes
        }

        public enum SCalBrotherAnimationType
        {
            HoverInPlace,
            AttackAnimation
        }

        public override int? NPCTypeToDeferToForTips => ModContent.NPCType<SCalNPC>();

        public override int NPCOverrideType => ModContent.NPCType<SupremeCataclysm>();

        #region AI
        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 120;
            npc.height = 120;
            npc.scale = 1f;
            npc.defense = 80;
            npc.DR_NERD(0.25f);
        }

        public override bool PreAI(NPC npc)
        {
            DoAI(npc);
            return false;
        }

        // In this AI, Cataclysm is considered the leader, managing things such as attack states, timers, and life.
        // Catastrophe, on the other hand, simply inherits these properties from Cataclysm. Together, the two share an HP pool,
        // so the concern of Catastrophe having to attack on his own is eliminated.
        public static void DoAI(NPC npc)
        {
            int cataclysmIndex = NPC.FindFirstNPC(ModContent.NPCType<SupremeCataclysm>());
            int catastropheIndex = NPC.FindFirstNPC(ModContent.NPCType<SupremeCatastrophe>());
            bool isCataclysm = npc.type == ModContent.NPCType<SupremeCataclysm>();
            bool isCatastrophe = npc.type == ModContent.NPCType<SupremeCatastrophe>();
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float currentFrame = ref npc.localAI[0];
            ref float attackSpecificTimer = ref npc.Infernum().ExtraAI[5];
            ref float firingFromRight = ref npc.Infernum().ExtraAI[6];

            // Die if the either brother is missing.
            if (cataclysmIndex == -1 || catastropheIndex == -1 || !NPC.AnyNPCs(ModContent.NPCType<SCalNPC>()) || npc.life < npc.lifeMax * 0.01f)
            {
                npc.life = 0;
                npc.HitEffect();
                npc.netUpdate = true;
                return;
            }

            if (isCatastrophe)
            {
                // Shamelessly steal variables from Cataclysm.
                NPC cataclysm = Main.npc[cataclysmIndex];

                // Sync if Catastrophe changed attack states or there's a noticeable discrepancy between attack timers.
                if (attackState != cataclysm.ai[0] || Distance(attackTimer, cataclysm.ai[1]) > 20f)
                    npc.netUpdate = true;

                npc.ai = cataclysm.ai;
                npc.target = cataclysm.target;
                npc.life = cataclysm.life;
                npc.lifeMax = cataclysm.lifeMax;
                npc.realLife = cataclysm.whoAmI;
                attackState = ref cataclysm.ai[0];
                attackTimer = ref cataclysm.ai[1];

                // Use a fallback target if Cataclysm doesn't have one at the moment. This will not care about large distances.
                npc.TargetClosestIfTargetIsInvalid(1000000f);
            }

            // Have Cataclysm increment the attack timer and handle targeting.
            else if (isCataclysm)
            {
                npc.TargetClosestIfTargetIsInvalid();
                attackTimer++;
            }

            // Become angry.
            npc.Calamity().CurrentlyEnraged = SupremeCalamitasBehaviorOverride.Enraged;

            Player target = Main.player[npc.target];
            npc.dontTakeDamage = SupremeCalamitasBehaviorOverride.Enraged;

            // Perform attacks.
            npc.Opacity = Clamp(npc.Opacity + 0.05f, 0f, 1f);
            switch ((SCalBrotherAttackType)attackState)
            {
                case SCalBrotherAttackType.AttackDelay:
                    DoBehavior_AttackDelay(npc, target, isCataclysm, ref currentFrame, ref attackTimer);
                    break;
                case SCalBrotherAttackType.SinusoidalBobbing:
                    DoBehavior_SinusoidalBobbing(npc, target, isCataclysm, ref attackSpecificTimer, ref currentFrame, ref firingFromRight, ref attackTimer);
                    break;
                case SCalBrotherAttackType.ProjectileShooting:
                    DoBehavior_ProjectileShooting(npc, target, isCataclysm, ref attackSpecificTimer, ref currentFrame, ref firingFromRight, ref attackTimer);
                    break;
                case SCalBrotherAttackType.Hyperdashes:
                    DoBehavior_Hyperdashes(npc, target, isCataclysm, ref attackSpecificTimer, ref currentFrame, ref firingFromRight, ref attackTimer);
                    break;
            }
        }

        public static void DoFastHoverMovement(NPC npc, Vector2 hoverDestination)
        {
            float distanceFromDestination = npc.Distance(hoverDestination);
            Vector2 closeMoveVelocity = npc.SafeDirectionTo(hoverDestination) * MathF.Min(distanceFromDestination, 24f);
            npc.velocity = Vector2.Lerp(closeMoveVelocity, (hoverDestination - npc.Center) * 0.0125f, Utils.GetLerpValue(360f, 1080f, distanceFromDestination, true));
            npc.rotation = Clamp(npc.velocity.X * 0.02f, -0.125f, 0.125f);
        }

        public static void DoBehavior_AttackDelay(NPC npc, Player target, bool isCataclysm, ref float currentFrame, ref float attackTimer)
        {
            int transitionDelay = 75;

            // Reset rotation to zero.
            npc.rotation = 0f;

            // Define the direction and animation type.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            currentFrame = (int)SCalBrotherAnimationType.HoverInPlace;

            // Hover to the side of the target.
            float acceleration = 0.925f;
            Vector2 idealVelocity = npc.SafeDirectionTo(target.Center + Vector2.UnitX * isCataclysm.ToDirectionInt() * -720f) * npc.Opacity * 30f;
            npc.SimpleFlyMovement(idealVelocity, acceleration);

            // Fade in.
            npc.Opacity = Utils.GetLerpValue(0f, 24f, attackTimer, true);

            if (attackTimer >= transitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_SinusoidalBobbing(NPC npc, Player target, bool isCataclysm, ref float attackSpecificTimer, ref float currentFrame, ref float firingFromRight, ref float attackTimer)
        {
            int shootTime = 420;
            int soulShootRate = 55;
            int soulCount = 9;
            int projectileFireThreshold = isCataclysm ? 105 : 85;
            float regularShotSpeed = 9.5f;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float shootIncrement = Lerp(1.85f, 3.1f, 1f - lifeRatio);
            if (lifeRatio < 0.5f)
                soulShootRate -= 5;
            if (lifeRatio < 0.25f)
                soulShootRate -= 9;
            if (SupremeCalamitasBehaviorOverride.Enraged)
            {
                soulShootRate = 12;
                regularShotSpeed = 24f;
            }

            // Define the direction and animation type.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            currentFrame = (int)SCalBrotherAnimationType.AttackAnimation;

            float acceleration = 0.95f;
            float sinusoidalOffset = Sin(attackTimer * TwoPi / shootTime) * (!isCataclysm).ToDirectionInt() * 400f;
            Vector2 hoverDestination = target.Center + new Vector2(isCataclysm.ToDirectionInt() * -720f, sinusoidalOffset);
            if (attackTimer < 72f)
                npc.Center = Vector2.Lerp(npc.Center, hoverDestination, 0.018f).MoveTowards(hoverDestination, 1f);

            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 30f;
            npc.SimpleFlyMovement(idealVelocity, acceleration);

            // Increment the attack timer and shoot.
            attackSpecificTimer += shootIncrement;
            if (attackSpecificTimer >= projectileFireThreshold)
            {
                attackSpecificTimer = 0f;
                SoundEngine.PlaySound(SCalNPC.HellblastSound, npc.Center);

                int type = ModContent.ProjectileType<SupremeCataclysmFist>();
                Vector2 projectileSpawnPosition = npc.Center + Vector2.UnitX * npc.spriteDirection * 74f;
                if (!isCataclysm)
                {
                    type = ModContent.ProjectileType<CatastropheSlash>();
                    projectileSpawnPosition = npc.Center + Vector2.UnitX * npc.spriteDirection * 125f;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(projectileSpawnPosition, Vector2.UnitX * npc.spriteDirection * regularShotSpeed, type, SupremeCalamitasBehaviorOverride.BrothersProjectileDamage, 0f, -1, 0f, firingFromRight);

                firingFromRight = firingFromRight == 0f ? 1f : 0f;
            }

            if (attackTimer % soulShootRate == soulShootRate - 1f)
            {
                SoundEngine.PlaySound(SCalNPC.BrimstoneShotSound, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < soulCount; i++)
                    {
                        Vector2 soulVelocity = (TwoPi * i / soulCount).ToRotationVector2() * 8.5f;
                        Utilities.NewProjectileBetter(npc.Center, soulVelocity, ModContent.ProjectileType<LostSoulProj>(), SupremeCalamitasBehaviorOverride.BrothersProjectileDamage, 0f);
                    }
                }
            }

            if (attackTimer >= shootTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ProjectileShooting(NPC npc, Player target, bool isCataclysm, ref float attackSpecificTimer, ref float currentFrame, ref float firingFromRight, ref float attackTimer)
        {
            // Define attack values when the other brother is alive.
            int attackShiftDelay = 60;
            int hoverTime = 60;
            int shootTime = 240;
            int fireBurstCount = 2;
            int projectileFireThreshold = isCataclysm ? 120 : 136;
            float fireShootSpeed = 17.5f;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            if (lifeRatio < 0.5f)
                projectileFireThreshold -= 13;
            if (lifeRatio < 0.25f)
                projectileFireThreshold -= 13;
            if (SupremeCalamitasBehaviorOverride.Enraged)
            {
                projectileFireThreshold = 12;
                fireShootSpeed = 28.5f;
            }

            int attackCycleTime = hoverTime + shootTime;
            int attackTime = (hoverTime + shootTime) * fireBurstCount;
            float wrappedTimer = attackTimer % attackCycleTime;

            if (attackTimer >= attackTime + attackShiftDelay)
                SelectNextAttack(npc);

            // Define the direction and animation type.
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
            currentFrame = (int)(isCataclysm || wrappedTimer < hoverTime ? SCalBrotherAnimationType.HoverInPlace : SCalBrotherAnimationType.AttackAnimation);

            // Slow down and do nothing prior to the attack ending.
            if (attackTimer >= attackTime)
            {
                npc.velocity *= 0.95f;
                npc.rotation *= 0.95f;
                return;
            }

            // Increment the attack timer.
            attackSpecificTimer += isCataclysm ? 4.75f : 3f;

            float hoverOffsetDirection = isCataclysm.ToDirectionInt() * ((int)(attackTimer / attackCycleTime) % 2 == 0).ToDirectionInt();
            Vector2 hoverDestination = target.Center + new Vector2(hoverOffsetDirection * 550f, isCataclysm.ToInt() * -255f);
            if (wrappedTimer < hoverTime)
            {
                // Slow down right before firing. This only happens if sufficiently far away from the target.
                if (wrappedTimer > hoverTime * 0.5f)
                {
                    if (!npc.WithinRange(target.Center, 320f))
                    {
                        npc.velocity *= 0.9f;
                        npc.rotation *= 0.9f;
                    }
                    else
                        npc.Center -= npc.SafeDirectionTo(target.Center) * 10f;
                }

                // Otherwise, do typical hover behavior, towards the upper right of the target.
                else
                {
                    DoFastHoverMovement(npc, hoverDestination);
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                }
            }
            else
            {
                if (isCataclysm)
                    npc.velocity = Vector2.Zero;
                else
                {
                    DoFastHoverMovement(npc, hoverDestination);
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                }

                // Rapidly approach a 0 rotation.
                npc.rotation = npc.rotation.AngleLerp(0f, 0.1f).AngleTowards(0f, 0.15f);

                if (attackSpecificTimer >= projectileFireThreshold)
                {
                    // Play a firing sound.
                    SoundEngine.PlaySound(SCalNPC.BrimstoneShotSound, npc.Center);

                    // And shoot the projectile serverside.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int projectileType = ModContent.ProjectileType<RedirectingHellfireSCal>();
                        Vector2 shootVelocity = -Vector2.UnitY.RotatedByRandom(Pi / 9f) * fireShootSpeed * Main.rand.NextFloat(0.9f, 1.125f);
                        Vector2 projectileSpawnPosition = npc.Center + shootVelocity * 5.4f;
                        if (!isCataclysm)
                        {
                            projectileType = ModContent.ProjectileType<CatastropheSlash>();
                            projectileSpawnPosition = npc.Center + Vector2.UnitX * npc.spriteDirection * 125f;
                            shootVelocity = Vector2.UnitX * npc.spriteDirection * 11.75f;
                            firingFromRight = firingFromRight == 0f ? 1f : 0f;
                        }

                        Utilities.NewProjectileBetter(projectileSpawnPosition, shootVelocity, projectileType, SupremeCalamitasBehaviorOverride.BrothersProjectileDamage, 0f);
                        attackSpecificTimer = 0f;
                        npc.netUpdate = true;
                    }
                }
            }
        }

        public static void DoBehavior_Hyperdashes(NPC npc, Player target, bool isCataclysm, ref float attackSpecificTimer, ref float currentFrame, ref float firingFromRight, ref float attackTimer)
        {
            int chargeTelegraphTime = 48;
            int chargeTime = 50;
            int chargeCount = 3;
            int soulCount = 11;
            float chargeSpeed = 55f;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            if (lifeRatio < 0.5f)
                chargeTelegraphTime -= 10;
            if (lifeRatio < 0.25f)
            {
                chargeSpeed += 6f;
                chargeTime -= 10;
            }
            if (SupremeCalamitasBehaviorOverride.Enraged)
            {
                soulCount = 60;
                chargeSpeed = 100f;
            }

            float wrappedAttackTimer = attackTimer % (chargeTelegraphTime + chargeTime);
            Vector2 hoverDestination = target.Center + Vector2.UnitX * isCataclysm.ToDirectionInt() * 600f;

            // Teleport to the side of the target.
            if (wrappedAttackTimer == 1f)
            {
                npc.Center = hoverDestination;
                npc.velocity = Vector2.Zero;
                npc.Opacity = 0f;
                SoundEngine.PlaySound(InfernumSoundRegistry.CalThunderStrikeSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(explosion =>
                    {
                        explosion.ModProjectile<DemonicExplosion>().MaxRadius = 300f;
                    });
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<DemonicExplosion>(), 0, 0f);
                    npc.netUpdate = true;
                }
            }

            // Charge incredibly quickly and release a circle of lost souls.
            if (wrappedAttackTimer == chargeTime)
            {
                // Define the direction and charge.
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.velocity = Vector2.UnitX * npc.spriteDirection * chargeSpeed;
                npc.netUpdate = true;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < soulCount; i++)
                    {
                        Vector2 soulVelocity = (TwoPi * i / soulCount).ToRotationVector2() * 10f;
                        Utilities.NewProjectileBetter(npc.Center, soulVelocity, ModContent.ProjectileType<LostSoulProj>(), SupremeCalamitasBehaviorOverride.BrothersProjectileDamage, 0f);
                    }
                }

                SoundEngine.PlaySound(!isCataclysm ? YanmeisKnife.HitSound : ScorchedEarth.ShootSound, npc.Center);
            }

            // Define the red-glow interpolant.
            npc.localAI[2] = Utils.GetLerpValue(0f, chargeTelegraphTime - 8f, wrappedAttackTimer, true) * Utils.GetLerpValue(-16f, -1f, wrappedAttackTimer - chargeTelegraphTime - chargeTime, true);

            // Hover to the side of the target before charging.
            if (wrappedAttackTimer < chargeTelegraphTime)
            {
                // Define the direction and animation type.
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                currentFrame = (int)SCalBrotherAnimationType.HoverInPlace;

                if (wrappedAttackTimer < chargeTelegraphTime - 8f)
                    DoFastHoverMovement(npc, hoverDestination);
                else
                    npc.velocity *= 0.8f;
            }
            else
                currentFrame = (int)SCalBrotherAnimationType.AttackAnimation;

            npc.rotation = Clamp(npc.velocity.X * 0.01f, -0.15f, 0.15f);

            if (attackTimer >= (chargeTelegraphTime + chargeTime) * chargeCount + 1f)
                SelectNextAttack(npc);
        }

        public static void SelectNextAttack(NPC npc)
        {
            // Catastrophe does not have control over when attack switches happen.
            bool isCatastrophe = npc.type == ModContent.NPCType<SupremeCatastrophe>();
            if (isCatastrophe)
                return;

            // The 6 instead of 5 is intentional in the loop below. The fifth index is reserved for the attack specific timer, which should be cleared alongside everything else.
            npc.ai[0] = (int)npc.ai[0] switch
            {
                (int)SCalBrotherAttackType.SinusoidalBobbing or (int)SCalBrotherAttackType.AttackDelay => (int)SCalBrotherAttackType.Hyperdashes,
                (int)SCalBrotherAttackType.Hyperdashes => (int)SCalBrotherAttackType.ProjectileShooting,
                _ => (int)SCalBrotherAttackType.SinusoidalBobbing,
            };
            npc.ai[1] = 0f;
            for (int i = 0; i < 6; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.netUpdate = true;
        }

        #endregion AI

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            ref float currentFrame = ref npc.localAI[1];
            ref float attackSpecificTimer = ref npc.Infernum().ExtraAI[5];
            ref float firingFromRight = ref npc.Infernum().ExtraAI[6];
            switch ((SCalBrotherAnimationType)npc.localAI[0])
            {
                case SCalBrotherAnimationType.HoverInPlace:
                    npc.frameCounter += 0.15;
                    if (npc.frameCounter >= 1D)
                    {
                        currentFrame = (currentFrame + 1f) % 12f;
                        npc.frameCounter = 0D;
                    }
                    break;
                case SCalBrotherAnimationType.AttackAnimation:
                    float punchInterpolant = Utils.GetLerpValue(10f, SupremeCataclysm.PunchCounterLimit * 2f, attackSpecificTimer + (firingFromRight != 0f ? 0f : SupremeCataclysm.PunchCounterLimit), true);
                    currentFrame = (int)Math.Round(Lerp(12f, 21f, punchInterpolant));
                    break;
            }

            int xFrame = (int)currentFrame / Main.npcFrameCount[npc.type];
            int yFrame = (int)currentFrame % Main.npcFrameCount[npc.type];

            npc.frame.Width = 212;
            npc.frame.Height = 208;
            npc.frame.X = xFrame * npc.frame.Width;
            npc.frame.Y = yFrame * npc.frame.Height;
        }

        public static bool DrawBrother(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 origin = npc.frame.Size() * 0.5f;
            int afterimageCount = 4;

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < afterimageCount; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, Color.White, 0.5f)) * ((afterimageCount - i) / 15f);
                    Vector2 drawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(texture, drawPosition, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            Vector2 mainDrawPosition = npc.Center - Main.screenPosition;

            // Draw backglow afterimages.
            if (npc.localAI[2] > 0f)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector2 drawOffset = (TwoPi * i / 8f).ToRotationVector2() * npc.localAI[2] * npc.scale * 5f;
                    Color backglowColor = Color.Red * npc.Opacity * npc.localAI[2];
                    backglowColor.A = 0;
                    spriteBatch.Draw(texture, mainDrawPosition + drawOffset, npc.frame, backglowColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }
            spriteBatch.Draw(texture, mainDrawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/SupremeCalamitas/SupremeCataclysmGlow").Value;
            if (npc.type == ModContent.NPCType<SupremeCatastrophe>())
                texture = ModContent.Request<Texture2D>("CalamityMod/NPCs/SupremeCalamitas/SupremeCatastropheGlow").Value;

            Color baseGlowmaskColor = npc.IsABestiaryIconDummy ? Color.White : Color.Lerp(Color.White, Color.Red, 0.5f);

            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < afterimageCount; i++)
                {
                    Color afterimageColor = Color.Lerp(baseGlowmaskColor, Color.White, 0.5f) * ((afterimageCount - i) / 15f);
                    Vector2 drawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(texture, drawPosition, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            spriteBatch.Draw(texture, mainDrawPosition, npc.frame, baseGlowmaskColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor) => DrawBrother(npc, spriteBatch, lightColor);
        #endregion Frames and Drawcode
    }
}
