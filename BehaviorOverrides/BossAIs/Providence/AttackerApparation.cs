using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Providence
{
    public class AttackerApparation : ModProjectile
    {
        private enum AttackState
        {
            TopLeftRedirect,
            TopLeftCharge,
            HorizontalRedirect,
            HorizontalCharge,
            FireBurst
        }

        internal float Time = 0f;
        internal int OtherGuardianIndex = -1;
        private AttackState State
        {
            get => (AttackState)(int)projectile.ai[1];
            set => projectile.ai[1] = (int)value;
        }
        private ref float AttackTimer => ref projectile.ai[0];
        private ref float DirectionBias => ref projectile.localAI[1];
        private Player Target => Main.player[Player.FindClosest(projectile.Center, 1, 1)];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Attacker Guardian");
            Main.projFrames[projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 112;
            projectile.hostile = true;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.timeLeft = ProvidenceBehaviorOverride.GuardianApparationTime;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Time);
            writer.Write(projectile.localAI[0]);
            writer.Write(projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Time = reader.ReadSingle();
            projectile.localAI[0] = reader.ReadSingle();
            projectile.localAI[1] = reader.ReadSingle();
        }

        internal void DecideValuesForNextAI()
        {
            switch (State)
            {
                case AttackState.TopLeftCharge:
                    projectile.velocity = projectile.SafeDirectionTo(Target.Center) * 22.5f;
                    projectile.spriteDirection = (projectile.velocity.X < 0).ToDirectionInt();
                    if (!Main.dayTime)
                        projectile.velocity *= 1.4f;
                    break;
                case AttackState.HorizontalCharge:
                    projectile.rotation = 0f;
                    projectile.velocity = Vector2.UnitX * -DirectionBias * 28f;
                    if (!Main.dayTime)
                        projectile.velocity *= 1.4f;

                    projectile.spriteDirection = (projectile.velocity.X < 0).ToDirectionInt();

                    // Fire a spear and a lot of fire.
                    if (Main.myPlayer == projectile.owner && projectile.alpha == 0)
                    {
                        Vector2 spawnPosition = projectile.Center - Vector2.UnitX * projectile.spriteDirection * 40f;
                        Utilities.NewProjectileBetter(spawnPosition, -Vector2.UnitX * projectile.spriteDirection * 45f, ModContent.ProjectileType<ProfanedSpear2>(), 290, 0f);

                        for (int i = 0; i < 25; i++)
                        {
                            Vector2 shootVelocity = (MathHelper.TwoPi * i / 25f).ToRotationVector2() * 10f;
                            Utilities.NewProjectileBetter(projectile.Center + shootVelocity * 3f, shootVelocity, ModContent.ProjectileType<HolyFireSpark>(), 270, 0f);
                        }
                    }

                    Main.PlaySound(SoundID.Item109, Target.Center);
                    break;
                case AttackState.TopLeftRedirect:
                    if (Main.myPlayer == projectile.owner && projectile.alpha == 0)
                    {
                        Vector2 baseShootDirection = projectile.SafeDirectionTo(Target.Center);
                        for (int i = 0; i < 13; i++)
                        {
                            Vector2 spawnPosition = projectile.Center;
                            Vector2 shootVelocity = baseShootDirection.RotatedBy(MathHelper.TwoPi * i / 13f) * 18f;
                            Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<ProfanedSpear2>(), 300, 0f);
                        }
                    }

                    Main.PlaySound(SoundID.Item109, Target.Center);
                    break;
            }
        }

        public override void AI()
        {
            void updateMyOthersState()
            {
                if (OtherGuardianIndex != -1)
                {
                    int otherGuardianUUID = Projectile.GetByUUID(projectile.owner, OtherGuardianIndex);
                    if (otherGuardianUUID == -1)
                        return;

                    if (AttackTimer == 0f)
                        Main.projectile[otherGuardianUUID].ai[0] = AttackTimer;
                    Main.projectile[otherGuardianUUID].ai[1] = projectile.ai[1];
                    (Main.projectile[otherGuardianUUID].modProjectile as AttackerApparation).DecideValuesForNextAI();
                    Main.projectile[otherGuardianUUID].netUpdate = true;
                }
            }

            if (Time < 90)
                projectile.alpha = Utils.Clamp(projectile.alpha - 16, 0, 255);
            if (projectile.timeLeft < 45)
                projectile.alpha = Utils.Clamp(projectile.alpha + 16, 0, 255);

            // Reset the direction bias if it's uninitialized for some reason.
            if (DirectionBias == 0f)
            {
                DirectionBias = Main.rand.NextBool(2).ToDirectionInt();
                projectile.netUpdate = true;
            }

            projectile.frameCounter++;
            projectile.frame = projectile.frameCounter / 7 % Main.projFrames[projectile.type];

            switch (State)
            {
                case AttackState.TopLeftRedirect:
                    ref float yOffset = ref projectile.localAI[0];
                    if (yOffset == 0f)
                    {
                        yOffset = Math.Abs(Target.velocity.Y) < 0.1f ? -1 : Math.Sign(Target.velocity.Y);
                        yOffset *= 385f;

                        projectile.netUpdate = true;
                    }

                    Vector2 destination = Target.Center + new Vector2(DirectionBias * 500f, yOffset);
                    projectile.spriteDirection = (projectile.velocity.X < 0).ToDirectionInt();
                    projectile.rotation *= 0.9f;

                    // Redirect towards the destination.
                    if ((projectile.Distance(destination) >= 38f + Target.velocity.Length() && AttackTimer < 75f) || Time <= 90f)
                    {
                        projectile.velocity *= 0.3f;
                        projectile.velocity += (destination - projectile.Center) * 0.065f;
                    }
                    else
                    {
                        // Charge at the target.
                        State = AttackState.TopLeftCharge;

                        yOffset = 0f;
                        AttackTimer = 0f;
                        DecideValuesForNextAI();
                        projectile.netUpdate = true;

                        updateMyOthersState();
                    }
                    break;

                case AttackState.TopLeftCharge:
                    // For a small period of time, adjust the direction of the velocity to have it arc towards the player.
                    // The magnitude is reset after directional charges are completed.
                    if (AttackTimer < 45f)
                    {
                        float initialSpeed = projectile.velocity.Length();
                        projectile.velocity += projectile.SafeDirectionTo(Target.Center).RotatedBy((float)Math.Cos(projectile.timeLeft / 30f) * 0.14f);
                        projectile.velocity = projectile.velocity.SafeNormalize(Vector2.UnitY) * initialSpeed;
                    }

                    projectile.rotation = projectile.velocity.ToRotation();
                    if (projectile.spriteDirection == 1)
                        projectile.rotation += MathHelper.Pi;
                    if (AttackTimer >= 75f)
                    {
                        AttackTimer = 0f;
                        State = AttackState.HorizontalRedirect;
                        projectile.netUpdate = true;

                        updateMyOthersState();
                    }
                    break;

                case AttackState.HorizontalRedirect:
                    ref float xOffset = ref projectile.localAI[0];
                    if (xOffset == 0f)
                    {
                        xOffset = DirectionBias * 530f;
                        projectile.netUpdate = true;
                    }

                    destination = Target.Center + Vector2.UnitX * xOffset;
                    projectile.spriteDirection = (projectile.velocity.X < 0).ToDirectionInt();
                    projectile.rotation *= 0.7f;

                    // Redirect towards the destination.
                    if (projectile.Distance(destination) >= 38f + Target.velocity.Length() && AttackTimer < 75f)
                    {
                        projectile.velocity *= 0.3f;
                        projectile.velocity += (destination - projectile.Center) * 0.14f;
                    }
                    else if (AttackTimer >= 40f)
                    {
                        // Charge at the target.
                        State = AttackState.HorizontalCharge;

                        xOffset = 0f;
                        AttackTimer = 0f;
                        DecideValuesForNextAI();
                        projectile.netUpdate = true;

                        updateMyOthersState();
                    }
                    break;
                case AttackState.HorizontalCharge:
                    projectile.velocity.X *= 1.005f;
                    if (!Main.dayTime)
                        projectile.velocity *= 1.003f;

                    if (AttackTimer >= 45f)
                        projectile.velocity.X *= 0.9f;

                    if (AttackTimer >= 70f)
                    {
                        State = AttackState.HorizontalRedirect;

                        AttackTimer = 0f;
                        DecideValuesForNextAI();
                        projectile.netUpdate = true;

                        updateMyOthersState();
                    }
                    break;
            }

            AttackTimer++;
            Time++;
        }

        public override bool CanDamage() => projectile.alpha <= 40 && State != AttackState.HorizontalRedirect && State != AttackState.TopLeftRedirect;

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = projectile;
        }
    }
}
