using CalamityMod;
using CalamityMod.NPCs.NormalNPCs;
using InfernumMode.OverridingSystem;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.AbyssAIs
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

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame | NPCOverrideContext.NPCPreDraw;

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
            npc.dontTakeDamage = false;
            npc.chaseable = isHostile == 1f;
            npc.damage = npc.defDamage;

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
                    result.Sound.SetLowPassFilter(0.9f);
                    result.Sound.Pitch = -0.15f;
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
            if (volume > 0f && isHostile == 0f)
            {
                npc.boss = true;
                npc.ModNPC.SceneEffectPriority = SceneEffectPriority.BossMedium;
                npc.ModNPC.Music = MusicLoader.GetMusicSlot(InfernumMode.Instance, "Sounds/Music/Nothing");
            }

            // Stop at this point and fade in if not hostile, and just sit in place.
            if (isHostile != 1f)
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.1f, 0f, 1f);
                return false;
            }

            // Despawn if the player went away while hostile.
            if (!npc.WithinRange(target.Center, 7200f))
                npc.active = false;

            // Disable tile collision and gravity by default.
            npc.noTileCollide = true;
            npc.noGravity = true;

            switch ((EidolistAttackType)attackType)
            {
                case EidolistAttackType.TeleportDashes:
                    DoBehavior_TeleportDashes(npc, target, groupIndex, totalEidolists, ref attackTimer, ref teleportFadeInterpolant);
                    break;
            }

            // Increment the attack timer.
            attackTimer++;

            return false;
        }

        public static void DoBehavior_TeleportDashes(NPC npc, Player target, float groupIndex, int totalEidolists, ref float attackTimer, ref float teleportFadeInterpolant)
        {
            int initalFadeOutTime = 30;
            int fadeInTime = 16;
            int chargeTime = 45;
            int chargeFadeOutTime = 12;
            float chargeSpeed = 29.75f;
            float teleportOffsetRadius = 400f;
            ref float teleportAngularOffset = ref npc.Infernum().ExtraAI[0];

            // Do a teleport fadeout.
            if (attackTimer <= initalFadeOutTime)
            {
                npc.dontTakeDamage = true;
                teleportFadeInterpolant = Utils.GetLerpValue(initalFadeOutTime, 0f, attackTimer, true);
                return;
            }

            // When four eidolists are present, each one does a single teleport dash, waiting completing for the next eidolist to attack.
            float adjustedAttackTimer = attackTimer - initalFadeOutTime;
            if (totalEidolists == 4)
            {
                // Decide the angular offset for all Eidolists. They should be evenly spread.
                if (adjustedAttackTimer == 1f)
                {
                    float teleportOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    AffectAllEidolists((n, gIndex) =>
                    {
                        n.Infernum().ExtraAI[0] = teleportOffsetAngle + MathHelper.TwoPi * gIndex / totalEidolists;
                    });
                }

                int attackCycleTime = fadeInTime + chargeTime + chargeFadeOutTime;
                float chargeTimer = adjustedAttackTimer - attackCycleTime * groupIndex;
                bool doneCharging = chargeTimer >= attackCycleTime;
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
                        AffectAllEidolists((n, gIndex) => SelectNextAttack(n));
                    return;
                }

                // Charge at the target.
                if (chargeTimer == fadeInTime + 1f)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath6 with { Pitch = 0.15f }, npc.Center);
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
                    npc.netUpdate = true;
                }

                // Fade out when done charging.
                if (chargeTimer == fadeInTime + chargeTime)
                {
                    teleportFadeInterpolant = Utils.GetLerpValue(fadeInTime + chargeTime, fadeInTime + chargeTime + chargeFadeOutTime, chargeTimer, true);
                    npc.velocity *= 0.95f;
                }

                // Rotate.
                npc.rotation = npc.velocity.X * 0.02f;
                return;
            }

            attackTimer = 0f;
        }
        
        public static void SelectNextAttack(NPC npc)
        {
            switch ((EidolistAttackType)npc.ai[0])
            {
                case EidolistAttackType.TeleportDashes:
                    npc.ai[0] = (int)EidolistAttackType.LightningOrbs;
                    break;
                case EidolistAttackType.LightningOrbs:
                    npc.ai[0] = (int)EidolistAttackType.SpinLaser;
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
                texture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/AbyssAIs/EidolistWorship").Value;

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
