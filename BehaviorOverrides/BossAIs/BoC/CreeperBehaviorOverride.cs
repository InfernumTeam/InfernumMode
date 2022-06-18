using CalamityMod.Events;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.BehaviorOverrides.BossAIs.BoC.BoCBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.BossAIs.BoC
{
    public class CreeperBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.Creeper;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            if (!Main.npc.IndexInRange(NPC.crimsonBoss) || !Main.npc[NPC.crimsonBoss].active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            npc.life = npc.lifeMax = 2;
            npc.dontTakeDamage = true;

            NPC owner = Main.npc[NPC.crimsonBoss];
            npc.target = owner.target;

            npc.damage = 0;

            Player target = Main.player[npc.target];
            int idealAlpha = owner.alpha;
            if (idealAlpha > 20)
                idealAlpha = 255;
            float ownerAttackTimer = owner.ai[1];
            ref float creeperOffsetAngleFactor = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            Vector2 destinationOffsetDirection = (MathHelper.TwoPi * creeperOffsetAngleFactor + attackTimer / 105f).ToRotationVector2();
            Vector2 destination = owner.Center + destinationOffsetDirection * 420f;
            BoCAttackState ownerAttackState = (BoCAttackState)(int)owner.ai[0];
            switch (ownerAttackState)
            {
                case BoCAttackState.DiagonalCharge:
                    destination = owner.Center + destinationOffsetDirection * npc.Size * 0.5f * 1.1f;
                    idealAlpha = 255;
                    break;
                case BoCAttackState.CreeperBloodDripping:
                    destination = target.Center + destinationOffsetDirection * 480f;
                    destination.Y += (float)Math.Sin(attackTimer / 56f + creeperOffsetAngleFactor * MathHelper.TwoPi) * 40f;
                    idealAlpha = ownerAttackTimer < 60f ? 255 : 40;
                    break;
                case BoCAttackState.PsionicBombardment:
                    destination = owner.Center + (MathHelper.TwoPi * creeperOffsetAngleFactor + attackTimer / 35f).ToRotationVector2() * 160f;
                    idealAlpha = owner.alpha + 20;
                    break;
                case BoCAttackState.DashingIllusions:
                case BoCAttackState.BloodDashSwoop:
                case BoCAttackState.SpinPull:
                    destination = owner.Center + destinationOffsetDirection * 16f;
                    idealAlpha = 255;
                    break;
            }

            int alphaMoveOffset = Math.Sign(idealAlpha - npc.alpha) * 20;
            npc.alpha = Utils.Clamp(npc.alpha + alphaMoveOffset, 0, 255);

            // Release blood upward into the air during the creeper blood attack.
            if (ownerAttackState == BoCAttackState.CreeperBloodDripping)
            {
                bool eligableToFire = npc.Top.Y < target.Center.Y;
                int shootRate = BossRushEvent.BossRushActive ? 10 : 35;
                if (attackTimer > 135f && eligableToFire && attackTimer % shootRate == shootRate - 1f && npc.alpha <= 80)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 bloodVelocity = Utilities.GetProjectilePhysicsFiringVelocity(npc.Center, target.Center + Vector2.UnitY * 480f + target.velocity * 120f, BloodGeyser2.Gravity, 16f, out _);
                        Utilities.NewProjectileBetter(npc.Center, bloodVelocity, ModContent.ProjectileType<BloodGeyser2>(), 100, 0f);
                    }
                    Main.PlaySound(SoundID.NPCHit20, npc.position);
                }
            }

            // Otherwise, if a target is close, release ichor at them, assuming no tiles are in the way.
            else if (npc.alpha <= 10)
            {
                bool obstanceInWayOfTarget = !Collision.CanHitLine(npc.position, npc.width, npc.height, target.position, target.width, target.height);
                if (Main.netMode != NetmodeID.MultiplayerClient && !obstanceInWayOfTarget && attackTimer % 45f == 44f && Main.rand.NextBool(3))
                {
                    float aimAwayAngle = Utils.InverseLerp(300f, 150f, npc.Distance(target.Center), true) * Main.rand.NextFloat(2.16f, 3.84f);
                    Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(target.Center).RotatedBy(aimAwayAngle) * 8f, ModContent.ProjectileType<IchorSpit>(), 100, 0f);
                }
            }

            // Drift towards the destination around the brain.
            if (!npc.WithinRange(destination, 75f))
                npc.velocity = (npc.velocity * 10f + npc.SafeDirectionTo(destination) * 12f) / 11f;
            else
            {
                npc.velocity *= 0.925f;
                npc.Center = Vector2.Lerp(npc.Center, destination, 0.15f);
            }

            attackTimer++;
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.npcTexture[npc.type];
            float cyanAuraStrength = Main.npc[NPC.crimsonBoss].localAI[1];

            void drawInstance(Vector2 drawPosition, Color color, float scale)
            {
                drawPosition -= Main.screenPosition;
                spriteBatch.Draw(texture, drawPosition, null, color, npc.rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
            if (cyanAuraStrength > 0f)
            {
                float scale = npc.scale * MathHelper.Lerp(0.9f, 1.125f, cyanAuraStrength);
                Color auraColor = Color.Lerp(Color.Transparent, Color.Cyan, cyanAuraStrength) * npc.Opacity * 0.3f;
                auraColor.A = 0;

                for (int i = 0; i < 7; i++)
                {
                    Vector2 drawPosition = npc.Center + (MathHelper.TwoPi * i / 7f + Main.GlobalTime * 4.3f).ToRotationVector2() * cyanAuraStrength * 4f;
                    drawInstance(drawPosition, auraColor, scale);
                }
            }
            return true;
        }
    }
}
