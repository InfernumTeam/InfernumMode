using Terraria.ModLoader;

namespace InfernumMode.Systems
{
    public class KeybindSystem : ModSystem
    {
        public static ModKeybind WayfinderCreateKey { get; private set; }

        public static ModKeybind WayfinderDestroyKey { get; private set; }

        public override void Load()
        {
            WayfinderCreateKey = KeybindLoader.RegisterKeybind(Mod, "Wayfinder Create Key", "W");
            WayfinderDestroyKey = KeybindLoader.RegisterKeybind(Mod, "Wayfinder Destroy Key", "Q");
        }
    }
}
