using System;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using CalamityMod.World;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas.SupremeCalamitasBehaviorOverride;
using SCalNPC = CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class DarkHeartBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SoulSeekerSupreme>();


        public override bool PreAI(NPC npc)
        {
            // This is a clone of the old, pre-update AI, which is much less volatile, and what the fight was designed for.

            // Setting this in SetDefaults will disable expert mode scaling, so put it here instead
            npc.damage = 0;

            bool revenge = CalamityWorld.revenge || BossRushEvent.BossRushActive;
            npc.TargetClosest();
            float npcSpeed = (CalamityWorld.LegendaryMode && CalamityWorld.revenge) ? 8f : revenge ? 4.5f : 4f;
            float velocityMult = (CalamityWorld.LegendaryMode && CalamityWorld.revenge) ? 1f : revenge ? 0.8f : 0.75f;
            if (BossRushEvent.BossRushActive)
            {
                npcSpeed *= 2f;
                velocityMult *= 2f;
            }

            Vector2 npcCenter = new Vector2(npc.Center.X, npc.Center.Y);
            float playerXDist = Main.player[npc.target].Center.X - npcCenter.X;
            float playerYDist = Main.player[npc.target].Center.Y - npcCenter.Y - ((CalamityWorld.LegendaryMode && CalamityWorld.revenge) ? 500f : 400f);
            float playerDistance = (float)Math.Sqrt(playerXDist * playerXDist + playerYDist * playerYDist);
            if (playerDistance < ((CalamityWorld.LegendaryMode && CalamityWorld.revenge) ? 10f : 20f))
            {
                playerXDist = npc.velocity.X;
                playerYDist = npc.velocity.Y;
            }
            else
            {
                playerDistance = npcSpeed / playerDistance;
                playerXDist *= playerDistance;
                playerYDist *= playerDistance;
            }
            if (npc.velocity.X < playerXDist)
            {
                npc.velocity.X = npc.velocity.X + velocityMult;
                if (npc.velocity.X < 0f && playerXDist > 0f)
                {
                    npc.velocity.X = npc.velocity.X + velocityMult * 2f;
                }
            }
            else if (npc.velocity.X > playerXDist)
            {
                npc.velocity.X = npc.velocity.X - velocityMult;
                if (npc.velocity.X > 0f && playerXDist < 0f)
                {
                    npc.velocity.X = npc.velocity.X - velocityMult * 2f;
                }
            }
            if (npc.velocity.Y < playerYDist)
            {
                npc.velocity.Y = npc.velocity.Y + velocityMult;
                if (npc.velocity.Y < 0f && playerYDist > 0f)
                {
                    npc.velocity.Y = npc.velocity.Y + velocityMult * 2f;
                }
            }
            else if (npc.velocity.Y > playerYDist)
            {
                npc.velocity.Y = npc.velocity.Y - velocityMult;
                if (npc.velocity.Y > 0f && playerYDist < 0f)
                {
                    npc.velocity.Y = npc.velocity.Y - velocityMult * 2f;
                }
            }
            if (npc.position.X + npc.width > Main.player[npc.target].position.X && npc.position.X < Main.player[npc.target].position.X + Main.player[npc.target].width && npc.position.Y + npc.height < Main.player[npc.target].position.Y && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.ai[0] += 1f;
                if (npc.ai[0] >= (Main.getGoodWorld ? 12f : 24f))
                {
                    npc.ai[0] = 0f;
                    int shaderainXPos = (int)(npc.position.X + 10f + Main.rand.Next(npc.width - 20));
                    int shaderainYos = (int)(npc.position.Y + npc.height + 4f);
                    int type = ModContent.ProjectileType<ShaderainHostile>();
                    int damage = npc.GetProjectileDamage(type);
                    float randomXVelocity = (CalamityWorld.LegendaryMode && CalamityWorld.revenge) ? Main.rand.NextFloat() * 5f : 0f;
                    Projectile.NewProjectile(npc.GetSource_FromAI(), shaderainXPos, shaderainYos, randomXVelocity, 4f, type, damage, 0f, Main.myPlayer);
                }
            }

            return false;
        }

    }
}
