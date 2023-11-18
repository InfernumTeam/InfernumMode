using CalamityMod.UI;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Assets.Fonts
{
    public class InfernumFontRegistry : ModSystem
    {
        public static DynamicSpriteFont BossIntroScreensFont => BossHealthBarManager.HPBarFont;

        public static DynamicSpriteFont BossIntroScreensFontChinese
        {
            get;
            private set;
        }

        public static DynamicSpriteFont HPBarFont
        {
            get;
            private set;
        }

        public static DynamicSpriteFont ProfanedTextFont
        {
            get;
            private set;
        }

        public override void Load()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            BossIntroScreensFontChinese = ModContent.Request<DynamicSpriteFont>("InfernumMode/Assets/Fonts/BossIntroScreensFont", AssetRequestMode.ImmediateLoad).Value;
            HPBarFont = ModContent.Request<DynamicSpriteFont>("InfernumMode/Assets/Fonts/HPBarFont", AssetRequestMode.ImmediateLoad).Value;
            ProfanedTextFont = ModContent.Request<DynamicSpriteFont>("InfernumMode/Assets/Fonts/ProfanedText", AssetRequestMode.ImmediateLoad).Value;
        }

        public override void Unload()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            BossIntroScreensFontChinese = null;
            HPBarFont = null;
            ProfanedTextFont = null;
        }
    }
}
