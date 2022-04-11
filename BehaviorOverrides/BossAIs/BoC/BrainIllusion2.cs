using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.BehaviorOverrides.BossAIs.BoC.BoCBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.BossAIs.BoC
{
    public class BrainIllusion2 : ModNPC
    {
        public Player Target => Main.player[NPC.target];
        public NPC Owner => Main.npc[NPC.crimsonBoss];
        public float OwnerAttackTime => Owner.ai[1];
        public ref float AttackTimer => ref NPC.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brain of Cthulhu");
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.TrailCacheLength[NPC.type] = 7;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 1f;
            NPC.aiStyle = aiType = -1;
            NPC.width = 160;
            NPC.height = 110;
            NPC.damage = 0;
            NPC.lifeMax = 2400;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.immortal = true;
            NPC.netAlways = true;
            NPC.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            // Disappear if the main boss is not present.
            if (!Main.npc.IndexInRange(NPC.crimsonBoss) || !Owner.active || (BoCAttackState)(int)Owner.ai[0] != BoCAttackState.SpinPull)
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            CopyOwnerAttributes();
            AttackTimer++;
        }

        public void CopyOwnerAttributes()
        {
            NPC.target = Owner.target;
            NPC.frame = Owner.frame;
            NPC.frame.Y += NPC.frame.Height * 4;
            NPC.Opacity = Owner.Opacity * Utils.GetLerpValue(0f, 10f, AttackTimer, true) * 0.925f;

            NPC.life = Owner.life;
            NPC.lifeMax = Owner.lifeMax;
            NPC.dontTakeDamage = Owner.dontTakeDamage;
        }

        public override Color? GetAlpha(Color drawColor) => NPC.crimsonBoss == -1 ? (Color?)null : Owner.GetAlpha(drawColor);

        public override bool CheckActive() => false;
    }
}
