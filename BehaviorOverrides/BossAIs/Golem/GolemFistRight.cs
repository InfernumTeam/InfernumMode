using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Golem
{
    public class GolemFistRight : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Golem Fist");
        }

        public override void SetDefaults()
        {
            npc.lifeMax = 1;
            npc.defDamage = npc.damage = 75;
            npc.dontTakeDamage = true;
            npc.width = 40;
            npc.height = 40;
            npc.lavaImmune = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
        }

        public override bool PreAI() => GolemFistLeft.DoFistAI(npc, false);

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor) => GolemFistLeft.DrawFist(npc, spriteBatch, drawColor, false);
    }
}