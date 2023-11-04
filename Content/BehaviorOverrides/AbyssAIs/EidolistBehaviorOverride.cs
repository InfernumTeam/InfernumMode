using CalamityMod;
using CalamityMod.NPCs.PrimordialWyrm;
using CalamityMod.NPCs.NormalNPCs;
using InfernumMode.Assets.Sounds;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class EidolistBehaviorOverride : NPCBehaviorOverride
    {
        public enum EidolistAttackType
        {
            TeleportDashes,
            LightningOrbs,
            SpinLaser
        }

        public override int NPCOverrideType => ModContent.NPCType<Eidolist>();

        #region AI and Behaviors
        public override bool PreAI(NPC npc)
        {
            int totalEidolists = NPC.CountNPCS(npc.type);
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float groupIndex = ref npc.ai[2];
            ref float choirSlotID = ref npc.ai[3];
            ref float teleportFadeInterpolant = ref npc.localAI[0];
            ref float isHostile = ref npc.Infernum().ExtraAI[5];

            // Reset things.
            npc.dontTakeDamage = NPC.AnyNPCs(ModContent.NPCType<PrimordialWyrmHead>());
            npc.chaseable = isHostile == 1f;

            // Disable natural despawning.
            npc.Infernum().DisableNaturalDespawning = true;

            // Don't naturally despawn if in silent worship.
            if (isHostile != 1f)
                npc.timeLeft = 7200;

            // Handle targeting.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Become hostile if hit. When this happens, all eidolists become hostile as well.
            if (npc.justHit && npc.Infernum().ExtraAI[5] != 1f)
            {
                SoundEngine.PlaySound(Eidolist.DeathSound with { Volume = 3f }, npc.Center);
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == npc.type)
                    {
                        Main.npc[i].Infernum().ExtraAI[5] = 1f;
                        Main.npc[i].netUpdate = true;
                    }
                }

                isHostile = 1f;
                npc.netUpdate = true;
            }

            // Update the choir sound in terms of position and other side things.
            float volume = Utils.Remap(npc.Distance(target.Center), 1250f, 640f, 0f, 1f);
            if (groupIndex == 2f)
            {
                if (SoundEngine.TryGetActiveSound(SlotId.FromFloat(choirSlotID), out ActiveSound result) && result.Sound.Volume > 0f)
                {
                    result.Position = npc.Center;
                    if (volume > 0f)
                        result.Volume = volume;
                    else
                        result.Stop();

                    if (isHostile == 1f)
                        result.Stop();
                }
                else if (isHostile == 0f && volume > 0f)
                    choirSlotID = SoundEngine.PlaySound(InfernumSoundRegistry.EidolistChoirSound, npc.Center).ToFloat();
            }

            // Why is this necessary?
            npc.boss = false;
            npc.Calamity().ShouldCloseHPBar = true;
            npc.Calamity().ProvidesProximityRage = isHostile == 1f;
            if (volume > 0f && isHostile == 0f && !AbyssMinibossSpawnSystem.MajorAbyssEnemyExists)
            {
                npc.boss = true;
                npc.ModNPC.SceneEffectPriority = SceneEffectPriority.BossMedium;
                npc.ModNPC.Music = MusicLoader.GetMusicSlot(InfernumMode.Instance, "Sounds/Music/Nothing");
            }

            // Stop at this point and fade in if not hostile, and just sit in place.
            if (isHostile != 1f)
            {
                npc.Opacity = Clamp(npc.Opacity + 0.1f, 0f, 1f);

                if (!AbyssMinibossSpawnSystem.MajorAbyssEnemyExists && npc.WithinRange(target.Center, 3000f))
                    target.Calamity().adrenaline = 0f;

                return false;
            }

            // Despawn if the player went away while hostile.
            if (!npc.WithinRange(target.Center, 7200f))
                npc.active = false;

            // Disable tile collision and gravity by default.
            npc.noTileCollide = true;
            npc.noGravity = true;

            // Do contact damage.
            npc.damage = npc.defDamage = 150;

            switch ((EidolistAttackType)attackType)
            {
                case EidolistAttackType.TeleportDashes:
                    DoBehavior_TeleportDashes(npc, target, groupIndex, totalEidolists, ref attackTimer, ref teleportFadeInterpolant);
                    break;
                case EidolistAttackType.LightningOrbs:
                    npc.damage = 0;
                    DoBehavior_LightningOrbs(npc, target, groupIndex, totalEidolists, ref attackTimer, ref teleportFadeInterpolant);
                    break;
                case EidolistAttackType.SpinLaser:
                    npc.damage = 0;
                    SelectNextAttack(npc);
                    break;
            }

            // Increment the attack timer.
            attackTimer++;

            return false;
        }

        #region Teleport Dashes
        public static void DoBehavior_TeleportDashes(NPC npc, Player target, float groupIndex, int totalEidolists, ref float attackTimer, ref float teleportFadeInterpolant)
        {
            int initalFadeOutTime = 30;
            int fadeInTime = 25;
            int chargeTime = 45;
            int chargeFadeOutTime = 12;
            int chargeCount = 1;
            float chargeSpeed = 26f;
            float teleportOffsetRadius = 400f;
            ref float teleportAngularOffset = ref npc.Infernum().ExtraAI[0];

            if (totalEidolists <= 3)
            {
                fadeInTime -= 2;
                chargeTime -= 6;
                chargeSpeed += 5f;
            }
            if (totalEidolists <= 2)
                chargeCount++;
            if (totalEidolists == 1)
                fadeInTime += 30;

            // Do a teleport fadeout.
            if (attackTimer <= initalFadeOutTime)
            {
                npc.damage = 0;
                npc.dontTakeDamage = true;
                teleportFadeInterpolant = Utils.GetLerpValue(initalFadeOutTime, 0f, attackTimer, true);
                return;
            }

            // When two or more eidolists are present, each one does a single teleport dash, waiting completing for the next eidolist to attack.
            // When there are only three eidolists, the charges are faster, and two can exist at once.
            // When there are only two eidolists, they both charge together.
            // When there is only one eidolist, they release exploding ice bombs.

            // Decide the angular offset for all Eidolists. They should be evenly spread.
            float adjustedAttackTimer = attackTimer - initalFadeOutTime;
            if (adjustedAttackTimer == 1f)
            {
                float teleportOffsetAngle = Main.rand.NextFloat(TwoPi);
                AffectAllEidolists((n, gIndex) =>
                {
                    n.Infernum().ExtraAI[0] = teleportOffsetAngle + TwoPi * gIndex / totalEidolists;
                });
            }

            int attackCycleTime = fadeInTime + chargeTime + chargeFadeOutTime;
            float chargeTimer = adjustedAttackTimer;
            if (totalEidolists >= 2)
                chargeTimer -= attackCycleTime * groupIndex;
            if (totalEidolists == 3)
                chargeTimer = adjustedAttackTimer - attackCycleTime * (int)(groupIndex * 0.5f);

            bool doneCharging = chargeTimer >= attackCycleTime;
            bool currentlyCharging = chargeTimer >= fadeInTime && !doneCharging;
            if (chargeTimer <= fadeInTime || doneCharging)
            {
                if (chargeTimer < 0f || doneCharging)
                    npc.Center = target.Center - Vector2.UnitY * 1400f;
                else
                {
                    npc.Center = target.Center + teleportAngularOffset.ToRotationVector2() * teleportOffsetRadius;
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.rotation = 0f;
                }

                teleportFadeInterpolant = doneCharging ? 1f : Utils.GetLerpValue(0f, fadeInTime, chargeTimer, true);

                // Go to the next attack state once the last eidolist has finished charging.
                if (groupIndex >= totalEidolists - 1f && doneCharging)
                {
                    AffectAllEidolists((n, gIndex) =>
                    {
                        n.ai[1] = 0f;
                        n.Infernum().ExtraAI[1]++;
                        n.netUpdate = true;
                        if (n.Infernum().ExtraAI[1] >= chargeCount)
                            SelectNextAttack(n);
                    });
                }
                return;
            }

            // Charge at the target.
            if (chargeTimer == fadeInTime + 1f)
            {
                SoundEngine.PlaySound(SoundID.Item105, npc.Center);
                npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
                npc.netUpdate = true;
            }

            // Release icicle bombs when charging if this is the last eidolist.
            if (Main.netMode != NetmodeID.MultiplayerClient && totalEidolists <= 1 && currentlyCharging && attackTimer % 9f == 8f && !npc.WithinRange(target.Center, 120f))
                Utilities.NewProjectileBetter(npc.Center, npc.velocity * 0.1f, ModContent.ProjectileType<EidolistIceBomb>(), 0, 0f);

            // Fade out when done charging.
            if (chargeTimer == fadeInTime + chargeTime)
            {
                teleportFadeInterpolant = Utils.GetLerpValue(fadeInTime + chargeTime, fadeInTime + chargeTime + chargeFadeOutTime, chargeTimer, true);
                npc.velocity *= 0.95f;
            }

            // Rotate.
            npc.rotation = npc.velocity.X * 0.02f;
        }
        #endregion Teleport Dashes

        #region Lightning Orbs
        public static void DoBehavior_LightningOrbs(NPC npc, Player target, float groupIndex, int totalEidolists, ref float attackTimer, ref float teleportFadeInterpolant)
        {
            int initalFadeOutTime = 30;
            List<NPC> eidolists = Main.npc.Take(Main.maxNPCs).Where(n => n.type == npc.type && n.active).OrderBy(n => n.Infernum().ExtraAI[5]).ToList();
            ref float teleportCounter = ref npc.Infernum().ExtraAI[0];

            // Do a teleport fadeout.
            if (attackTimer <= initalFadeOutTime)
            {
                // Don't rotate.
                npc.rotation = 0f;

                // Teleport above the target on the first frame.
                if (attackTimer == 1f)
                {
                    Vector2 hoverOffset = new(Main.rand.NextFloatDirection() * 500f, -400f);
                    if (totalEidolists <= 1)
                        hoverOffset = -Vector2.UnitY * 500f;

                    if (totalEidolists is < 4 and >= 2)
                    {
                        int localIndex = eidolists.IndexOf(npc);
                        hoverOffset = hoverOffset.RotatedBy(TwoPi * localIndex / eidolists.Count + PiOver2);
                    }
                    npc.Center = target.Center + hoverOffset;

                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                }

                npc.dontTakeDamage = true;
                teleportFadeInterpolant = Utils.GetLerpValue(initalFadeOutTime, 0f, attackTimer, true);
                if (groupIndex >= 1f && totalEidolists >= 4)
                    teleportFadeInterpolant = 1f;
                return;
            }

            // When four eidolists are present, only the first one attacks, casting an orb with telegraph lines above the player.
            float adjustedAttackTimer = attackTimer - initalFadeOutTime;
            if (totalEidolists == 4)
            {
                DoBehavior_LightningOrbs4(npc, target, groupIndex, adjustedAttackTimer, ref attackTimer, ref teleportFadeInterpolant, ref teleportCounter);
                return;
            }

            // When three eidolists are present, all three perform a single orb cast.
            if (totalEidolists == 3)
            {
                DoBehavior_LightningOrbs3(npc, target, adjustedAttackTimer);
                return;
            }

            // When two eidolists are present, the first casts an orb while the second performs teleport charges.
            if (totalEidolists == 2)
            {
                DoBehavior_LightningOrbs2(npc, target, eidolists, adjustedAttackTimer, ref attackTimer);
                return;
            }

            // When one eidolist is present, it casts directional orbs at all sides around the player.
            if (totalEidolists == 1)
            {
                DoBehavior_LightningOrbs1(npc, target, adjustedAttackTimer, ref attackTimer, ref teleportCounter);
                return;
            }
        }

        public static void DoBehavior_LightningOrbs4(NPC npc, Player target, float groupIndex, float adjustedAttackTimer, ref float attackTimer, ref float teleportFadeInterpolant, ref float teleportCounter)
        {
            int teleportCount = 3;
            int orbCastDelay = 40;
            int lightningShootTime = EidolistElectricOrb.Lifetime + 45;

            // Don't rotate.
            npc.rotation = 0f;

            // Do nothing if not the first eidolist.
            if (groupIndex >= 1f)
            {
                npc.dontTakeDamage = true;
                npc.Center = target.Center + Vector2.UnitY * 1200f;
                teleportFadeInterpolant = 1f;
                return;
            }

            // Create the lightning orb.
            if (Main.netMode != NetmodeID.MultiplayerClient && adjustedAttackTimer == orbCastDelay)
            {
                // Look at the target.
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.netUpdate = true;

                int lightningOrb = Utilities.NewProjectileBetter(npc.Top, Vector2.Zero, ModContent.ProjectileType<EidolistElectricOrb>(), 0, 0f);
                if (Main.projectile.IndexInRange(lightningOrb))
                    Main.projectile[lightningOrb].ai[0] = npc.whoAmI;
            }

            if (adjustedAttackTimer >= orbCastDelay + lightningShootTime)
            {
                teleportCounter++;
                attackTimer = 0f;
                npc.netUpdate = true;

                if (teleportCounter >= teleportCount)
                    AffectAllEidolists((n, gIndex) => SelectNextAttack(n));
            }
        }

        public static void DoBehavior_LightningOrbs3(NPC npc, Player target, float adjustedAttackTimer)
        {
            int orbCastDelay = 40;
            int lightningShootTime = EidolistElectricOrb2.Lifetime + 45;

            // Don't rotate.
            npc.rotation = 0f;

            // Create the lightning orb.
            if (Main.netMode != NetmodeID.MultiplayerClient && adjustedAttackTimer == orbCastDelay)
            {
                // Look at the target.
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.netUpdate = true;

                int lightningOrb = Utilities.NewProjectileBetter(npc.Top, Vector2.Zero, ModContent.ProjectileType<EidolistElectricOrb2>(), 0, 0f);
                if (Main.projectile.IndexInRange(lightningOrb))
                {
                    Main.projectile[lightningOrb].ai[0] = npc.whoAmI;
                    Main.projectile[lightningOrb].ai[1] = 8f;
                }
            }

            if (adjustedAttackTimer >= orbCastDelay + lightningShootTime)
                AffectAllEidolists((n, gIndex) => SelectNextAttack(n));

        }

        public static void DoBehavior_LightningOrbs2(NPC npc, Player target, List<NPC> eidolists, float adjustedAttackTimer, ref float attackTimer)
        {
            int orbCastDelay = 40;
            int teleportCount = 4;
            int lightningShootTime = EidolistElectricOrb2.Lifetime + 45;
            ref float teleportCounter = ref npc.Infernum().ExtraAI[0];

            // Do cast behaviors.
            if (eidolists.First() == npc)
            {
                // Don't rotate.
                npc.rotation = 0f;

                // Create the lightning orb.
                if (Main.netMode != NetmodeID.MultiplayerClient && adjustedAttackTimer == orbCastDelay)
                {
                    // Look at the target.
                    npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                    npc.netUpdate = true;

                    int lightningOrb = Utilities.NewProjectileBetter(npc.Top, Vector2.Zero, ModContent.ProjectileType<EidolistElectricOrb2>(), 0, 0f);
                    if (Main.projectile.IndexInRange(lightningOrb))
                    {
                        Main.projectile[lightningOrb].ai[0] = npc.whoAmI;
                        Main.projectile[lightningOrb].ai[1] = 13f;
                    }
                }

                if (adjustedAttackTimer >= orbCastDelay + lightningShootTime)
                {
                    teleportCounter++;
                    attackTimer = 0f;
                    npc.netUpdate = true;

                    if (teleportCounter >= teleportCount)
                        AffectAllEidolists((n, gIndex) => SelectNextAttack(n));
                }
            }

            // Do teleport charge behaviors.
            else
            {
                int teleportFadeTime = 23;
                int chargeTime = 44;
                int attackCycleTime = teleportFadeTime * 2 + chargeTime;
                float chargeSpeed = 24.5f;
                float wrappedAttackTimer = adjustedAttackTimer % attackCycleTime;
                float teleportOffsetRadius = 550f;

                // Fade in.
                if (wrappedAttackTimer <= teleportFadeTime)
                {
                    if (wrappedAttackTimer == 1f)
                    {
                        npc.Center = target.Center + Main.rand.NextVector2CircularEdge(teleportOffsetRadius, teleportOffsetRadius);
                        npc.velocity = Vector2.Zero;
                        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                        npc.netUpdate = true;
                    }
                    npc.Opacity = Utils.GetLerpValue(0f, teleportFadeTime, wrappedAttackTimer, true);
                }

                // Charge.
                if (wrappedAttackTimer == teleportFadeTime)
                {
                    SoundEngine.PlaySound(SoundID.Item105, npc.Center);
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
                    npc.netUpdate = true;
                }

                // Fade out.
                if (wrappedAttackTimer >= teleportFadeTime + chargeTime)
                    npc.Opacity = Utils.GetLerpValue(teleportFadeTime, 0f, wrappedAttackTimer - teleportFadeTime - chargeTime, true);

                // Decide rotation.
                npc.rotation = npc.velocity.X * 0.02f;
            }
        }

        public static void DoBehavior_LightningOrbs1(NPC npc, Player target, float adjustedAttackTimer, ref float attackTimer, ref float teleportCounter)
        {
            int teleportCount = 2;
            int orbCastDelay = 40;
            int lightningShootTime = EidolistElectricOrb.Lifetime + 45;
            float orbOffsetRadius = 1300f;

            // Don't rotate.
            npc.rotation = 0f;

            // Create the lightning orbs.
            if (Main.netMode != NetmodeID.MultiplayerClient && adjustedAttackTimer == orbCastDelay)
            {
                // Look at the target.
                npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
                npc.netUpdate = true;

                int lightningOrb = Utilities.NewProjectileBetter(npc.Top, Vector2.Zero, ModContent.ProjectileType<EidolistElectricOrb>(), 0, 0f);
                if (Main.projectile.IndexInRange(lightningOrb))
                    Main.projectile[lightningOrb].ai[0] = npc.whoAmI;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 orbOffset = -Vector2.UnitY.RotatedBy(TwoPi * i / 3f) * orbOffsetRadius;
                    if (orbOffset.AngleBetween(-Vector2.UnitY) < 0.01f)
                        continue;

                    lightningOrb = Utilities.NewProjectileBetter(target.Center + orbOffset, Vector2.Zero, ModContent.ProjectileType<EidolistElectricOrb>(), 0, 0f);
                    if (Main.projectile.IndexInRange(lightningOrb))
                        Main.projectile[lightningOrb].ai[0] = -1f;
                }
            }

            if (adjustedAttackTimer >= orbCastDelay + lightningShootTime)
            {
                teleportCounter++;
                attackTimer = 0f;
                npc.netUpdate = true;

                if (teleportCounter >= teleportCount)
                    AffectAllEidolists((n, gIndex) => SelectNextAttack(n));
            }
        }
        #endregion Lightning Orbs

        public static void SelectNextAttack(NPC npc)
        {
            int totalEidolists = NPC.CountNPCS(npc.type);
            switch ((EidolistAttackType)npc.ai[0])
            {
                case EidolistAttackType.TeleportDashes:
                    npc.ai[0] = (int)EidolistAttackType.LightningOrbs;
                    break;
                case EidolistAttackType.LightningOrbs:
                    npc.ai[0] = (int)(totalEidolists <= 3 ? EidolistAttackType.SpinLaser : EidolistAttackType.TeleportDashes);
                    break;
                case EidolistAttackType.SpinLaser:
                    npc.ai[0] = (int)EidolistAttackType.TeleportDashes;
                    break;
            }

            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.netUpdate = true;
        }

        public static void AffectAllEidolists(Action<NPC, int> action)
        {
            int eidolistID = ModContent.NPCType<Eidolist>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == eidolistID)
                {
                    action(Main.npc[i], (int)Main.npc[i].ai[2]);
                    Main.npc[i].netUpdate = true;
                }
            }
        }
        #endregion AI and Behaviors

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            float animateRate = npc.Infernum().ExtraAI[5] == 1f ? 0.15f : 0.1f;
            npc.frameCounter = (npc.frameCounter + animateRate) % Main.npcFrameCount[npc.type];
            npc.frame.Y = (int)(npc.frameCounter + npc.ai[2]) % Main.npcFrameCount[npc.type] * frameHeight;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            bool teleporting = npc.localAI[0] is > 0f and < 1f;
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            if (teleporting)
            {
                Main.spriteBatch.EnterShaderRegion();
                GameShaders.Misc["Infernum:Teleport"].UseSaturation(npc.localAI[0]);
                GameShaders.Misc["Infernum:Teleport"].UseImage1("Images/Misc/noise");
                GameShaders.Misc["Infernum:Teleport"].UseColor(Color.Cyan);
                GameShaders.Misc["Infernum:Teleport"].Shader.Parameters["actualImageSize"].SetValue(texture.Size());
                GameShaders.Misc["Infernum:Teleport"].Shader.Parameters["uActualSourceRect"].SetValue(new Vector4(npc.frame.X, npc.frame.Y, npc.frame.Width, npc.frame.Height));
                GameShaders.Misc["Infernum:Teleport"].Apply();
            }

            if (npc.Infernum().ExtraAI[5] == 0f)
                texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/AbyssAIs/EidolistWorship").Value;

            Vector2 drawPosition = npc.Center - Main.screenPosition;
            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(Color.Gray), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);

            if (teleporting)
                Main.spriteBatch.ExitShaderRegion();
            return false;
        }
        #endregion Frames and Drawcode
    }
}
