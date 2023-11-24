using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.CrossCompatibility
{
    public class WikiThisHandler : ModSystem
    {
        public const string WikiLink = "https://infernummod.wiki.gg/wiki/{}";

        public override void PostSetupContent()
        {
            if (!ModLoader.TryGetMod("Wikithis", out Mod wikithis) || Main.netMode == NetmodeID.Server)
                return;

            wikithis.Call("AddModURL", Mod, WikiLink);

            wikithis.Call("AddWikiTexture", Mod, ModContent.Request<Texture2D>("InfernumMode/icon_small", AssetRequestMode.ImmediateLoad));
        }
    }
}
