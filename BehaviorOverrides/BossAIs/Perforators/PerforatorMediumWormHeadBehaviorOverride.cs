using CalamityMod;
using CalamityMod.NPCs.Perforator;
using InfernumMode.BehaviorOverrides.BossAIs.BoC;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Perforators
{
    public class PerforatorMediumWormHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PerforatorHeadMedium>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;
        public override bool PreAI(NPC npc)
        {
            ref float shootTimer = ref npc.Infernum().ExtraAI[0];
            shootTimer++;

            int shootRate = (int)MathHelper.Lerp(100f, 45f, 1f - npc.life / (float)npc.lifeMax);

            if (Main.netMode != NetmodeID.MultiplayerClient && shootTimer >= shootRate)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector2 ichorVelocity = (npc.velocity.ToRotation() + MathHelper.Lerp(-0.53f, 0.53f, i / 4f)).ToRotationVector2() * 10f;
                    Utilities.NewProjectileBetter(npc.Center, ichorVelocity, ModContent.ProjectileType<IchorSpit>(), 80, 0f);
                }
                shootTimer = 0f;
                npc.netUpdate = true;
            }
            npc.Calamity().DR = 0.1f;
            return true;
        }
    }
}
