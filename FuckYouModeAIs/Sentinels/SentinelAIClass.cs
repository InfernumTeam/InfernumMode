using CalamityMod.NPCs;
using CalamityMod.World;
using InfernumMode.FuckYouModeAIs.Sentinels;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace InfernumMode.FuckYouModeAIs.DoG
{
    public class SentinelAIClass
    {
        public const float energyChaseVelFactor = 12f;
        public const float energyVelocityFactor = 17f;
        public static void DarkEnergyAIUniversal(NPC npc)
        {
            if (npc.Infernum().ExtraAI[0] == 0f && npc.Infernum().ExtraAI[1] == 0f)
            {
                npc.Infernum().ExtraAI[0] = 120f;
                npc.Infernum().ExtraAI[1] = 1f;
            }
            if (npc.Infernum().ExtraAI[0] > 0)
            {
                npc.Infernum().ExtraAI[0]--;
            }
            else
            {
                npc.damage = 240;
                if (CalamityWorld.revenge)
                    npc.damage = 300;
                npc.dontTakeDamage = false;
            }
            npc.TargetClosest(true);
            Player player = Main.player[npc.target];

            double mult = 0.5 +
                (CalamityWorld.revenge ? 0.2 : 0.0) +
                (CalamityWorld.death ? 0.2 : 0.0);
            if (npc.life < npc.lifeMax * mult)
            {
                npc.knockBackResist = 0f;
            }

            if (npc.ai[1] == 0f)
            {
                npc.scale -= 0.01f;
                npc.alpha += 15;
                if (npc.alpha >= 125)
                {
                    npc.alpha = 130;
                    npc.ai[1] = 1f;
                }
            }
            else if (npc.ai[1] == 1f)
            {
                npc.scale += 0.01f;
                npc.alpha -= 15;
                if (npc.alpha <= 0)
                {
                    npc.alpha = 0;
                    npc.ai[1] = 0f;
                }
            }
            if (!player.active || player.dead || CalamityGlobalNPC.voidBoss < 0 || !Main.npc[CalamityGlobalNPC.voidBoss].active)
            {
                npc.TargetClosest(false);
                player = Main.player[npc.target];
                if (!player.active || player.dead)
                {
                    npc.velocity = new Vector2(0f, -10f);
                    if (npc.timeLeft > 150)
                    {
                        npc.timeLeft = 150;
                    }
                    return;
                }
            }
            else if (npc.timeLeft < 2400)
            {
                npc.timeLeft = 2400;
            }
        }
        public static bool DarkEnergyAI1(NPC npc)
        {
            DarkEnergyAIUniversal(npc);
            npc.knockBackResist = 0f;
            npc.TargetClosest(false);
            Player player = Main.player[npc.target];
            if (npc.ai[0] == 0f)
            {
                Vector2 vectorDelta = new Vector2(600f * (player.Center.X - npc.Center.X > 0).ToDirectionInt(), 0f);
                if (npc.velocity.Length() != energyVelocityFactor)
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitX) * energyChaseVelFactor;
                npc.velocity = (npc.velocity * 20f + npc.DirectionTo(player.Center + vectorDelta) * 16f) / 21f;
                if (Math.Abs(player.Center.Y - npc.Center.Y) < 65f && Math.Abs(player.Center.X - npc.Center.X) < 600f)
                {
                    npc.velocity = new Vector2((player.Center.X - npc.Center.X > 0).ToDirectionInt() * energyVelocityFactor, 0f);
                    npc.ai[0] = 1f;
                }
            }
            else
            {
                npc.ai[2] += 1f;
                if (npc.ai[2] > 360f || (Math.Abs(player.Center.X - npc.Center.X) > 700f && npc.ai[2] >= 105))
                {
                    npc.ai[0] = 0f;
                    npc.ai[2] = 0f;
                }
            }
            if (npc.velocity.Length() < 11f)
            {
                npc.velocity = Vector2.One.RotatedByRandom(MathHelper.TwoPi) * 11f;
            }
            return false;
        }
        public static bool DarkEnergyAI2(NPC npc)
        {
            DarkEnergyAIUniversal(npc);
            npc.knockBackResist = 0f;
            npc.TargetClosest(false);
            Player player = Main.player[npc.target];
            if (npc.ai[0] == 0f)
            {
                Vector2 vectorDelta = new Vector2(0f, 600f * (player.Center.Y - npc.Center.Y > 0).ToDirectionInt());
                if (npc.velocity.Length() != energyVelocityFactor)
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * energyChaseVelFactor;
                npc.velocity = (npc.velocity * 20f + npc.DirectionTo(player.Center + vectorDelta) * 16f) / 21f;
                if (Math.Abs(player.Center.X - npc.Center.X) < 65f && Math.Abs(player.Center.Y - npc.Center.Y) < 600f)
                {
                    npc.velocity = new Vector2(0f, (player.Center.Y - npc.Center.Y > 0).ToDirectionInt() * energyVelocityFactor);
                    npc.ai[0] = 1f;
                }
            }
            else
            {
                npc.ai[2] += 1f;
                if (npc.ai[2] > 360f || (Math.Abs(player.Center.Y - npc.Center.Y) > 700f && npc.ai[2] >= 105))
                {
                    npc.ai[0] = 0f;
                    npc.ai[2] = 0f;
                }
            }
            if (npc.velocity.Length() < 11f)
            {
                npc.velocity = Vector2.One.RotatedByRandom(MathHelper.TwoPi) * 11f;
            }
            return false;
        }
        public static bool DarkEnergyAI3(NPC npc)
        {
            DarkEnergyAIUniversal(npc);
            npc.TargetClosest(false);
            Player player = Main.player[npc.target];
            npc.ai[0] += 1f;
            if (npc.ai[0] % 110f + npc.ai[2] == 60f + (int)npc.ai[2] / 2)
            {
                npc.velocity = npc.DirectionTo(player.Center).RotatedByRandom(MathHelper.ToRadians(16f)) * energyVelocityFactor;
            }
            if (npc.ai[0] % 110f + npc.ai[2] > 60f + (int)npc.ai[2] / 2)
            {
                npc.velocity *= 0.975f;
            }
            else if (npc.velocity.Length() < 14f)
            {
                npc.velocity = Vector2.One.RotatedByRandom(MathHelper.TwoPi) * 14f;
            }
            return false;
        }

        [OverrideAppliesTo("CeaselessVoid", typeof(SentinelAIClass), "CeaselessVoidPreDraw", EntityOverrideContext.NPCPreDraw, true)]
        public static bool CeaselessVoidPreDraw(NPC npc, SpriteBatch spriteBatch, Color drawColor)
        {
            if (npc.Infernum().angleTarget != default)
            {
                Utils.DrawLine(spriteBatch, npc.Center, npc.Center + npc.AngleTo(npc.Infernum().angleTarget).ToRotationVector2() * CeaselessBeam.maximumLength, new Color(215, 43, 190), new Color(148, 255, 172), 6f);
                for (int i = 0; i < Main.rand.Next(800, (int)CeaselessBeam.maximumLength); i++)
                {
                    if (Main.rand.NextBool(20))
                    {
                        var color = Utils.SelectRandom(Main.rand, new Color(215, 43, 190), new Color(148, 255, 172));
                        Dust.NewDustPerfect(npc.Center + npc.AngleTo(npc.Infernum().angleTarget).ToRotationVector2() * i, 123, Vector2.Zero, 0, color, 1f - Main.rand.NextFloat() * 0.3f);
                    }
                }
                return true;
            }
            return true;
        }
    }
}
