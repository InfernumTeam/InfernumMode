using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ProvidenceNPC = CalamityMod.NPCs.Providence.Providence;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class DefenderGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianDefender>();

        public override int? NPCIDToDeferToForTips => ModContent.NPCType<ProfanedGuardianCommander>();

        public enum DefenderAttackType
        {
            SpawnEffects,
            HoverAndFireDeathray,
        }

        public override bool PreAI(NPC npc)
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss) || !Main.npc[CalamityGlobalNPC.doughnutBoss].active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            NPC commander = Main.npc[CalamityGlobalNPC.doughnutBoss];
            Player target = Main.player[commander.target];

            switch ((DefenderAttackType)attackState)
            {
                // They all share the same thing for heading away to the enterance.
                case DefenderAttackType.SpawnEffects:
                    AttackerGuardianBehaviorOverride.DoBehavior_SpawnEffects(npc, target, ref attackTimer);
                    break;
                case DefenderAttackType.HoverAndFireDeathray:
                    DoBehavior_HoverAndFireDeathray(npc, target, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public void DoBehavior_HoverAndFireDeathray(NPC npc, Player target, ref float attackTimer)
        {
            float deathrayFireRate = 240;
            npc.velocity *= npc.DirectionTo(new(WorldSaveSystem.ProvidenceDoorXPosition - 100f, target.Center.Y)) * 5;

            // If time to fire, and they are close enough.
            if (attackTimer % deathrayFireRate == 0 && target.WithinRange(npc.Center, 3500f))
            {
                // Fire deathray.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 aimDirection = npc.SafeDirectionTo(target.Center);
                    Utilities.NewProjectileBetter(npc.Center, aimDirection, ModContent.ProjectileType<HolyAimedDeathrayTelegraph>(), 0, 0f, -1, 0f, npc.whoAmI);
                }
            }
        }
    }
}