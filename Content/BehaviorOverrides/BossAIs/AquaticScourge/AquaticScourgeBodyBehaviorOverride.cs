using CalamityMod;
using CalamityMod.NPCs.AquaticScourge;
using CalamityMod.Particles;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class AquaticScourgeBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AquaticScourgeBody>();

        public override void SetDefaults(NPC npc)
        {
            // Set defaults that, if were to be changed by Calamity, would cause significant issues to the fight.
            npc.width = 32;
            npc.height = 32;
            npc.scale = 1f;
            npc.Opacity = 1f;
            npc.defense = 20;
            npc.alpha = 255;
            npc.DR_NERD(0.1f);
            npc.chaseable = false;
        }

        public override bool PreAI(NPC npc)
        {
            DoAI(npc);
            return false;
        }

        public static void DoAI(NPC npc)
        {
            // Due to lag, a client's segment can perform its AI before its npc.realLife can be assigned. This causes the worm to decapitate itself immediately.
            if (Main.netMode == NetmodeID.MultiplayerClient && npc.realLife == -1)
            {
                npc.timeLeft -= 100;
                if (npc.timeLeft < 100)
                {
                    npc.life = 0;
                    npc.HitEffect();
                    npc.active = false;
                }
                return;
            }

            // Go away if the ahead segment is not present.
            if (!Main.npc.IndexInRange((int)npc.ai[1]) || !Main.npc[(int)npc.ai[1]].active)
            {
                npc.life = 0;
                npc.HitEffect(0, 10.0);
                npc.active = false;
                npc.netUpdate = true;
                return;
            }

            int segmentIndex = (int)npc.ai[3];
            ref float segmentGrowInterpolant = ref npc.Infernum().ExtraAI[0];
            ref float segmentRegrowRate = ref npc.Infernum().ExtraAI[1];
            ref float totalSpawnedLeeches = ref npc.Infernum().ExtraAI[2];
            ref float hasCreatedSplash = ref npc.Infernum().ExtraAI[4];

            // Make segments slowly regrow their spikes.
            segmentGrowInterpolant = Clamp(segmentGrowInterpolant + segmentRegrowRate, 0f, 1f);
            if (segmentRegrowRate <= 0f)
            {
                segmentRegrowRate = Main.rand.NextFloat(0.0014f, 0.0023f);
                segmentGrowInterpolant = 1f;
                npc.netUpdate = true;
            }

            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            NPC headSegment = Main.npc[npc.realLife];

            // Fade in if the ahead segment has faded in sufficiently, resulting into the entire worm smoothly appearing.
            if (aheadSegment.alpha < 128)
                npc.alpha = Utils.Clamp(npc.alpha - 42, 0, 255);

            // Inherit attributes from the head.
            npc.target = aheadSegment.target;
            npc.defense = aheadSegment.defense;
            npc.damage = headSegment.damage >= 1 ? 60 : 0;
            npc.dontTakeDamage = headSegment.dontTakeDamage;
            npc.chaseable = headSegment.chaseable;
            npc.noTileCollide = headSegment.noTileCollide;
            npc.gfxOffY = headSegment.gfxOffY;
            npc.Opacity = headSegment.Opacity;
            npc.Calamity().newAI[0] = npc.chaseable.ToInt();
            npc.Calamity().DR = MathF.Min(npc.Calamity().DR, 0.4f);

            // Always use max HP. This doesn't affect the worm as a whole, but it does prevent problems in the death animation where segments otherwise just disappear when killed.
            npc.lifeMax = headSegment.lifeMax;
            npc.life = npc.lifeMax;

            // Stay behind the previous segment.
            if (npc.noTileCollide)
            {
                Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
                if (aheadSegment.rotation != npc.rotation)
                {
                    float segmentMoveInterpolant = 0.075f;
                    directionToNextSegment = directionToNextSegment.RotatedBy(WrapAngle(aheadSegment.rotation - npc.rotation) * segmentMoveInterpolant);
                }

                npc.rotation = directionToNextSegment.ToRotation() + PiOver2;
                npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;

                // Shudder if the head says to do so.
                if (headSegment.ai[2] == (int)AquaticScourgeHeadBehaviorOverride.AquaticScourgeAttackType.PerpendicularSpikeBarrage)
                {
                    if (headSegment.Infernum().ExtraAI[3] >= 1f && npc.ai[3] >= 2f)
                        npc.Center += directionToNextSegment.SafeNormalize(Vector2.Zero).RotatedBy(PiOver2) * Sin(Pi * npc.ai[3] / 35f + headSegment.ai[3] / 15f) * 3.6f;
                }

                if (AquaticScourgeHeadBehaviorOverride.WormSegments.Any())
                    AquaticScourgeHeadBehaviorOverride.WormSegments[segmentIndex].Position = npc.Center;
            }

            // Follow the verlet segment as directed by the head.
            else
            {
                npc.Center = AquaticScourgeHeadBehaviorOverride.WormSegments[segmentIndex + 1].Position;

                float idealRotation = (AquaticScourgeHeadBehaviorOverride.WormSegments[segmentIndex].Position - npc.Center).ToRotation() + PiOver2;
                npc.rotation = npc.rotation.AngleTowards(idealRotation, 0.03f).AngleLerp(idealRotation, 0.05f);

                // Release blood if in water. If the flesh is eaten, release acid instead as it disappears.
                if (Collision.WetCollision(npc.Top, npc.width, npc.height))
                {
                    if (npc.localAI[1] <= 0.4f && Main.rand.NextBool(560))
                    {
                        float bloodOpacity = 0.7f;
                        CloudParticle bloodCloud = new(npc.Center, Main.rand.NextVector2Circular(4f, 4f), Color.Red * bloodOpacity, Color.DarkRed * bloodOpacity, 270, Main.rand.NextFloat(1.9f, 2.12f));
                        GeneralParticleHandler.SpawnParticle(bloodCloud);
                    }
                    else if (npc.localAI[1] >= 0.9f && Main.rand.NextFloat() <= npc.Opacity * 0.02f && npc.Opacity <= 0.6f)
                    {
                        float acidOpacity = 0.8f;
                        CloudParticle acidCloud = new(npc.Center, Main.rand.NextVector2Circular(4f, 4f), Color.YellowGreen * acidOpacity, Color.Olive * acidOpacity * 0.7f, 120, Main.rand.NextFloat(1.9f, 2.12f));
                        GeneralParticleHandler.SpawnParticle(acidCloud);
                    }

                    // Handle splash effects.
                    if (hasCreatedSplash == 0f)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            float acidOpacity = 0.56f;
                            CloudParticle acidCloud = new(npc.Center, -Vector2.UnitY.RotatedByRandom(1.23f) * Main.rand.NextFloat(1f, 14f), Color.YellowGreen * acidOpacity, Color.Olive * acidOpacity * 0.7f, 120, Main.rand.NextFloat(1.9f, 2.12f));
                            GeneralParticleHandler.SpawnParticle(acidCloud);
                        }
                        hasCreatedSplash = 1f;
                    }
                }

                // Spawn leeches that will eat away at the segment.
                if (Main.netMode != NetmodeID.MultiplayerClient && totalSpawnedLeeches < 3f && Main.rand.NextBool(32))
                {
                    Utilities.NewProjectileBetter(npc.Center + Main.rand.NextVector2CircularEdge(180f, 180f), Vector2.Zero, ModContent.ProjectileType<LeechFeeder>(), 0, 0f, -1, npc.whoAmI);
                    totalSpawnedLeeches++;
                }
            }
        }

        public static IEnumerable<Vector2> GetSpikePositions(NPC npc)
        {
            yield return npc.Center + Vector2.UnitY * npc.gfxOffY + new Vector2(16f, 4f).RotatedBy(npc.rotation) * npc.scale;
            yield return npc.Center + Vector2.UnitY * npc.gfxOffY + new Vector2(16f, -10f).RotatedBy(npc.rotation) * npc.scale;
            yield return npc.Center + Vector2.UnitY * npc.gfxOffY + new Vector2(-18f, 4f).RotatedBy(npc.rotation) * npc.scale;
            yield return npc.Center + Vector2.UnitY * npc.gfxOffY + new Vector2(-18f, -10f).RotatedBy(npc.rotation) * npc.scale;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            float skeletonInterpolant = npc.localAI[1];
            Vector2 drawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
            Texture2D bodyTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AquaticScourge/AquaticScourgeBody").Value;
            Texture2D bodyTextureSkeleton = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AquaticScourge/AquaticScourgeBodySkeleton").Value;
            Texture2D spikeTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AquaticScourge/AquaticScourgeBodySpike").Value;
            Vector2 origin = bodyTexture.Size() * 0.5f;
            Main.EntitySpriteDraw(bodyTexture, drawPosition, null, npc.GetAlpha(lightColor) * (1f - skeletonInterpolant), npc.rotation, origin, npc.scale, 0, 0);
            Main.EntitySpriteDraw(bodyTextureSkeleton, drawPosition, null, npc.GetAlpha(lightColor) * skeletonInterpolant, npc.rotation, origin, npc.scale, 0, 0);

            // Draw spikes.
            int index = 0;
            float spikeScale = Pow(npc.Infernum().ExtraAI[0], 1.64f) * npc.scale;
            foreach (Vector2 spikePosition in GetSpikePositions(npc))
            {
                SpriteEffects direction = index < 2 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                origin = spikeTexture.Size() * Vector2.UnitX;
                if (index < 2)
                    origin.X = spikeTexture.Width - origin.X;

                Main.EntitySpriteDraw(spikeTexture, spikePosition - Main.screenPosition, null, npc.GetAlpha(lightColor), npc.rotation, origin, spikeScale, direction, 0);
                index++;
            }

            return false;
        }
    }
}
