using CalamityMod.Dusts;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Calamitas;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.CalamitasClone
{
    public class SoulSeekerBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SoulSeeker>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            // Setting this in SetDefaults will disable expert mode scaling, so put it here instead
            npc.damage = 0;

            if (CalamityGlobalNPC.calamitas < 0 || !Main.npc[CalamityGlobalNPC.calamitas].active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            NPC parent = Main.npc[CalamityGlobalNPC.calamitas];
            if (npc.localAI[0] == 0f)
            {
                for (int d = 0; d < 15; d++)
                    Dust.NewDust(npc.position, npc.width, npc.height, (int)CalamityDusts.Brimstone, 0f, 0f, 100, default, 2f);

                npc.localAI[0] = 1f;
            }

            npc.TargetClosest();

            Vector2 velocity = npc.SafeDirectionTo(Main.player[npc.target].Center) * 9f;
            npc.rotation = velocity.ToRotation() + MathHelper.Pi;

            npc.ai[2]++;
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[2] % 180f == 179f)
            {
                for (int d = 0; d < 3; d++)
                    Dust.NewDust(npc.position, npc.width, npc.height, (int)CalamityDusts.Brimstone, 0f, 0f, 100, default, 2f);

                int type = ModContent.ProjectileType<BrimstoneBarrage>();
                Utilities.NewProjectileBetter(npc.Center, velocity, type, 155, 1f, npc.target, 1f, 0f);
            }

            npc.position = parent.Center - npc.ai[0].ToRotationVector2() * 180f - npc.Size * 0.5f;
            npc.ai[0] += MathHelper.ToRadians(0.5f);
            return false;
        }
    }
}
