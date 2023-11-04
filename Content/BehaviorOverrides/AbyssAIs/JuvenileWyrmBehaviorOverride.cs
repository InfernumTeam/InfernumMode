using CalamityMod;
using CalamityMod.NPCs.Abyss;
using CalamityMod.NPCs.PrimordialWyrm;
using InfernumMode.Assets.Sounds;
using InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm;
using InfernumMode.Content.Projectiles.Generic;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class JuvenileWyrmBehaviorOverride : NPCBehaviorOverride
    {
        public enum WyrmAttackState
        {
            StalkTarget,

            HammerheadRams,
            ElectricPulse,
            AbyssalSoulDash
        }

        public override int NPCOverrideType => ModContent.NPCType<EidolonWyrmHead>();

        #region AI and Behaviors

        public override bool PreAI(NPC npc)
        {
            // Pick a target if a valid one isn't already decided.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float initializedFlag = ref npc.ai[2];
            ref float eyeOpacity = ref npc.localAI[0];

            // Reset damage.
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            // Hide the wyrm from the lifeform analyzer.
            npc.rarity = 0;

            // Don't naturally despawn.
            npc.timeLeft = 7200;

            // Handle music stuff.
            npc.boss = true;
            npc.Calamity().ShouldCloseHPBar = true;
            npc.ModNPC.SceneEffectPriority = (SceneEffectPriority)9;
            npc.ModNPC.Music = (InfernumMode.CalamityMod as CalamityMod.CalamityMod).GetMusicFromMusicMod("AdultEidolonWyrm") ?? MusicID.OtherworldlyBoss2;

            // Create segments on the first frame of existence.
            if (Main.netMode != NetmodeID.MultiplayerClient && initializedFlag == 0f)
            {
                AEWHeadBehaviorOverride.CreateSegments(npc, 32, ModContent.NPCType<EidolonWyrmBody>(), ModContent.NPCType<EidolonWyrmBodyAlt>(), ModContent.NPCType<EidolonWyrmTail>());
                initializedFlag = 1f;
                npc.netUpdate = true;
            }

            // Try to get away if the AEW is coming or present.
            if (NPC.AnyNPCs(ModContent.NPCType<PrimordialWyrmHead>()) || Utilities.AnyProjectiles(ModContent.ProjectileType<TerminusAnimationProj>()))
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * -30f, 0.03f);
                npc.spriteDirection = (npc.velocity.X < 0f).ToDirectionInt();
                npc.rotation = npc.velocity.ToRotation();

                if (!npc.WithinRange(target.Center, 1950f))
                    npc.active = false;

                return false;
            }

            // Despawn if the player has left the final layer of the abyss or died.
            if (!target.Calamity().ZoneAbyssLayer4 || target.dead)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 22f, 0.1f);
                if (!npc.WithinRange(target.Center, 1300f))
                    npc.active = false;
                return false;
            }

            // Disable not taking damage from minions.
            npc.ModNPC<EidolonWyrmHead>().detectsPlayer = true;

            switch ((WyrmAttackState)attackType)
            {
                case WyrmAttackState.StalkTarget:
                    DoBehavior_StalkTarget(npc, target, ref attackTimer, ref eyeOpacity);
                    break;
                case WyrmAttackState.HammerheadRams:
                    DoBehavior_HammerheadRams(npc, target, ref attackTimer);
                    break;
                case WyrmAttackState.ElectricPulse:
                    DoBehavior_ElectricPulse(npc, target, ref attackTimer);
                    break;
                case WyrmAttackState.AbyssalSoulDash:
                    DoBehavior_AbyssalSoulDash(npc, target, ref attackTimer);
                    break;
            }

            // Decide rotation.
            npc.rotation = npc.velocity.ToRotation() + PiOver2;

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

            // Prevent reading the name when the mouse hovers over the wyrm.
            npc.GivenName = " ";

            // Handle teleports.
            if (nextTeleportDelay > 0f)
                nextTeleportDelay--;
            else if (attackTimer < stalkTime)
            {
                // Play a roar sound.
                soundSlotID = SoundEngine.PlaySound(PrimordialWyrmHead.ChargeSound, target.Center).ToFloat();

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
            Vector2 left = npc.velocity.RotatedBy(-0.0084f);
            Vector2 right = npc.velocity.RotatedBy(0.0084f);
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

        public static void DoBehavior_HammerheadRams(NPC npc, Player target, ref float attackTimer)
        {
            int attackDelay = 90;
            int ramTime = 38;
            int redirectTime = 26;
            int ramCount = 4;
            float chargeSpeed = 33f;
            float chargeAcceleration = 1.06f;
            float wrappedAttackTimer = (attackTimer - attackDelay) % (ramTime + redirectTime);
            ref float ramCounter = ref npc.Infernum().ExtraAI[0];

            // Roar on the first frame as a warning.
            if (attackTimer == 1f)
            {
                SoundEngine.PlaySound(PrimordialWyrmHead.ChargeSound, target.Center);
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 10f, 0.7f);
            }

            // Look at the player before attacking.
            if (attackTimer < attackDelay)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 10f, 0.24f);
                npc.Opacity = Clamp(npc.Opacity + 0.04f, 0f, 1f);
                return;
            }

            // Do the charge.
            if (wrappedAttackTimer < redirectTime)
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.2f) * 0.96f;
            else if (npc.velocity.Length() < chargeSpeed)
                npc.velocity *= chargeAcceleration;

            if (wrappedAttackTimer == redirectTime + ramTime - 1f)
            {
                ramCounter++;
                if (ramCounter >= ramCount)
                    SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_ElectricPulse(NPC npc, Player target, ref float attackTimer)
        {
            int shootDelay = 90;
            int pulseReleaseRate = 120;
            int shootTime = 480;
            float pulseMaxRadius = 700f;

            // Slowly move towards the target.
            Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * 6.7f;
            if (npc.WithinRange(target.Center, 200f))
                npc.velocity = (npc.velocity * 1.01f).ClampMagnitude(0f, idealVelocity.Length() * 1.5f);
            else
                npc.velocity = Vector2.Lerp(npc.velocity, idealVelocity, 0.125f);

            if (attackTimer < shootDelay)
                return;

            // Release electric pulses and bolts.
            if (attackTimer % pulseReleaseRate == pulseReleaseRate - 1f)
            {
                SoundEngine.PlaySound(SoundID.DD2_WitherBeastAuraPulse, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 24; i++)
                    {
                        Vector2 electricBoltVelocity = (TwoPi * i / 24f).ToRotationVector2() * Main.rand.NextFloat(18f, 23f) + Main.rand.NextVector2Circular(1.6f, 1.6f);
                        Utilities.NewProjectileBetter(npc.Center, electricBoltVelocity, ProjectileID.MartianTurretBolt, 275, 0f);
                    }

                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ElectricPulse>(), 300, 0f, -1, 0f, pulseMaxRadius);
                }
            }

            if (attackTimer >= shootDelay + shootTime)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_AbyssalSoulDash(NPC npc, Player target, ref float attackTimer)
        {
            int attackDelay = 45;
            int ramTime = 46;
            int redirectTime = 31;
            int ramCount = 3;
            float chargeSpeed = 29f;
            float chargeAcceleration = 1.09f;
            float wrappedAttackTimer = (attackTimer - attackDelay) % (ramTime + redirectTime);
            ref float ramCounter = ref npc.Infernum().ExtraAI[0];

            // Roar on the first frame as a warning.
            if (attackTimer == 1f)
            {
                SoundEngine.PlaySound(PrimordialWyrmHead.ChargeSound, target.Center);
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 10f, 0.7f);
            }

            // Look at the player before attacking.
            if (attackTimer < attackDelay)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 13f, 0.24f);
                npc.Opacity = Clamp(npc.Opacity + 0.04f, 0f, 1f);
                return;
            }

            // Do the charge.
            if (wrappedAttackTimer < redirectTime)
                npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), 0.26f) * 0.95f;
            else if (npc.velocity.Length() < chargeSpeed)
                npc.velocity *= chargeAcceleration;

            // Release a bunch of souls from behind during the charge.
            if (wrappedAttackTimer == redirectTime + 2f)
            {
                SoundEngine.PlaySound(InfernumSoundRegistry.AEWIceBurst, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 15; i++)
                    {
                        Vector2 abyssalSoulSpawnPosition = npc.Center - npc.velocity.SafeNormalize(Vector2.Zero) * 700f + Main.rand.NextVector2Circular(250f, 250f);
                        Vector2 abyssalSoulVelocity = npc.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(19f, 24f);
                        Utilities.NewProjectileBetter(abyssalSoulSpawnPosition, abyssalSoulVelocity, ModContent.ProjectileType<SimpleAbyssalSoul>(), 275, 0f);
                    }
                }
            }

            if (wrappedAttackTimer == redirectTime + ramTime - 1f)
            {
                ramCounter++;
                if (ramCounter >= ramCount)
                    SelectNextAttack(npc);
            }
        }

        public static void SelectNextAttack(NPC npc)
        {
            WyrmAttackState currentAttack = (WyrmAttackState)npc.ai[0];
            WyrmAttackState nextAttack = currentAttack;

            // Handle spawn animation triggers.
            if (currentAttack == WyrmAttackState.StalkTarget)
                nextAttack = WyrmAttackState.HammerheadRams;

            if (currentAttack == WyrmAttackState.HammerheadRams)
                nextAttack = WyrmAttackState.ElectricPulse;
            if (currentAttack == WyrmAttackState.ElectricPulse)
                nextAttack = WyrmAttackState.AbyssalSoulDash;
            if (currentAttack == WyrmAttackState.AbyssalSoulDash)
                nextAttack = WyrmAttackState.HammerheadRams;

            npc.ai[0] = (int)nextAttack;
            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.netUpdate = true;
        }

        #endregion AI and Behaviors
    }
}
