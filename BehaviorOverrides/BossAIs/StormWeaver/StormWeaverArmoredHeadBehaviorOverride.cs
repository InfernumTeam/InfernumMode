using CalamityMod.NPCs;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.NPCs.StormWeaver;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode.BehaviorOverrides.BossAIs.StormWeaver
{
    /*
    public class StormWeaverArmoredHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<StormWeaverHead>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        public enum StormWeaverArmoredAttackType
        {
            NormalMove,
            SparkBurst,
            TailDischarge,
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            switch ((StormWeaverArmoredAttackType)(int)attackState)
            {
                case StormWeaverArmoredAttackType.NormalMove:
                    DoAttack_NormalMove(npc, target, attackTimer);
                    break;
                case StormWeaverArmoredAttackType.SparkBurst:
                    DoAttack_SparkBurst(npc, target, attackTimer);
                    break;
                case StormWeaverArmoredAttackType.TailDischarge:
                    DoAttack_TailDischarge(npc, target, attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public static void DoAttack_NormalMove(NPC npc, Player target, float attackTimer)
        {
            float turnSpeed = (!npc.WithinRange(target.Center, 220f)).ToInt() * 0.027f;
            float moveSpeed = npc.velocity.Length();

            if (!npc.WithinRange(target.Center, 285f))
                moveSpeed *= 1.015f;
            else if (npc.velocity.Length() > 13f + attackTimer / 65f)
                moveSpeed *= 0.98f;

            moveSpeed = MathHelper.Clamp(moveSpeed, 12f, 25f);

            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * moveSpeed;

            if (attackTimer >= 480f)
                SelectNewAttack(npc);
        }


        public static void DoAttack_SparkBurst(NPC npc, Player target, float attackTimer)
        {
            float turnSpeed = (!npc.WithinRange(target.Center, 220f)).ToInt() * 0.024f;
            float moveSpeed = npc.velocity.Length();

            if (!npc.WithinRange(target.Center, 285f))
                moveSpeed *= 1.01f;
            else if (npc.velocity.Length() > 13f)
                moveSpeed *= 0.98f;

            moveSpeed = MathHelper.Clamp(moveSpeed, 13f, 25f);

            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * moveSpeed;

            if (attackTimer % 75f == 74f && !npc.WithinRange(target.Center, 280f))
            {
                Main.PlaySound(SoundID.Item94, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-0.41f, 0.41f, i / 4f);
                        Vector2 sparkVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY).RotatedBy(offsetAngle) * 8f;
                        Utilities.NewProjectileBetter(npc.Center + sparkVelocity * 3f, sparkVelocity, ModContent.ProjectileType<WeaverSpark>(), 245, 0f);
                    }
                }
            }

            if (attackTimer >= 360f)
                SelectNewAttack(npc);
        }

        public static void SelectNewAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            npc.netUpdate = true;
            for (int i = 0; i < 4; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            ref float attackState = ref npc.ai[0];
            float oldAttackState = npc.ai[0];
            WeightedRandom<float> newStatePicker = new WeightedRandom<float>(Main.rand);
            newStatePicker.Add((int)StormWeaverArmoredAttackType.NormalMove, 1.5);
            newStatePicker.Add((int)StormWeaverArmoredAttackType.SparkBurst);
            newStatePicker.Add((int)StormWeaverArmoredAttackType.TailDischarge);

            do
                attackState = newStatePicker.Get();
            while (attackState == oldAttackState);
        }
        #endregion AI
    }
    */
}
