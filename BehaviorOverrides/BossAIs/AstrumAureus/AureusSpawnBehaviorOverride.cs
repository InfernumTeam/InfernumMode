using CalamityMod.Events;
using CalamityMod.NPCs.AstrumAureus;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

using AureusBoss = CalamityMod.NPCs.AstrumAureus.AstrumAureus;

namespace InfernumMode.BehaviorOverrides.BossAIs.AstrumAureus
{
    public class AureusSpawnBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AureusSpawn>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI;

        public override bool PreAI(NPC npc)
        {
            int aureus = NPC.FindFirstNPC(ModContent.NPCType<AureusBoss>());
            ref float ringAngle = ref npc.ai[0];
            ref float ringRadius = ref npc.ai[1];
            ref float ringIrregularity = ref npc.ai[2];
            ref float time = ref npc.ai[3];
            ref float explodeTimer = ref npc.Infernum().ExtraAI[0];

            npc.DeathSound = SoundID.Item11;

            if (explodeTimer > 0f)
            {
                npc.velocity *= 0.96f;
                npc.rotation = Math.Abs(npc.velocity.X) * npc.spriteDirection * 0.03f;
                if (explodeTimer >= 35f)
                {
                    npc.life = 0;
                    npc.HitEffect();
                    npc.checkDead();
                    npc.active = false;
                }
                else
                    npc.scale *= 1.03f;

                explodeTimer++;
                return false;
            }

            // Explode if Astrum Aureus is not present or enough time has passed.
            if (aureus == -1 || time >= 600f)
            {
                explodeTimer = 1f;
                npc.netUpdate = true;
                return false;
            }

            // Fade in and out over time.
            npc.Opacity = Utils.InverseLerp(0f, 15f, time, true) * Utils.InverseLerp(600f, 550f, time, true);

            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Orbit around Aureus until a certain amount of life is lost.
            if (lifeRatio > 0.97f)
            {
                if (npc.direction == 0)
                    npc.direction = Main.rand.NextBool(2).ToDirectionInt();

                ringAngle += npc.direction * 0.018f;
                Vector2 orbitOffset = ringAngle.ToRotationVector2() * ringRadius;
                orbitOffset.Y *= 1f + ringIrregularity;

                npc.Center = Main.npc[aureus].Center + orbitOffset;
                npc.spriteDirection = (Main.npc[aureus].Center.X < npc.Center.X).ToDirectionInt();
                npc.velocity = Vector2.Zero;
                npc.damage = 0;
            }
            else
            {
                // Come closer to death by lifetime.
                if (time < 350f)
                    time = 350f;

                // Get a target.
                npc.TargetClosest();
                Player target = Main.player[npc.target];

                // Determine if tile collision is happening.
                Tile tile = Framing.GetTileSafely(npc.Center.ToTileCoordinates());
                bool collidingWithTile = tile.nactive() && Main.tileSolid[tile.type] && !Main.tileSolidTop[tile.type] && !TileID.Sets.Platforms[tile.type];

                if (collidingWithTile)
                {
                    npc.life = 0;
                    npc.HitEffect();
                    npc.checkDead();
                    npc.active = false;
                    return false;
                }

                if (npc.WithinRange(target.Center, 60f))
                {
                    explodeTimer = 1f;
                    return false;
                }

                float flySpeed = BossRushEvent.BossRushActive ? 31f : 20.5f;
                npc.velocity = (npc.velocity * 50f + npc.SafeDirectionTo(target.Center) * flySpeed) / 51f;
                npc.spriteDirection = (target.Center.X < npc.Center.X).ToDirectionInt();

                // Do damage and become invincible.
                npc.dontTakeDamage = true;
                npc.damage = 140;
            }

            npc.rotation = Math.Abs(npc.velocity.X) * npc.spriteDirection * 0.03f;

            time++;
            return false;
        }
    }
}
