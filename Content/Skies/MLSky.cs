using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class MLSkyScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        public override bool IsSceneEffectActive(Player player) => !Main.gameMenu && NPC.AnyNPCs(NPCID.MoonLordCore) && InfernumMode.CanUseCustomAIs;

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("InfernumMode:MoonLord", isActive);
        }
    }

    public class MLSky : CustomSky
    {
        public override void Deactivate(params object[] args) { }

        public override void Reset() { }

        public override bool IsActive() => !Main.gameMenu && NPC.AnyNPCs(NPCID.MoonLordCore) && InfernumMode.CanUseCustomAIs;

        public override void Activate(Vector2 position, params object[] args) { }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth) { }

        public override void Update(GameTime gameTime) => Opacity = 1f;

        public override float GetCloudAlpha() => 1f;
    }
}
