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
        }

		public override void AI()
        {
            // Disappear if the main boss is not present.
            if (!Main.npc.IndexInRange(NPC.crimsonBoss) || !Owner.active || (BoCAttackState)(int)Owner.ai[0] != BoCAttackState.ConvergingIllusions)
			{
				npc.active = false;
				npc.netUpdate = true;
				return;
			}

            CopyOwnerAttributes();

            // Slow down during the start of the attack.
            if (OwnerAttackTime < 95f)
            {
                npc.velocity *= 0.96f;
                npc.Opacity = (float)Math.Pow(Owner.Opacity, 2D);
            }

            // Otherwise maintain an original offset angle but inherit the distance
            // from the target that the main boss has, while also fading away rapidly.
            else
            {
                npc.Center = Target.Center + ConvergeOffsetAngle.ToRotationVector2() * Owner.Distance(Target.Center);
                npc.alpha = Utils.Clamp(Owner.alpha + 65, 0, 255);
            }
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

        public override void HitEffect(int hitDirection, double damage)
        {
            // Register an "incorrect" hit and notify the main boss.
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                bool incorrectType = Main.npc[i].type != npc.type && Main.npc[i].type != NPCID.BrainofCthulhu;
                if (incorrectType || !Main.npc[i].active)
                    continue;

                Main.PlaySound(SoundID.Roar, Target.Center, 0);
                Main.npc[i].velocity *= 2f;
                if (Main.npc[i].type == NPCID.BrainofCthulhu && Main.npc[i].Infernum().ExtraAI[0] != 1f)
                {
                    Main.npc[i].Infernum().ExtraAI[0] = 1f;
                    Main.npc[i].netUpdate = true;
                }
            }
        }

        public override bool CheckActive() => false;
    }
}
