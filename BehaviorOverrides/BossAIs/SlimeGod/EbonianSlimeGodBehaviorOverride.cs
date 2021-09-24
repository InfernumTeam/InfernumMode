using CalamityMod.NPCs;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using EbonianSlimeGod = CalamityMod.NPCs.SlimeGod.SlimeGod;

namespace InfernumMode.BehaviorOverrides.BossAIs.SlimeGod
{
    public class EbonianSlimeGodBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<EbonianSlimeGod>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        public enum EbonianSlimeGodAttackType
        {
            LongLeaps,
            SplitSwarm,
            PowerfulSlam
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // This will affect the other gods as well in terms of behavior.
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
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
            CalamityGlobalNPC.slimeGodPurple = npc.whoAmI;

            // Disappear if the core is missing.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.slimeGod))
            {
                npc.active = false;
                return false;
            }

            switch ((EbonianSlimeGodAttackType)(int)attackState)
            {
                case EbonianSlimeGodAttackType.LongLeaps:
                    DoAttack_LongLeaps(npc, target, ref attackTimer);
                    break;
                case EbonianSlimeGodAttackType.SplitSwarm:
                    DoAttack_SplitSwarm(npc, target, ref attackTimer);
                    break;
                case EbonianSlimeGodAttackType.PowerfulSlam:
                    if (CrimulanSlimeGodBehaviorOverride.DoAttack_PowerfulSlam(npc, target, false, ref attackTimer))
                        GotoNextAttackState(npc);
                    break;
            }

            // Enforce gravity more heavily.
            if (!npc.noGravity && npc.velocity.Y < 11f)
                npc.velocity.Y += 0.1f;

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

            if (jumpCounter >= 4)
                GotoNextAttackState(npc);
        }

        public static void DoAttack_SplitSwarm(NPC npc, Player target, ref float attackTimer)
        {
            npc.noTileCollide = !Collision.CanHit(target.Center, 1, 1, npc.Center, 1, 1) ||
                Collision.SolidCollision(npc.position, npc.width, npc.height) || npc.Center.Y < target.Center.Y - 200f;
            npc.noGravity = true;

            if (attackTimer == 1f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int totalSlimesToSpawn = (int)MathHelper.Lerp(10f, 15f, 1f - npc.life / (float)npc.lifeMax);
                    int lifePerSlime = (int)Math.Ceiling(npc.life / (float)totalSlimesToSpawn);

                    for (int i = 0; i < totalSlimesToSpawn; i++)
                    {
                        int slime = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<SlimeSpawnCorrupt2>(), npc.whoAmI);
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

            if (attackTimer < 420f)
            {
                if (!npc.WithinRange(target.Center, 200f))
                    npc.velocity = (npc.velocity * 24f + npc.SafeDirectionTo(target.Center) * 13f) / 25f;
            }
            else
            {
                npc.velocity.X *= 0.925f;
                npc.noGravity = false;

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

        public static void GotoNextAttackState(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 4; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            float lifeRatio = npc.life / (float)npc.lifeMax;
            EbonianSlimeGodAttackType oldAttackState = (EbonianSlimeGodAttackType)(int)npc.ai[0];
            EbonianSlimeGodAttackType newAttackState = oldAttackState;
            switch (oldAttackState)
            {
                case EbonianSlimeGodAttackType.LongLeaps:
                    newAttackState = EbonianSlimeGodAttackType.SplitSwarm;
                    break;
                case EbonianSlimeGodAttackType.SplitSwarm:
                    newAttackState = lifeRatio < 0.5f ? EbonianSlimeGodAttackType.PowerfulSlam : EbonianSlimeGodAttackType.LongLeaps;
                    break;
                case EbonianSlimeGodAttackType.PowerfulSlam:
                    newAttackState = EbonianSlimeGodAttackType.LongLeaps;
                    break;
            }

            npc.ai[0] = (int)newAttackState;
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI
    }
}
