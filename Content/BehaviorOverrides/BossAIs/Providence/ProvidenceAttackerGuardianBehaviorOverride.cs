using CalamityMod.NPCs;
using CalamityMod.NPCs.Providence;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using InfernumMode.Core.OverridingSystem;
using Terraria.ID;
using Terraria.Audio;
using CalamityMod.Sounds;
using System;
using static InfernumMode.Content.BehaviorOverrides.BossAIs.Providence.ProvidenceBehaviorOverride;
using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using CalamityMod;
using InfernumMode.Common.Graphics;

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
                for (int j = 0; j < 100; j++)
                {
                    Vector2 ashSpawnPosition = npc.Center + Main.rand.NextVector2Circular(200f, 200f);
                    Vector2 ashVelocity = npc.SafeDirectionTo(ashSpawnPosition) * Main.rand.NextFloat(1.5f, 2f);
                    Particle ash = new MediumMistParticle(ashSpawnPosition, ashVelocity, new Color(255, 191, 73), Color.Gray, Main.rand.NextFloat(0.75f, 0.95f), 400f, Main.rand.NextFloat(-0.04f, 0.04f));
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
    }
}
