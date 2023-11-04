using CalamityMod;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.PrimordialWyrm;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.Projectiles.Generic;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class ReaperSharkBehaviorOverride : NPCBehaviorOverride
    {
        public enum ReaperSharkAttackState
        {
            StalkTarget,
            RoarAnimation,
            RushAtTarget,
            UpwardCharges,
            IceBreath,
            MiniSharkFakeoutCharges
        }

        public override int NPCOverrideType => ModContent.NPCType<ReaperShark>();

        #region AI and Behaviors
        public override bool PreAI(NPC npc)
        {
            // Pick a target if a valid one isn't already decided.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            ReaperSharkAttackState currentAttack = (ReaperSharkAttackState)npc.ai[0];
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

            // Disable water slowness.
            npc.waterMovementSpeed = 0f;

            // Try to get away if the AEW is coming or present.
            if (NPC.AnyNPCs(ModContent.NPCType<PrimordialWyrmHead>()) || Utilities.AnyProjectiles(ModContent.ProjectileType<TerminusAnimationProj>()))
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * -36f, 0.08f);
                npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
                npc.rotation = npc.velocity.ToRotation();

                if (!npc.WithinRange(target.Center, 1400f))
                    npc.active = false;

                return false;
            }

            // Despawn if the player has left the final layer of the abyss or is dead.
            if (!target.Calamity().ZoneAbyssLayer4 || target.dead)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 27f, 0.1f);
                if (!npc.WithinRange(target.Center, 1200f))
                    npc.active = false;
                return false;
            }

            // Disable not taking damage from minions.
            npc.ModNPC<ReaperShark>().hasBeenHit = true;

            switch (currentAttack)
            {
                case ReaperSharkAttackState.StalkTarget:
                    DoBehavior_StalkTarget(npc, target, ref attackTimer, ref eyeOpacity);
                    break;
                case ReaperSharkAttackState.RoarAnimation:
                    DoBehavior_RoarAnimation(npc, target, ref attackTimer, ref eyeOpacity);
                    break;
                case ReaperSharkAttackState.RushAtTarget:
                    DoBehavior_RushAtTarget(npc, target, ref attackTimer);
                    break;
                case ReaperSharkAttackState.UpwardCharges:
                    DoBehavior_UpwardCharges(npc, target, ref attackTimer);
                    break;
                case ReaperSharkAttackState.IceBreath:
                    DoBehavior_IceBreath(npc, target, ref attackTimer);
                    break;
                case ReaperSharkAttackState.MiniSharkFakeoutCharges:
                    DoBehavior_MiniSharkFakeoutCharges(npc, target, ref attackTimer);
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
            else if (attackTimer < stalkTime)
            {
                // Play a roar sound.
                soundSlotID = SoundEngine.PlaySound(ReaperShark.SearchRoarSound, target.Center).ToFloat();

                // Determine the teleport offset.
                teleportOffset = Lerp(1436f, 450f, Pow(Utils.GetLerpValue(0f, stalkTime - 180f, attackTimer, true), 1.81f));

                // Teleport near target.
                float angleOffsetDirection = Main.rand.NextBool().ToDirectionInt();
                do
                    teleportOffsetDirection += Main.rand.NextFloat(PiOver2);
                while ((teleportOffsetDirection + angleOffsetDirection * PiOver2).ToRotationVector2().AngleBetween(directionToTarget) < 1.18f ||
                        (teleportOffsetDirection + angleOffsetDirection * PiOver2).ToRotationVector2().AngleBetween(target.velocity) < 0.9f);

                npc.Center = target.Center + teleportOffsetDirection.ToRotationVector2() * teleportOffset;
                npc.velocity = (teleportOffsetDirection + angleOffsetDirection * PiOver2).ToRotationVector2() * swimSpeed;
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
                result.Position = target.Center;

            // Decide the eye opacity.
            eyeOpacity = Utils.GetLerpValue(delayBetweenTeleports - 20f, delayBetweenTeleports - 50f, nextTeleportDelay, true) * Utils.GetLerpValue(4f, 54f, nextTeleportDelay, true);
            eyeOpacity *= Utils.GetLerpValue(250f, 390f, npc.Distance(target.Center), true);

            // Disable damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Be invisible.
            npc.Opacity = 0f;

            // Decide direction and rotation.
            npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
            npc.rotation = npc.velocity.ToRotation();

            if (attackTimer >= stalkTime && Distance(target.Center.X, npc.Center.X) > 800f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_RoarAnimation(NPC npc, Player target, ref float attackTimer, ref float eyeOpacity)
        {
            int fadeInTime = 32;
            int waterBlackFadeTime = 92;
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

            // Disable contact damage.
            npc.damage = 0;

            // Create a shockwave to accompany the charge.
            if (attackTimer == fadeInTime - 8f)
            {
                SoundEngine.PlaySound(ReaperShark.EnragedRoarSound with { Volume = 2.5f }, target.Center);
                Utilities.CreateShockwave(npc.Center, 2, 6, 92, false);
            }

            if (attackTimer >= waterBlackFadeTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_RushAtTarget(NPC npc, Player target, ref float attackTimer)
        {
            int rushTime = 480;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float rushSpeed = Lerp(28f, 36.5f, 1f - lifeRatio);
            float rushAcceleration = rushSpeed * 0.009f;

            // Rush towards the player if sufficiently far away.
            if (!npc.WithinRange(target.Center, 100f))
                npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center) * rushSpeed, rushAcceleration);

            // Decide direction and rotation.
            if (Distance(target.Center.X, npc.Center.X) > 50f)
                npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
            npc.rotation = Clamp(npc.velocity.X * 0.02f, -0.3f, 0.3f);
            if (npc.spriteDirection == 1)
                npc.rotation += Pi;

            if (attackTimer >= rushTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_UpwardCharges(NPC npc, Player target, ref float attackTimer)
        {
            int chargeCount = 4;
            int chargeDelay = 42;
            int chargeTime = 72;
            int swimDisappearTime = 60;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float startingChargeSpeed = 13f;
            float chargeSpeed = Lerp(32f, 40f, 1f - lifeRatio);
            float angularTurnSpeed = 0.011f;
            Vector2 directionToTarget = npc.SafeDirectionTo(target.Center);
            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];
            ref float hasReachedTarget = ref npc.Infernum().ExtraAI[1];
            ref float initialDisappearTimer = ref npc.Infernum().ExtraAI[2];

            // Swim downward and disappear before attacking.
            if (initialDisappearTimer < swimDisappearTime)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 25f, 0.04f);
                npc.rotation = npc.velocity.ToRotation();
                initialDisappearTimer++;
                attackTimer = 0f;
                return;
            }

            // Hover to the bottom left/right of the player.
            if (attackTimer < chargeDelay)
            {
                // Roar on the first frame.
                if (attackTimer == 1f)
                    SoundEngine.PlaySound(ReaperShark.EnragedRoarSound, target.Center);
                npc.Opacity = attackTimer / (chargeDelay - 1f);
                npc.Center = target.Center + new Vector2((chargeCounter % 2f == 0f).ToDirectionInt() * 150f, 700f);
                npc.velocity = Vector2.Zero;
                return;
            }

            // Charge at the target.
            if (attackTimer == chargeDelay)
            {
                npc.velocity = directionToTarget * startingChargeSpeed;
                npc.netUpdate = true;
            }
            npc.velocity = (npc.velocity * 1.05f).ClampMagnitude(startingChargeSpeed, chargeSpeed);
            npc.rotation = npc.velocity.ToRotation();

            // Turn towards the target once they've been reached.
            if (hasReachedTarget == 1f || npc.WithinRange(target.Center, 180f))
            {
                Vector2 left = npc.velocity.RotatedBy(-angularTurnSpeed);
                Vector2 right = npc.velocity.RotatedBy(angularTurnSpeed);
                if (left.AngleBetween(directionToTarget) < right.AngleBetween(directionToTarget))
                    npc.velocity = left;
                else
                    npc.velocity = right;

                if (hasReachedTarget != 1f)
                {
                    hasReachedTarget = 1f;
                    npc.netUpdate = true;
                }
            }

            if (attackTimer == chargeDelay + 8f)
                Utilities.CreateShockwave(npc.Center, 2, 8, 85, false);

            if (attackTimer >= chargeDelay + chargeTime)
            {
                attackTimer = 0f;
                chargeCounter++;
                if (chargeCounter >= chargeCount)
                {
                    npc.Center = target.Center + new Vector2(Main.rand.NextFloatDirection() * 500f, 800f);
                    SelectNextAttack(npc);
                }

                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_IceBreath(NPC npc, Player target, ref float attackTimer)
        {
            int riseTime = 240;
            int breathTime = 132;
            int iceSpikeReleaseRate = 6;
            float horizontalHoverSpeed = 20f;

            // Fly towards the hover destination near the target.
            if (attackTimer < riseTime)
            {
                Vector2 destination = target.Center + new Vector2((npc.Center.X > target.Center.X).ToDirectionInt() * 750f, -300f);
                Vector2 idealVelocity = npc.SafeDirectionTo(destination) * horizontalHoverSpeed;

                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.035f);
                npc.rotation = npc.rotation.AngleTowards(npc.spriteDirection == 1 ? Pi : 0f, 0.25f);

                npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();

                // Once it has been reached, change the attack timer to begin the carpet bombing.
                if (npc.WithinRange(destination, 32f))
                    attackTimer = riseTime - 1f;
            }

            // Begin flying horizontally.
            else if (attackTimer == riseTime)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.ReaperSharkIceBreathSound, npc.Center);

                Vector2 moveDirection = (npc.SafeDirectionTo(target.Center) * new Vector2(1f, 0.2f)).SafeNormalize(Vector2.UnitX * npc.spriteDirection);
                npc.velocity = moveDirection * horizontalHoverSpeed;
                npc.netUpdate = true;
            }

            // And release ice.
            else
            {
                npc.position.X += npc.SafeDirectionTo(target.Center).X * 7f;
                npc.position.Y += npc.SafeDirectionTo(target.Center + Vector2.UnitY * -400f).Y * 6f;
                npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
                npc.rotation = npc.velocity.ToRotation();

                Vector2 aimDirection = npc.velocity.SafeNormalize(Vector2.UnitX * npc.spriteDirection);
                Vector2 mouthPosition = npc.Center + aimDirection * 54f;
                Vector2 iceBreathVelocity = aimDirection.RotatedBy(npc.spriteDirection * -0.2f) * 7.5f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (attackTimer % 8f == 7f)
                        Utilities.NewProjectileBetter(mouthPosition + aimDirection * 190f, iceBreathVelocity, ModContent.ProjectileType<ReaperSharkIceBreath>(), 350, 0f);
                    if (attackTimer % iceSpikeReleaseRate == iceSpikeReleaseRate - 1f)
                    {
                        Vector2 icicleVelocity = -Vector2.UnitY.RotatedBy(npc.spriteDirection * 0.57f) * new Vector2(9f, 4f);
                        Utilities.NewProjectileBetter(mouthPosition, icicleVelocity, ModContent.ProjectileType<AbyssalIce>(), 300, 0f);
                    }
                }
            }
            if (attackTimer >= riseTime + breathTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_MiniSharkFakeoutCharges(NPC npc, Player target, ref float attackTimer)
        {
            int miniSharkCount = 13;
            int hoverTime = 105;
            int reelbackTime = 45;
            int chargeTime = 96;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float chargeSpeed = 26.67f;
            float sharkShootSpeed = Lerp(8f, 12.5f, 1f - lifeRatio);

            // Sound sharks.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 1f)
            {
                for (int i = 0; i < miniSharkCount; i++)
                {
                    float offsetAngle = Lerp(-Pi, Pi, i / (float)(miniSharkCount - 1f));
                    int shark = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<MiniReaperShark>(), 300, 0f);
                    if (Main.projectile.IndexInRange(shark))
                    {
                        Main.projectile[shark].ai[0] = npc.whoAmI;
                        Main.projectile[shark].ModProjectile<MiniReaperShark>().SpinOffsetAngle = offsetAngle;
                    }
                }
            }

            // Hover to the top left/right of the target.
            if (attackTimer < hoverTime)
            {
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 600f, -350f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 24f, 0.6f);
                npc.rotation = npc.AngleTo(target.Center);
                npc.spriteDirection = (npc.Center.X > target.Center.X).ToDirectionInt();
                return;
            }

            // Reel back.
            if (attackTimer < hoverTime + reelbackTime)
            {
                npc.velocity = -npc.SafeDirectionTo(target.Center) * Utils.Remap(attackTimer - hoverTime, 0f, reelbackTime, 2.5f, 8f);
                npc.velocity.Y -= Utils.GetLerpValue(0f, reelbackTime, attackTimer - hoverTime, true) * 3.5f;
                npc.rotation = npc.AngleTo(target.Center);
            }

            // Rise into the air after the charge.
            else
            {
                npc.velocity.X *= 0.95f;
                npc.velocity.Y -= 0.3f;
                npc.rotation = npc.velocity.ToRotation();
            }

            // Charge at the target.
            if (attackTimer == hoverTime + reelbackTime)
            {
                // Make the sharks charge.
                foreach (Projectile miniShark in Utilities.AllProjectilesByID(ModContent.ProjectileType<MiniReaperShark>()))
                {
                    float chargeOffsetAngle = WrapAngle(npc.AngleTo(target.Center) - miniShark.ModProjectile<MiniReaperShark>().SpinOffsetAngle) * 0.2f;
                    Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(chargeOffsetAngle) * sharkShootSpeed;
                    miniShark.velocity = chargeVelocity;
                    miniShark.netUpdate = true;
                }

                Utilities.CreateShockwave(npc.Center, 2, 8, 75, false);
                SoundEngine.PlaySound(ReaperShark.EnragedRoarSound, target.Center);
                npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                npc.netUpdate = true;
            }

            if (attackTimer >= hoverTime + reelbackTime + chargeTime && npc.Center.Y < target.Center.Y - 800f)
            {
                npc.Center = target.Center + new Vector2(Main.rand.NextFloatDirection() * 500f, -700f);
                npc.velocity *= 0.2f;
                SelectNextAttack(npc);
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            ReaperSharkAttackState currentAttack = (ReaperSharkAttackState)npc.ai[0];
            ReaperSharkAttackState nextAttack = currentAttack;

            // Handle spawn animation triggers.
            if (currentAttack == ReaperSharkAttackState.StalkTarget)
                nextAttack = ReaperSharkAttackState.RoarAnimation;
            if (currentAttack == ReaperSharkAttackState.RoarAnimation)
                nextAttack = ReaperSharkAttackState.RushAtTarget;

            if (currentAttack == ReaperSharkAttackState.RushAtTarget)
                nextAttack = ReaperSharkAttackState.UpwardCharges;
            if (currentAttack == ReaperSharkAttackState.UpwardCharges)
                nextAttack = ReaperSharkAttackState.IceBreath;
            if (currentAttack == ReaperSharkAttackState.IceBreath)
                nextAttack = ReaperSharkAttackState.MiniSharkFakeoutCharges;
            if (currentAttack == ReaperSharkAttackState.MiniSharkFakeoutCharges)
                nextAttack = ReaperSharkAttackState.RushAtTarget;

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
            Texture2D eyeTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/AbyssAIs/ReaperSharkEyes").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            SpriteEffects direction = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            float rotation = npc.rotation;
            if (npc.spriteDirection == 1)
                rotation += Pi;
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);

            for (int i = 0; i < 7; i++)
            {
                Color eyeColor = Color.White with { A = 0 } * npc.localAI[0] * Lerp(0.6f, 0f, i / 6f);
                Vector2 eyeOffset = npc.rotation.ToRotationVector2() * i * npc.velocity.Length() * -0.1f;
                ScreenOverlaysSystem.ThingsToDrawOnTopOfBlur.Add(new(eyeTexture, drawPosition + eyeOffset, npc.frame, eyeColor, rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0));
            }
            return false;
        }
        #endregion Frames and Drawcode
    }
}
