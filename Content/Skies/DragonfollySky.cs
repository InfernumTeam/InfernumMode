using CalamityMod.NPCs.Bumblebirb;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class DragonfollySkyScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player)
        {
            int bumblefuckID = ModContent.NPCType<CalamityMod.NPCs.Bumblebirb.Dragonfolly>();
            bool enabled = NPC.AnyNPCs(bumblefuckID) && Main.npc[NPC.FindFirstNPC(bumblefuckID)].Infernum().ExtraAI[8] > 0f;
            return enabled;
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("InfernumMode:Dragonfolly", isActive);
        }
    }

    public class DragonfollySky : CustomSky
    {
        public bool isActive;
        public float Intensity;
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
            float Intensity = GetIntensity();
            return new Color(Vector4.Lerp(new Vector4(0.5f, 0.8f, 1f, 1f), inColor.ToVector4(), 1f - Intensity));
        }

        private bool UpdatePIndex()
        {
            int ProvType = ModContent.NPCType<Dragonfolly>();
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
                float intensity = GetIntensity();
                Main.spriteBatch.Draw(TextureAssets.BlackTile.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), new Color(52, 42, 82) * intensity);
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
