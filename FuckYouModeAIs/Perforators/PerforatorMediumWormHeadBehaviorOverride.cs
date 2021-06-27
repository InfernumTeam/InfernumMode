using CalamityMod.NPCs;
using CalamityMod.NPCs.Perforator;
using CalamityMod.Projectiles.Boss;
using InfernumMode.FuckYouModeAIs.BoC;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Perforators
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
                    Vector2 ichorVelocity = (npc.velocity.ToRotation() + MathHelper.Lerp(-0.43f, 0.43f, i / 3f)).ToRotationVector2() * 12f;
                    Utilities.NewProjectileBetter(npc.Center, ichorVelocity, ModContent.ProjectileType<IchorSpit>(), 80, 0f);
                }
                shootTimer = 0f;
                npc.netUpdate = true;
            }
            return true;
        }
    }
}
