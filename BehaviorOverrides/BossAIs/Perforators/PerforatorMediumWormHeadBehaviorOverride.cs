using CalamityMod;
using CalamityMod.Events;
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

            int burstCount = 4;
            float burstSpeed = 10f;
            int shootRate = (int)MathHelper.Lerp(100f, 45f, 1f - npc.life / (float)npc.lifeMax);
            if (BossRushEvent.BossRushActive)
            {
                burstCount = 9;
                burstSpeed = 21f;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && shootTimer >= shootRate)
            {
                for (int i = 0; i < burstCount; i++)
                {
                    Vector2 ichorVelocity = (npc.velocity.ToRotation() + MathHelper.Lerp(-0.53f, 0.53f, i / (float)(burstCount - 1f))).ToRotationVector2() * burstSpeed;
                    Utilities.NewProjectileBetter(npc.Center, ichorVelocity, ModContent.ProjectileType<IchorSpit>(), 95, 0f);
                }
                shootTimer = 0f;
                npc.netUpdate = true;
            }
            npc.Calamity().DR = 0.1f;
            return true;
        }
    }
}
