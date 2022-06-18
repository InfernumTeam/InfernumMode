using CalamityMod.NPCs;
using CalamityMod.NPCs.SlimeGod;
using InfernumMode.BehaviorOverrides.BossAIs.Ravager;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using EbonianSlimeGod = CalamityMod.NPCs.SlimeGod.SlimeGod;

namespace InfernumMode.BehaviorOverrides.BossAIs.SlimeGod
{
    public class CrimulanSlimeGodBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SlimeGodRun>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        public enum CrimulanSlimeGodAttackType
        {
            LongLeaps,
            SplitSwarm,
            PowerfulSlam
        }
        #endregion

        #region AI
        public override bool PreAI(NPC npc)
        {
            // Disappear if the core is not present.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.slimeGod))
            {
                npc.active = false;
                return false;
            }

            // Do targeting.
            npc.target = Main.npc[CalamityGlobalNPC.slimeGod].target;
            Player target = Main.player[npc.target];

            // This will affect the other gods as well in terms of behavior.
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float doneWithSpawnAnimationFlag = ref npc.ai[2];
            ref float stuckTimer = ref npc.Infernum().ExtraAI[5];
            ref float stuckTeleportCountdown = ref npc.Infernum().ExtraAI[6];

            if (stuckTeleportCountdown > 0f)
            {
                stuckTeleportCountdown--;

                npc.velocity.X = 0f;
                npc.velocity.Y += 0.3f;
                npc.scale = 1f - stuckTeleportCountdown / 40f;
                npc.damage = 0;
                return false;
            }

            // Reset things.
            npc.timeLeft = 3600;
            npc.Opacity = 1f;
            npc.damage = npc.defDamage;
            npc.noGravity = false;
            npc.noTileCollide = false;

            if (!Collision.CanHit(target.Center, 1, 1, npc.Center, 1, 1))
            {
                stuckTimer++;
                if (stuckTimer > 180f)
                {
                    stuckTimer = 0f;
                    do
                        npc.Center = target.Center + Main.rand.NextVector2CircularEdge(360f, 360f);
                    while (Collision.SolidCollision(npc.Center, 4, 4));

                    stuckTeleportCountdown = 40f;
                    npc.netUpdate = true;
                }
            }
            else if (stuckTimer > 0f)
                stuckTimer--;

            // Set the universal whoAmI variable.
            CalamityGlobalNPC.slimeGodRed = npc.whoAmI;

            if (doneWithSpawnAnimationFlag == 0f)
            {
                if (npc.velocity.Y == 0f)
                {
                    for (int x = (int)npc.Left.X - 30; x < (int)npc.Right.X + 30; x += 10)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            Dust stompDust = Dust.NewDustDirect(new Vector2(x, npc.Bottom.Y), npc.width + 30, 4, 4, 0f, 0f, 100, default, 1.5f);
                            stompDust.velocity *= 0.2f;
                        }
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Vector2 slimeVelocity = -Vector2.UnitY.RotatedByRandom(0.59f) * Main.rand.NextFloat(4f, 10f);
                            Utilities.NewProjectileBetter(npc.Center, slimeVelocity, ModContent.ProjectileType<RedirectingIchorBall>(), 80, 0f);
                        }
                    }
                    doneWithSpawnAnimationFlag = 1f;
                    npc.netUpdate = true;
                }
                npc.velocity = Vector2.UnitY * 16f;
                return false;
            }

            switch ((CrimulanSlimeGodAttackType)(int)attackState)
            {
                case CrimulanSlimeGodAttackType.LongLeaps:
                    DoAttack_LongLeaps(npc, target, ref attackTimer);
                    break;
                case CrimulanSlimeGodAttackType.SplitSwarm:
                    DoAttack_SplitSwarm(npc, target, ref attackTimer);
                    break;
                case CrimulanSlimeGodAttackType.PowerfulSlam:
                    if (DoAttack_PowerfulSlam(npc, target, true, ref attackTimer))
                        GotoNextAttackState(npc);
                    break;
            }

            // Enforce gravity more heavily.
            if (!npc.noGravity && npc.velocity.Y < 11f)
                npc.velocity.Y += 0.15f;

            if (npc.Opacity <= 0f)
            {
                npc.scale = 0.001f;
                npc.dontTakeDamage = true;
            }
            else
                npc.dontTakeDamage = false;

            return false;
        }

        public static void DoAttack_LongLeaps(NPC npc, Player target, ref float attackTimer)
        {
            npc.Opacity = 1f;
            npc.scale = 1f;
            ref float jumpCounter = ref npc.Infernum().ExtraAI[0];
            ref float noTileCollisionCountdown = ref npc.Infernum().ExtraAI[1];

            // Slow down and prepare to jump if on the ground.
            if (npc.velocity.Y == 0f)
            {
                npc.velocity.X *= 0.5f;
                attackTimer++;

                float lifeRatio = npc.life / (float)npc.lifeMax;
                float jumpDelay = MathHelper.Lerp(27f, 8f, 1f - lifeRatio);
                if (attackTimer >= jumpDelay)
                {
                    attackTimer = 0f;
                    noTileCollisionCountdown = 10f;
                    jumpCounter++;

                    npc.velocity.Y -= 6f;
                    if (target.position.Y + target.height < npc.Center.Y)
                        npc.velocity.Y -= 1.25f;
                    if (target.position.Y + target.height < npc.Center.Y - 40f)
                        npc.velocity.Y -= 1.5f;
                    if (target.position.Y + target.height < npc.Center.Y - 80f)
                        npc.velocity.Y -= 1.75f;
                    if (target.position.Y + target.height < npc.Center.Y - 120f)
                        npc.velocity.Y -= 2.5f;
                    if (target.position.Y + target.height < npc.Center.Y - 160f)
                        npc.velocity.Y -= 3f;
                    if (target.position.Y + target.height < npc.Center.Y - 200f)
                        npc.velocity.Y -= 3f;
                    if (target.position.Y + target.height < npc.Center.Y - 400f)
                        npc.velocity.Y -= 6.1f;
                    if (!Collision.CanHit(npc.Center, 1, 1, target.Center, 1, 1))
                        npc.velocity.Y -= 3.25f;
                    npc.velocity.Y *= 1.2f;

                    npc.velocity.X = (target.Center.X > npc.Center.X).ToDirectionInt() * 16f;
                    npc.netUpdate = true;
                }
            }
            else
                npc.noTileCollide = !Collision.SolidCollision(npc.position, npc.width, npc.height + 16) && npc.Bottom.Y < target.Center.Y;

            if (noTileCollisionCountdown > 0f)
            {
                npc.noTileCollide = true;
                noTileCollisionCountdown--;
            }

            if (jumpCounter >= 5)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_SplitSwarm(NPC npc, Player target, ref float attackTimer)
        {
            ref float jumpTimer = ref npc.Infernum().ExtraAI[0];
            ref float noTileCollisionCountdown = ref npc.Infernum().ExtraAI[1];
            npc.noTileCollide = !Collision.CanHit(target.Center, 1, 1, npc.Center, 1, 1) ||
                Collision.SolidCollision(npc.position, npc.width, npc.height) || npc.Center.Y < target.Center.Y - 200f;

            if (attackTimer == 1f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int totalSlimesToSpawn = (int)MathHelper.Lerp(7f, 12f, 1f - npc.life / (float)npc.lifeMax);
                    int lifePerSlime = (int)Math.Ceiling(npc.life / (float)totalSlimesToSpawn);

                    for (int i = 0; i < totalSlimesToSpawn; i++)
                    {
                        int slime = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SlimeSpawnCrimson3>(), npc.whoAmI);
                        if (Main.npc.IndexInRange(slime))
                        {
                            Main.npc[slime].velocity = Main.rand.NextVector2CircularEdge(6f, 6f);
                            Main.npc[slime].Center += Main.rand.NextVector2Circular(15f, 15f);
                            Main.npc[slime].lifeMax = Main.npc[slime].life = lifePerSlime;
                            Main.npc[slime].netUpdate = true;
                        }
                    }
                }

                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/SlimeGodPossession"), npc.Center);
                for (int k = 0; k < 50; k++)
                    Dust.NewDust(npc.position, npc.width, npc.height, 4, Main.rand.NextFloatDirection() * 3f, -1f, 0, default, 1f);
            }

            if (NPC.CountNPCS(ModContent.NPCType<SlimeSpawnCrimson3>()) > 0)
                npc.life = Main.npc.Where(n => n.active && n.type == ModContent.NPCType<SlimeSpawnCrimson3>()).Sum(n => n.life);

            if (attackTimer < 420f)
            {
                // Slow down and prepare to jump if on the ground.
                if (npc.velocity.Y == 0f)
                {
                    npc.velocity.X *= 0.5f;
                    jumpTimer++;

                    float lifeRatio = npc.life / (float)npc.lifeMax;
                    float jumpDelay = MathHelper.Lerp(27f, 8f, 1f - lifeRatio);
                    if (jumpTimer >= jumpDelay)
                    {
                        jumpTimer = 0f;
                        noTileCollisionCountdown = 10f;

                        npc.velocity.Y -= 6f;
                        if (target.position.Y + target.height < npc.Center.Y)
                            npc.velocity.Y -= 1.25f;
                        if (target.position.Y + target.height < npc.Center.Y - 40f)
                            npc.velocity.Y -= 1.5f;
                        if (target.position.Y + target.height < npc.Center.Y - 80f)
                            npc.velocity.Y -= 1.75f;
                        if (target.position.Y + target.height < npc.Center.Y - 120f)
                            npc.velocity.Y -= 2.5f;
                        if (target.position.Y + target.height < npc.Center.Y - 160f)
                            npc.velocity.Y -= 3f;
                        if (target.position.Y + target.height < npc.Center.Y - 200f)
                            npc.velocity.Y -= 3f;
                        if (target.position.Y + target.height < npc.Center.Y - 400f)
                            npc.velocity.Y -= 6.1f;
                        if (!Collision.CanHit(npc.Center, 1, 1, target.Center, 1, 1))
                            npc.velocity.Y -= 3.25f;

                        npc.velocity.X = (target.Center.X > npc.Center.X).ToDirectionInt() * 13f;
                        npc.netUpdate = true;
                    }
                }
                else
                    npc.noTileCollide = !Collision.SolidCollision(npc.position, npc.width, npc.height + 16) && npc.Bottom.Y < target.Center.Y;

                if (noTileCollisionCountdown > 0f)
                {
                    npc.noTileCollide = true;
                    noTileCollisionCountdown--;
                }
            }
            else
            {
                npc.velocity.X *= 0.925f;
                if (attackTimer > 540f)
                    GotoNextAttackState(npc);
            }

            if (attackTimer > 500f)
            {
                npc.Opacity = 1f;
                npc.scale = MathHelper.Clamp(npc.scale + 0.075f, 0f, 1f);
            }
            else
                npc.Opacity = 0f;
            npc.damage = 0;
            attackTimer++;
        }

        public static bool DoAttack_PowerfulSlam(NPC npc, Player target, bool crimulanSlime, ref float attackTimer)
        {
            npc.Opacity = 1f;
            npc.scale = 1f;

            // Attempt to hover to a position above the player.
            if (attackTimer < 420f)
            {
                float flySpeed = MathHelper.Lerp(12.5f, 27f, Utils.InverseLerp(150f, 300f, attackTimer, true));
                Vector2 destination = target.Center - Vector2.UnitY * 340f;
                npc.velocity = (npc.velocity * 4f + npc.SafeDirectionTo(destination) * flySpeed) / 5f;

                if (npc.WithinRange(destination, 40f))
                {
                    attackTimer = 420f;
                    npc.netUpdate = true;
                }

                // Disable gravity and tile collision.
                npc.noGravity = true;
                npc.noTileCollide = true;
            }

            // Once reached, slam downward.
            else
            {
                if (attackTimer == 421f)
                    npc.velocity = Vector2.UnitY * 4f;

                // If velocity is 0 (indicating something has been hit) create a shockwave and some other things.
                if (npc.velocity.Y == 0f && attackTimer < 900f)
                {
                    Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 70, 1.25f, -0.25f);
                    for (int x = (int)npc.Left.X - 30; x < (int)npc.Right.X + 30; x += 10)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            Dust stompDust = Dust.NewDustDirect(new Vector2(x, npc.Bottom.Y), npc.width + 30, 4, 4, 0f, 0f, 100, default, 1.5f);
                            stompDust.velocity *= 0.2f;
                        }
                    }

                    // Create the shockwave and other projectiles.
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Utilities.NewProjectileBetter(npc.Bottom + Vector2.UnitY * 40f, Vector2.Zero, ModContent.ProjectileType<StompShockwave>(), 135, 0f);

                        int projectileType = crimulanSlime ? ModContent.ProjectileType<RedirectingIchorBall>() : ModContent.ProjectileType<RedirectingCursedBall>();
                        int projectileCount = 7;

                        // Fire more projectiles if alone.
                        if ((crimulanSlime && !NPC.AnyNPCs(ModContent.NPCType<EbonianSlimeGod>())) || (!crimulanSlime && !NPC.AnyNPCs(ModContent.NPCType<SlimeGodRun>())))
                            projectileCount = 13;

                        for (int i = 0; i < projectileCount; i++)
                        {
                            Vector2 shootVelocity = (MathHelper.TwoPi * i / projectileCount).ToRotationVector2() * 8f;
                            Utilities.NewProjectileBetter(npc.Center + shootVelocity * 4f, shootVelocity, projectileType, 95, 0f);
                        }
                    }

                    attackTimer = 900f;
                    npc.netUpdate = true;
                    return true;
                }

                // Enforce extra gravity.
                if (npc.velocity.Y < 42f)
                    npc.velocity.Y += MathHelper.Lerp(0.3f, 1.5f, Utils.InverseLerp(420f, 455f, attackTimer, true));
                npc.velocity.X *= 0.9f;

                // Custom gravity is used.
                npc.noGravity = true;

                // Fall through tiles in the way.
                if (!target.dead)
                {
                    if ((target.position.Y > npc.Bottom.Y && npc.velocity.Y > 0f) || (target.position.Y < npc.Bottom.Y && npc.velocity.Y < 0f))
                        npc.noTileCollide = true;
                    else if ((npc.velocity.Y > 0f && npc.Bottom.Y > target.Top.Y) || (Collision.CanHit(npc.position, npc.width, npc.height, target.Center, 1, 1) && !Collision.SolidCollision(npc.position, npc.width, npc.height)))
                        npc.noTileCollide = false;
                }
            }

            attackTimer++;
            return attackTimer > 960f;
        }

        public static void GotoNextAttackState(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 4; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            CrimulanSlimeGodAttackType oldAttackState = (CrimulanSlimeGodAttackType)(int)npc.ai[0];
            CrimulanSlimeGodAttackType newAttackState = oldAttackState;
            switch (oldAttackState)
            {
                case CrimulanSlimeGodAttackType.LongLeaps:
                    newAttackState = CrimulanSlimeGodAttackType.SplitSwarm;
                    break;
                case CrimulanSlimeGodAttackType.SplitSwarm:
                    newAttackState = lifeRatio < 0.5f ? CrimulanSlimeGodAttackType.PowerfulSlam : CrimulanSlimeGodAttackType.LongLeaps;
                    break;
                case CrimulanSlimeGodAttackType.PowerfulSlam:
                    newAttackState = CrimulanSlimeGodAttackType.LongLeaps;
                    break;
            }

            npc.ai[0] = (int)newAttackState;
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI
    }
}
