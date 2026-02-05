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
        #region Slime God
        public static Asset<Texture2D> SlimeGodCore { get; private set; }
        public static Asset<Texture2D> CrimulanPaladin { get; private set; }
        public static Asset<Texture2D> EbonianPaladin { get; private set; }
        public static Asset<Texture2D> PrimordialWyrmHead { get; private set; }
        public static Asset<Texture2D> PrimordialWyrmBody { get; private set; }
        public static Asset<Texture2D> PrimordialWyrmBodyAlt { get; private set; }
        public static Asset<Texture2D> PrimordialWyrmTail { get; private set; }
        public static string SlimeGodCoreMapIcon { get; private set; }
        public static string CrimulanPaladinMapIcon { get; private set; }
        public static string EbonianPaladinMapIcon { get; private set; }
        public static string PrimordialWyrmMapIcon { get; private set; }
        #endregion
        #endregion

        #region Loading
        internal static void LoadBossHeadTextures(Mod mod)
        {
            SlimeGodCoreMapIcon = $"{BasePath}/SlimeGod/SlimeGodCore_Head_Boss";
            mod.AddBossHeadTexture(SlimeGodCoreMapIcon);
            CrimulanPaladinMapIcon = $"{BasePath}/SlimeGod/CrimulanPaladin_Head_Boss";
            mod.AddBossHeadTexture(CrimulanPaladinMapIcon);
            EbonianPaladinMapIcon = $"{BasePath}/SlimeGod/EbonianPaladin_Head_Boss";
            mod.AddBossHeadTexture(EbonianPaladinMapIcon);
            PrimordialWyrmMapIcon = $"{BasePath}/PrimordialWyrm/PrimordialWyrmHead_Head_Boss";
            mod.AddBossHeadTexture(PrimordialWyrmMapIcon);
        }

        public override void Load()
        {
            SlimeGodCore = ModContent.Request<Texture2D>($"{BasePath}/SlimeGod/SlimeGodCore");
            CrimulanPaladin = ModContent.Request<Texture2D>($"{BasePath}/SlimeGod/CrimulanPaladin");
            EbonianPaladin = ModContent.Request<Texture2D>($"{BasePath}/SlimeGod/EbonianPaladin");
            PrimordialWyrmHead = ModContent.Request<Texture2D>($"{BasePath}/PrimordialWyrm/PrimordialWyrmHead");
            PrimordialWyrmBody = ModContent.Request<Texture2D>($"{BasePath}/PrimordialWyrm/PrimordialWyrmBody");
            PrimordialWyrmBodyAlt = ModContent.Request<Texture2D>($"{BasePath}/PrimordialWyrm/PrimordialWyrmBodyAlt");
            PrimordialWyrmTail = ModContent.Request<Texture2D>($"{BasePath}/PrimordialWyrm/PrimordialWyrmTail");
        }

        public override void Unload()
        {
            SlimeGodCore = null;
            CrimulanPaladin = null;
            EbonianPaladin = null;
            PrimordialWyrmHead = null;
            PrimordialWyrmBody = null;
            PrimordialWyrmBodyAlt = null;
            PrimordialWyrmTail = null;
        }
        #endregion
    }
}
