using CalamityMod.NPCs.SupremeCalamitas;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Skies
{
    public class SCalSkyScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;
        
        public override bool IsSceneEffectActive(Player player) => !Main.gameMenu && NPC.AnyNPCs(ModContent.NPCType<SupremeCalamitas>()) && InfernumMode.CanUseCustomAIs;

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("InfernumMode:SCal", isActive);
        }
    }

    public class SCalSkyInfernum : CustomSky
    {
        public override void Deactivate(params object[] args) { }

        public override void Reset() { }

        public override bool IsActive() => !Main.gameMenu && NPC.AnyNPCs(ModContent.NPCType<SupremeCalamitas>()) && InfernumMode.CanUseCustomAIs;

        public override void Activate(Vector2 position, params object[] args) { }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth) { }

        public override void Update(GameTime gameTime) => Opacity = 1f;

        public override float GetCloudAlpha() => 1f;
    }
}
