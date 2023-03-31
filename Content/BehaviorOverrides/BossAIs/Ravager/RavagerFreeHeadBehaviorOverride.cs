using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Ravager;
using InfernumMode.Content.Dusts;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Ravager
{
    public class RavagerFreeHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<RavagerHead2>();

        public override bool PreAI(NPC npc)
        {
            // Die if the main body does not exist anymore.
            if (CalamityGlobalNPC.scavenger < 0 || !Main.npc[CalamityGlobalNPC.scavenger].active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            // Inherit attributes from the main body.
            NPC ravager = Main.npc[CalamityGlobalNPC.scavenger];
            var currentAttack = (RavagerBodyBehaviorOverride.RavagerAttackType)ravager.ai[0];
            npc.target = ravager.target;
            npc.damage = 0;

            Player target = Main.player[npc.target];
            ref float attackTimer = ref npc.ai[1];
            ref float telegraphInterpolant = ref npc.ai[2];

            // Circle around the player slowly, releasing bolts when at the cardinal directions.
            int cinderShootRate = 270;
            Vector2 hoverOffset = (MathHelper.TwoPi * (attackTimer / cinderShootRate) / 4f).ToRotationVector2() * 360f;
            Vector2 hoverDestination = target.Center + hoverOffset;

            // Look at the player.
            Vector2 aimDirection = npc.SafeDirectionTo(target.Center, -Vector2.UnitY);
            Vector2 cinderShootPosition = npc.Center + aimDirection * 36f;
            npc.rotation = aimDirection.ToRotation() - MathHelper.PiOver2;
            bool dontFire = currentAttack is RavagerBodyBehaviorOverride.RavagerAttackType.DownwardFistSlam or RavagerBodyBehaviorOverride.RavagerAttackType.SlamAndCreateMovingFlamePillars;

            // Create a dust telegraph prior to releasing cinders.
            float wrappedAttackTimer = attackTimer % cinderShootRate;
            if (currentAttack != RavagerBodyBehaviorOverride.RavagerAttackType.DetachedHeadCinderRain)
            {
                telegraphInterpolant = Utils.GetLerpValue(cinderShootRate - 50f, cinderShootRate, wrappedAttackTimer, true);
                if (dontFire)
                    telegraphInterpolant = 0f;

                if (wrappedAttackTimer > cinderShootRate - 40f && !dontFire)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Dust fire = Dust.NewDustPerfect(cinderShootPosition + Main.rand.NextVector2Circular(25f, 25f), ModContent.DustType<RavagerMagicDust>());
                        fire.velocity = (fire.position - cinderShootPosition).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2f, 8f);
                        fire.scale = 1.3f;
                        fire.noGravity = true;
                    }
                }

                // Shoot the cinder.
                if (wrappedAttackTimer == cinderShootRate - 1f && !dontFire)
                {
                    SoundEngine.PlaySound(SoundID.Item72, cinderShootPosition);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(cinderShootPosition, aimDirection * 11f, ModContent.ProjectileType<DarkMagicCinder>(), 185, 0f);
                        npc.velocity -= aimDirection * 8f;
                        npc.netUpdate = true;
                    }
                }
            }

            // Hover into position.
            float acceleration = target.HoldingTrueMeleeWeapon() ? 0.2f : 0.7f;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 20f, acceleration);

            attackTimer++;
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Calculate the appropriate direction.
            SpriteEffects direction = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                direction = SpriteEffects.FlipHorizontally;

            // Draw the base texture and backglow.
            float telegraphInterpolant = npc.ai[2];
            Texture2D baseTexture = TextureAssets.Npc[npc.type].Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            npc.DrawBackglow(Color.MediumPurple with { A = 0 } * telegraphInterpolant, 10f * telegraphInterpolant, direction, npc.frame, Main.screenPosition);
            Main.spriteBatch.Draw(baseTexture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0f);
            return false;
        }
    }
}
