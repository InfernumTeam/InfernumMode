using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Skies
{
    public class MadnessScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

        // Fix this later.
        public override bool IsSceneEffectActive(Player player) => false;

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("InfernumMode:Madness", isActive);
        }
    }

    public class MadnessSky : CustomSky
    {
        public override void Deactivate(params object[] args) { }

        public override void Reset() { }

        public override bool IsActive() => !Main.gameMenu && NPC.AnyNPCs(NPCID.Deerclops);

        public override void Activate(Vector2 position, params object[] args) { }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth) { }

        public override void Update(GameTime gameTime) => Opacity = 1f;

        public override float GetCloudAlpha() => 1f;
    }
}
