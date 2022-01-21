using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class ShadowDemon : ModNPC
    {
        public Player Target => Main.player[npc.target];
        public ref float AttackTimer => ref npc.ai[0];
        public float WrappedAttackTimer => AttackTimer % 85f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadow Demon");
        }

        public override void SetDefaults()
        {
            npc.npcSlots = 0f;
            npc.width = npc.height = 124;
            npc.scale = 0.7f;
            npc.lifeMax = 333333;
            npc.dontTakeDamage = true;
            npc.aiStyle = aiType = -1;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.canGhostHeal = false;
        }

        public override void AI()
        {
            // Handle despawn stuff.
            if (CalamityGlobalNPC.SCal == -1)
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.15f, 0f, 1f);
                if (npc.Opacity <= 0f)
                    npc.active = false;

                for (int i = 0; i < 6; i++)
                {
                    Dust shadow = Dust.NewDustDirect(npc.position, npc.width, npc.height, 267);
                    shadow.color = Color.Lerp(Color.White, Color.Black, Main.rand.NextFloat(0.7f, 1f));
                    shadow.velocity = Main.rand.NextVector2Circular(5f, 5f);
                    shadow.scale *= Main.rand.NextFloat(1f, 1.3f);
                    shadow.noGravity = true;
                }
                return;
            }

            if (npc.timeLeft < 3600)
                npc.timeLeft = 3600;

            // Inherit Supreme Calamitas' target.
            npc.target = Main.npc[CalamityGlobalNPC.SCal].target;

            // Have a brief moment of no damage.
            npc.damage = 0;
            npc.spriteDirection = (Target.Center.X < npc.Center.X).ToDirectionInt();
            npc.rotation = Math.Abs(npc.velocity.Y * 0.02f);

            // Fade away and stop attacking if brothers are present.
            if (NPC.AnyNPCs(ModContent.NPCType<SupremeCataclysm>()) || NPC.AnyNPCs(ModContent.NPCType<SupremeCatastrophe>()))
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.035f, 0f, 1f);
                Vector2 hoverDestination = Main.npc[CalamityGlobalNPC.SCal].Center;
                if (!npc.WithinRange(hoverDestination, 100f))
                {
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 26f, 0.75f);
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 29f, 0.05f);
                }
                return;
            }

            if (Main.npc[CalamityGlobalNPC.SCal].ai[2] > 0f || SupremeCalamitasBehaviorOverride.CurrentAttack(Main.npc[CalamityGlobalNPC.SCal]) == SupremeCalamitasBehaviorOverride.SCalAttackType.LightningLines)
                AttackTimer = 0f;

            // Disappear and be absorbed as necessary.
            if (Main.npc[CalamityGlobalNPC.SCal].Infernum().ExtraAI[8] == 1f)
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.01f, 0f, 1f);
                Vector2 hoverDestination = Main.npc[CalamityGlobalNPC.SCal].Center;
                hoverDestination.X += (Main.npc[CalamityGlobalNPC.SCal].Center.X < npc.Center.X).ToDirectionInt() * 250f;
                if (!npc.WithinRange(hoverDestination, 100f))
                {
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 26f, 165f);
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 29f, 0.08f);
                }
                else
                    npc.velocity *= 0.95f;

                if (npc.Opacity <= 0f)
                    npc.active = false;

                for (int i = 0; i < 5; i++)
                {
                    if (Main.rand.NextFloat() < 1f - npc.Opacity)
                    {
                        Dust shadow = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2Circular(75f, 75f), 267);
                        shadow.color = Color.Lerp(Color.White, Color.Black, Main.rand.NextFloat(0.7f, 1f));
                        shadow.velocity = Vector2.Lerp(Main.rand.NextVector2Unit(), npc.SafeDirectionTo(Main.npc[CalamityGlobalNPC.SCal].Center), 0.7f);
                        shadow.velocity *= Main.rand.NextFloat(10f, 19f);
                        shadow.scale *= Main.rand.NextFloat(1f, 1.3f);
                        shadow.noGravity = true;
                    }
                }
                return;
            }

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.08f, 0f, 1f);

            // Release shadow particles.
            if (Main.rand.NextBool(20))
            {
                Dust shadow = Dust.NewDustDirect(npc.position, npc.width, npc.height, 267);
                shadow.color = Color.Lerp(Color.White, Color.Black, Main.rand.NextFloat(0.7f, 1f));
                shadow.velocity = Main.rand.NextVector2Circular(5f, 5f);
                shadow.scale *= Main.rand.NextFloat(1f, 1.3f);
                shadow.fadeIn = 1.4f;
                shadow.noGravity = true;
            }

            // Fly near the target and release blasts of shadow.
            if (WrappedAttackTimer < 40f)
            {
                Vector2 hoverDestination = Target.Center - Vector2.UnitY * 420f;
                if (!npc.WithinRange(hoverDestination, 100f))
                {
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 24f, 1.3f);
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 29f, 0.05f);
                }
            }
            else
            {
                npc.velocity *= 0.96f;
                npc.velocity = npc.velocity.MoveTowards(Vector2.Zero, 0.15f);
                if (WrappedAttackTimer == 65f && SupremeCalamitasBehaviorOverride.CurrentAttack(Main.npc[CalamityGlobalNPC.SCal]) != SupremeCalamitasBehaviorOverride.SCalAttackType.LightningLines)
                {
                    Main.PlaySound(SoundID.DD2_SkyDragonsFuryShot, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            Vector2 shootVelocity = npc.SafeDirectionTo(Target.Center).RotatedBy(MathHelper.TwoPi * i / 16f) * 5.5f;
                            Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<ShadowBlast>(), 550, 0f);
                        }
                    }
                }
            }
            AttackTimer++;
        }

        public override bool PreNPCLoot() => false;
    }
}
