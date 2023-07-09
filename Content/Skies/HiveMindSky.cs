using CalamityMod.NPCs.HiveMind;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class HiveMindSkyScene : ModSceneEffect
    {
        public override bool IsSceneEffectActive(Player player)
        {
            int hiveMindID = InfernumMode.CalamityMod.Find<ModNPC>("HiveMind").Type;
            int hiveMind = NPC.FindFirstNPC(hiveMindID);
            NPC hiveMindNPC = hiveMind >= 0 ? Main.npc[hiveMind] : null;
            bool enabled = hiveMindNPC != null && (hiveMindNPC.Infernum().ExtraAI[10] == 1f || hiveMindNPC.life < hiveMindNPC.lifeMax * 0.2f);
            return enabled;
        }

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("InfernumMode:HiveMind", isActive);
        }
    }

    public class HiveMindSky : CustomSky
    {
        private bool isActive;
        private float intensity;
        private int HiveIndex = -1;

        public static readonly Color SkyColor = new(52, 42, 82);

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
                if (HiveIndex != -1)
                    x = Vector2.Distance(Main.player[Main.myPlayer].Center, Main.npc[HiveIndex].Center);

                float colorFadeInterpolant = 1f - Utils.SmoothStep(3000f, 6000f, x);
                return (0.65f + (Main.npc[HiveIndex].life < Main.npc[HiveIndex].lifeMax * 0.2f || Main.npc[HiveIndex].Infernum().ExtraAI[10] == 1f ? 0.15f : 0f)) * colorFadeInterpolant;
            }
            return 0.7f; //0.5
        }

        public override Color OnTileColor(Color inColor)
        {
            float intensity = GetIntensity();
            return new Color(Vector4.Lerp(new Vector4(0.5f, 0.8f, 1f, 1f), inColor.ToVector4(), 1f - intensity));
        }

        private bool UpdatePIndex()
        {
            int ProvType = ModContent.NPCType<HiveMind>();
            if (HiveIndex >= 0 && Main.npc[HiveIndex].active && Main.npc[HiveIndex].type == ProvType)
            {
                return true;
            }
            HiveIndex = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ProvType)
                {
                    HiveIndex = i;
                    break;
                }
            }
            return HiveIndex != -1;
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (maxDepth >= 0 && minDepth < 0)
            {
                float intensity = GetIntensity();
                Main.spriteBatch.Draw(TextureAssets.BlackTile.Value, new Rectangle(0, 0, Main.screenWidth * 2, Main.screenHeight * 2), SkyColor * intensity);
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
