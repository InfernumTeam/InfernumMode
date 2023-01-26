using Terraria;
using Terraria.ID;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria.Audio;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class LacewingBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.EmpressButterfly;

        #region AI and Behaviors
        public override bool PreAI(NPC npc)
        {
            ref float dying = ref npc.Infernum().ExtraAI[0];
            ref float hasSummonedEmpress = ref npc.Infernum().ExtraAI[1];

            if (dying == 0f)
            {
                DoDefaultButterflyBehavior(npc);
                return false;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && hasSummonedEmpress == 0f)
            {
                NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y - 400, NPCID.HallowBoss);
                hasSummonedEmpress = 1f;
            }

            DoDeathBehavior(npc);
            npc.dontTakeDamage = true;
            npc.hide = npc.Infernum().ExtraAI[2] == 1f;

            return false;
        }

        public static void DoDefaultButterflyBehavior(NPC npc)
        {
            // Acquire a target.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            float speedX = npc.ai[0];
            float speedY = npc.ai[1];
            ref float moveAwayDelay = ref npc.localAI[1];

            // Emit rainbow light.
            Vector3 rainbowLight = Main.hslToRgb(Main.GlobalTimeWrappedHourly * 0.33f % 1f, 1f, 0.5f).ToVector3() * 0.3f + Vector3.One * 0.1f;
            Lighting.AddLight(npc.Center, rainbowLight);

            // Decide the scale of the butterfly.
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[3] == 0f)
            {
                npc.ai[3] = Main.rand.NextFloat(0.8f, 0.85f);
                npc.netUpdate = true;
            }
            npc.scale = npc.ai[3];

            // Curiously follow the player.
            if (!npc.WithinRange(target.Center, 300f))
            {
                Vector2 idealVelocity = ((target.Center - npc.Center) * 0.05f).ClampMagnitude(0f, 4f);
                speedX = idealVelocity.X;
                speedY = idealVelocity.Y;
            }
            
            else if (Main.rand.NextBool(120))
            {
                speedX = Main.rand.NextFloat(-2f, 2f);
                speedY = Main.rand.NextFloat(-2f, 2f);
                npc.netUpdate = true;
            }

            // Approach the ideal velocity.
            npc.velocity = (npc.velocity * 59f + new Vector2(speedX, speedY)) / 60f;

            // Try to not crash into the ground.
            if (npc.velocity.Y > 0f)
            {
                int centerX = (int)npc.Center.X / 16;
                int centerY = (int)npc.Center.Y / 16;
                for (int i = centerY; i < centerY + 3; i++)
                {
                    if ((Main.tile[centerX, i].HasUnactuatedTile && Main.tileSolid[Main.tile[centerX, i].TileType]) || Main.tile[centerX, i].LiquidType > 0)
                    {
                        speedY *= -1f;
                        if (npc.velocity.Y > 0f)
                            npc.velocity.Y *= 0.9f;
                    }
                }
            }

            // Try to not fly too high above the ground.
            if (npc.velocity.Y < 0f)
            {
                int centerX = (int)npc.Center.X / 16;
                int centerY = (int)npc.Center.Y / 16;
                bool groundFarBelow = true;
                for (int i = centerY; i < centerY + 20; i++)
                {
                    if (Main.tile[centerX, i].HasUnactuatedTile && Main.tileSolid[Main.tile[centerX, i].TileType])
                        groundFarBelow = false;
                }
                if (groundFarBelow)
                {
                    speedY *= -1f;
                    if (npc.velocity.Y < 0f)
                        npc.velocity.Y *= 0.9f;
                }
            }

            // Periodically move away from nearby enemies.
            if (moveAwayDelay > 0f)
                moveAwayDelay--;
            else
            {
                moveAwayDelay = 10f;

                float totalNearbyEnemies = 0f;
                Vector2 acceleration = Vector2.Zero;
                for (int i = 0; i < 200; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.active && n.damage > 0 && !n.friendly && npc.Hitbox.Distance(n.Center) <= 100f)
                    {
                        totalNearbyEnemies++;
                        acceleration -= npc.SafeDirectionTo(n.Center);
                    }
                }
                if (totalNearbyEnemies >= 1f)
                {
                    acceleration /= totalNearbyEnemies;
                    npc.velocity = (npc.velocity + acceleration * 3f).ClampMagnitude(0f, 16f);
                }
            }

            // Rebound on colliding horizontal walls.
            if (npc.collideX)
                npc.velocity.X *= -0.2f;
            
            npc.spriteDirection = (npc.velocity.X > 0f).ToDirectionInt();
            npc.ai[0] = speedX;
            npc.ai[1] = speedY;

            // Rotate.
            npc.rotation = npc.velocity.X * 0.3f;
        }

        public static void DoDeathBehavior(NPC npc)
        {
            npc.velocity.X *= 0.9f;
            npc.velocity.Y = MathHelper.Lerp(npc.velocity.Y, 0.92f, 0.03f);
            if (npc.velocity.Y < -3f)
                npc.velocity.Y = -3f;

            npc.rotation = npc.rotation.AngleTowards(MathHelper.PiOver4 * npc.spriteDirection, 0.08f);
            npc.ai[2] = 9999999f;
        }

        #endregion AI and Behaviors

        #region Drawing and Frames
        public override void FindFrame(NPC npc, int frameHeight)
        {
            // Use the spread wings frame.
            if (npc.Infernum().ExtraAI[0] == 1f)
            {
                npc.frame.Y = frameHeight;
                return;
            }

            int wingFlapRate = 7;
            
            npc.frameCounter += (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) * 0.5f + 1D;
            if (npc.frameCounter < wingFlapRate)
                npc.frame.Y = 0;
            else if (npc.frameCounter < wingFlapRate * 2)
                npc.frame.Y = frameHeight;
            else if (npc.frameCounter < wingFlapRate * 3)
                npc.frame.Y = frameHeight * 2;
            else
            {
                npc.frame.Y = frameHeight;
                if (npc.frameCounter >= wingFlapRate * 4 - 1)
                    npc.frameCounter = 0.0;
            }
        }
        #endregion Drawing and Frames
        
        #region Death Effects
        public override bool CheckDead(NPC npc)
        {
            if (npc.Infernum().ExtraAI[0] >= 1f)
                return false;

            // Have the empress scream.
            SoundEngine.PlaySound(SoundID.Item160 with { Pitch = -0.1f, Volume = 2f }, npc.Center);

            npc.Infernum().ExtraAI[0] = 1f;
            npc.dontTakeDamage = true;
            npc.life = npc.lifeMax;
            npc.netUpdate = true;
            return false;
        }
        #endregion Death Effects
    }
}
