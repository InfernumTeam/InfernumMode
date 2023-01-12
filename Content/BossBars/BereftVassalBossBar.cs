using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace InfernumMode.Content.BossBars
{
    public class BereftVassalBossBar : ModBossBar
    {
        public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame) =>
            ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/GreatSandShark/BereftVassal_Head_Boss");

        public override bool PreDraw(SpriteBatch spriteBatch, NPC npc, ref BossBarDrawParams drawParams)
        {
            return !npc.dontTakeDamage && npc.Calamity().DR < 0.99f;
        }
    }
}