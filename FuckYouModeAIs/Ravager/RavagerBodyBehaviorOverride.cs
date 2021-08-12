using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Ravager;
using CalamityMod.NPCs.SlimeGod;
using CalamityMod.Projectiles.Boss;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Ravager
{
    public class RavagerBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<RavagerBody>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        public enum RavagerAttackType
        {

        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // Create limbs.
            if (npc.localAI[0] == 0f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPC((int)npc.Center.X - 70, (int)npc.Center.Y + 88, ModContent.NPCType<RavagerLegLeft>(), npc.whoAmI);
                NPC.NewNPC((int)npc.Center.X + 70, (int)npc.Center.Y + 88, ModContent.NPCType<RavagerLegRight>(), npc.whoAmI);
                NPC.NewNPC((int)npc.Center.X - 120, (int)npc.Center.Y + 50, ModContent.NPCType<RavagerClawLeft>(), npc.whoAmI);
                NPC.NewNPC((int)npc.Center.X + 120, (int)npc.Center.Y + 50, ModContent.NPCType<RavagerClawRight>(), npc.whoAmI);
                NPC.NewNPC((int)npc.Center.X + 1, (int)npc.Center.Y - 20, ModContent.NPCType<RavagerHead>(), npc.whoAmI);
                npc.localAI[0] = 1f;
            }

            CalamityGlobalNPC.scavenger = npc.whoAmI;

            // Fade in.
            bool shouldNotAttack = false;
            if (npc.alpha > 0)
            {
                npc.alpha = Utils.Clamp(npc.alpha - 10, 0, 255);
                shouldNotAttack = true;
            }

            // Sap the target's life if it's above a certain threshold.
            if (target.statLife > target.statLifeMax2 * 0.75)
            {

            }

            npc.noTileCollide = false;
            if (!target.active || target.dead || !npc.WithinRange(target.Center, 7200f))
            {
                npc.noTileCollide = true;
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * -30f, 0.2f);
                if (!npc.WithinRange(target.Center, 3000f))
                {
                    npc.life = 0;
                    npc.active = false;
                    npc.netUpdate = true;
                }
                return false;
            }

            // Constantly give the target Weak Pertrification.
            if (Main.netMode != NetmodeID.Server)
            {
                if (!target.dead && target.active)
                    target.AddBuff(ModContent.BuffType<WeakPetrification>(), 15);
            }

            bool leftLegActive = false;
            bool rightLegActive = false;
            bool leftClawActive = false;
            bool rightClawActive = false;
            bool headActive = false;

            ref float darkMagicFireballShootTimer = ref npc.ai[1];
            ref float jumpTimer = ref npc.ai[2];
            ref float jumpState = ref npc.ai[3];

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<RavagerHead>())
                    headActive = true;
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<RavagerClawRight>())
                    rightClawActive = true;
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<RavagerClawLeft>())
                    leftClawActive = true;
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<RavagerLegRight>())
                    rightLegActive = true;
                if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<RavagerLegLeft>())
                    leftLegActive = true;
            }

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool anyLimbsArePresent = leftLegActive || rightLegActive || leftClawActive || rightClawActive || headActive;
            bool shouldBeBuffed = CalamityWorld.downedProvidence && !BossRushEvent.BossRushActive;

            int darkMagicFireballShootRate = 90;
            int jumpDelay = !leftClawActive || !rightClawActive ? 270 : 210;
            float jumpSpeed = 22f;
            float gravity = 0.625f;
            if (!anyLimbsArePresent)
            {
                darkMagicFireballShootRate += 35;
                jumpSpeed += 4f;
                gravity += 0.15f;
            }
            if (shouldBeBuffed)
            {
                darkMagicFireballShootRate -= 10;
                jumpSpeed += 6.25f;
            }

            npc.dontTakeDamage = anyLimbsArePresent;

            // Periodically release bursts of dark magic fireballs.
            if (!shouldNotAttack && darkMagicFireballShootTimer >= darkMagicFireballShootRate && jumpState == 0f)
            {
                Main.PlaySound(SoundID.Item100, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int fireballsPerBurst = shouldBeBuffed ? 11 : 8;
                    int darkMagicFireballDamage = shouldBeBuffed ? 335 : 215;
                    float darkMagicFireballSpeed = shouldBeBuffed ? 16f : 10f;
                    for (int i = 0; i < fireballsPerBurst; i++)
                    {
                        Vector2 darkMagicFireballVelocity = (MathHelper.TwoPi * i / fireballsPerBurst).ToRotationVector2() * darkMagicFireballSpeed;
                        Utilities.NewProjectileBetter(npc.Center + darkMagicFireballVelocity * 2f, darkMagicFireballVelocity, ModContent.ProjectileType<DarkMagicFireball>(), darkMagicFireballDamage, 0f);
                    }
                    darkMagicFireballShootTimer = 0f;
                    npc.netUpdate = true;
                }
            }
            
            // Jump towards the target if they're far enough away and enough time passes.
            if (!shouldNotAttack && !npc.WithinRange(target.Center, 200f) && jumpState == 0f && npc.velocity.Y == 0f)
            {
                jumpTimer++;
                if (jumpTimer >= jumpDelay)
                {
                    jumpTimer = 0f;
                    jumpState = 1f;

                    if (MathHelper.Distance(npc.Center.X, target.Center.X) < 225f)
                        jumpSpeed += 4f;

                    npc.velocity = Utilities.GetProjectilePhysicsFiringVelocity(npc.Center, target.Center, gravity, jumpSpeed, out _);
                    npc.netUpdate = true;
                }
            }

            if (jumpState == 1f)
            {
                // Make stomp sounds and dusts when hitting the ground again.
                if (npc.velocity.Y == 0f)
                {
                    Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 14, 1.25f, -0.25f);
                    for (int x = (int)npc.Left.X - 30; x < (int)npc.Right.X + 30; x += 10)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            Dust stompDust = Dust.NewDustDirect(new Vector2(x, npc.Bottom.Y), npc.width + 30, 4, 31, 0f, 0f, 100, default, 1.5f);
                            stompDust.velocity *= 0.2f;
                        }

                        Gore stompGore = Gore.NewGoreDirect(new Vector2(x, npc.Bottom.Y - 12f), default, Main.rand.Next(61, 64), 1f);
                        stompGore.velocity *= 0.4f;
                    }

                    jumpState = 0f;
                    npc.netUpdate = true;
                }
                
                // Fall through tiles in the way.
                if (!target.dead)
                {
                    if ((target.position.Y > npc.Bottom.Y && npc.velocity.Y > 0f) || (target.position.Y < npc.Bottom.Y && npc.velocity.Y < 0f))
                        npc.noTileCollide = true;
                    else if ((npc.velocity.Y > 0f && npc.Bottom.Y > target.Top.Y) || (Collision.CanHit(npc.position, npc.width, npc.height, target.Center, 1, 1) && !Collision.SolidCollision(npc.position, npc.width, npc.height)))
                        npc.noTileCollide = false;
                }
            }
            else
                npc.velocity.X *= 0.8f;

            // Do custom gravity stuff.
            npc.noGravity = true;
            EnforceCustomGravity(npc, gravity);

            darkMagicFireballShootTimer++;

            return false;
        }

        public static void EnforceCustomGravity(NPC npc, float gravity)
        {
            float maxFallSpeed = 19f;
            if (npc.wet)
            {
                if (npc.honeyWet)
                {
                    gravity *= 0.33f;
                    maxFallSpeed *= 0.4f;
                }
                else
                {
                    gravity *= 0.66f;
                    maxFallSpeed *= 0.7f;
                }
            }

            npc.velocity.Y += gravity;
            if (npc.velocity.Y > maxFallSpeed)
                npc.velocity.Y = maxFallSpeed;
        }
        #endregion AI
    }
}
