using CalamityMod;
using CalamityMod.NPCs.OldDuke;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.OldDuke
{
    public class OldDukeToothBallBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<OldDukeToothBall>();

        public override bool PreAI(NPC npc)
        {
            npc.rotation += npc.velocity.X * 0.05f;

            if (npc.alpha > 0)
                npc.alpha -= 15;

            npc.TargetClosest(false);
            Player player = Main.player[npc.target];
            if (!player.active || player.dead)
            {
                npc.TargetClosest(false);
                player = Main.player[npc.target];
                if (!player.active || player.dead)
                {
                    if (npc.timeLeft > 10)
                        npc.timeLeft = 10;

                    return false;
                }
            }
            else if (npc.timeLeft < 600)
                npc.timeLeft = 600;

            Vector2 targetOffset = player.Center - npc.Center;
            if (targetOffset.Length() < 40f || npc.ai[3] >= 250f)
            {
                npc.dontTakeDamage = false;
                npc.life = 0;
                npc.HitEffect(0, 10.0);
                npc.checkDead();
                npc.active = false;
                return false;
            }

            npc.ai[3]++;
            npc.dontTakeDamage = npc.ai[3] > 200f;
            if (npc.ai[3] >= 150f)
            {
                npc.velocity *= 0.985f;
                return false;
            }

            Player target = Main.player[npc.target];
            float distanceFromTarget = npc.Distance(target.Center);
            Vector2 idealVelocity = npc.SafeDirectionTo(target.Center) * 12.5f;

            npc.ai[0] -= Main.rand.Next(6);
            if (distanceFromTarget < 300f || npc.ai[0] > 0f)
            {
                if (distanceFromTarget < 300f)
                    npc.ai[0] = 100f;
                return false;
            }

            npc.velocity = (npc.velocity * 50f + idealVelocity) / 51f;
            if (distanceFromTarget < 350f)
                npc.velocity = (npc.velocity * 11f + idealVelocity) / 12f;

            float pushAwayFactor = 0.5f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && i != npc.whoAmI && Main.npc[i].type == npc.type)
                {
                    if (npc.WithinRange(Main.npc[i].Center, 48f))
                    {
                        if (npc.position.X < Main.npc[i].position.X)
                            npc.velocity.X -= pushAwayFactor;
                        else
                            npc.velocity.X += pushAwayFactor;

                        if (npc.position.Y < Main.npc[i].position.Y)
                            npc.velocity.Y -= pushAwayFactor;
                        else
                            npc.velocity.Y += pushAwayFactor;
                    }
                }
            }
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            npc.DrawBackglow(Color.Lime with { A = 0 }, 4f, 0, npc.frame, Main.screenPosition);
            return true;
        }
    }
}
