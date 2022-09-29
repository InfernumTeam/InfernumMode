using CalamityMod;
using CalamityMod.NPCs.StormWeaver;
using InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge;
using InfernumMode.OverridingSystem;
using InfernumMode.Projectiles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode.BehaviorOverrides.BossAIs.StormWeaver
{
    public class StormWeaverArmoredHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<StormWeaverHead>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public const float Phase2LifeRatio = 0.9f;

        public const float Phase3LifeRatio = 0.4f;

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio
        };

        #region Enumerations
        public enum StormWeaverArmoredAttackType
        {
            NormalMove,
            SparkBurst,
            LightningDischarge,
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            ref float phase2 = ref npc.Infernum().ExtraAI[20];

            if (!target.active || target.dead)
            {
                npc.active = false;
                return false;
            }

            // Don't naturally despawn.
            npc.timeLeft = 3600;

            if (npc.life < npc.lifeMax * Phase2LifeRatio)
            {
                if (phase2 == 0f)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath14, npc.Center);

                    npc.Calamity().DR = 0f;
                    npc.Calamity().unbreakableDR = false;
                    npc.chaseable = true;
                    npc.HitSound = SoundID.NPCHit13;
                    npc.DeathSound = SoundID.NPCDeath13;
                    npc.frame = new Rectangle(0, 0, 62, 86);
                    HatGirl.SayThingWhileOwnerIsAlive(target, "The Weaver has shed its exterior. It will now move far faster!");

                    phase2 = 1f;
                    npc.netUpdate = true;
                }
                return StormWeaverHeadBehaviorOverride.PreAI(npc);
            }

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.2f, 0f, 1f);

            // Create segments.
            if (npc.localAI[0] == 0f)
            {
                AquaticScourgeHeadBehaviorOverride.CreateSegments(npc, 32, ModContent.NPCType<StormWeaverBody>(), ModContent.NPCType<StormWeaverTail>());
                npc.localAI[0] = 1f;
            }

            ref float attackState = ref npc.ai[1];
            ref float attackTimer = ref npc.ai[2];

            switch ((StormWeaverArmoredAttackType)(int)attackState)
            {
                case StormWeaverArmoredAttackType.NormalMove:
                    DoAttack_NormalMove(npc, target, attackTimer);
                    break;
                case StormWeaverArmoredAttackType.SparkBurst:
                    DoAttack_SparkBurst(npc, target, attackTimer);
                    break;
                case StormWeaverArmoredAttackType.LightningDischarge:
                    DoAttack_LightningDischarge(npc, target, attackTimer);
                    break;
            }

            // Determine rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            attackTimer++;
            return false;
        }

        public static void DoAttack_NormalMove(NPC npc, Player target, float attackTimer)
        {
            float turnSpeed = (!npc.WithinRange(target.Center, 220f)).ToInt() * 0.039f;
            float moveSpeed = npc.velocity.Length();

            if (npc.WithinRange(target.Center, 285f))
                moveSpeed *= 1.015f;
            else if (npc.velocity.Length() > 24.5f + attackTimer / 35f)
                moveSpeed *= 0.98f;

            moveSpeed = MathHelper.Clamp(moveSpeed, 20f, 31f);

            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * moveSpeed;

            if (attackTimer >= 180f)
                SelectNewAttack(npc);
        }


        public static void DoAttack_SparkBurst(NPC npc, Player target, float attackTimer)
        {
            float turnSpeed = (!npc.WithinRange(target.Center, 220f)).ToInt() * 0.054f;
            float moveSpeed = npc.velocity.Length();

            if (npc.WithinRange(target.Center, 285f))
                moveSpeed *= 1.01f;
            else if (npc.velocity.Length() > 13f)
                moveSpeed *= 0.98f;

            moveSpeed = MathHelper.Clamp(moveSpeed, 13f, 25f);

            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * moveSpeed;

            if (attackTimer % 45f == 44f && !npc.WithinRange(target.Center, 210f))
            {
                // Create some mouth dust.
                for (int i = 0; i < 20; i++)
                {
                    Dust electricity = Dust.NewDustPerfect(npc.Center + npc.velocity.SafeNormalize(Vector2.Zero) * 30f, 229);
                    electricity.velocity = Main.rand.NextVector2Circular(5f, 5f) + npc.velocity;
                    electricity.scale = 1.9f;
                    electricity.noGravity = true;
                }

                SoundEngine.PlaySound(SoundID.Item94, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 11; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-0.44f, 0.44f, i / 10f);
                        Vector2 sparkVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY).RotatedBy(offsetAngle) * 8f;
                        Utilities.NewProjectileBetter(npc.Center + sparkVelocity * 3f, sparkVelocity, ModContent.ProjectileType<WeaverSpark>(), 245, 0f);
                    }
                }
            }

            if (attackTimer >= 300f)
                SelectNewAttack(npc);
        }

        public static void DoAttack_LightningDischarge(NPC npc, Player target, float attackTimer)
        {
            float turnSpeed = (!npc.WithinRange(target.Center, 220f)).ToInt() * 0.041f;
            float moveSpeed = npc.velocity.Length();

            if (npc.WithinRange(target.Center, 285f))
                moveSpeed *= 1.01f;
            else if (npc.velocity.Length() > 9f)
                moveSpeed *= 0.98f;

            moveSpeed = MathHelper.Clamp(moveSpeed, 6f, 13f);

            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * moveSpeed;

            if (attackTimer % 48f == 47f)
            {
                if (!npc.WithinRange(target.Center, 210f))
                {
                    SoundEngine.PlaySound(SoundID.Item122, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            Vector2 lightningVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * (i + Main.rand.NextFloatDirection() * 0.1f) / 5f) * 6.6f;
                            Utilities.NewProjectileBetter(npc.Center + lightningVelocity * 2f, lightningVelocity, ModContent.ProjectileType<HomingWeaverSpark>(), 245, 0f);
                        }
                        for (int i = 0; i < 11; i++)
                        {
                            Vector2 lightningVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * (i + Main.rand.NextFloatDirection() * 0.05f) / 11f) * 6.6f;
                            Utilities.NewProjectileBetter(npc.Center + lightningVelocity * 2f, lightningVelocity, ModContent.ProjectileType<WeaverSpark>(), 245, 0f);
                        }
                    }
                }
            }

            if (attackTimer >= 270f)
                SelectNewAttack(npc);
        }

        public static void SelectNewAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 4; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            ref float attackState = ref npc.ai[1];
            float oldAttackState = npc.ai[1];
            WeightedRandom<float> newStatePicker = new(Main.rand);
            newStatePicker.Add((int)StormWeaverArmoredAttackType.NormalMove, 1.5);
            newStatePicker.Add((int)StormWeaverArmoredAttackType.SparkBurst);
            newStatePicker.Add((int)StormWeaverArmoredAttackType.LightningDischarge);

            do
                attackState = newStatePicker.Get();
            while (attackState == oldAttackState);

            npc.ai[1] = (int)attackState;
            npc.ai[2] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Try to not move too far away when the Weaver spins in place, so you can see the bolts before they accelerate too much!";
        }
        #endregion Tips
    }
}
