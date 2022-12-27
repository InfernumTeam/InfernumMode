using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.WallOfFlesh
{
    public class WallOfFleshEyeBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.WallofFleshEye;

        public const int IsDetachedFlagIndex = 2;

        public const int DetachDamage = 1550;

        #region AI

        public override bool PreAI(NPC npc)
        {
            ref float attackTimer = ref npc.ai[1];

            // Disappear if the WoF body is not present.
            if (!Main.npc.IndexInRange(Main.wofNPCIndex))
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            Player target = Main.player[Main.npc[Main.wofNPCIndex].target];

            int circleHoverCount = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (!Main.npc[i].active || Main.npc[i].type != npc.type || Main.npc[i].Infernum().ExtraAI[IsDetachedFlagIndex] == 0f)
                    continue;

                circleHoverCount++;
            }

            // Disable contact damage.
            npc.damage = 0;

            // Attack the target independently after being "killed".
            if (npc.Infernum().ExtraAI[IsDetachedFlagIndex] == 1f)
            {
                int laserShootRate = 120;
                float wallAttackTimer = Main.npc[Main.wofNPCIndex].ai[3];
                float hoverSpeedFactor = 1f;
                bool doCircleAttack = wallAttackTimer % 1200f < 600f || Main.npc[Main.wofNPCIndex].life > Main.npc[Main.wofNPCIndex].lifeMax * WallOfFleshMouthBehaviorOverride.Phase2LifeRatio;
                Vector2 hoverOffset = (MathHelper.TwoPi * (npc.Infernum().ExtraAI[1] + wallAttackTimer / laserShootRate) / 4f).ToRotationVector2() * 360f;
                Vector2 hoverDestination = target.Center + hoverOffset;
                if (!doCircleAttack)
                {
                    hoverSpeedFactor = 1.6f;
                    hoverDestination = new Vector2(Main.npc[Main.wofNPCIndex].Center.X, target.Center.Y);
                    hoverDestination.Y += (float)Math.Sin(wallAttackTimer / 70f + npc.Infernum().ExtraAI[1] * MathHelper.E) * 350f;
                }

                if (!Main.npc[Main.wofNPCIndex].WithinRange(target.Center, 4000f))
                    hoverDestination = Main.npc[Main.wofNPCIndex].Center;

                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeedFactor * 18f, hoverSpeedFactor * 0.9f);
                npc.rotation = npc.AngleTo(target.Center) + MathHelper.Pi;
                npc.dontTakeDamage = true;

                int circleHoverOffsetIndex = 0;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (!Main.npc[i].active || Main.npc[i].type != npc.type || Main.npc[i].Infernum().ExtraAI[IsDetachedFlagIndex] == 0f)
                        continue;

                    Main.npc[i].Infernum().ExtraAI[1] = circleHoverOffsetIndex;
                    circleHoverOffsetIndex++;
                }

                if (doCircleAttack)
                {
                    Vector2 laserShootVelocity = npc.SafeDirectionTo(target.Center) * 8.5f;
                    Vector2 laserShootPosition = npc.Center + laserShootVelocity * 7.5f;

                    // Create a dust telegraph prior to releasing lasers.
                    if (wallAttackTimer % laserShootRate > laserShootRate - 40f)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            Dust laser = Dust.NewDustPerfect(laserShootPosition + Main.rand.NextVector2Circular(25f, 25f), 182);
                            laser.velocity = (laserShootPosition - laser.position).SafeNormalize(Vector2.UnitY) * -Main.rand.NextFloat(2f, 8f);
                            laser.noGravity = true;
                        }
                    }

                    // Fire the laser. This doesn't happen if extremely close to players, to prevent cheap hits.
                    if (wallAttackTimer % laserShootRate == laserShootRate - 1f && !npc.WithinRange(target.Center, 115f) && npc.WithinRange(hoverDestination, 105f))
                    {
                        SoundEngine.PlaySound(SoundID.Item12, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int laser = Utilities.NewProjectileBetter(laserShootPosition, laserShootVelocity, ProjectileID.EyeLaser, 105, 0f);
                            if (Main.projectile.IndexInRange(laser))
                            {
                                Main.projectile[laser].hostile = true;
                                Main.projectile[laser].tileCollide = false;
                            }
                        }
                    }
                }
                else
                {
                    if (wallAttackTimer % 28f == 27f && npc.WithinRange(hoverDestination, 80f) && wallAttackTimer % 1200f > 680f)
                    {
                        SoundEngine.PlaySound(SoundID.Item12, npc.Center);
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            float laserShootSpeed = 8.5f;
                            int laser = Utilities.NewProjectileBetter(npc.Center, Vector2.UnitX * Math.Sign(Main.npc[Main.wofNPCIndex].velocity.X) * laserShootSpeed, ProjectileID.EyeLaser, 105, 0f);
                            if (Main.projectile.IndexInRange(laser))
                            {
                                Main.projectile[laser].hostile = true;
                                Main.projectile[laser].tileCollide = false;
                            }
                        }
                    }
                }

                return false;
            }

            float destinationOffset = MathHelper.Clamp(npc.Distance(target.Center), 60f, 210f);
            destinationOffset += MathHelper.Lerp(0f, 215f, (float)Math.Sin(npc.whoAmI % 4f / 4f * MathHelper.Pi + attackTimer / 16f) * 0.5f + 0.5f);
            destinationOffset += npc.Distance(target.Center) * 0.1f;

            float destinationAngularOffset = MathHelper.Lerp(-1.04f, 1.04f, npc.ai[0]);
            destinationAngularOffset += (float)Math.Sin(attackTimer / 32f + npc.whoAmI % 4f / 4f * MathHelper.Pi) * 0.16f;

            // Move in sharp, sudden movements while releasing things at the target.
            Vector2 destination = Main.npc[Main.wofNPCIndex].Center;
            destination += Main.npc[Main.wofNPCIndex].velocity.SafeNormalize(Vector2.UnitX).RotatedBy(destinationAngularOffset) * destinationOffset;

            float maxSpeed = Utilities.AnyProjectiles(ModContent.ProjectileType<FireBeamWoF>()) ? 1.5f : 15f;

            npc.velocity = (destination - npc.Center).SafeNormalize(Vector2.Zero) * MathHelper.Min(npc.Distance(destination) * 0.5f, maxSpeed);
            if (!npc.WithinRange(Main.npc[Main.wofNPCIndex].Center, 750f))
                npc.Center = Main.npc[Main.wofNPCIndex].Center + Main.npc[Main.wofNPCIndex].SafeDirectionTo(npc.Center) * 750f;

            npc.spriteDirection = 1;
            npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center), MathHelper.Pi * 0.1f);

            attackTimer++;

            int beamShootRate = 1600 - circleHoverCount * 270;
            if (attackTimer % beamShootRate == (beamShootRate + npc.whoAmI * 300) % beamShootRate)
                WallOfFleshMouthBehaviorOverride.PrepareFireBeam(npc, target);

            return false;
        }

        #endregion

        #region Drawing

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            ref float verticalOffsetFactor = ref npc.ai[0];

            // Don't draw any chains once free.
            if (npc.Infernum().ExtraAI[IsDetachedFlagIndex] == 1f)
            {
                Texture2D texture = TextureAssets.Npc[npc.type].Value;
                Vector2 drawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
                Vector2 origin = npc.frame.Size() * 0.5f;
                Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, 0, 0f);
                return false;
            }

            if (Main.wofNPCIndex == -1)
                return false;

            float yStart = MathHelper.Lerp(Main.wofDrawAreaBottom, Main.wofDrawAreaTop, verticalOffsetFactor);
            Vector2 start = new(Main.npc[Main.wofNPCIndex].Center.X, yStart);

            Texture2D fleshRopeTexture = TextureAssets.Chain12.Value;
            void drawChainFrom(Vector2 startingPosition)
            {
                Vector2 drawPosition = startingPosition;
                float rotation = npc.AngleFrom(drawPosition) - MathHelper.PiOver2;
                while (Vector2.Distance(drawPosition, npc.Center) > 40f)
                {
                    drawPosition += npc.DirectionFrom(drawPosition) * fleshRopeTexture.Height;
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 drawOffset = Vector2.UnitX.RotatedBy(rotation) * (float)Math.Cos(MathHelper.TwoPi * i / 4f) * 4f;
                        Color color = Lighting.GetColor((int)(drawPosition + drawOffset).X / 16, (int)(drawPosition + drawOffset).Y / 16);
                        Main.spriteBatch.Draw(fleshRopeTexture, drawPosition + drawOffset - Main.screenPosition, null, color, rotation, fleshRopeTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                    }
                }
            }

            drawChainFrom(start);
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type != NPCID.WallofFleshEye || !Main.npc[i].active || Main.npc[i].whoAmI == npc.whoAmI)
                    continue;

                // Draw order depends on index. Therefore, if the other index is greater than this one, that means it will draw
                // a chain of its own. This is done to prevent duplicates.
                if (Main.npc[i].whoAmI < npc.whoAmI)
                    drawChainFrom(Main.npc[i].Center);
            }
            return true;
        }
        #endregion

        #region Death Effects
        public override bool CheckDead(NPC npc)
        {
            // Do direct damage to the wall and have the eye "pop" out, as though it's detatching.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Main.npc.IndexInRange(Main.wofNPCIndex))
                    Main.npc[Main.wofNPCIndex].StrikeNPC(DetachDamage, 0f, 0);
            }

            npc.life = 1;
            npc.ai[1] = 0f;
            npc.Infernum().ExtraAI[IsDetachedFlagIndex] = 1f;
            npc.active = true;
            npc.netUpdate = true;
            return false;
        }
        #endregion Death Effects
    }
}
