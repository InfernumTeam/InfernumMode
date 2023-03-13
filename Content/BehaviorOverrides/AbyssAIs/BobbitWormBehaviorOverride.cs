using CalamityMod;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.NPCs.Abyss;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Core.GlobalInstances.Systems;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.AbyssAIs
{
    public class BobbitWormBehaviorOverride : NPCBehaviorOverride
    {
        public enum BobbitWormAttackState
        {
            WaitForTarget,
            SnatchTarget,
            RipApartTarget
        }

        public override int NPCOverrideType => ModContent.NPCType<BobbitWormHead>();

        #region AI and Behaviors
        public override bool PreAI(NPC npc)
        {
            // Ensure that the devilfish can target critters.
            npc.Infernum().IsAbyssPredator = true;
            NPCID.Sets.UsesNewTargetting[npc.type] = true;

            // Emit faint light.
            if (npc.Opacity >= 1f)
                Lighting.AddLight(npc.Center, Color.Cyan.ToVector3() * 0.3f);

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float groundBottomX = ref npc.Infernum().ExtraAI[1];
            ref float groundBottomY = ref npc.Infernum().ExtraAI[2];
            ref float frame = ref npc.Infernum().ExtraAI[3];

            // Pick a target if a valid one isn't already decided and not already attacking.
            if (npc.ai[0] == (int)BobbitWormAttackState.WaitForTarget || npc.GetTargetData().Invalid)
                Utilities.TargetClosestAbyssPredator(npc, false, 1300f, 1300f);
            NPCAimedTarget target = npc.GetTargetData();

            // Initialize the ground position.
            if (npc.localAI[0] == 0f)
            {
                npc.Center = Utilities.GetGroundPositionFrom(npc.Center.ToTileCoordinates()).ToWorldCoordinates(8f, -36f);
                groundBottomX = npc.Bottom.X;
                groundBottomY = npc.Bottom.Y;
                npc.localAI[0] = 1f;
                npc.RemoveWaterSlowness();
                npc.netUpdate = true;
            }

            // Hide if there's an abyss miniboss.
            if (AbyssMinibossSpawnSystem.MajorAbyssEnemyExists)
            {
                attackState = (int)BobbitWormAttackState.WaitForTarget;
                groundBottomY += 6f;
                npc.behindTiles = true;
                npc.Opacity -= 0.03f;
                if (npc.Opacity <= 0f)
                    npc.active = false;
            }
            else
            {
                npc.Opacity = 1f;
                npc.behindTiles = false;
            }

            // Disable knockback and go through tiles.
            npc.knockBackResist = 0f;
            npc.noTileCollide = true;

            switch ((BobbitWormAttackState)attackState)
            {
                case BobbitWormAttackState.WaitForTarget:
                    DoBehavior_WaitForTarget(npc, target, ref attackTimer, ref frame);
                    break;
                case BobbitWormAttackState.SnatchTarget:
                    DoBehavior_SnatchTarget(npc, target, ref attackTimer, ref frame);
                    break;
                case BobbitWormAttackState.RipApartTarget:
                    DoBehavior_RipApartTarget(npc, target, ref attackTimer, ref frame);
                    break;
            }

            attackTimer++;

            return false;
        }

        public static void DoBehavior_WaitForTarget(NPC npc, NPCAimedTarget target, ref float attackTimer, ref float frame)
        {
            // Keep jaws open in anticipation.
            frame = 0f;

            Vector2 restingPosition = new(npc.Infernum().ExtraAI[1], npc.Infernum().ExtraAI[2]);
            if (npc.WithinRange(restingPosition, 8f))
                npc.rotation = npc.rotation.AngleLerp(0f, 0.1f).AngleTowards(0f, 0.2f);
            else
            {
                npc.rotation = npc.AngleFrom(restingPosition) + MathHelper.PiOver2;
                attackTimer = 0f;
            }

            // Slow down and move towards the resting position.
            npc.velocity *= 0.9f;
            npc.Center = Vector2.Lerp(npc.Center.MoveTowards(restingPosition, 8f), restingPosition, 0.018f);

            // Check to see if the target can be attacked.
            float sensingRange = 885f;
            float initialChargeSpeed = 6f;
            bool noObstructionsToTarget = Collision.CanHitLine(npc.TopLeft, npc.width, 1, target.Position, target.Width, target.Height);
            bool targetIsAbove = npc.SafeDirectionTo(target.Center).AngleBetween(-Vector2.UnitY) < 0.46f;
            bool targetIsCloseEnough = npc.WithinRange(target.Center, sensingRange);
            bool enoughTimeHasPassed = attackTimer >= 60f;

            if (noObstructionsToTarget && targetIsAbove && targetIsCloseEnough && enoughTimeHasPassed)
            {
                npc.ai[0] = (int)BobbitWormAttackState.SnatchTarget;
                npc.velocity = npc.SafeDirectionTo(target.Center) * initialChargeSpeed;
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_SnatchTarget(NPC npc, NPCAimedTarget target, ref float attackTimer, ref float frame)
        {
            int maxSnatchTime = 40;
            float maxSpeed = 33f;
            float acceleration = 1.15f;

            // Open jaws.
            frame = (int)MathHelper.Clamp(attackTimer / 4f, 0f, 2f);

            // Rapidly accelerate.
            npc.velocity = (npc.velocity * acceleration).ClampMagnitude(3f, maxSpeed);
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Create a shockwave on the first frame.
            if (attackTimer == 1f)
            {
                Utilities.CreateShockwave(npc.Center, 2, 8, 30, false);
                SoundEngine.PlaySound(ScorchedEarth.ShootSound, target.Center);
            }

            // Grab onto the target if they're reached.
            if (npc.WithinRange(target.Center, 72f))
            {
                Utilities.CreateShockwave(npc.Center, 2, 8, 30, false);

                // Create some audio and visual effects to indicate a very strong impact.
                // If a target is organic, it emits a lot of blood. Otherwise, it emits sparks.
                SoundEngine.PlaySound(CommonCalamitySounds.MeatySlashSound, target.Center);
                if (TargetIsOrganic(npc, target))
                    SoundEngine.PlaySound(SoundID.NPCHit18, target.Center);
                else
                    SoundEngine.PlaySound(SoundID.NPCDeath56, target.Center);
                CreateImpactParticlesForTarget(npc, target, true);

                npc.ai[0] = (int)BobbitWormAttackState.RipApartTarget;
                npc.velocity *= -0.7f;
                attackTimer = 0f;
                npc.netUpdate = true;
                return;
            }

            // Go back to the resting position if the target was not caught.
            if (attackTimer >= maxSnatchTime)
            {
                npc.ai[0] = (int)BobbitWormAttackState.WaitForTarget;
                npc.velocity *= 0.5f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_RipApartTarget(NPC npc, NPCAimedTarget target, ref float attackTimer, ref float frame)
        {
            // Close jaws.
            frame = 3f;

            // Wobble around in place erratically while approaching the resting position.
            float wobbleRotation = CalamityUtils.AperiodicSin(attackTimer / 30f) * 0.98f;
            Vector2 restingPosition = new(npc.Infernum().ExtraAI[1], npc.Infernum().ExtraAI[2]);
            npc.Center = npc.Center.MoveTowards(restingPosition + (npc.Center - restingPosition).SafeNormalize(Vector2.Zero) * 90f, 18f);

            if (attackTimer >= 10f)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, -Vector2.UnitY.RotatedBy(wobbleRotation) * 16f, 0.12f);
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            }

            // Force the player into the jaws of the worm.
            if (target.Type == NPCTargetType.Player)
            {
                Main.player[npc.TranslatedTargetIndex].Center = npc.Center - Vector2.UnitY * 16f;
                Main.player[npc.TranslatedTargetIndex].mount?.Dismount(Main.player[npc.TranslatedTargetIndex]);
            }
            if (target.Type == NPCTargetType.NPC)
            {
                Main.npc[npc.TranslatedTargetIndex].HitSound = Main.npc[npc.TranslatedTargetIndex].HitSound.Value with
                {
                    Volume = 0f
                };
                Main.npc[npc.TranslatedTargetIndex].StrikeNPCNoInteraction(Main.rand.Next(200, 225), 0f, 0);
                Main.npc[npc.TranslatedTargetIndex].Center = npc.Center - Vector2.UnitY * 16f;
            }

            // Create blood/spark particles.
            if (Main.rand.NextBool(4))
                CreateImpactParticlesForTarget(npc, target, false);

            // Return to the original position if the target is dead.
            if (npc.GetTargetData().Invalid)
            {
                npc.ai[0] = (int)BobbitWormAttackState.WaitForTarget;
                npc.velocity *= 0.3f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }
        }

        public static void CreateImpactParticlesForTarget(NPC npc, NPCAimedTarget target, bool initialImpact)
        {
            if (TargetIsOrganic(npc, target))
            {
                SoundEngine.PlaySound(SoundID.DD2_LightningBugZap, target.Center);
                for (int i = 0; i < (initialImpact ? 13 : 4); i++)
                {
                    int bloodLifetime = Main.rand.Next(22, 36);
                    float bloodScale = Main.rand.NextFloat(0.6f, 0.8f);
                    Color bloodColor = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat());
                    bloodColor = Color.Lerp(bloodColor, new Color(51, 22, 94), Main.rand.NextFloat(0.65f));

                    if (Main.rand.NextBool(20))
                        bloodScale *= 2f;

                    Vector2 bloodVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.81f) * Main.rand.NextFloat(11f, 23f);
                    bloodVelocity.Y -= 12f;
                    BloodParticle blood = new(target.Center, bloodVelocity, bloodLifetime, bloodScale, bloodColor);
                    GeneralParticleHandler.SpawnParticle(blood);
                }
                for (int i = 0; i < (initialImpact ? 6 : 2); i++)
                {
                    float bloodScale = Main.rand.NextFloat(0.2f, 0.33f);
                    Color bloodColor = Color.Lerp(Color.Red, Color.DarkRed, Main.rand.NextFloat(0.5f, 1f));
                    Vector2 bloodVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.9f) * Main.rand.NextFloat(9f, 14.5f);
                    BloodParticle2 blood = new(target.Center, bloodVelocity, 20, bloodScale, bloodColor);
                    GeneralParticleHandler.SpawnParticle(blood);
                }

                CloudParticle bloodCloud = new(target.Center, Main.rand.NextVector2Circular(12f, 12f), Color.Red, Color.DarkRed, 270, Main.rand.NextFloat(1.9f, 2.12f));
                GeneralParticleHandler.SpawnParticle(bloodCloud);
                return;
            }

            for (int i = 0; i < (initialImpact ? 12 : 3); i++)
            {
                int sparkLifetime = Main.rand.Next(22, 36);
                float sparkScale = Main.rand.NextFloat(0.7f, 0.8f);
                Color sparkColor = Color.Lerp(Color.Silver, Color.Gold, Main.rand.NextFloat(0.7f));
                sparkColor = Color.Lerp(sparkColor, Color.Orange, Main.rand.NextFloat());

                if (Main.rand.NextBool(6))
                    sparkScale *= 2f;

                Vector2 sparkVelocity = npc.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.8f) * Main.rand.NextFloat(9f, 14f);
                sparkVelocity.Y -= 4f;
                SparkParticle spark = new(target.Center, sparkVelocity, true, sparkLifetime, sparkScale, sparkColor);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        public static bool TargetIsOrganic(NPC npc, NPCAimedTarget target)
        {
            if (target.Type == NPCTargetType.NPC)
                return Main.npc[npc.TranslatedTargetIndex].Organic();

            Player player = Main.player[npc.TranslatedTargetIndex];
            if (player.Calamity().abyssalDivingSuit && !player.Calamity().abyssalDivingSuitHide)
                return false;
            if (player.Calamity().andromedaState != AndromedaPlayerState.Inactive)
                return false;

            return true;
        }

        #endregion AI and Behaviors

        #region Frames and Drawcode

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frame.Y = (int)npc.Infernum().ExtraAI[3] * frameHeight;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Draw segments.
            DrawSegments(npc);

            // Draw the head.
            Texture2D headTexture = TextureAssets.Npc[npc.type].Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = npc.frame.Size() * 0.5f;
            Main.spriteBatch.Draw(headTexture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, 0, 0f);
            return false;
        }

        public static void DrawSegments(NPC npc)
        {
            Vector2 currentSegmentPosition = npc.Center;
            Vector2 drawBottom = new(npc.Infernum().ExtraAI[1], npc.Infernum().ExtraAI[2]);
            Vector2 drawOffset = drawBottom - currentSegmentPosition;
            float rotation = drawOffset.ToRotation() - MathHelper.PiOver2;
            float maxSegmentOffset = npc.scale * 16f;
            Texture2D segmentTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/Abyss/BobbitWormSegment").Value;

            // A typical for loop is used to ensure that a stupid amount of segments don't draw due to infinite loops.
            for (int i = 0; i < 300; i++)
            {
                float totalDrawDistance = drawOffset.Length();
                if (totalDrawDistance < maxSegmentOffset)
                    break;

                // Increment the draw position for the next segment.
                currentSegmentPosition += drawOffset.ClampMagnitude(0f, maxSegmentOffset);
                drawOffset = drawBottom - currentSegmentPosition;

                // Calculate the draw position and color for the given segment.
                Color color = Lighting.GetColor((int)(currentSegmentPosition.X / 16f), (int)(currentSegmentPosition.Y / 16f)) * npc.Opacity;
                Vector2 drawPosition = currentSegmentPosition - Main.screenPosition;

                Main.spriteBatch.Draw(segmentTexture, drawPosition, null, color, rotation, segmentTexture.Size() * 0.5f, npc.scale, 0, 0f);
            }
        }
        #endregion Frames and Drawcode
    }
}
