using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class HealerGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianBoss3>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        public override bool PreAI(NPC npc)
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.doughnutBoss) || !Main.npc[CalamityGlobalNPC.doughnutBoss].active)
            {
                npc.active = false;
                npc.netUpdate = true;
                return false;
            }

            NPC attacker = Main.npc[CalamityGlobalNPC.doughnutBoss];
            Player target = Main.player[attacker.target];
            ref float attackTimer = ref npc.Infernum().ExtraAI[0];

            npc.damage = 0;
            npc.target = attacker.target;
            npc.spriteDirection = (npc.velocity.X > 0).ToDirectionInt();

            // Hover near the target.
            npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();
            Vector2 hoverDestination = target.Center + new Vector2(npc.spriteDirection * 600f, -300f);
            if (!npc.WithinRange(hoverDestination, 80f))
            {
                Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * 14.5f;
                npc.SimpleFlyMovement(idealVelocity, 0.2f);
                npc.velocity = npc.velocity.MoveTowards(idealVelocity, 0.15f);
            }
            else
                npc.velocity *= 0.98f;

            // Release a burst of crystals.
            float wrappedAttackTimer = attackTimer % 360f;

            if (Main.netMode != NetmodeID.Server && wrappedAttackTimer == 100f)
                Main.PlaySound(SoundID.DD2_DarkMageCastHeal.WithVolume(1.6f), target.Center);

            if (wrappedAttackTimer == 145f)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    Main.PlaySound(SoundID.DD2_PhantomPhoenixShot.WithVolume(1.6f), target.Center);
                    Main.PlaySound(SoundID.DD2_DarkMageHealImpact.WithVolume(1.6f), target.Center);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 projectileSpawnPosition = npc.Center + new Vector2(npc.spriteDirection * -32f, 12f);
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 shootVelocity = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 12.5f;
                        Utilities.NewProjectileBetter(projectileSpawnPosition, shootVelocity, ModContent.ProjectileType<MagicCrystalShot>(), 230, 0f);
                    }
                }
            }

            attackTimer++;
            return false;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            float wrappedAttackTimer = npc.Infernum().ExtraAI[0] % 360f;
            float gleamInterpolant = Utils.InverseLerp(100f, 145f, wrappedAttackTimer, true) * Utils.InverseLerp(165f, 145f, wrappedAttackTimer, true);
            Texture2D texture = Main.npcTexture[npc.type];
            Texture2D glowmask = ModContent.GetTexture("CalamityMod/NPCs/ProfanedGuardians/ProfanedGuardianBoss3Glow");
            Texture2D glowmask2 = ModContent.GetTexture("CalamityMod/NPCs/ProfanedGuardians/ProfanedGuardianBoss3Glow2");
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = npc.frame.Size() * 0.5f;
            SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), 0f, origin, npc.scale, direction, 0f);
            spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(Color.White), 0f, origin, npc.scale, direction, 0f);
            spriteBatch.Draw(glowmask2, drawPosition, npc.frame, npc.GetAlpha(Color.White), 0f, origin, npc.scale, direction, 0f);
            if (gleamInterpolant > 0f)
            {
                Texture2D gleamTexture = ModContent.GetTexture("CalamityMod/Projectiles/StarProj");
                Vector2 gleamOrigin = gleamTexture.Size() * 0.5f;
                Vector2 gleamDrawPosition = drawPosition + new Vector2(npc.spriteDirection * -32f, 12f);
                Color gleamColor = Color.Lerp(Color.Transparent, new Color(0.95f, 0.95f, 0.25f, 0f), gleamInterpolant);
                Vector2 gleamScale = new Vector2(1f, 2f) * npc.scale * gleamInterpolant;
                float gleamRotation = MathHelper.Pi * Utils.InverseLerp(100f, 165f, wrappedAttackTimer, true) * 3f;
                spriteBatch.Draw(gleamTexture, gleamDrawPosition, null, gleamColor, gleamRotation, gleamOrigin, gleamScale, 0, 0f);
                spriteBatch.Draw(gleamTexture, gleamDrawPosition, null, gleamColor, -gleamRotation, gleamOrigin, gleamScale, 0, 0f);
            }

            return false;
        }
    }
}