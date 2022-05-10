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
        public Player Target => Main.player[npc.target];
        public NPC Owner => Main.npc[NPC.crimsonBoss];
        public float OwnerAttackTime => Owner.ai[1];
        public ref float AttackTimer => ref npc.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Brain of Cthulhu");
            Main.npcFrameCount[npc.type] = 8;
            NPCID.Sets.TrailingMode[npc.type] = 0;
            NPCID.Sets.TrailCacheLength[npc.type] = 7;
        }

        public override void SetDefaults()
        {
            npc.npcSlots = 1f;
            npc.aiStyle = aiType = -1;
            npc.width = 160;
            npc.height = 110;
            npc.damage = 0;
            npc.lifeMax = 2400;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.immortal = true;
            npc.netAlways = true;
            npc.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            // Disappear if the main boss is not present.
            if (!Main.npc.IndexInRange(NPC.crimsonBoss) || !Owner.active || (BoCAttackState)(int)Owner.ai[0] != BoCAttackState.SpinPull)
            {
                npc.active = false;
                npc.netUpdate = true;
                return;
            }

            CopyOwnerAttributes();
            AttackTimer++;
        }

        public void CopyOwnerAttributes()
        {
            npc.target = Owner.target;
            npc.frame = Owner.frame;
            npc.frame.Y += npc.frame.Height * 4;
            npc.Opacity = Owner.Opacity * Utils.InverseLerp(0f, 10f, AttackTimer, true) * 0.925f;

            npc.life = Owner.life;
            npc.lifeMax = Owner.lifeMax;
            npc.dontTakeDamage = Owner.dontTakeDamage;
        }

        public override Color? GetAlpha(Color drawColor) => NPC.crimsonBoss == -1 ? (Color?)null : Owner.GetAlpha(drawColor);

        public override bool CheckActive() => false;
    }
}
