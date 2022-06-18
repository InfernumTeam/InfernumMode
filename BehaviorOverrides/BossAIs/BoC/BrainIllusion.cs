using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.BehaviorOverrides.BossAIs.BoC.BoCBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.BossAIs.BoC
{
    public class BrainIllusion : ModNPC
    {
        public PrimitiveTrailCopy FireDrawer;
        public Player Target => Main.player[npc.target];
        public NPC Owner => Main.npc[NPC.crimsonBoss];
        public float OwnerAttackTime => Owner.ai[1];
        public ref float ConvergeOffsetAngle => ref npc.ai[1];

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
            npc.netAlways = true;
            npc.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            // Disappear if the main boss is not present.
            if (!Main.npc.IndexInRange(NPC.crimsonBoss) || !Owner.active || (BoCAttackState)(int)Owner.ai[0] != BoCAttackState.DashingIllusions)
            {
                npc.active = false;
                npc.netUpdate = true;
                return;
            }

            CopyOwnerAttributes();

            // Maintain an original offset angle but inherit the distance
            // from the target that the main boss has, while also fading away rapidly.
            npc.Center = Target.Center + (ConvergeOffsetAngle + Owner.AngleFrom(Target.Center)).ToRotationVector2() * Owner.Distance(Target.Center);
            npc.Opacity = (float)Math.Pow(Owner.Opacity, 2D);
        }

        public void CopyOwnerAttributes()
        {
            npc.target = Owner.target;
            npc.frame = Owner.frame;
            npc.frame.Y += npc.frame.Height * 4;

            npc.life = Owner.life;
            npc.lifeMax = Owner.lifeMax;
            npc.dontTakeDamage = Owner.dontTakeDamage;
        }

        public override Color? GetAlpha(Color drawColor) => NPC.crimsonBoss == -1 ? (Color?)null : Owner.GetAlpha(drawColor);

        public override bool CheckActive() => false;
    }
}
