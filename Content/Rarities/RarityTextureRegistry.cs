using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace InfernumMode.Content.Rarities
{
    public class RarityTextureRegistry : ModSystem
    {
        public const string Path = "InfernumMode/Content/Rarities/Textures/";

        public static Texture2D BaseRarityGlow
        {
            get;
            private set;
        }

        public static Texture2D BaseRaritySparkleTexture
        {
            get;
            private set;
        }

        public static Texture2D BookTexture
        {
            get;
            private set;
        }

        public static Texture2D CodeSymbolTexture
        {
            get;
            private set;
        }

        public static Texture2D DropletTexture
        {
            get;
            private set;
        }

        public static Texture2D HourglassTexture
        {
            get;
            private set;
        }

        public static Texture2D HourglassSandTexture
        {
            get;
            private set;
        }

        public static Texture2D MusicNoteTextures
        {
            get;
            private set;
        }

        public static Texture2D SakuraTexture
        {
            get;
            private set;
        }

        public static Texture2D TransTexture
        {
            get;
            private set;
        }

        public override void Load()
        {
            BaseRarityGlow = ModContent.Request<Texture2D>($"{Path}BaseRarityGlow", AssetRequestMode.ImmediateLoad).Value;
            BaseRaritySparkleTexture = ModContent.Request<Texture2D>($"{Path}BaseRaritySparkleTexture", AssetRequestMode.ImmediateLoad).Value;
            BookTexture = ModContent.Request<Texture2D>($"{Path}Book", AssetRequestMode.ImmediateLoad).Value;
            CodeSymbolTexture = ModContent.Request<Texture2D>($"{Path}CodeSymbolTextures", AssetRequestMode.ImmediateLoad).Value;
            DropletTexture = ModContent.Request<Texture2D>($"{Path}DropletTexture", AssetRequestMode.ImmediateLoad).Value;
            HourglassTexture = ModContent.Request<Texture2D>($"{Path}Hourglass", AssetRequestMode.ImmediateLoad).Value;
            HourglassSandTexture = ModContent.Request<Texture2D>($"{Path}HourglassSand", AssetRequestMode.ImmediateLoad).Value;
            MusicNoteTextures = ModContent.Request<Texture2D>($"{Path}MusicNoteTextures", AssetRequestMode.ImmediateLoad).Value;
            SakuraTexture = ModContent.Request<Texture2D>($"{Path}SakuraSparkle", AssetRequestMode.ImmediateLoad).Value;
            TransTexture = ModContent.Request<Texture2D>($"{Path}TransTexture", AssetRequestMode.ImmediateLoad).Value;
        }

        public override void Unload()
        {
            BaseRarityGlow = null;
            BaseRaritySparkleTexture = null;
            BookTexture = null;
            CodeSymbolTexture = null;
            DropletTexture = null;
            HourglassTexture = null;
            HourglassSandTexture = null;
            MusicNoteTextures = null;
            SakuraTexture = null;
            TransTexture = null;
        }
    }
}
