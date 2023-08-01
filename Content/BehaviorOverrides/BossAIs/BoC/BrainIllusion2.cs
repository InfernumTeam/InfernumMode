using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.BoC.BoCBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.BoC
{
    public class BrainIllusion2 : ModNPC
    {
        public Player Target => Main.player[NPC.target];

        public static NPC Owner => Main.npc[NPC.crimsonBoss];

        public ref float AttackTimer => ref NPC.ai[1];

        public override string Texture => $"Terraria/Images/NPC_{NPCID.BrainofCthulhu}";

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            // DisplayName.SetDefault("Brain of Cthulhu");
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.TrailCacheLength[NPC.type] = 7;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 1f;
            NPC.aiStyle = AIType = -1;
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

        public override Color? GetAlpha(Color drawColor) => NPC.crimsonBoss == -1 ? null : global::InfernumMode.Content.BehaviorOverrides.BossAIs.BoC.BrainIllusion2.Owner.GetAlpha(drawColor);

        public override bool CheckActive() => false;
    }
}
