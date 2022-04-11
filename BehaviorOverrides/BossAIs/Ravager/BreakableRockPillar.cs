using CalamityMod;
using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.Ravager
{
    public class BreakableRockPillar : ModNPC
    {
        public Player Target => Main.player[NPC.target];
        public ref float AttackTimer => ref NPC.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Rock Pillar");
        }

        public override void SetDefaults()
        {
            NPC.damage = 180;
            NPC.width = 60;
            NPC.height = 60;
            NPC.defense = 50;
            NPC.DR_NERD(0.3f);
            NPC.chaseable = false;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.canGhostHeal = false;
            NPC.lifeMax = CalamityWorld.downedProvidence ? 5600 : 1300;
            NPC.alpha = 255;
            NPC.aiStyle = -1;
            aiType = -1;
            NPC.knockBackResist = 0f;
            NPC.HitSound = SoundID.NPCHit41;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            // Handle despawn stuff.
            if (CalamityGlobalNPC.scavenger == -1)
            {
                NPC.active = false;
                return;
            }

            if (NPC.timeLeft < 3600)
                NPC.timeLeft = 3600;

            // Inherit Ravager's target.
            NPC.target = Main.npc[CalamityGlobalNPC.scavenger].target;

            // Rise up at first and spin.
            if (AttackTimer < 180f)
            {
                NPC.damage = 0;
                NPC.rotation += MathHelper.Lerp(0f, 0.3f, Utils.GetLerpValue(0f, 24f, AttackTimer, true) * Utils.GetLerpValue(180f, 30f, AttackTimer, true));
                NPC.Opacity = Utils.GetLerpValue(0f, 12f, AttackTimer, true);
                NPC.velocity = Vector2.UnitY * MathHelper.Lerp(-40f, 0f, Utils.GetLerpValue(0f, 30f, AttackTimer, true));

                // Stop if enough time has passed and the ideal direction is being aimed at.
                Vector2 idealDirection = NPC.SafeDirectionTo(Target.Center);
                idealDirection = new Vector2(idealDirection.X, idealDirection.Y * 0.15f).SafeNormalize(Vector2.Zero);

                // Lunge in the ideal direction if enough time has passed or aiming in the direction of the ideal velocity.
                bool canLunge = NPC.rotation.ToRotationVector2().AngleBetween(idealDirection) < 0.39f || AttackTimer > 175f;
                if (AttackTimer > 35f && canLunge)
                {
                    NPC.velocity = idealDirection * 19f;
                    AttackTimer = 180f;
                    NPC.netUpdate = true;
                }
            }

            // Release rocks downward after being launched.
            else if (AttackTimer % 12f == 11f)
            {
                SoundEngine.PlaySound(SoundID.Item51, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int rockDamage = CalamityWorld.downedProvidence && !BossRushEvent.BossRushActive ? 340 : 205;
                    Vector2 rockSpawnPosition = NPC.Center + NPC.rotation.ToRotationVector2() * Main.rand.NextFloatDirection() * 120f;
                    Utilities.NewProjectileBetter(rockSpawnPosition, Vector2.UnitY * 5f, ModContent.ProjectileType<RockPiece>(), rockDamage, 0f);
                }
            }

            if (AttackTimer > 180f)
            {
                NPC.damage = NPC.defDamage;
                NPC.rotation = NPC.velocity.ToRotation();
            }

            // Die naturally after enough time has passed.
            if (AttackTimer > 330f)
            {
                NPC.life = 0;
                NPC.HitEffect();
                NPC.checkDead();
            }

            AttackTimer++;
        }

        public override bool PreNPCLoot() => false;
    }
}
