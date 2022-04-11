using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class ShadowDemon : ModNPC
    {
        public Player Target => Main.player[NPC.target];
        public ref float AttackTimer => ref NPC.ai[0];
        public float WrappedAttackTimer => AttackTimer % 85f;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shadow Demon");
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 0f;
            NPC.width = NPC.height = 124;
            NPC.scale = 0.7f;
            NPC.lifeMax = 333333;
            NPC.dontTakeDamage = true;
            NPC.aiStyle = aiType = -1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.canGhostHeal = false;
        }

        public override void AI()
        {
            // Handle despawn stuff.
            if (CalamityGlobalNPC.SCal == -1)
            {
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity - 0.15f, 0f, 1f);
                if (NPC.Opacity <= 0f)
                    NPC.active = false;

                for (int i = 0; i < 6; i++)
                {
                    Dust shadow = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 267);
                    shadow.color = Color.Lerp(Color.White, Color.Black, Main.rand.NextFloat(0.7f, 1f));
                    shadow.velocity = Main.rand.NextVector2Circular(5f, 5f);
                    shadow.scale *= Main.rand.NextFloat(1f, 1.3f);
                    shadow.noGravity = true;
                }
                return;
            }

            if (NPC.timeLeft < 3600)
                NPC.timeLeft = 3600;

            // Inherit Supreme Calamitas' target.
            NPC.target = Main.npc[CalamityGlobalNPC.SCal].target;

            // Have a brief moment of no damage.
            NPC.damage = 0;
            NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();
            NPC.rotation = Math.Abs(NPC.velocity.Y * 0.02f);

            // Fade away and stop attacking if brothers are present.
            if (NPC.AnyNPCs(ModContent.NPCType<SupremeCataclysm>()) || NPC.AnyNPCs(ModContent.NPCType<SupremeCatastrophe>()))
            {
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity - 0.035f, 0f, 1f);
                Vector2 hoverDestination = Main.npc[CalamityGlobalNPC.SCal].Center;
                if (!NPC.WithinRange(hoverDestination, 100f))
                {
                    NPC.SimpleFlyMovement(NPC.SafeDirectionTo(hoverDestination) * 26f, 0.75f);
                    NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.SafeDirectionTo(hoverDestination) * 29f, 0.05f);
                }
                return;
            }

            if (Main.npc[CalamityGlobalNPC.SCal].ai[2] > 0f || SupremeCalamitasBehaviorOverride.CurrentAttack(Main.npc[CalamityGlobalNPC.SCal]) == SupremeCalamitasBehaviorOverride.SCalAttackType.LightningLines)
                AttackTimer = 0f;

            // Disappear and be absorbed as necessary.
            if (Main.npc[CalamityGlobalNPC.SCal].Infernum().ExtraAI[8] == 1f)
            {
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity - 0.01f, 0f, 1f);
                Vector2 hoverDestination = Main.npc[CalamityGlobalNPC.SCal].Center;
                hoverDestination.X += (Main.npc[CalamityGlobalNPC.SCal].Center.X < NPC.Center.X).ToDirectionInt() * 250f;
                if (!NPC.WithinRange(hoverDestination, 100f))
                {
                    NPC.SimpleFlyMovement(NPC.SafeDirectionTo(hoverDestination) * 26f, 165f);
                    NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.SafeDirectionTo(hoverDestination) * 29f, 0.08f);
                }
                else
                    NPC.velocity *= 0.95f;

                if (NPC.Opacity <= 0f)
                    NPC.active = false;

                for (int i = 0; i < 5; i++)
                {
                    if (Main.rand.NextFloat() < 1f - NPC.Opacity)
                    {
                        Dust shadow = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(75f, 75f), 267);
                        shadow.color = Color.Lerp(Color.White, Color.Black, Main.rand.NextFloat(0.7f, 1f));
                        shadow.velocity = Vector2.Lerp(Main.rand.NextVector2Unit(), NPC.SafeDirectionTo(Main.npc[CalamityGlobalNPC.SCal].Center), 0.7f);
                        shadow.velocity *= Main.rand.NextFloat(10f, 19f);
                        shadow.scale *= Main.rand.NextFloat(1f, 1.3f);
                        shadow.noGravity = true;
                    }
                }
                return;
            }

            // Fade in.
            NPC.Opacity = MathHelper.Clamp(NPC.Opacity + 0.08f, 0f, 1f);

            // Release shadow particles.
            if (Main.rand.NextBool(20))
            {
                Dust shadow = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 267);
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
                if (!NPC.WithinRange(hoverDestination, 100f))
                {
                    NPC.SimpleFlyMovement(NPC.SafeDirectionTo(hoverDestination) * 24f, 1.3f);
                    NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.SafeDirectionTo(hoverDestination) * 29f, 0.05f);
                }
            }
            else
            {
                NPC.velocity *= 0.96f;
                NPC.velocity = NPC.velocity.MoveTowards(Vector2.Zero, 0.15f);
                if (WrappedAttackTimer == 65f && SupremeCalamitasBehaviorOverride.CurrentAttack(Main.npc[CalamityGlobalNPC.SCal]) != SupremeCalamitasBehaviorOverride.SCalAttackType.LightningLines)
                {
                    SoundEngine.PlaySound(SoundID.DD2_SkyDragonsFuryShot, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            Vector2 shootVelocity = NPC.SafeDirectionTo(Target.Center).RotatedBy(MathHelper.TwoPi * i / 16f) * 5.5f;
                            Utilities.NewProjectileBetter(NPC.Center, shootVelocity, ModContent.ProjectileType<ShadowBlast>(), 500, 0f);
                        }
                    }
                }
            }
            AttackTimer++;
        }

        public override bool PreNPCLoot() => false;
    }
}
