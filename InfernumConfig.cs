using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace InfernumMode
{
	[Label("Config")]
	[BackgroundColor(96, 30, 53, 216)]
	public class InfernumConfig : ModConfig
	{
		public static InfernumConfig Instance;
		public override ConfigScope Mode => ConfigScope.ClientSide;

		[Label("Boss Introduction Animations")]
		[BackgroundColor(224, 127, 180, 192)]
		[DefaultValue(true)]
		[Tooltip("Enables boss introduction animations. They only activate when Infernum Mode is active.")]
		public bool BossIntroductionAnimationsAreAllowed { get; set; }

		public override bool AcceptClientChanges(ModConfig pendingConfig, int whoAmI, ref string message) => false;
	}
}