using CalamityMod.NPCs;
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
            npc.damage = 0;
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
            npc.damage = AttackTimer > 20f ? npc.defDamage : 0;
            npc.spriteDirection = (Target.Center.X < npc.Center.X).ToDirectionInt();
            npc.rotation = Math.Abs(npc.velocity.Y * 0.02f);

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
                Vector2 hoverDestination = Target.Center - Vector2.UnitY * 350f;
                if (!npc.WithinRange(hoverDestination, 100f))
                {
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 24f, 0.7f);
                    npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 29f, 0.05f);
                }
            }
            else
            {
                npc.velocity *= 0.96f;
                npc.velocity = npc.velocity.MoveTowards(Vector2.Zero, 0.15f);
                if (WrappedAttackTimer == 65f)
                {
                    Main.PlaySound(SoundID.DD2_SkyDragonsFuryShot, npc.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 shootVelocity = npc.SafeDirectionTo(Target.Center).RotatedBy(MathHelper.Lerp(-0.3f, 0.3f, i / 2f)) * 7f;
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
