using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class SeekerBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<SoulSeekerSupreme>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            // Disappear if Supreme Calamitas is not present.
            if (CalamityGlobalNPC.SCal == -1)
            {
                npc.active = false;
                return false;
            }

            npc.knockBackResist = 0f;
            npc.target = Main.npc[CalamityGlobalNPC.SCal].target;
            Player target = Main.player[npc.target];
            Rectangle arena = Main.npc[CalamityGlobalNPC.SCal].Infernum().arenaRectangle;

            ref float variant = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            switch ((int)variant)
            {
                // Arena huggers.
                case 0:
                    // Explode if few seekers remain, to prevent dragging out the fight.
                    if (NPC.CountNPCS(npc.type) <= 4)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, npc.Center);
                        Utilities.CreateGenericDustExplosion(npc.Center, (int)CalamityDusts.Brimstone, 25, 8f, 1.45f);
                        npc.active = false;
                    }

                    ref float fuck = ref npc.ai[2];
                    if (npc.velocity == Vector2.Zero)
                    {
                        npc.velocity = -Vector2.UnitY;
                        fuck = 1f;
                    }

                    if (npc.Center.Y < arena.Top && fuck == 1f)
                    {
                        npc.velocity = -Vector2.UnitX;
                        fuck = 2f;
                    }

                    if (npc.Center.X < arena.Left + 50f && fuck == 2f)
                    {
                        npc.velocity = Vector2.UnitY;
                        fuck = 3f;
                    }

                    if (npc.Center.Y > arena.Bottom && fuck == 3f)
                    {
                        npc.velocity = Vector2.UnitX;
                        fuck = 4f;
                    }

                    if (npc.Center.X > arena.Right - 50f && fuck == 4f)
                    {
                        npc.velocity = -Vector2.UnitY;
                        fuck = 1f;
                    }

                    int totalArenaHuggers = 0;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        if (Main.npc[i].active && Main.npc[i].type == npc.type && Main.npc[i].ai[0] == 0f)
                            totalArenaHuggers++;
                    }
                    float moveSpeed = 11f;
                    float shootSpeed = MathHelper.Lerp(9f, 20f, 1f - totalArenaHuggers / 4f);

                    npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * moveSpeed;
                    npc.Center = Vector2.Clamp(npc.Center, arena.TopLeft() - Vector2.One * 4f, arena.BottomRight() + Vector2.One * 4f);

                    // Release fire cyclicly.
                    if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 70f > 35f && attackTimer % 4f == 3f)
                    {
                        Vector2 shootVelocity = npc.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero);
                        if (Vector2.Dot(npc.SafeDirectionTo(arena.Center.ToVector2()), shootVelocity) < 0f)
                            shootVelocity *= -1f;
                        shootVelocity *= shootSpeed;
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<ShadowBlast2>(), 530, 0f);
                    }
                    break;

                // Circle the target and shoot blasts inward.
                case 1:
                    ref float offsetAngle = ref npc.ai[2];
                    Vector2 hoverDestination = target.Center + offsetAngle.ToRotationVector2() * 560f;
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 26f, 1.2f);

                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.WithinRange(hoverDestination, 50f) && attackTimer % 24f == 23f)
                    {
                        Vector2 shootVelocity = npc.SafeDirectionTo(target.Center) * 4f;
                        Utilities.NewProjectileBetter(npc.Center, shootVelocity, ModContent.ProjectileType<AcceleratingDarkMagicBurst>(), 530, 0f);
                        npc.netUpdate = true;
                    }
                    offsetAngle += MathHelper.ToRadians(0.67f);
                    break;

                // Slowly hover near the target and harm them if they're close with Abyssal Flames.
                case 2:
                    npc.dontTakeDamage = true;

                    // Explode if other seekers are gone.
                    if (NPC.CountNPCS(npc.type) <= 2)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, npc.Center);
                        Utilities.CreateGenericDustExplosion(npc.Center, (int)CalamityDusts.Brimstone, 25, 8f, 1.45f);
                        npc.active = false;
                    }

                    if (npc.WithinRange(target.Center, 125f))
                        target.AddBuff(ModContent.BuffType<AbyssalFlames>(), 45);
                    npc.SimpleFlyMovement(npc.SafeDirectionTo(target.Center + Vector2.UnitY * ((npc.whoAmI * 461f) % 100f)) * 8f, 0.4f);
                    break;
            }
            npc.timeLeft = 3600;
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            attackTimer++;
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            if (npc.ai[0] != 2f)
                return true;

            Texture2D pulseTexture = Main.glowMaskTexture[239];
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = pulseTexture.Size() * 0.5f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.spriteBatch.Draw(pulseTexture, drawPosition, null, Color.MediumPurple, 0f, origin, npc.scale * 1.8f, SpriteEffects.None, 0f);
            Main.spriteBatch.ResetBlendState();
            return true;
        }
    }
}
