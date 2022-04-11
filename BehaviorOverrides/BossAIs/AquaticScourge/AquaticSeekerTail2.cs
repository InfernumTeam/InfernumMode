using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class AquaticSeekerTail2 : ModNPC
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Aquatic Seeker");

        public override void SetDefaults()
        {
            NPC.damage = 50;
            NPC.width = 16;
            NPC.height = 22;
            NPC.defense = 20;
            NPC.lifeMax = AquaticSeekerHead2.TotalLife;
            NPC.aiStyle = aiType = -1;
            NPC.knockBackResist = 0f;
            NPC.alpha = 255;
            NPC.behindTiles = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.netAlways = true;
            NPC.dontCountMe = true;
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;

        public override void AI() => AquaticSeekerBody2.DoSegmentBehavior(NPC);

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale) => NPC.lifeMax = AquaticSeekerHead2.TotalLife;

        public override bool CheckActive() => false;

        public override bool PreNPCLoot()
        {
            return false;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            for (int k = 0; k < 3; k++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);
            }
            if (NPC.life <= 0)
            {
                for (int k = 0; k < 10; k++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, hitDirection, -1f, 0, default, 1f);
                }
                Gore.NewGore(NPC.position, NPC.velocity, InfernumMode.CalamityMod.GetGoreSlot("Gores/AquaticScourgeGores/AquaticSeekerTail"), 1f);
            }
        }
    }
}
