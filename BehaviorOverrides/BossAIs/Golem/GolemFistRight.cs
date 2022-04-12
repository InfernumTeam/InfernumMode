using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
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
            NPC.lifeMax = 1;
            NPC.defDamage = NPC.damage = 75;
            NPC.dontTakeDamage = true;
            NPC.width = 40;
            NPC.height = 40;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
        }

        public override bool PreAI() => GolemFistLeft.DoFistAI(NPC, false);

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) => GolemFistLeft.DrawFist(NPC, screenPos, drawColor, false);
    }
}