using InfernumMode.Content.BehaviorOverrides.BossAIs.Twins;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class TwinsSkySkyScene : ModSceneEffect
    {
        // This doesn't work well. If it can be made to be a better effect feel free to enable it again.
        public override bool IsSceneEffectActive(Player player) => false;

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("InfernumMode:Twins", isActive);
        }
    }

    public class TwinsSky : CustomSky
    {
        private bool isActive;
        private float intensity;

        private static Color previousScreenBackgroundColor;

        public static Color ScreenBackgroundColor
        {
            get
            {
                bool retinazerIsPresent = NPC.AnyNPCs(NPCID.Retinazer);
                bool spazmatismIsPresent = NPC.AnyNPCs(NPCID.Spazmatism);
                Color baseColor = retinazerIsPresent ? Color.IndianRed : Color.YellowGreen;
                if (!retinazerIsPresent && !spazmatismIsPresent)
                    baseColor = previousScreenBackgroundColor;
                else
                    previousScreenBackgroundColor = baseColor;

                return baseColor * TwinsAttackSynchronizer.BackgroundColorIntensity * 0.45f;
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (isActive && intensity < 1f)
            {
                intensity += 0.01f;
            }
            else if (!isActive && intensity > 0f)
            {
                intensity -= 0.01f;
            }
        }

        public override Color OnTileColor(Color inColor)
        {
            return Color.Lerp(inColor, new(0.7f, 0.6f, 0.6f, 1f), TwinsAttackSynchronizer.BackgroundColorIntensity);
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (maxDepth >= 0 && minDepth < 0)
                Main.spriteBatch.Draw(TextureAssets.BlackTile.Value, new Rectangle(0, 0, Main.screenWidth * 2, Main.screenHeight * 2), ScreenBackgroundColor);
        }

        public override float GetCloudAlpha() => 1f - TwinsAttackSynchronizer.BackgroundColorIntensity;

        public override void Activate(Vector2 position, params object[] args)
        {
            isActive = true;
        }

        public override void Deactivate(params object[] args)
        {
            isActive = false;
        }

        public override void Reset()
        {
            isActive = false;
        }

        public override bool IsActive()
        {
            return isActive || intensity > 0f;
        }
    }
}
