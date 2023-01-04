using CalamityMod.NPCs;
using Terraria;
using Terraria.Graphics.Shaders;

namespace InfernumMode.Skies
{
    public class AresScreenShaderData : ScreenShaderData
    {
        public AresScreenShaderData(string passName)
            : base(passName)
        {
        }

        public override void Apply()
        {
            if (CalamityGlobalNPC.draedonExoMechPrime == -1 || !Main.npc[CalamityGlobalNPC.draedonExoMechPrime].active)
                return;

            NPC ares = Main.npc[CalamityGlobalNPC.draedonExoMechPrime];
            UseTargetPosition(Main.LocalPlayer.Center);
            UseIntensity(ares.localAI[3] * 0.2f);
            base.Apply();
        }
    }
}
