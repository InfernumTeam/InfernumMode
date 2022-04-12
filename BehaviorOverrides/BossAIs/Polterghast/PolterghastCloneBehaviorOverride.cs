using CalamityMod.NPCs;
using CalamityMod.NPCs.Polterghast;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.Polterghast
{
    public class PolterghastCloneBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PolterPhantom>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            npc.scale = 0.7f;

            // Disappear if the main boss is not present.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.ghostBoss))
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            NPC polterghast = Main.npc[CalamityGlobalNPC.ghostBoss];
            Player target = Main.player[polterghast.target];

            // Manually set the number of souls released by Polter.
            // These will be brought back once these clones disappear.
            polterghast.ai[2] = 54f;
            polterghast.scale = 0.85f;

            float attackTimer = polterghast.ai[1] % 180f;

            if (attackTimer > 50f)
            {
                npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * polterghast.velocity.Length();
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            }
            else
            {
                npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;
                if (attackTimer < 40f && !npc.WithinRange(target.Center, 300f))
                {
                    Vector2 destination = target.Center - Vector2.UnitY * 300f;
                    destination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 240f;

                    npc.velocity = (npc.velocity * 15f + npc.SafeDirectionTo(destination) * 18f) / 16f;
                }
                else
                    npc.velocity *= 0.97f;
            }

            if (attackTimer > 100f)
                npc.Opacity = MathHelper.Lerp(1f, 0.35f, Utils.GetLerpValue(120f, 100f, attackTimer, true));
            else
                npc.Opacity = MathHelper.Lerp(1f, 0.05f, Utils.GetLerpValue(45f, 30f, attackTimer, true));

            if (attackTimer == 120f || Main.npc[CalamityGlobalNPC.ghostBoss].Infernum().ExtraAI[6] > 0f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 18; i++)
                    {
                        Vector2 shootVelocity = npc.SafeDirectionTo(polterghast.Center).RotatedByRandom(0.4f) * Main.rand.NextFloat(15f, 20f);
                        int soul = Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<NotSpecialSoul>(), 0, 0f);
                        if (Main.projectile.IndexInRange(soul))
                            Main.projectile[soul].timeLeft = 20;
                    }

                    npc.active = false;
                    npc.netUpdate = true;
                }
                SoundEngine.PlaySound(SoundID.NPCHit36, npc.Center);
                return false;
            }

            return false;
        }
    }
}
