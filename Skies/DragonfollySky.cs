using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Effects;

namespace InfernumMode.Skies
{
    public class DragonfollySky : CustomSky
    {
        public bool isActive = false;
        public float Intensity = 0f;
        public int BirdbrainIndex = -1;

        public override void Update(GameTime gameTime)
        {
            if (isActive && Intensity < 1f)
                Intensity += 0.01f;
            else if (!isActive && Intensity > 0f)
                Intensity -= 0.01f;
        }

        private float GetIntensity()
        {
            return UpdatePIndex() ? Main.npc[BirdbrainIndex].Infernum().ExtraAI[8] : 0.5f;
        }

        public override Color OnTileColor(Color inColor)
        {
            float Intensity = this.GetIntensity();
            return new Color(Vector4.Lerp(new Vector4(0.5f, 0.8f, 1f, 1f), inColor.ToVector4(), 1f - Intensity));
        }

        private bool UpdatePIndex()
        {
            int ProvType = InfernumMode.CalamityMod.NPCType("HiveMindP2");
            if (BirdbrainIndex >= 0 && Main.npc[BirdbrainIndex].active && Main.npc[BirdbrainIndex].type == ProvType)
            {
                return true;
            }
            BirdbrainIndex = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ProvType)
                {
                    BirdbrainIndex = i;
                    break;
                }
            }
            return BirdbrainIndex != -1;
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (maxDepth >= 0 && minDepth < 0)
            {
                float Intensity = this.GetIntensity();
                spriteBatch.Draw(Main.blackTileTexture, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), InfernumMode.HiveMindSkyColor * Intensity);
            }
        }

        public override float GetCloudAlpha()
        {
            return 0f;
        }

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
            return isActive || Intensity > 0f;
        }
    }
}
