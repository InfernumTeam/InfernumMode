using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.PlaguebringerGoliath
{
    public class SmallDrone : ModNPC
    {
        public int SpinDirection = 1;
        public float MoveIncrement = 0;
        public Vector2 InitialTargetPosition;
        public Player Target => Main.player[npc.target];
        public ref float AttackTimer => ref npc.ai[0];
        public ref float NextDroneIndex => ref npc.ai[1];
        public ref float OffsetDirection => ref npc.ai[2];
        public ref float MoveOffset => ref npc.ai[3];
        public int LaserAttackTime => (int)MoveIncrement * TimeOffsetPerIncrement + 250;
        public const int TimeOffsetPerIncrement = 45;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Small Drone");
            Main.npcFrameCount[npc.type] = 5;
        }

        public override void SetDefaults()
        {
            npc.damage = 140;
            npc.npcSlots = 0f;
            npc.width = npc.height = 42;
            npc.defense = 15;
            npc.lifeMax = 1000;
            npc.aiStyle = aiType = -1;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath14;
        }

        public override void AI()
        {
            Lighting.AddLight(npc.Center, 0.03f, 0.2f, 0f);

            // Handle despawn stuff.
            if (!Target.active || Target.dead)
            {
                npc.TargetClosest(false);
                if (!Target.active || Target.dead)
                {
                    if (npc.timeLeft > 10)
                        npc.timeLeft = 10;
                    return;
                }
            }
            else if (npc.timeLeft > 600)
                npc.timeLeft = 600;

            if (InitialTargetPosition == Vector2.Zero)
                InitialTargetPosition = Target.Center;

            MoveOffset = MathHelper.Lerp(0f, MoveIncrement * 100f + 1400f, 1f - AttackTimer / LaserAttackTime);
            MoveOffset += MathHelper.Lerp(450f, 0f, Utils.InverseLerp(-35f, 0f, AttackTimer, true));
            npc.Center = InitialTargetPosition + OffsetDirection.ToRotationVector2() * MoveOffset;

            if (AttackTimer == 0f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/MechGaussRifle"), npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 laserDirection = npc.SafeDirectionTo(Main.npc[(int)NextDroneIndex].Center, Vector2.UnitY);
                    int laser = Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<PlagueDeathray>(), 270, 0f);
                    if (Main.projectile.IndexInRange(laser))
                    {
                        Main.projectile[laser].ModProjectile<PlagueDeathray>().LocalLifetime = 1200;
                        Main.projectile[laser].ai[1] = npc.whoAmI;
                    }
                }
            }
            AttackTimer++;

            // Explode if close to the target or enough time has passed.
            if (npc.WithinRange(Target.Center, 40f) || AttackTimer > LaserAttackTime)
            {
                npc.life = 0;
                npc.checkDead();
                npc.active = false;
            }
        }

        public override bool PreNPCLoot() => false;

        public override bool CheckDead()
        {
            Main.PlaySound(SoundID.DD2_KoboldExplosion, npc.position);

            npc.position = npc.Center;
            npc.width = npc.height = 84;
            npc.Center = npc.position;

            for (int i = 0; i < 15; i++)
            {
                Dust plague = Dust.NewDustDirect(npc.position, npc.width, npc.height, 89, 0f, 0f, 100, default, 1.4f);
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
                Dust plague = Dust.NewDustDirect(npc.position, npc.width, npc.height, 89, 0f, 0f, 100, default, 1.85f);
                plague.noGravity = true;
                plague.velocity *= 5f;

                plague = Dust.NewDustDirect(npc.position, npc.width, npc.height, 89, 0f, 0f, 100, default, 2f);
                plague.velocity *= 2f;
                plague.noGravity = true;
            }
            return true;
        }

        public override void FindFrame(int frameHeight)
        {
            npc.frameCounter++;

            if (npc.frameCounter >= 5D)
            {
                npc.frame.Y += frameHeight;
                if (npc.frame.Y >= frameHeight * Main.npcFrameCount[npc.type])
                    npc.frame.Y = 0;

                npc.frameCounter = 0D;
            }
        }
    }
}
