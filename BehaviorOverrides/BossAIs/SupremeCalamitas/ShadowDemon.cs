using CalamityMod;
using CalamityMod.DataStructures;
using CalamityMod.NPCs;
using CalamityMod.Particles.Metaballs;
using InfernumMode.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

using SCalNPC = CalamityMod.NPCs.SupremeCalamitas.SupremeCalamitas;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class ShadowDemon : ModNPC
    {
        public class DemonHead
        {
            public Vector2 Center;

            public Vector2 Velocity;

            public Vector2[] OldVelocities = new Vector2[20];

            public int Frame;

            public float HoverOffset;

            public float HoverOffsetAngle;

            public float Rotation;

            public void AdjustOldVelocityArray()
            {
                for (int i = OldVelocities.Length - 1; i > 0; i--)
                {
                    OldVelocities[i] = OldVelocities[i - 1];
                }
                OldVelocities[0] = Velocity;
            }
        }

        public DemonHead[] Heads = new DemonHead[3];

        public Player Target => Main.player[NPC.target];

        public ref float GeneralTimer => ref NPC.ai[0];

        public const float CongregationDiameter = 250f;

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("Shadow Demon");
            Main.npcFrameCount[NPC.type] = 2;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 25f;
            NPC.aiStyle = AIType = -1;
            NPC.damage = 0;
            NPC.width = NPC.height = 60;
            NPC.lifeMax = 20000;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.dontTakeDamage = true;
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale) => NPC.life = 20000;

        public override void AI()
        {
            // Disappear if SCal is not present.
            if (!NPC.AnyNPCs(ModContent.NPCType<SCalNPC>()))
            {
                NPC.active = false;
                return;
            }

            // Inherit things from SCal.
            NPC scal = Main.npc[CalamityGlobalNPC.SCal];
            NPC.target = scal.target;

            GeneralTimer++;
            if (GeneralTimer % 160f == 2f)
                RedetermineHeadOffsets();

            // Update heads.
            UpdateHeads();

            // Emit gas.
            EmitShadowGas();

            // Hover around the target.
            NPC.SimpleFlyMovement(NPC.SafeDirectionTo(Target.Center) * 11f, 0.2f);
        }

        public void RedetermineHeadOffsets()
        {
            for (int i = 0; i < Heads.Length; i++)
            {
                Heads[i].HoverOffset = Main.rand.NextFloat(180f, 300f);
                Heads[i].HoverOffsetAngle = MathHelper.Lerp(-0.87f, 0.87f, i / (float)(Heads.Length - 1f));
                Heads[i].HoverOffsetAngle += Main.rand.NextFloatDirection() * 0.17f;
            }
        }

        public void UpdateHeads()
        {
            for (int i = 0; i < Heads.Length; i++)
            {
                if (Heads[i] is null)
                {
                    Heads[i] = new()
                    {
                        Center = NPC.Center
					};
                }

                Vector2 hoverDestination = NPC.Center - Vector2.UnitY.RotatedBy(Heads[i].HoverOffsetAngle) * Heads[i].HoverOffset;
                Vector2 idealHeadVelocity = Vector2.Zero.MoveTowards(hoverDestination - Heads[i].Center, 16f);
                Heads[i].Velocity = Vector2.Lerp(Heads[i].Velocity, idealHeadVelocity, 0.03f).MoveTowards(idealHeadVelocity, 0.3f);
                Heads[i].Center += Heads[i].Velocity;
                Heads[i].Rotation = (Target.Center - Heads[i].Center).ToRotation();
                Heads[i].Frame = (int)(GeneralTimer / 6f + i * 4) % 6;
                Heads[i].AdjustOldVelocityArray();
            }
        }

        public void EmitShadowGas()
        {
            float particleSize = CongregationDiameter;
            if (NPC.oldPosition != NPC.position && GeneralTimer >= 3f)
                particleSize += (NPC.oldPosition - NPC.position).Length() * 4.2f;

            // Place a hard limit on particle sizes.
            if (particleSize > 500f)
                particleSize = 500f;

            int particleSpawnCount = Main.rand.NextBool(8) ? 3 : 1;
            for (int i = 0; i < particleSpawnCount; i++)
            {
                // Summon a base particle.
                Vector2 spawnPosition = NPC.Center + Main.rand.NextVector2Circular(1f, 1f) * particleSize / 26f;
                FusableParticleManager.GetParticleSetByType<ShadowDemonParticleSet>()?.SpawnParticle(spawnPosition, particleSize);

                // And an "ahead" particle that spawns based on current movement.
                // This causes the "head" of the overall thing to have bumps when moving.
                spawnPosition += NPC.velocity.RotatedByRandom(1.38f) * particleSize / 105f;
                FusableParticleManager.GetParticleSetByType<ShadowDemonParticleSet>()?.SpawnParticle(spawnPosition, particleSize * 0.4f);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            float headScale = NPC.scale * 1.6f;
            for (int i = 0; i < Heads.Length; i++)
            {
                int maxFrame = 6;
                Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/SpiritCongregation").Value;
                Texture2D backTexture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/SpiritCongregationBack").Value;
                Texture2D auraTexture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/SpiritCongregationAura").Value;

                float offsetFactor = NPC.scale * ((CongregationDiameter - 54f) / 90f + 1.5f);
                offsetFactor *= texture.Width / 90f;
                Vector2 teethOffset = Heads[i].Rotation.ToRotationVector2() * offsetFactor * 4f;
                Vector2 drawPosition = Heads[i].Center - Main.screenPosition;
                Rectangle frame = texture.Frame(1, maxFrame, 0, Heads[i].Frame);
                Vector2 origin = frame.Size() * 0.5f;
                SpriteEffects direction = Math.Cos(Heads[i].Rotation) > 0f ? SpriteEffects.None : SpriteEffects.FlipVertically;

                // Draw the neck.
                Texture2D neckTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/PhotovisceratorLight").Value;

                Vector2 start = drawPosition;
                Vector2 end = NPC.Center - Main.screenPosition;
                List<Vector2> controlPoints = new()
                {
                    start
                };
                for (int j = 0; j < Heads[i].OldVelocities.Length; j++)
                {
                    // Incorporate the past movement into neck turns, giving it rubber band-like movment.
                    // Become less responsive at the neck ends. Having the ends have typical movement can look strange sometimes.
                    float swayResponsiveness = Utils.GetLerpValue(0f, 6f, j, true) * Utils.GetLerpValue(Heads[i].OldVelocities.Length, Heads[i].OldVelocities.Length - 6f, j, true);
                    swayResponsiveness *= 2.5f;
                    Vector2 swayTotalOffset = Heads[i].OldVelocities[j] * swayResponsiveness;
                    controlPoints.Add(Vector2.Lerp(start, end, j / (float)Heads[i].OldVelocities.Length) + swayTotalOffset);
                }
                controlPoints.Add(end);

                int chainPointCount = (int)(Vector2.Distance(controlPoints.First(), controlPoints.Last()) / 12f);
                if (chainPointCount < 12)
                    chainPointCount = 12;
                BezierCurve bezierCurve = new(controlPoints.ToArray());
                List<Vector2> chainPoints = bezierCurve.GetPoints(chainPointCount);

                for (int j = 0; j < chainPoints.Count; j++)
                {
                    Vector2 positionAtPoint = chainPoints[j];
                    if (Vector2.Distance(positionAtPoint, end) < 10f)
                        continue;

                    DrawData neckDraw = new(neckTexture, positionAtPoint, null, Color.White, 0f, neckTexture.Size() / 2f, 1.6f, 0, 0);
                    FusableParticleManager.GetParticleSetByType<ShadowDemonParticleSet>()?.PrepareSpecialDrawingForNextFrame(neckDraw);
                }

                // Draw the head.
                DrawData backTextureDraw = new(backTexture, drawPosition + NPC.position - NPC.oldPosition, frame, Color.White, Heads[i].Rotation, origin, headScale, direction, 0);
                DrawData auraTextureDraw = new(auraTexture, drawPosition + NPC.position - NPC.oldPosition, frame, Color.White, Heads[i].Rotation, origin, headScale, direction, 0);
                FusableParticleManager.GetParticleSetByType<ShadowDemonParticleSet>()?.PrepareSpecialDrawingForNextFrame(backTextureDraw, auraTextureDraw);
                Main.EntitySpriteDraw(texture, drawPosition + teethOffset, frame, Color.White, Heads[i].Rotation, origin, headScale, direction, 0);
            }
            return false;
        }

        public override bool CheckActive() => false;
    }
}
