using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.SlimeGod;
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

using CrimulanSGBig = CalamityMod.NPCs.SlimeGod.CrimulanSlimeGod;
using EbonianSGBig = CalamityMod.NPCs.SlimeGod.EbonianSlimeGod;

namespace InfernumMode.BehaviorOverrides.BossAIs.SlimeGod
{
    public class SlimeGodCoreBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SlimeGodCore>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        public enum SlimeGodCoreAttackType
        {
            HoverAndDoNothing
        }
        #endregion

        #region AI

        public static bool AnyLargeSlimes => NPC.AnyNPCs(ModContent.NPCType<CrimulanSGBig>()) || NPC.AnyNPCs(ModContent.NPCType<EbonianSGBig>());

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // These debuffs are not fun.
            if (target.HasBuff(BuffID.VortexDebuff))
                target.ClearBuff(BuffID.VortexDebuff);
            if (target.HasBuff(BuffID.Cursed))
                target.ClearBuff(BuffID.Cursed);

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            // Summon the big slime.
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[3] == 0f)
            {
                int slimeGodID = WorldGen.crimson ? ModContent.NPCType<CrimulanSGBig>() : ModContent.NPCType<EbonianSGBig>();
                int fuck = NPC.NewNPC(npc.GetSource_FromAI(), (int)target.Center.X - 500, (int)target.Center.Y - 750, slimeGodID);
                Main.npc[fuck].velocity = Vector2.UnitY * 8f;
                npc.localAI[3] = 1f;
            }

            // Don't take damage if any large slimes are present.
            npc.dontTakeDamage = AnyLargeSlimes;

            // Disappear if the target is gone.
            npc.timeLeft = 3600;
            if (!target.active || target.dead || !npc.WithinRange(target.Center, 5000f))
            {
                npc.TargetClosest();
                target = Main.player[npc.target];
                if (!target.active || target.dead || !npc.WithinRange(target.Center, 5000f))
                    npc.active = false;
            }

            // Set the universal whoAmI variable.
            CalamityGlobalNPC.slimeGod = npc.whoAmI;

            switch ((SlimeGodCoreAttackType)(int)attackState)
            {
                case SlimeGodCoreAttackType.HoverAndDoNothing:
                    DoBehavior_HoverAndDoNothing(npc, target, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }
        
        public static void DoBehavior_HoverAndDoNothing(NPC npc, Player target, ref float attackTimer)
        {
            Vector2 destination = target.Center - Vector2.UnitY * 350f;
            if (!npc.WithinRange(destination, 90f))
            {
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * 13.5f, 0.85f);
                npc.rotation = npc.velocity.X * 0.15f;
            }
            else
            {
                if (npc.velocity.Length() > 4.5f)
                    npc.velocity *= 0.97f;
                npc.rotation += npc.velocity.X * 0.04f;
            }
            
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= 480f)
                SelectAttackState(npc);
        }
        public static void SelectAttackState(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.TargetClosest();

            ref float localState = ref npc.ai[0];
            float oldLocalState = localState;

            int tries = 0;
            WeightedRandom<SlimeGodCoreAttackType> newStatePicker = new(Main.rand);
            newStatePicker.Add(SlimeGodCoreAttackType.HoverAndDoNothing);
            do
            {
                localState = (int)newStatePicker.Get();
                tries++;
            }
            while (localState == oldLocalState && tries < 1000);

            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI
    }
}
