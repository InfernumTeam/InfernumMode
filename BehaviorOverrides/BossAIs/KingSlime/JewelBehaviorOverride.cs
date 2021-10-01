using CalamityMod.NPCs.NormalNPCs;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.KingSlime
{
    public class JewelBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<KingSlimeJewel>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            ref float time = ref npc.ai[0];

            // Disappear if the main boss is not present.
            if (!NPC.AnyNPCs(NPCID.KingSlime))
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            // Idly emit dust.
            if (Main.rand.NextBool(3))
            {
                Dust shimmer = Dust.NewDustDirect(npc.position, npc.width, npc.height, 264);
                shimmer.color = Color.Red;
                shimmer.velocity = -Vector2.UnitY * Main.rand.NextFloat(2f, 3f);
                shimmer.velocity -= npc.oldPosition - npc.position;
                shimmer.scale = Main.rand.NextFloat(1f, 1.2f);
                shimmer.fadeIn = 0.4f;
                shimmer.noLight = true;
                shimmer.noGravity = true;
            }

            if (!Main.player.IndexInRange(npc.type) || !Main.player[npc.target].active || Main.player[npc.target].dead)
                npc.TargetClosest();

            Player target = Main.player[npc.target];
            npc.Center = target.Center - Vector2.UnitY * (350f + (float)Math.Sin(MathHelper.TwoPi * time / 120f) * 10f);

            time++;

            if (Main.netMode != NetmodeID.MultiplayerClient && time % 75f == 74f)
            {
                float shootSpeed = NPC.AnyNPCs(ModContent.NPCType<Ninja>()) ? 6f : 9f;
                Vector2 aimDirection = npc.SafeDirectionTo(target.Center + target.velocity * 45f);
                Utilities.NewProjectileBetter(npc.Center, aimDirection * shootSpeed, ModContent.ProjectileType<JewelBeam>(), 84, 0f);
            }

            return false;
        }
    }
}