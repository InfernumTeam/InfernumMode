using CalamityMod;
using CalamityMod.NPCs.StormWeaver;
using InfernumMode.BehaviorOverrides.BossAIs.AquaticScourge;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode.BehaviorOverrides.BossAIs.StormWeaver
{
    public class StormWeaverArmoredHeadBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<StormWeaverHead>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        #region Enumerations
        public enum StormWeaverArmoredAttackType
        {
            NormalMove,
            SparkBurst,
            LightningDischarge,
        }
        #endregion

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            ref float phase2 = ref npc.Infernum().ExtraAI[20];

            if (!target.active || target.dead)
            {
                npc.active = false;
                return false;
            }

            if (npc.life < npc.lifeMax * 0.9f)
			{
                if (phase2 == 0f)
                {
                    Main.PlaySound(SoundID.NPCDeath14, (int)npc.Center.X, (int)npc.Center.Y);

                    npc.Calamity().DR = 0f;
                    npc.Calamity().unbreakableDR = false;
                    npc.chaseable = true;
                    npc.HitSound = SoundID.NPCHit13;
                    npc.DeathSound = SoundID.NPCDeath13;
                    npc.frame = new Rectangle(0, 0, 62, 86);
                    phase2 = 1f;
                }
                return StormWeaverHeadBehaviorOverride.PreAI(npc);
			}

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.2f, 0f, 1f);

            // Create segments.
            if (npc.localAI[0] == 0f)
            {
                AquaticScourgeHeadBehaviorOverride.CreateSegments(npc, 32, ModContent.NPCType<StormWeaverBody>(), ModContent.NPCType<StormWeaverTail>());
                npc.localAI[0] = 1f;
            }

            ref float attackState = ref npc.ai[1];
            ref float attackTimer = ref npc.ai[2];

            switch ((StormWeaverArmoredAttackType)(int)attackState)
            {
                case StormWeaverArmoredAttackType.NormalMove:
                    DoAttack_NormalMove(npc, target, attackTimer);
                    break;
                case StormWeaverArmoredAttackType.SparkBurst:
                    DoAttack_SparkBurst(npc, target, attackTimer);
                    break;
                case StormWeaverArmoredAttackType.LightningDischarge:
                    DoAttack_LightningDischarge(npc, target, attackTimer);
                    break;
            }

            // Determine rotation.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            attackTimer++;
            return false;
        }

        public static void DoAttack_NormalMove(NPC npc, Player target, float attackTimer)
        {
            float turnSpeed = (!npc.WithinRange(target.Center, 220f)).ToInt() * 0.039f;
            float moveSpeed = npc.velocity.Length();

            if (npc.WithinRange(target.Center, 285f))
                moveSpeed *= 1.015f;
            else if (npc.velocity.Length() > 13f + attackTimer / 35f)
                moveSpeed *= 0.98f;

            moveSpeed = MathHelper.Clamp(moveSpeed, 12f, 25f);

            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * moveSpeed;

            if (attackTimer >= 480f)
                SelectNewAttack(npc);
        }


        public static void DoAttack_SparkBurst(NPC npc, Player target, float attackTimer)
        {
            float turnSpeed = (!npc.WithinRange(target.Center, 220f)).ToInt() * 0.054f;
            float moveSpeed = npc.velocity.Length();

            if (npc.WithinRange(target.Center, 285f))
                moveSpeed *= 1.01f;
            else if (npc.velocity.Length() > 13f)
                moveSpeed *= 0.98f;

            moveSpeed = MathHelper.Clamp(moveSpeed, 13f, 25f);

            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * moveSpeed;

            if (attackTimer % 45f == 44f && !npc.WithinRange(target.Center, 210f))
            {
                // Create some mouth dust.
                for (int i = 0; i < 20; i++)
                {
                    Dust electricity = Dust.NewDustPerfect(npc.Center + npc.velocity.SafeNormalize(Vector2.Zero) * 30f, 229);
                    electricity.velocity = Main.rand.NextVector2Circular(5f, 5f) + npc.velocity;
                    electricity.scale = 1.9f;
                    electricity.noGravity = true;
                }

                Main.PlaySound(SoundID.Item94, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        float offsetAngle = MathHelper.Lerp(-0.41f, 0.41f, i / 6f);
                        Vector2 sparkVelocity = npc.SafeDirectionTo(target.Center, -Vector2.UnitY).RotatedBy(offsetAngle) * 8f;
                        Utilities.NewProjectileBetter(npc.Center + sparkVelocity * 3f, sparkVelocity, ModContent.ProjectileType<WeaverSpark>(), 245, 0f);
                    }
                }
            }

            if (attackTimer >= 450f)
                SelectNewAttack(npc);
        }

        public static void DoAttack_LightningDischarge(NPC npc, Player target, float attackTimer)
        {
            float turnSpeed = (!npc.WithinRange(target.Center, 220f)).ToInt() * 0.041f;
            float moveSpeed = npc.velocity.Length();

            if (npc.WithinRange(target.Center, 285f))
                moveSpeed *= 1.01f;
            else if (npc.velocity.Length() > 10.5f)
                moveSpeed *= 0.98f;

            moveSpeed = MathHelper.Clamp(moveSpeed, 7f, 17f);

            npc.velocity = npc.velocity.RotateTowards(npc.AngleTo(target.Center), turnSpeed, true) * moveSpeed;

            if (attackTimer % 60f == 59f)
            {
                if (!npc.WithinRange(target.Center, 210f))
                {
                    Main.PlaySound(SoundID.Item122, target.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            Vector2 lightningVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * (i + Main.rand.NextFloatDirection() * 0.05f) / 10f) * 6.6f;
                            int arc = Utilities.NewProjectileBetter(npc.Center + lightningVelocity * 2f, lightningVelocity, ProjectileID.CultistBossLightningOrbArc, 245, 0f);
                            if (Main.projectile.IndexInRange(arc))
                            {
                                Main.projectile[arc].ai[0] = lightningVelocity.ToRotation();
                                Main.projectile[arc].ai[1] = Main.rand.Next(100);
                                Main.projectile[arc].tileCollide = false;
                            }
                        }
                    }
                }
            }

            if (attackTimer >= 320f)
                SelectNewAttack(npc);
        }

        public static void SelectNewAttack(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int i = 0; i < 4; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            ref float attackState = ref npc.ai[1];
            float oldAttackState = npc.ai[1];
            WeightedRandom<float> newStatePicker = new WeightedRandom<float>(Main.rand);
            newStatePicker.Add((int)StormWeaverArmoredAttackType.NormalMove, 1.5);
            newStatePicker.Add((int)StormWeaverArmoredAttackType.SparkBurst);
            newStatePicker.Add((int)StormWeaverArmoredAttackType.LightningDischarge);

            do
                attackState = newStatePicker.Get();
            while (attackState == oldAttackState);

            npc.ai[1] = (int)attackState;
            npc.ai[2] = 0f;
            npc.netUpdate = true;
        }
        #endregion AI
    }
}
