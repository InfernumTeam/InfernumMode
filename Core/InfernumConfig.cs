using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace InfernumMode.Core
{
    #pragma warning disable CS0618 // Type or member is obsolete
    [Label("Config")]
    [BackgroundColor(96, 30, 53, 216)]
    public class InfernumConfig : ModConfig
    {
        public static InfernumConfig Instance => ModContent.GetInstance<InfernumConfig>();

        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Label("Boss Introduction Animations")]
        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(true)]
        [Tooltip("Enables boss introduction animations. They only activate when Infernum Mode is active.")]
        public bool BossIntroductionAnimationsAreAllowed { get; set; }

        [Label("Blasted Tophat Tips in Chat")]
        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(true)]
        [Tooltip("Determines whether the pet from the Blasted Tophat should display its tips in chat or not.")]
        public bool DisplayTipsInChat { get; set; }

        [Label("Reduced Graphical Settings")]
        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(false)]
        [Tooltip("Enables reduced graphics mode. Use this if lag is an issue.")]
        public bool ReducedGraphicsConfig { get; set; }

        [Label("Saturation Bloom Intensity")]
        [BackgroundColor(224, 127, 180, 192)]
        [SliderColor(224, 165, 56, 128)]
        [Range(0f, 1f)]
        [DefaultValue(0f)]
        [Tooltip("How intense color saturation bloom effects should be. Such effects are disabled when this value is zero. Be warned that high values may be overwhelming.")]
        public float SaturationBloomIntensity { get; set; }

        [Label("Screen Overlays")]
        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(true)]
        [Tooltip("Enables screen overlay 'flashbang' effects. This will not directly affect gameplay mechanics.")]
        public bool FlashbangOverlays { get; set; }

        [Label("Boss Footage Credits Recording")]
        [BackgroundColor(224, 127, 180, 192)]
        [DefaultValue(false)]
        [Tooltip("Enables boss footage recordings for the playback during the credits.")]

        public bool CreditsRecordings { get; set; }

        public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message) => false;
    }
    #pragma warning restore CS0618 // Type or member is obsolete

}
