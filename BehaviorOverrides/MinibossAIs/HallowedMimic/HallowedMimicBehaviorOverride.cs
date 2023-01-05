using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.BehaviorOverrides.MinibossAIs.CorruptionMimic.CorruptionMimicBehaviorOverride;

namespace InfernumMode.BehaviorOverrides.MinibossAIs.HallowedMimic
{
    public class HallowedMimicBehaviorOverride : NPCBehaviorOverride
    {
        public enum HallowedMimicAttackState
        {
            Inactive,
            RapidJumps,
            GroundPound,
            CrystalSpikeBarrage,
            FireHolyStars,
            SummonFlyingKnife
        }

        public override int NPCOverrideType => NPCID.BigMimicHallow;

        public override bool PreAI(NPC npc)
        {
            // Pick a target.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];

            // Reset things.
            npc.defense = 10;
            npc.npcSlots = 16f;
            npc.knockBackResist = 0f;
            npc.noTileCollide = false;
            npc.noGravity = false;

            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float isHostile = ref npc.ai[2];
            ref float currentFrame = ref npc.localAI[0];
            
            if ((npc.justHit || target.WithinRange(npc.Center, 200f)) && isHostile == 0f)
            {
                isHostile = 1f;
                attackTimer = 0f;
                npc.netUpdate = true;
            }

            switch ((HallowedMimicAttackState)(int)attackState)
            {
                case HallowedMimicAttackState.Inactive:
                    if (DoBehavior_Inactive(npc, target, isHostile == 1f, ref attackTimer, ref currentFrame))
                        SelectNextAttack(npc);
                    break;
                case HallowedMimicAttackState.RapidJumps:
                    if (DoBehavior_RapidJumps(npc, target, false, ref attackTimer, ref currentFrame))
                        SelectNextAttack(npc);
                    break;
                case HallowedMimicAttackState.GroundPound:
                    if (DoBehavior_GroundPound(npc, target, ref attackTimer, ref currentFrame))
                        SelectNextAttack(npc);
                    break;
                case HallowedMimicAttackState.CrystalSpikeBarrage:
                    DoBehavior_CrystalSpikeBarrage(npc, target, ref attackTimer, ref currentFrame);
                    break;
                case HallowedMimicAttackState.FireHolyStars:
                    DoBehavior_FireHolyStars(npc, target, ref attackTimer, ref currentFrame);
                    break;
                case HallowedMimicAttackState.SummonFlyingKnife:
                    DoBehavior_SummonFlyingKnife(npc, target, ref attackTimer, ref currentFrame);
                    break;
            }
            attackTimer++;

            return false;
        }

        public static void DoBehavior_CrystalSpikeBarrage(NPC npc, Player target, ref float attackTimer, ref float currentFrame)
        {
            int hideTime = 45;
            int crystalShootDelay = 90;
            int crystalCount = 7;
            int shootCount = 3;
            ref float aimDirection = ref npc.Infernum().ExtraAI[0];
            ref float shootCounter = ref npc.Infernum().ExtraAI[1];

            // Wait for the mimic to reach ground before shooting.
            if (npc.velocity.Y != 0f)
            {
                attackTimer = 0f;
                currentFrame = 13f;
                return;
            }
            currentFrame = (int)Math.Round(Utils.Remap(attackTimer, 0f, hideTime, 6f, 0f));

            // Cast telegraph lines outward to mark where the guillotines will spawn.
            if (attackTimer == hideTime)
            {
                aimDirection = npc.AngleTo(target.Center);
                for (int i = 0; i < crystalCount; i++)
                {
                    float offsetAngle = MathHelper.Lerp(-0.59f, 0.59f, i / (float)(crystalCount - 1f));
                    for (int j = 0; j < 80; j++)
                    {
                        Vector2 dustSpawnPosition = npc.Center + (offsetAngle + aimDirection).ToRotationVector2() * j * 24f;
                        Dust telegraph = Dust.NewDustPerfect(dustSpawnPosition, 267, Vector2.Zero);
                        telegraph.color = Color.Lerp(Color.Purple, Color.DeepPink, 0.5f);
                        telegraph.scale = 1.8f;
                        telegraph.noGravity = true;
                    }
                }

                npc.netUpdate = true;
            }

            // Release the guillotines.
            if (attackTimer == crystalShootDelay)
            {
                SoundEngine.PlaySound(SoundID.Item101, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < crystalCount; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-0.59f, 0.59f, i / (float)(crystalCount - 1f));
                        Vector2 crystalShootVelocity = (offsetAngle + aimDirection).ToRotationVector2() * 13f;
                        Utilities.NewProjectileBetter(npc.Center, crystalShootVelocity, ModContent.ProjectileType<PiercingCrystalShard>(), 120, 0f, -1, 0f, npc.whoAmI);
                    }
                }
            }

