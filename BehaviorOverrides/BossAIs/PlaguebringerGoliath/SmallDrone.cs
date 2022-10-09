using CalamityMod;
using CalamityMod.Items.Weapons.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class SmallDrone : ModNPC
    {
        public int SpinDirection = 1;
        public float MoveIncrement = 0;
        public Vector2 InitialTargetPosition;
        public Player Target => Main.player[NPC.target];
        public ref float AttackTimer => ref NPC.ai[0];
        public ref float NextDroneIndex => ref NPC.ai[1];
        public ref float OffsetDirection => ref NPC.ai[2];
        public ref float MoveOffset => ref NPC.ai[3];
        public int LaserAttackTime => (int)MoveIncrement * TimeOffsetPerIncrement + 250;
        public const int TimeOffsetPerIncrement = 45;
        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("Small Drone");
            Main.npcFrameCount[NPC.type] = 5;
        }

        public override void SetDefaults()
        {
            NPC.damage = 140;
            NPC.npcSlots = 0f;
            NPC.width = NPC.height = 42;
            NPC.defense = 15;
            NPC.lifeMax = 1000;
            NPC.aiStyle = AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
        }

        public override void AI()
        {
            Lighting.AddLight(NPC.Center, 0.03f, 0.2f, 0f);

            // Handle despawn stuff.
            if (!Target.active || Target.dead)
            {
                NPC.TargetClosest(false);
                if (!Target.active || Target.dead)
                {
                    if (NPC.timeLeft > 10)
                        NPC.timeLeft = 10;
                    return;
                }
            }
            else if (NPC.timeLeft > 600)
                NPC.timeLeft = 600;

            if (InitialTargetPosition == Vector2.Zero)
                InitialTargetPosition = Target.Center;

            MoveOffset = MathHelper.Lerp(0f, MoveIncrement * 100f + 1400f, 1f - AttackTimer / LaserAttackTime);
            MoveOffset += MathHelper.Lerp(450f, 0f, Utils.GetLerpValue(-35f, 0f, AttackTimer, true));
            NPC.Center = InitialTargetPosition + OffsetDirection.ToRotationVector2() * MoveOffset;

            if (AttackTimer == 0f)
            {
                SoundEngine.PlaySound(Karasawa.FireSound, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 laserDirection = NPC.SafeDirectionTo(Main.npc[(int)NextDroneIndex].Center, Vector2.UnitY);
                    int laser = Utilities.NewProjectileBetter(NPC.Center, laserDirection, ModContent.ProjectileType<PlagueDeathray>(), 270, 0f);
                    if (Main.projectile.IndexInRange(laser))
                    {
                        Main.projectile[laser].ModProjectile<PlagueDeathray>().LocalLifetime = 1200;
                        Main.projectile[laser].ai[1] = NPC.whoAmI;
                    }
                }
            }
            AttackTimer++;

            // Explode if close to the target or enough time has passed.
            if (NPC.WithinRange(Target.Center, 40f) || AttackTimer > LaserAttackTime)
            {
                NPC.life = 0;
                NPC.checkDead();
                NPC.active = false;
            }
        }

        public override bool PreKill() => false;

        public override bool CheckDead()
        {
            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, NPC.position);

            NPC.position = NPC.Center;
            NPC.width = NPC.height = 84;
            NPC.Center = NPC.position;

            for (int i = 0; i < 15; i++)
            {
                Dust plague = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 89, 0f, 0f, 100, default, 1.4f);
                if (Main.rand.NextBool(2))
                {
                    plague.scale = 0.5f;
                    plague.fadeIn = Main.rand.NextFloat(1f, 2f);
                }
                plague.velocity *= 3f;
                plague.noGravity = true;
            }

            for (int i = 0; i < 30; i++)
            {
                Dust plague = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 89, 0f, 0f, 100, default, 1.85f);
                plague.noGravity = true;
                plague.velocity *= 5f;

                plague = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 89, 0f, 0f, 100, default, 2f);
                plague.velocity *= 2f;
                plague.noGravity = true;
            }
            return true;
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frameCounter++;

            if (NPC.frameCounter >= 5D)
            {
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= frameHeight * Main.npcFrameCount[NPC.type])
                    NPC.frame.Y = 0;

                NPC.frameCounter = 0D;
            }
        }
    }
}
