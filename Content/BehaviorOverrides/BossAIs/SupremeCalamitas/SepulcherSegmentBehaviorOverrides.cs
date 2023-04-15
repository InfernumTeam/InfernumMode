using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.NPCs.SupremeCalamitas;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas.SepulcherHeadBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SepulcherBody1BehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SepulcherBody>();

        public override void SetDefaults(NPC npc) => DoSetDefaults(npc);

        public override bool PreAI(NPC npc) => DoAI(npc);

        public static void DoSetDefaults(NPC npc)
        {
            npc.damage = 0;
            npc.npcSlots = 5f;
            npc.width = npc.height = 48;
            if (npc.type == ModContent.NPCType<SepulcherBodyEnergyBall>())
                npc.width = npc.height = 20;

            npc.defense = 0;
            npc.lifeMax = 582000;
            npc.aiStyle = -1;
            npc.knockBackResist = 0f;
            npc.scale *= 1.2f;
            npc.alpha = 255;
            npc.behindTiles = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.canGhostHeal = false;
            npc.chaseable = true;
            npc.netAlways = true;
            npc.dontCountMe = true;
            npc.HitSound = SoundID.DD2_SkeletonHurt with { Volume = 0.925f };
        }

        public static bool DoAI(NPC npc)
        {
            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            if (!aheadSegment.active || npc.realLife <= -1)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                    return false;

                npc.life = 0;
                npc.HitEffect();
                npc.active = false;
                npc.netUpdate = true;
            }
            NPC head = Main.npc[npc.realLife];

            // Inherit various attributes from the ahead segment.
            // This code will go upstream across every segment, until it reaches the head.
            npc.Opacity = aheadSegment.Opacity;
            npc.chaseable = true;
            npc.friendly = false;
            npc.dontTakeDamage = aheadSegment.dontTakeDamage;
            npc.defDamage = 200;
            npc.damage = npc.dontTakeDamage ? 0 : npc.defDamage;
            npc.defense = 20;
            npc.Calamity().DR = 0.56f;

            npc.buffImmune[ModContent.BuffType<ExoFreeze>()] = true;
            npc.buffImmune[ModContent.BuffType<GlacialState>()] = true;
            npc.buffImmune[ModContent.BuffType<Eutrophication>()] = true;
            npc.buffImmune[ModContent.BuffType<TemporalSadness>()] = true;
            npc.buffImmune[ModContent.BuffType<MiracleBlight>()] = true;

            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.04f);

            directionToNextSegment = directionToNextSegment.SafeNormalize(Vector2.Zero);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment * npc.width * npc.scale * 0.725f;
            npc.spriteDirection = (directionToNextSegment.X > 0).ToDirectionInt();

            // Disable dart spreads from the energy balls if Sepulcher is creating soul bombs.
            var attackType = (SepulcherAttackType)head.ai[1];
            if (npc.type == ModContent.NPCType<SepulcherBodyEnergyBall>() && (attackType == SepulcherAttackType.SoulBombBursts || attackType == SepulcherAttackType.ErraticCharges))
                npc.ModNPC<SepulcherBodyEnergyBall>().AttackTimer = 0f;

            return false;
        }
    }

    public class SepulcherBody2BehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SepulcherBodyEnergyBall>();

        public override void SetDefaults(NPC npc) => SepulcherBody1BehaviorOverride.DoSetDefaults(npc);

        public override bool PreAI(NPC npc) => SepulcherBody1BehaviorOverride.DoAI(npc);
    }

    public class SepulcherTailBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SepulcherTail>();

        public override void SetDefaults(NPC npc) => SepulcherBody1BehaviorOverride.DoSetDefaults(npc);

        public override bool PreAI(NPC npc) => SepulcherBody1BehaviorOverride.DoAI(npc);
    }
}