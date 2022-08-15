using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using SCalNPC = CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas;
using static CalamityMod.NPCs.SupremeCalamitas.SoulSeekerSupreme;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SoulSeekerSupremeBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SoulSeekerSupreme>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

		public override bool PreAI(NPC npc)
        {
            // Die if SCal is no longer present.
            if (CalamityGlobalNPC.SCal < 0 || !SCal.active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            npc.target = SCal.target;
            Player Target = Main.player[npc.target];
            Vector2 eyePosition = npc.Center + new Vector2(npc.spriteDirection == -1 ? 40f : -36f, 16f);
            ref float attackTimer = ref npc.Infernum().ExtraAI[0];

            // Initialize the turn rotation.
            if (npc.localAI[0] == 0f)
            {
                for (int i = 0; i < 10; i++)
                    Dust.NewDust(npc.position, npc.width, npc.height, (int)CalamityDusts.Brimstone, 0f, 0f, 100, default, 2f);
                npc.ai[1] = npc.ai[0];
                npc.localAI[0] = 1f;
            }

            // Increase DR if the target leaves SCal's arena.
            npc.Calamity().DR = NormalDR;
            if (SupremeCalamitasBehaviorOverride.Enraged)
                npc.Calamity().DR = 0.99999f;

            // Get a target
            if (npc.target < 0 || npc.target == Main.maxPlayers || Target.dead || !Target.active)
                npc.TargetClosest();

            // Target another player if the current player target is too far away
            if (!npc.WithinRange(Target.Center, CalamityGlobalNPC.CatchUpDistance200Tiles))
                npc.TargetClosest();

            npc.spriteDirection = (Target.Center.X < npc.Center.X).ToDirectionInt();

            // Shoot darts at the target.
            int shootRate = BossRushEvent.BossRushActive ? 120 : 180;
            if (attackTimer > shootRate)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC seeker = Main.npc[i];
                    if (seeker.type == npc.type)
                    {
                        if (seeker == npc)
                            SoundEngine.PlaySound(SCalNPC.BrimstoneShotSound, SCal.Center);
                        break;
                    }
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int type = ModContent.ProjectileType<BrimstoneBarrage>();
                    int damage = npc.GetProjectileDamage(type);
                    Vector2 shootVelocity = (Target.Center - eyePosition).SafeNormalize(Vector2.UnitY) * 9f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), eyePosition, shootVelocity, type, damage, 1f, Main.myPlayer);
                }
                attackTimer = 0;
                npc.netUpdate = true;
            }

            npc.dontTakeDamage = true;
            npc.knockBackResist = 0f;
            npc.position = SCal.Center - MathHelper.ToRadians(npc.ai[1]).ToRotationVector2() * 300f - npc.Size * 0.5f;

            // In the time it takes to complete the summoning circle with Vigilance seekers already
            // drift somewhat rotationally, meaning that without this check there will be a single large gap in the ring.
            if (SCal.ai[0] != (int)SupremeCalamitasBehaviorOverride.SCalAttackType.SummonSeekers)
            {
                npc.ai[1] += 0.5f;
                npc.dontTakeDamage = false;
                attackTimer++;
            }
            return false;
		}
	}
}