using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Bumblebirb;
using CalamityMod.Projectiles.Boss;
using InfernumMode.FuckYouModeAIs.Twins;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

using CrimulanSGBig = CalamityMod.NPCs.SlimeGod.SlimeGodRun;
using CrimulanSGSmall = CalamityMod.NPCs.SlimeGod.SlimeGodRunSplit;
using EbonianSGBig = CalamityMod.NPCs.SlimeGod.SlimeGod;
using EbonianSGSmall = CalamityMod.NPCs.SlimeGod.SlimeGodSplit;

namespace InfernumMode.FuckYouModeAIs.SlimeGod
{
    public class SlimeGodCoreAIClass
    {
        #region Enumerations
        public enum SlimeGodCoreAttackType
        {
            HoverAndFireAbyssBalls,
            SlowHorizontalCharge,
            SlowVerticalCharge,
        }
        #endregion

        #region AI

        [OverrideAppliesTo("SlimeGodCore", typeof(SlimeGodCoreAIClass), "SlimeGodCoreAI", EntityOverrideContext.NPCAI)]
        public static bool SlimeGodCoreAI(NPC npc)
        {
            bool anyBigBois = NPC.AnyNPCs(ModContent.NPCType<CrimulanSGBig>()) || NPC.AnyNPCs(ModContent.NPCType<CrimulanSGSmall>()) || NPC.AnyNPCs(ModContent.NPCType<EbonianSGBig>()) || NPC.AnyNPCs(ModContent.NPCType<EbonianSGSmall>());
            npc.dontTakeDamage = anyBigBois;

            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // This will affect the other gods as well in terms of behavior.
            ref float universalState = ref npc.ai[0];
            ref float universalTimer = ref npc.ai[1];
            ref float localState = ref npc.ai[2];
            ref float localTimer = ref npc.ai[3];

            // Set the universal whoAmI variable.
            CalamityGlobalNPC.slimeGod = npc.whoAmI;

            universalTimer++;
            int attackLength = 900;
            if (universalState == (int)CrimulanSlimeGodAIClass.CrimulanSlimeGodAttackType.IchorSlam)
                attackLength = 270;
            if (universalState == (int)CrimulanSlimeGodAIClass.CrimulanSlimeGodAttackType.BigSlam)
                attackLength = 270;
            if (universalTimer > attackLength)
            {
                universalTimer = 0f;
                universalState = (universalState + 1f) % 3f;
                UpdateOtherSlimeAIValues();
                npc.netUpdate = true;
            }

            switch ((SlimeGodCoreAttackType)(int)localState)
            {
                case SlimeGodCoreAttackType.HoverAndFireAbyssBalls:
                    DoAttack_HoverAndFireAbyssBalls(npc, target, ref localTimer);
                    break;
                case SlimeGodCoreAttackType.SlowHorizontalCharge:
                    DoAttack_SlowHorizontalCharge(npc, target, ref localTimer);
                    break;
                case SlimeGodCoreAttackType.SlowVerticalCharge:
                    DoAttack_SlowVerticalCharge(npc, target, ref localTimer);
                    break;
            }

            localTimer++;
            return false;
        }

        public static void UpdateOtherSlimeAIValues()
        {
            IEnumerable<NPC> slimes = Main.npc.Where(n => (n.type == ModContent.NPCType<CrimulanSGBig>() || n.type == ModContent.NPCType<EbonianSGBig>()) && n.active && n.whoAmI != 255);
            foreach (NPC slime in slimes)
            {
                for (int i = 0; i < 5; i++)
                    slime.Infernum().ExtraAI[i] = 0f;
                slime.netUpdate = true;
            }
        }

