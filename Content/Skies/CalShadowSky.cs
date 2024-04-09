using CalamityMod.NPCs.CalClone;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class CalShadowSkyScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player)
        {
            int calShadowID = ModContent.NPCType<CalamitasClone>();
            int calShadowIndex = NPC.FindFirstNPC(calShadowID);
            NPC calShadowNPC = calShadowIndex >= 0 ? Main.npc[calShadowIndex] : null;
            bool enabled = calShadowNPC != null && calShadowNPC.localAI[1] > 0f;
            return enabled;
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("InfernumMode:CalShadow", isActive);
        }
    }

    public class CalShadowSky : CustomSky
    {
        private bool isActive;
        private float intensity;
        private int CalShadowIndex = -1;

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

        private float GetIntensity()
        {
            if (UpdatePIndex())
            {
                float x = 0f;
                if (CalShadowIndex != -1)
                {
                    x = Vector2.Distance(Main.player[Main.myPlayer].Center, Main.npc[CalShadowIndex].Center);
                }
                return (1f - Utils.SmoothStep(3000f, 6000f, x)) * Main.npc[CalShadowIndex].localAI[1];
            }
            return 0f;
        }

        public override Color OnTileColor(Color inColor)
        {
            return inColor * Lerp(1f, 5f, GetIntensity());
        }

        private bool UpdatePIndex()
        {
            int calShadowType = ModContent.NPCType<CalamitasClone>();
            if (CalShadowIndex >= 0 && Main.npc[CalShadowIndex].active && Main.npc[CalShadowIndex].type == calShadowType)
                return true;

            CalShadowIndex = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == calShadowType)
                {
                    CalShadowIndex = i;
                    break;
                }
            }
            return CalShadowIndex != -1;
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (maxDepth >= 0 && minDepth < 0)
            {
                float intensity = GetIntensity();
                Main.spriteBatch.Draw(TextureAssets.BlackTile.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.Black * intensity);
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
            return isActive || intensity > 0f;
        }
    }
}
