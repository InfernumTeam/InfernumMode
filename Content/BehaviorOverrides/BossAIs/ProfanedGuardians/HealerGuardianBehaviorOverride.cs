using CalamityMod.NPCs;
using CalamityMod.NPCs.ProfanedGuardians;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians.AttackerGuardianBehaviorOverride;


namespace InfernumMode.Content.BehaviorOverrides.BossAIs.ProfanedGuardians
{
    public class HealerGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ProfanedGuardianHealer>();

        public override int? NPCIDToDeferToForTips => ModContent.NPCType<ProfanedGuardianCommander>();

        public enum HealerAttackType
        {
            SpawnEffects,
            SitAndMaintainFrontShield,
            SitAndShieldCommander
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

            NPC commander = Main.npc[CalamityGlobalNPC.doughnutBoss];
            Player target = Main.player[commander.target];
            ref float attackState = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];

            npc.damage = 0;
            npc.target = commander.target;
            npc.spriteDirection = (npc.velocity.X > 0).ToDirectionInt();
            // Reset taking damage.
            npc.dontTakeDamage = false;

            switch ((HealerAttackType)attackState)
            {
                case HealerAttackType.SpawnEffects:
                    DoBehavior_SpawnEffects(npc, target, ref attackTimer);
                    break;
                case HealerAttackType.SitAndMaintainFrontShield:
                    DoBehavior_SitAndMaintainFrontShield(npc, target, ref attackTimer, commander);
                    break;
                case HealerAttackType.SitAndShieldCommander:
                    break;
            }

            attackTimer++;
            return false;
        }

        public void DoBehavior_SitAndMaintainFrontShield(NPC npc, Player target, ref float attackTimer, NPC commander)
        {
            // Spawn the shield if this is the first frame.
            if (attackTimer == 1f)
                NPC.NewNPCDirect(npc.GetSource_FromAI(), npc.Center + new Vector2(-350f, 0f), ModContent.NPCType<HealerShieldCrystal>(), target: target.whoAmI);

            // Bob up and down on the spot.
            float sine = -MathF.Sin(attackTimer * 0.05f);
            npc.velocity.Y = sine * 1.5f;

            // Take no damage.
            npc.dontTakeDamage = true;

            // Check if the commander is on the next attack, if so, join it.
            if ((AttackerGuardianAttackState)commander.ai[0] == AttackerGuardianAttackState.EmpoweringDefender)
            {
                attackTimer = 0f;
                npc.ai[0] = 2f;
            }
        }

        public void DoBehavior_SitAndShieldCommander(NPC npc, Player target, ref float attackTimer, NPC commander)
        {
            // The healer is placing a shield around the commander, and will continue to do so until they die. They periodically emit stars in this attack too.
            int crystalAmount = 4;
            float crystalReleaseRate = 240;

            // Bob up and down on the spot.
            float sine = -MathF.Sin(attackTimer * 0.05f);
            npc.velocity.Y = sine * 1.5f;
                
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
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ProfanedGuardians/ProfanedGuardianHealerGlow").Value;
            Texture2D glowmask2 = ModContent.Request<Texture2D>("CalamityMod/NPCs/ProfanedGuardians/ProfanedGuardianHealerGlow2").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = npc.frame.Size() * 0.5f;
            SpriteEffects direction = npc.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Asset<Texture2D> shaderTexture = InfernumTextureRegistry.HolyCrystalLayer;
            Vector2 shaderScale = new Vector2(1.05f, 1.05f) * npc.scale;

            // If maintaining the front shield or shielding the commander, glow.
            if ((HealerAttackType)npc.ai[0] is HealerAttackType.SitAndMaintainFrontShield or HealerAttackType.SitAndShieldCommander)
                DrawNPCBackglow(npc, spriteBatch, texture, direction);

            // Draw the npc.
            Main.spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, direction, 0f);
            Main.spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, direction, 0f);
            Main.spriteBatch.Draw(glowmask2, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, direction, 0f);
            return false;
        }

        public static void DrawNPCBackglow(NPC npc, SpriteBatch spriteBatch, Texture2D npcTexture, SpriteEffects direction)
        {
            int backglowAmount = 12;
            for (int i = 0; i < backglowAmount; i++)
            {
                Vector2 backglowOffset = (MathHelper.TwoPi * i / backglowAmount).ToRotationVector2() * 4f;
                Color backglowColor = MagicCrystalShot.ColorSet[0];
                backglowColor.A = 0;
                spriteBatch.Draw(npcTexture, npc.Center + backglowOffset - Main.screenPosition, npc.frame, backglowColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, direction, 0);
            }
        }
        #endregion
    }
}