using CalamityMod.Cooldowns;
using Microsoft.Xna.Framework;

namespace InfernumMode.Content.Cooldowns
{
    public class SealocketForcefieldRecharge : CooldownHandler
    {
        public static new string ID => "SealocketForcefield";

        public override bool ShouldDisplay => true;

        public override string DisplayName => "Sealocket Forcefield Cooldown";

        public override string Texture => "InfernumMode/Content/Cooldowns/SealocketForcefield";

        public override Color OutlineColor => new(79, 255, 193);

        public override Color CooldownStartColor => Color.Lerp(new(149, 127, 109), new(86, 226, 208), 1f - instance.Completion);

        public override Color CooldownEndColor => CooldownStartColor;
    }
}
