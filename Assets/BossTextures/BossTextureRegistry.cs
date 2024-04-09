using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.ModLoader;

namespace InfernumMode.Assets.BossTextures
{
    public class BossTextureRegistry : ModSystem
    {
        #region Constants
        public const string BasePath = "InfernumMode/Assets/BossTextures";
        #endregion

        #region Textures
        #region DesertScourge
        public static Asset<Texture2D> DesertScourgeBody { get; private set; }
        public static Asset<Texture2D> DesertScourgeHead { get; private set; }
        public static Asset<Texture2D> DesertScourgeTail { get; private set; }
        public static string DesertScourgeMapIcon { get; private set; }
        #endregion
        #region Slime God
        public static Asset<Texture2D> SlimeGodCore { get; private set; }
        public static Asset<Texture2D> CrimulanPaladin { get; private set; }
        public static Asset<Texture2D> EbonianPaladin { get; private set; }
        public static string SlimeGodCoreMapIcon { get; private set; }
        public static string CrimulanPaladinMapIcon { get; private set; }
        public static string EbonianPaladinMapIcon { get; private set; }
        #endregion
        #endregion

        #region Loading
        internal static void LoadBossHeadTextures(Mod mod)
        {
            DesertScourgeMapIcon = $"{BasePath}/DesertScourge/DesertScourgeHead_Head_Boss";
            mod.AddBossHeadTexture(DesertScourgeMapIcon);
            SlimeGodCoreMapIcon = $"{BasePath}/SlimeGod/SlimeGodCore_Head_Boss";
            mod.AddBossHeadTexture(SlimeGodCoreMapIcon);
            CrimulanPaladinMapIcon = $"{BasePath}/SlimeGod/CrimulanPaladin_Head_Boss";
            mod.AddBossHeadTexture(CrimulanPaladinMapIcon);
            EbonianPaladinMapIcon = $"{BasePath}/SlimeGod/EbonianPaladin_Head_Boss";
            mod.AddBossHeadTexture(EbonianPaladinMapIcon);
        }

        public override void Load()
        {
            DesertScourgeBody = ModContent.Request<Texture2D>($"{BasePath}/DesertScourge/DesertScourgeBody");
            DesertScourgeHead = ModContent.Request<Texture2D>($"{BasePath}/DesertScourge/DesertScourgeHead");
            DesertScourgeTail = ModContent.Request<Texture2D>($"{BasePath}/DesertScourge/DesertScourgeTail");
            SlimeGodCore = ModContent.Request<Texture2D>($"{BasePath}/SlimeGod/SlimeGodCore");
            CrimulanPaladin = ModContent.Request<Texture2D>($"{BasePath}/SlimeGod/CrimulanPaladin");
            EbonianPaladin = ModContent.Request<Texture2D>($"{BasePath}/SlimeGod/EbonianPaladin");
        }

        public override void Unload()
        {
            DesertScourgeBody = null;
            DesertScourgeHead = null;
            DesertScourgeTail = null;
            SlimeGodCore = null;
            CrimulanPaladin = null;
            EbonianPaladin = null;
        }
        #endregion
    }
}