            if (attackTimer >= crystalShootDelay + PiercingCrystalShard.PierceTime + PiercingCrystalShard.FadeOutTime)
            {
                attackTimer = hideTime - 1f;
                shootCounter++;
                if (shootCounter >= shootCount)
                    SelectNextAttack(npc);
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_FireHolyStars(NPC npc, Player target, ref float attackTimer, ref float currentFrame)
        {
            int hideTime = 45;
            int starFireTime = 300;
            int starFireRate = 11;

            // Wait for the mimic to reach ground before shooting.
            if (npc.velocity.Y != 0f)
            {
                attackTimer = 0f;
                currentFrame = 13f;
                return;
            }
            currentFrame = (int)Math.Round(Utils.Remap(attackTimer, 0f, hideTime, 6f, 0f));

            if (attackTimer < hideTime)
                return;

            // Fire stars above the target.
            if (attackTimer % starFireRate == starFireRate - 1f && attackTimer < hideTime + starFireTime)
            {
                SoundEngine.PlaySound(SoundID.Item28, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 starSpawnPosition = target.Center + new Vector2(Main.rand.NextFloatDirection() * 650f, -750f);
                    Vector2 starShootVelocity = (target.Center + Vector2.UnitX * (Main.rand.NextFloatDirection() * 160f + target.velocity.X * 35f) - starSpawnPosition).SafeNormalize(Vector2.UnitY) * 9.6f;
                    Utilities.NewProjectileBetter(starSpawnPosition, starShootVelocity, ModContent.ProjectileType<HolyStar>(), 115, 0f);
                }
            }

            if (attackTimer >= hideTime + starFireTime + 60f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_SummonFlyingKnife(NPC npc, Player target, ref float attackTimer, ref float currentFrame)
        {
            int knifeAttackTime = FlyingKnife.Lifetime;

            if (npc.velocity.Y != 0f && attackTimer < 3f)
            {
                currentFrame = 13f;
                attackTimer = 0f;
                return;
            }

            npc.velocity.X *= 0.8f;
            currentFrame = (int)Math.Round(Utils.Remap(attackTimer, 0f, 50f, 12f, 7f));

            // Summon the flying knife.
            if (attackTimer == 5f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, -Vector2.UnitY * 8f, ModContent.ProjectileType<FlyingKnife>(), 120, 0f);
            }

            if (attackTimer >= knifeAttackTime)
                SelectNextAttack(npc);
        }

        public static void SelectNextAttack(NPC npc)
        {
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            switch ((HallowedMimicAttackState)npc.ai[0])
            {
                case HallowedMimicAttackState.Inactive:
                    npc.ai[0] = (int)HallowedMimicAttackState.RapidJumps;
                    break;
                case HallowedMimicAttackState.RapidJumps:
                    npc.ai[0] = (int)HallowedMimicAttackState.GroundPound;
                    break;
                case HallowedMimicAttackState.GroundPound:
                    npc.ai[0] = (int)HallowedMimicAttackState.CrystalSpikeBarrage;
                    break;
                case HallowedMimicAttackState.CrystalSpikeBarrage:
                    npc.ai[0] = (int)HallowedMimicAttackState.FireHolyStars;
                    break;
                case HallowedMimicAttackState.FireHolyStars:
                    npc.ai[0] = (int)HallowedMimicAttackState.SummonFlyingKnife;
                    break;
                case HallowedMimicAttackState.SummonFlyingKnife:
                    npc.ai[0] = (int)HallowedMimicAttackState.RapidJumps;
                    break;
            }

            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            npc.frame.Y = (int)(frameHeight * Math.Round(npc.localAI[0]));
        }
    }
}
