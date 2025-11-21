using System.ComponentModel;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace InfernumMode.Core
{
    [BackgroundColor(96, 30, 53, 216)]
    public class InfernumConfig : ModConfig
    {
        public static InfernumConfig Instance => ModContent.GetInstance<InfernumConfig>();

        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Header("BaseInfernum")]
        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(true)]
        public bool BossIntroductionAnimationsAreAllowed { get; set; }

        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(true)]
        public bool DisplayTipsInChat { get; set; }

        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(false)]
        public bool ReducedGraphicsConfig { get; set; }

        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(true)]
        public bool FlashbangOverlays { get; set; }

        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(true)]
        public bool CameraAnimations { get; set; }

        [Header("Patch")]
        [DefaultValue(true)]
        [BackgroundColor(64, 171, 229, 192)]
        public bool SuperScaler { get; set; }

        [Range(0, 1)]
        [BackgroundColor(64, 171, 229, 192)]
        [DefaultValue(0.8f)]
        public float WallSpeed { get; set; }

        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref NetworkText message) => false;
    }
}
