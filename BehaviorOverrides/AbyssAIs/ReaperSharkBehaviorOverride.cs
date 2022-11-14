using CalamityMod;
using CalamityMod.NPCs.Abyss;
using InfernumMode.OverridingSystem;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.AbyssAIs
{
    public class ReaperSharkBehaviorOverride : NPCBehaviorOverride
    {
        public enum ReaperSharkAttackState
        {
            StalkTarget,
            RoarAnimation
        }

        public override int NPCOverrideType => ModContent.NPCType<ReaperShark>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        #region AI and Behaviors
        public override bool PreAI(NPC npc)
        {
            // Decide a target.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            ref float attackTimer = ref npc.ai[1];
            ref float eyeOpacity = ref npc.localAI[0];

            // Disable tile collision and gravity.
            npc.noTileCollide = true;
            npc.noGravity = true;

            // Reset damage.
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            // Hide the Reaper Shark from the lifeform analyzer.
            npc.rarity = 0;

            // Don't naturally despawn.
            npc.timeLeft = 7200;

            // Handle music stuff.
            npc.boss = true;
            npc.Calamity().ShouldCloseHPBar = true;
            npc.ModNPC.SceneEffectPriority = (SceneEffectPriority)9;
            npc.ModNPC.Music = (InfernumMode.CalamityMod as CalamityMod.CalamityMod).GetMusicFromMusicMod("AdultEidolonWyrm") ?? MusicID.OtherworldlyBoss2;

            // Reset the name of the NPC. It may be changed below to ensure that nothing is drawn when the mouse is over it.
            npc.GivenName = string.Empty;

            switch ((ReaperSharkAttackState)npc.ai[0])
            {
                case ReaperSharkAttackState.StalkTarget:
                    DoBehavior_StalkTarget(npc, target, ref attackTimer, ref eyeOpacity);
                    break;
                case ReaperSharkAttackState.RoarAnimation:
                    DoBehavior_RoarAnimation(npc, target, ref attackTimer, ref eyeOpacity);
                    break;
            }
            attackTimer++;
            return false;
        }

        public static void DoBehavior_StalkTarget(NPC npc, Player target, ref float attackTimer, ref float eyeOpacity)
        {
            int stalkTime = 720;
            int delayBetweenTeleports = 198;
            float swimSpeed = 15f;
            Vector2 directionToTarget = npc.SafeDirectionTo(target.Center);
            ref float soundSlotID = ref npc.Infernum().ExtraAI[0];
            ref float teleportOffsetDirection = ref npc.Infernum().ExtraAI[1];
            ref float teleportOffset = ref npc.Infernum().ExtraAI[2];
            ref float nextTeleportDelay = ref npc.Infernum().ExtraAI[3];

            // Disable music.
            npc.ModNPC.Music = MusicLoader.GetMusicSlot(InfernumMode.Instance, "Sounds/Music/Nothing");

            // Prevent reading the name when the mouse hovers over the Reaper.
            npc.GivenName = " ";

            // Handle teleports.
            if (nextTeleportDelay > 0f)
                nextTeleportDelay--;
            else
            {
                // Play a roar sound.
                soundSlotID = SoundEngine.PlaySound(ReaperShark.SearchRoarSound, target.Center).ToFloat();

                // Determine the teleport offset.
                teleportOffset = MathHelper.Lerp(936f, 350f, (float)Math.Pow(Utils.GetLerpValue(0f, stalkTime - 180f, attackTimer, true), 1.81f));

                // Teleport near target.
                float angleOffsetDirection = Main.rand.NextBool().ToDirectionInt();
                do
                    teleportOffsetDirection += Main.rand.NextFloat(MathHelper.PiOver2);
                while ((teleportOffsetDirection + angleOffsetDirection * MathHelper.PiOver2).ToRotationVector2().AngleBetween(directionToTarget) < 1.18f);

                npc.Center = target.Center + teleportOffsetDirection.ToRotationVector2() * teleportOffset;
                npc.velocity = (teleportOffsetDirection + angleOffsetDirection * MathHelper.PiOver2).ToRotationVector2() * swimSpeed;
                npc.Center -= npc.velocity.SafeNormalize(Vector2.UnitY) * 600f;

                nextTeleportDelay = delayBetweenTeleports;
                npc.netUpdate = true;
            }

            // Very, very gradually turn towards the target.
            Vector2 left = npc.velocity.RotatedBy(-0.007f);
            Vector2 right = npc.velocity.RotatedBy(0.007f);
            if (left.AngleBetween(directionToTarget) < right.AngleBetween(directionToTarget))
                npc.velocity = left;
            else
                npc.velocity = right;

            // Update played sounds.
            if (SoundEngine.TryGetActiveSound(SlotId.FromFloat(soundSlotID), out ActiveSound result)) 
            {
                float lowPassFilter = Utils.GetLerpValue(854f, 360f, teleportOffset, true) * 0.8f + 0.05f;
                result.Sound.SetLowPassFilter(lowPassFilter);
                result.Sound.SetReverb(1f - lowPassFilter);
                result.Position = target.Center;
            }

            // Decide the body and eye opacity.
            eyeOpacity = Utils.GetLerpValue(delayBetweenTeleports - 20f, delayBetweenTeleports - 50f, nextTeleportDelay, true) * Utils.GetLerpValue(4f, 54f, nextTeleportDelay, true);

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Be invisible.
            npc.Opacity = 0f;

            // Decide direction and rotation.
            npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
            npc.rotation = npc.velocity.ToRotation();

            if (attackTimer >= stalkTime && MathHelper.Distance(target.Center.X, npc.Center.X) > 600f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_RoarAnimation(NPC npc, Player target, ref float attackTimer, ref float eyeOpacity)
        {
            int fadeInTime = 32;
            int waterBlackFadeTime = 54;
            float chargeSpeed = 29f;

            // Teleport next to the target on the first frame.
            if (attackTimer == 1f)
            {
                SoundEngine.PlaySound(ReaperShark.SearchRoarSound, target.Center);
                npc.Center = target.Center + (npc.Center - target.Center).ClampMagnitude(100f, 720f);
                npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed / 20f;
                npc.netUpdate = true;
            }

            // Fade in.
            npc.Opacity = Utils.GetLerpValue(0f, fadeInTime, attackTimer, true);
            eyeOpacity = npc.Opacity;

            // Accelerate.
            npc.velocity = (npc.velocity * 1.04f).ClampMagnitude(1f, chargeSpeed);

            // Decide direction and rotation.
            npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
            npc.rotation = npc.velocity.ToRotation();

            // Make the water darknesse recede away, so that the player can see the shark.
            AbyssWaterColorSystem.WaterBlacknessInterpolant = Utils.GetLerpValue(waterBlackFadeTime, 0f, attackTimer, true);

            // Create a shockwave to accompany the charge.
            if (attackTimer == fadeInTime - 8f)
            {
                SoundEngine.PlaySound(ReaperShark.EnragedRoarSound with { Volume = 2.5f }, target.Center);
                Utilities.CreateShockwave(npc.Center, 2, 6, 92, false);
            }

            if (attackTimer >= waterBlackFadeTime)
            {
                //SelectNextAttack(npc);
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            ReaperSharkAttackState currentAttack = (ReaperSharkAttackState)npc.ai[0];
            ReaperSharkAttackState nextAttack = currentAttack;

            if (currentAttack == ReaperSharkAttackState.StalkTarget)
                nextAttack = ReaperSharkAttackState.RoarAnimation;

            npc.ai[0] = (int)nextAttack;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI and Behaviors

        #region Frames and Drawcode
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D eyeTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/AbyssAIs/ReaperSharkEyes").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            float rotation = npc.rotation;
            if (npc.spriteDirection == 1)
                rotation += MathHelper.Pi;
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);

            for (int i = 0; i < 7; i++)
            {
                Color eyeColor = Color.White with { A = 0 } * npc.localAI[0] * MathHelper.Lerp(0.6f, 0f, i / 6f);
                Vector2 eyeOffset = npc.rotation.ToRotationVector2() * i * npc.velocity.Length() * -0.1f;
                ScreenSaturationBlurSystem.ThingsToDrawOnTopOfBlur.Add(new(eyeTexture, drawPosition + eyeOffset, npc.frame, eyeColor, rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0));
            }
            return false;
        }
        #endregion Frames and Drawcode
    }
}
