using CalamityMod;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Players
{
    public class FlowerOceanPlayer : ModPlayer
    {
        public bool VisualsActive;

        public bool MechanicsActive;

        public override void ResetEffects()
        {
            VisualsActive = false;
            MechanicsActive = false;
        }

        public override void UpdateDead()
        {
            VisualsActive = false;
            MechanicsActive = false;
        }

        public override void PostUpdateMiscEffects()
        {
            // If underwater and not in the last zone of the abyss.
            // TODO: Make this actually work :)
            if (Player.wet && !Player.Calamity().ZoneAbyssLayer4 && MechanicsActive)
                Lighting.AddLight((int)Player.Center.X, (int)Player.Center.Y, TorchID.Blue, 10f);
        }
    }
}
