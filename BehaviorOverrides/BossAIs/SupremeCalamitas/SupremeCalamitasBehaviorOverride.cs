using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Tiles;
using InfernumMode.BehaviorOverrides.BossAIs.Twins;
using InfernumMode.BehaviorOverrides.BossAIs.Yharon;
using InfernumMode.BossIntroScreens;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

using SCalBoss = CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SupremeCalamitasBehaviorOverride : NPCBehaviorOverride
    {
        public enum SCalAttackType
        {
            HorizontalDarkSoulRelease
        }

        public enum SCalFrameType
        {
            UpwardDraft,
            FasterUpwardDraft,
            Casting,
            BlastCast,
            BlastPunchCast,
            OutwardHandCast,
            PunchHandCast,
            Count
        }

        public override int NPCOverrideType => ModContent.NPCType<SCalBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCFindFrame;

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            Vector2 handPosition = npc.Center + new Vector2(npc.spriteDirection * -18f, 2f);
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float frameType = ref npc.localAI[2];

            // Vanish if the target is gone.
            if (!target.active || target.dead)
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.1f, 0f, 1f);

                for (int i = 0; i < 2; i++)
                {
                    Dust fire = Dust.NewDustPerfect(npc.Center, (int)CalamityDusts.Brimstone);
                    fire.position += Main.rand.NextVector2Circular(36f, 36f);
                    fire.velocity = Main.rand.NextVector2Circular(8f, 8f);
                    fire.noGravity = true;
                    fire.scale *= Main.rand.NextFloat(1f, 1.2f);
                }

                if (npc.Opacity <= 0f)
                    npc.active = false;
                return false;
            }
            return false;
        }

        public static void DoBehavior_HorizontalDarkSoulRelease(NPC npc, Player target, Vector2 handPosition, ref float frameType, ref float attackTimer)
        {
            int boltBurstReleaseCount = 2;
            int shootDelay = 60;
            int shootTime = 180;
            int shootRate = 24;
            float soulShootSpeed = 17f;
            ref float boltBurstCounter = ref npc.Infernum().ExtraAI[0];

            // Use the hands out casting animation.
            frameType = (int)SCalFrameType.Casting;

            // Hover to the side of the target.
            Vector2 hoverDestination = target.Center + Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt() * 700f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 32f, 1.2f);

            if (attackTimer >= shootDelay)
            {
                // Release energy particles at the hand position.
                Dust brimstoneMagic = Dust.NewDustPerfect(handPosition, 264);
                brimstoneMagic.velocity = Vector2.UnitY.RotatedByRandom(0.14f) * Main.rand.NextFloat(-3.5f, -3f) + npc.velocity;
                brimstoneMagic.scale = Main.rand.NextFloat(1.25f, 1.35f);
                brimstoneMagic.noGravity = true;
                brimstoneMagic.noLight = true;

                // Fire the souls.
                if ((attackTimer - shootDelay) % shootRate == shootRate - 1f)
                {
                    SoundEngine.PlaySound(SoundID.NPCDeath52, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int shootCounter = (int)((attackTimer - shootDelay) / shootRate);
                        float offsetAngle = MathHelper.Lerp(-0.67f, 0.67f, shootCounter % 3f / 2f) + Main.rand.NextFloatDirection() * 0.25f;
                        Vector2 soulVelocity = (Vector2.UnitX * (target.Center.X < npc.Center.X).ToDirectionInt()).RotatedBy(offsetAngle) * soulShootSpeed;
                        Utilities.NewProjectileBetter(handPosition, soulVelocity, ModContent.ProjectileType<RedirectingDarkSoul>(), 500, 0f);
                    }
                }

                if (attackTimer >= shootDelay + shootTime)
                {
                    attackTimer = 0f;
                    boltBurstCounter++;

                    if (boltBurstCounter >= boltBurstReleaseCount)
                        SelectNewAttack(npc);
                }
            }
        }

        public static void SelectNewAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.ai[0]++;
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI

        #region Frames and Drawcode
        public override void FindFrame(NPC npc, int frameHeight)
        {
            SCalFrameType frameType = (SCalFrameType)(int)npc.localAI[2];
            npc.frameCounter += npc.localAI[1];
            npc.frameCounter %= 6;
            npc.frame.Y = (int)npc.frameCounter + (int)frameType * 6;
        }
        #endregion Frames and Drawcode
    }
}
