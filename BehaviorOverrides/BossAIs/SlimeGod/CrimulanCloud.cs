using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SlimeGod
{
    public class CrimulanCloud : ModNPC
    {
        public ref float Time => ref NPC.ai[0];
        public Player Target => Main.player[NPC.target];
        public override void SetStaticDefaults() => DisplayName.SetDefault("Crimulan Cloud");

        public override void SetDefaults()
        {
            NPC.damage = 0;
            NPC.width = NPC.height = 42;
            NPC.lifeMax = 20;
            NPC.noTileCollide = false;
            NPC.noGravity = true;
            NPC.netAlways = true;
            NPC.dontTakeDamage = true;
            NPC.aiStyle = aiType = -1;
            NPC.scale = 1f;
            NPC.knockBackResist = 0f;
            NPC.noTileCollide = true;
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.slimeGodRed))
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            NPC.TargetClosest();

            Vector2 destination = Target.Center + Vector2.UnitX * 450f;
            NPC.Center = NPC.Center.MoveTowards(destination, 5f);
            if (NPC.Center != destination)
                NPC.velocity = (NPC.velocity * 15f + NPC.SafeDirectionTo(destination) * 13f) / 16f;
            else
                NPC.velocity = Vector2.Zero;

            for (int i = 0; i < 4; i++)
            {
                Dust gel = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, Main.rand.NextBool(2) ? 4 : 267);
                gel.color = Color.Red;
                gel.velocity = Main.rand.NextVector2Circular(3f, 3f);
                gel.noGravity = true;
            }

            Time++;
        }

        public override bool CheckActive() => false;
    }
}
