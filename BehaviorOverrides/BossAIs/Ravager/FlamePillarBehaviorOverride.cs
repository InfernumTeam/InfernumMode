using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Ravager;
using CalamityMod.World;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class FlamePillarBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<FlamePillar>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCSetDefaults;

        public override void SetDefaults(NPC npc)
        {
            npc.damage = 0;
            npc.width = 40;
            npc.height = 150;
            npc.lifeMax = 100;
            npc.alpha = 255;
            npc.aiStyle = -1;
            npc.knockBackResist = 0f;
            npc.LifeMaxNERB(4145, 4145, 96960);
            if (CalamityWorld.downedProvidence && !BossRushEvent.BossRushActive)
                npc.lifeMax = 38650;

            for (int k = 0; k < npc.buffImmune.Length; k++)
                npc.buffImmune[k] = true;

            npc.HitSound = SoundID.NPCHit42;
            npc.DeathSound = SoundID.NPCDeath55;
        }

        public override bool PreAI(NPC npc)
        {
            npc.chaseable = true;
            npc.dontTakeDamage = false;
            ref float attackTimer = ref npc.Infernum().ExtraAI[0];

            npc.TargetClosest();

            // Fuck off if the Ravager isn't around.
            if (CalamityGlobalNPC.scavenger < 0 || !Main.npc[CalamityGlobalNPC.scavenger].active)
            {
                npc.life = 0;
                npc.HitEffect(npc.direction, 9999);
                npc.netUpdate = true;
                return false;
            }

            // Prevent despawning.
            if (npc.timeLeft < 1800)
                npc.timeLeft = 1800;

            // Emit light.
            Lighting.AddLight(npc.Center, 0f, 0.5f, 0.5f);

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.03f, 0f, 1f);

            Player target = Main.player[npc.target];

            // Emit cinders that empower the Ravager.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer > 35f && Main.rand.NextBool(6))
            {
                Vector2 flameVelocity = -Vector2.UnitY.RotatedByRandom(0.56f) * Main.rand.NextFloat(7f, 11f);
                float spawnOffsetFactor = Main.rand.NextFloat(3.5f, 5f);
                Utilities.NewProjectileBetter(npc.Center - flameVelocity * spawnOffsetFactor, flameVelocity, ModContent.ProjectileType<RitualFlame>(), 0, 0f);
            }

            bool shouldBeBuffed = CalamityWorld.downedProvidence && !BossRushEvent.BossRushActive;

            // Release bursts of dark flames.
            if (attackTimer > 25f && attackTimer % 75f == 74f)
            {
                Main.PlaySound(SoundID.Item100, npc.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int fireballsPerBurst = shouldBeBuffed ? 5 : 4;
                    int darkMagicFireballDamage = shouldBeBuffed ? 335 : 215;
                    float darkMagicFireballSpeed = shouldBeBuffed ? 17f : 11f;
                    for (int i = 0; i < fireballsPerBurst; i++)
                    {
                        Vector2 darkMagicFireballVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(MathHelper.TwoPi * i / fireballsPerBurst) * darkMagicFireballSpeed;
                        Utilities.NewProjectileBetter(npc.Center + darkMagicFireballVelocity * 2f, darkMagicFireballVelocity, ModContent.ProjectileType<DarkMagicEmber>(), darkMagicFireballDamage, 0f);
                    }
                    npc.netUpdate = true;
                }
            }

            attackTimer++;
            return false;
        }
    }
}
