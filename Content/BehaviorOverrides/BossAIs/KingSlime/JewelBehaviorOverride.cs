using CalamityMod.Events;
using CalamityMod.NPCs.NormalNPCs;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.KingSlime
{
    public class JewelBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<KingSlimeJewel>();

        public override bool PreAI(NPC npc)
        {
            ref float attackTimer = ref npc.ai[0];
            ref float backglowInterpolant = ref npc.ai[1];

            // Disappear if the main boss is not present.
            if (!NPC.AnyNPCs(NPCID.KingSlime))
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            // Idly emit dust.
            Player target = Main.player[npc.target];
            bool canReachTarget = Collision.CanHit(npc.position, npc.width, npc.height, target.position, target.width, target.height);
            int shootRate = canReachTarget ? 75 : 30;
            int dustReleaseRate = 3;
            float dustScaleFactor = 1f;
            bool unstableDust = false;
            backglowInterpolant = 0f;
            if (attackTimer % shootRate >= shootRate * 0.5f)
            {
                unstableDust = true;
                dustReleaseRate = 1;
                dustScaleFactor = 1.6f;

                backglowInterpolant = LumUtils.Convert01To010(Utils.GetLerpValue(shootRate * 0.5f, shootRate, attackTimer % shootRate, true));
            }

            if (Main.rand.NextBool(dustReleaseRate))
            {
                Dust shimmer = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.PortalBoltTrail);
                shimmer.color = Color.Red;

                if (unstableDust)
                    shimmer.velocity = (TwoPi * attackTimer / 30f).ToRotationVector2() * Main.rand.NextFloat(5f, 9f);
                else
                    shimmer.velocity = -Vector2.UnitY * Main.rand.NextFloat(2f, 3f);

                shimmer.velocity -= npc.oldPosition - npc.position;
                shimmer.scale = Main.rand.NextFloat(1f, 1.2f) * dustScaleFactor;
                shimmer.fadeIn = 0.4f;
                shimmer.noLight = true;
                shimmer.noGravity = true;
            }

            if (!Main.player.IndexInRange(npc.type) || !Main.player[npc.target].active || Main.player[npc.target].dead)
                npc.TargetClosest();

            npc.Center = target.Center - Vector2.UnitY * (350f + Sin(TwoPi * attackTimer / 120f) * 10f);

            if (shootRate >= 1 && attackTimer % shootRate == shootRate - 1f)
            {
                MakeJewelFire();
                SoundEngine.PlaySound(SoundID.Item28, target.Center);
            }

            attackTimer++;

            return false;
        }

        public static void MakeJewelFire()
        {
            int jewelIndex = NPC.FindFirstNPC(ModContent.NPCType<KingSlimeJewel>());
            if (jewelIndex <= -1 || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            NPC jewel = Main.npc[jewelIndex];
            Player target = Main.player[jewel.target];
            bool canReachTarget = Collision.CanHit(jewel.position, jewel.width, jewel.height, target.position, target.width, target.height);
            float shootSpeed = NPC.AnyNPCs(ModContent.NPCType<Ninja>()) && !canReachTarget ? 8f : 11f;
            float predictivenessFactor = canReachTarget ? 40f : 20f;
            if (BossRushEvent.BossRushActive)
            {
                shootSpeed *= 2.25f;
                predictivenessFactor = 14f;
            }

            Vector2 aimDirection = jewel.SafeDirectionTo(target.Center + target.velocity * predictivenessFactor);
            int beam = Utilities.NewProjectileBetter(jewel.Center, aimDirection * shootSpeed, ModContent.ProjectileType<JewelBeam>(), KingSlimeBehaviorOverride.JewelBeamDamage, 0f);
            if (Main.projectile.IndexInRange(beam))
                Main.projectile[beam].tileCollide = canReachTarget;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            float backglowInterpolant = npc.ai[1];
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;

            if (backglowInterpolant > 0.001f)
            {
                for (int i = 0; i < 12; i++)
                {
                    Vector2 drawOffset = (TwoPi * i / 12f).ToRotationVector2() * backglowInterpolant * 6f;
                    Main.spriteBatch.Draw(texture, drawPosition + drawOffset, npc.frame, npc.GetAlpha(Color.White) with { A = 0 }, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0f);
                }
            }

            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0f);
            return false;
        }
    }
}
