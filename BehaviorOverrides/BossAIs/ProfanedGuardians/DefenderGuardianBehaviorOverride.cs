using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.OverridingSystem;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using ProvidenceNPC = CalamityMod.NPCs.Providence.Providence;

namespace InfernumMode.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class DefenderGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianDefender>();

        public override int? NPCIDToDeferToForTips => ModContent.NPCType<ProfanedGuardianCommander>();

        public enum DefenderAttackType
        {
            SpawnEffects,
            HoverAndFireSpears,
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
                case DefenderAttackType.HoverAndFireSpears:
                    DoBehavior_HoverAndFireSpears(npc, target, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public void DoBehavior_HoverAndFireSpears(NPC npc, Player target, ref float attackTimer)
        {
            float spearReleaseRate = 90;
            npc.velocity *= npc.DirectionTo(new(WorldSaveSystem.ProvidenceDoorXPosition - 100f, target.Center.Y)) * 5;

            if (attackTimer % spearReleaseRate == 0)
            {
                // Play SFX if not the server.
                if (Main.netMode != NetmodeID.Server)
                {
                    SoundEngine.PlaySound(SoundID.DD2_PhantomPhoenixShot with { Volume = 1.6f }, target.Center);
                    SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact with { Volume = 1.6f }, target.Center);
                }

                // Fire projectiles.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 projectileSpawnPosition = npc.Center + new Vector2(npc.spriteDirection * -32f, 12f);

                    Vector2 shootVelocity = npc.SafeDirectionTo(target.Center) * 9f;
                    Utilities.NewProjectileBetter(projectileSpawnPosition, shootVelocity, ModContent.ProjectileType<ProfanedSpearInfernum>(), 230, 0f);
                }
            }
        }
    }
}