        public static void DoAttack_HoverAndFireAbyssBalls(NPC npc, Player target, ref float attackTimer)
        {
            Vector2 destination = target.Center - Vector2.UnitY * 300f;
            if (!npc.WithinRange(destination, 200f))
            {
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * 10f, 0.4f);
                npc.rotation = npc.velocity.X * 0.15f;
            }
            else
                npc.rotation += npc.velocity.X * 0.04f;

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 90f == 89f)
            {
                Vector2 mineShootVelocity = npc.SafeDirectionTo(target.Center) * 12f;
                Utilities.NewProjectileBetter(npc.Center + mineShootVelocity * 3f, mineShootVelocity, ModContent.ProjectileType<ExplosiveAbyssMine>(), 90, 0f);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= 520f)
            {
                attackTimer = 0f;
                SelectAttackState(npc);
                npc.netUpdate = true;
            }
        }

        public static void DoAttack_SlowHorizontalCharge(NPC npc, Player target, ref float attackTimer)
        {
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];

            // Line up for the charge.
            if (attackSubstate == 0f)
            {
                float verticalOffsetLeniance = 65f;
                float flySpeed = 9f;
                float flyInertia = 4f;
                float horizontalOffset = 720f;
                Vector2 destination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * horizontalOffset;

                // Fly towards the destination beside the player.
                npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(destination) * flySpeed) / flyInertia;

                // If within a good approximation of the player's position, prepare charging.
                if (Math.Abs(npc.Center.X - target.Center.X) > horizontalOffset - 50f && Math.Abs(npc.Center.Y - target.Center.Y) < verticalOffsetLeniance)
                {
                    attackSubstate = 1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Prepare for the charge.
            if (attackSubstate == 1f)
            {
                int chargeDelay = 30;
                float flySpeed = 12f;
                float flyInertia = 8f;
                Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                npc.velocity = (npc.velocity * (flyInertia - 1f) + chargeVelocity) / flyInertia;

                if (attackTimer >= chargeDelay)
                {
                    attackTimer = 0f;
                    attackSubstate = 2f;
                    npc.velocity = chargeVelocity;
                    npc.netUpdate = true;
                }
            }

            // Do the actual charge.
            if (attackSubstate == 2f)
            {
                npc.rotation += (npc.velocity.X > 0f).ToDirectionInt() * 0.15f;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= 75f)
                {
                    attackTimer = 0f;
                    SelectAttackState(npc);
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoAttack_SlowVerticalCharge(NPC npc, Player target, ref float attackTimer)
        {
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];

            // Line up for the charge.
            if (attackSubstate == 0f)
            {
                float flySpeed = 8f;
                float flyInertia = 4f;
                float verticalOffset = 400f;
                Vector2 destination = target.Center - Vector2.UnitY * Math.Sign(target.Center.Y - npc.Center.Y) * verticalOffset;

                // Fly towards the destination beside the player.
                npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(destination) * flySpeed) / flyInertia;

                // If within a good approximation of the player's position, prepare charging.
                if (Math.Abs(npc.Center.Y - target.Center.Y) > verticalOffset - 50f)
                {
                    attackSubstate = 1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Prepare for the charge.
            if (attackSubstate == 1f)
            {
                int chargeDelay = 30;
                float flySpeed = 10.5f;
                float flyInertia = 8f;
                Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                npc.velocity = (npc.velocity * (flyInertia - 1f) + chargeVelocity) / flyInertia;

                if (attackTimer >= chargeDelay)
                {
                    attackTimer = 0f;
                    attackSubstate = 2f;
                    npc.velocity = chargeVelocity;
                    npc.netUpdate = true;
                }
            }

            // Do the actual charge.
            if (attackSubstate == 2f)
            {
                npc.rotation += (npc.velocity.Y > 0f).ToDirectionInt() * 0.15f;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= 75f)
                {
                    attackTimer = 0f;
                    SelectAttackState(npc);
                    npc.netUpdate = true;
                }
            }
        }

        public static void SelectAttackState(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float universalState = npc.ai[0];
            ref float localState = ref npc.ai[2];
            float oldLocalState = npc.ai[2];
            WeightedRandom<float> newStatePicker = new WeightedRandom<float>(Main.rand);
            newStatePicker.Add(0, universalState == 1f || universalState == 3f ? 3f : 1f);
            newStatePicker.Add(1, universalState == 2f ? 3f : 1f);
            newStatePicker.Add(2, universalState == 4f ? 3f : 1f);

            for (int i = 0; i < 4; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            localState = newStatePicker.Get();
            while (localState == oldLocalState)
                localState = (localState + 1f) % 3f;
        }
        #endregion AI
    }
}
