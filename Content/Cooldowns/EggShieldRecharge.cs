using CalamityMod.Cooldowns;
using Microsoft.Xna.Framework;
using Terraria.Localization;

namespace InfernumMode.Content.Cooldowns
{
    public class EggShieldRecharge : CooldownHandler
    {
        public static new string ID => "EggShieldRecharge";

        public override bool ShouldDisplay => true;

        public override LocalizedText DisplayName => Language.GetText("Mods.InfernumMode.Cooldowns.EggShieldRecharge");

        public override string Texture => "InfernumMode/Content/Cooldowns/EggShieldRecharge";

        public override Color OutlineColor => Color.Gold;

        public override Color CooldownStartColor => Color.Lerp(new(255, 233, 218), new(217, 160, 102), 1f - instance.Completion);

        public override Color CooldownEndColor => CooldownStartColor;

        public override bool SavedWithPlayer => false;
    }
}
