using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class HealerGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianHealer>();

        public override int? NPCIDToDeferToForTips => ModContent.NPCType<ProfanedGuardianCommander>();

        public enum HealerAttackType
        {
            SpawnEffects,
            SitAndFireCrystals,
        }

        #region AI
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
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            npc.damage = 0;
            npc.target = attacker.target;
            npc.spriteDirection = (npc.velocity.X > 0).ToDirectionInt();

            switch ((HealerAttackType)attackState)
            {
                case HealerAttackType.SpawnEffects:
                    AttackerGuardianBehaviorOverride.DoBehavior_SpawnEffects(npc, target, ref attackTimer);
                    break;
                case HealerAttackType.SitAndFireCrystals:
                    DoBehavior_SitAndFireCrystals(npc, target, ref attackTimer);
                    break;
            }

            attackTimer++;
            return false;
        }

        public void DoBehavior_SitAndFireCrystals(NPC npc, Player target, ref float attackTimer)
        {
            float crystalAmount = 3;
            float crystalReleaseRate = 180;

            // Only fire crystals if the player is close enough.
            if (target.WithinRange(npc.Center, 1500f) && attackTimer % crystalReleaseRate == 0)
            {
                // Play SFX if not the server.
                if (Main.netMode != NetmodeID.Server)
                {
                    SoundEngine.PlaySound(SoundID.DD2_PhantomPhoenixShot with { Volume = 1.6f }, target.Center);
                    SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact with { Volume = 1.6f }, target.Center);
                }

                // Fire projectiles.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 projectileSpawnPosition = npc.Center + new Vector2(npc.spriteDirection * -32f, 12f);
                    for (int i = 0; i < crystalAmount; i++)
                    {
                        Vector2 shootVelocity = (MathHelper.TwoPi * i / crystalAmount).ToRotationVector2() * 9f;
                        Utilities.NewProjectileBetter(projectileSpawnPosition, shootVelocity, ModContent.ProjectileType<MagicCrystalShot>(), 230, 0f);
                    }
                }
            }
        }

        #endregion

        #region Drawing
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            float wrappedAttackTimer = npc.Infernum().ExtraAI[0] % 360f;
            float gleamInterpolant = Utils.GetLerpValue(70f, 145f, wrappedAttackTimer, true) * Utils.GetLerpValue(165f, 145f, wrappedAttackTimer, true);
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ProfanedGuardians/ProfanedGuardianHealerGlow").Value;
            Texture2D glowmask2 = ModContent.Request<Texture2D>("CalamityMod/NPCs/ProfanedGuardians/ProfanedGuardianHealerGlow2").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = npc.frame.Size() * 0.5f;
            SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), 0f, origin, npc.scale, direction, 0f);
            Main.spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(Color.White), 0f, origin, npc.scale, direction, 0f);
            Main.spriteBatch.Draw(glowmask2, drawPosition, npc.frame, npc.GetAlpha(Color.White), 0f, origin, npc.scale, direction, 0f);
            if (gleamInterpolant > 0f)
            {
                Texture2D gleamTexture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/StarProj").Value;
                Vector2 gleamOrigin = gleamTexture.Size() * 0.5f;
                Vector2 gleamDrawPosition = drawPosition + new Vector2(npc.spriteDirection * -32f, 12f);
                Color gleamColor = Color.Lerp(Color.Transparent, new Color(0.95f, 0.95f, 0.25f, 0f), gleamInterpolant);
                Vector2 gleamScale = new Vector2(1f, 2f) * npc.scale * gleamInterpolant * 2.8f;
                float gleamRotation = MathHelper.Pi * Utils.GetLerpValue(100f, 165f, wrappedAttackTimer, true) * 3f;
                Main.spriteBatch.Draw(gleamTexture, gleamDrawPosition, null, gleamColor, gleamRotation, gleamOrigin, gleamScale, 0, 0f);
                Main.spriteBatch.Draw(gleamTexture, gleamDrawPosition, null, gleamColor, -gleamRotation, gleamOrigin, gleamScale, 0, 0f);
            }

            return false;
        }
        #endregion
    }
}