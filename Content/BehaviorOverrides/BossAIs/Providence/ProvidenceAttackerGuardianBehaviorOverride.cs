using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Providence;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Providence.ProvidenceBehaviorOverride;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class ProvidenceAttackerGuardianBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<ProvSpawnOffense>();

        #region AI

        public override bool PreAI(NPC npc)
        {
            // Disappear if Providence is not present.
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.holyBoss))
            {
                npc.active = false;
                return false;
            }

            NPC providence = Main.npc[CalamityGlobalNPC.holyBoss];

            npc.target = providence.target;
            Player target = Main.player[npc.target];
            ref float spearAttackState = ref providence.Infernum().ExtraAI[0];
            ref float offsetRadius = ref npc.ai[0];
            ref float offsetAngle = ref npc.ai[1];

            // Stick to Providence and look towards her target.
            npc.Center = providence.Bottom - Vector2.UnitY.RotatedBy(offsetAngle) * offsetRadius + Vector2.UnitY * providence.Infernum().ExtraAI[2];
            npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();

            // Disable contact damage.
            npc.damage = 0;

            // Disable HP bar effects since these things die quickly.
            npc.Calamity().ShouldCloseHPBar = true;

            // Create a spear on the first frame for this guardian.
            if (npc.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(CommonCalamitySounds.MeatySlashSound, npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<CommanderSpear2>(), HolySpearDamage, 0f, -1, npc.whoAmI);
                npc.localAI[0] = 1f;
            }

            // Explode if Providence permits it and colliding with tiles.
            if (providence.Infernum().ExtraAI[3] == 1f && Collision.SolidCollision(npc.TopLeft, npc.width, npc.height))
            {
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 18f;
                ScreenEffectSystem.SetBlurEffect(npc.Center, 0.1f, 15);

                SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceLavaEruptionSound with { Pitch = 0.4f, Volume = 0.8f }, npc.Center);

                Color ashColor = IsEnraged ? Color.Teal : new Color(255, 191, 73);
                for (int j = 0; j < 100; j++)
                {
                    Vector2 ashSpawnPosition = npc.Center + Main.rand.NextVector2Circular(200f, 200f);
                    Vector2 ashVelocity = npc.SafeDirectionTo(ashSpawnPosition) * Main.rand.NextFloat(1.5f, 2f);
                    Particle ash = new MediumMistParticle(ashSpawnPosition, ashVelocity, ashColor, Color.Gray, Main.rand.NextFloat(0.75f, 0.95f), 400f, Main.rand.NextFloat(-0.04f, 0.04f));
                    GeneralParticleHandler.SpawnParticle(ash);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<ProvidenceWave>(), 0, 0f);

                    // Release fireballs below the target.
                    for (int i = 0; i < 3; i++)
                        Utilities.NewProjectileBetter(target.Center + new Vector2(Main.rand.NextFloatDirection() * 400f, Main.rand.NextFloat(720f, 780f)), -Vector2.UnitY * 11f, ModContent.ProjectileType<HolyBasicFireball>(), BasicFireballDamage, 0f);
                }

                npc.active = false;
            }

            return false;
        }

        #endregion

        #region Drawing

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Calculate the appropriate direction and various other important draw variables.
            int afterimageCount = 5;
            Vector2 origin = npc.frame.Size() * 0.5f;
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (npc.spriteDirection == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("CalamityMod/NPCs/ProfanedGuardians/ProfanedGuardianCommanderGlow").Value;
            if (IsEnraged)
            {
                texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/ProvidenceAttackerGuardianNight").Value;
                glowmask = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Providence/ProvidenceAttackerGuardianNightGlow").Value;
            }

            // Draw the base texture.
            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < afterimageCount; i += 2)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, Color.White, 0.5f)) * ((afterimageCount - i) / 15f);
                    Vector2 afterimageDrawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(texture, afterimageDrawPosition, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            Vector2 drawPosition = npc.Center - Main.screenPosition;
            spriteBatch.Draw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, spriteEffects, 0f);

            // Draw the glowmask.
            if (CalamityConfig.Instance.Afterimages)
            {
                for (int i = 1; i < afterimageCount; i++)
                {
                    Color afterimageColor = npc.GetAlpha(Color.Lerp(lightColor, Color.White, 0.5f)) * ((afterimageCount - i) / 15f);
                    Vector2 afterimageDrawPosition = npc.oldPos[i] + npc.Size * 0.5f - Main.screenPosition;
                    spriteBatch.Draw(glowmask, afterimageDrawPosition, npc.frame, afterimageColor, npc.rotation, origin, npc.scale, spriteEffects, 0f);
                }
            }

            spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, spriteEffects, 0f);
            return false;
        }
        #endregion Drawing
    }
}
