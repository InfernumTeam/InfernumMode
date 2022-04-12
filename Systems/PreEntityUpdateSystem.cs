using InfernumMode.BehaviorOverrides.BossAIs.Twins;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace InfernumMode.Systems
{
    public class PreEntityUpdateSystem : ModSystem
    {
        public override void PreUpdateEntities()
        {
            InfernumMode.BlackFade = MathHelper.Clamp(InfernumMode.BlackFade - 0.025f, 0f, 1f);
            NetcodeHandler.Update();
            TwinsAttackSynchronizer.DoUniversalUpdate();
            TwinsAttackSynchronizer.PostUpdateEffects();
        }
    }
}