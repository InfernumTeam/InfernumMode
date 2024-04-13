using CalamityMod.NPCs;
using Terraria;
using Terraria.Graphics.Shaders;

namespace InfernumMode.Content.Skies
{
    public class AresScreenShaderData(string passName) : ScreenShaderData(passName)
    {
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
