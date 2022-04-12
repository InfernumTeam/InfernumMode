using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

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
            get => (AttackState)(int)Projectile.ai[1];
            set => Projectile.ai[1] = (int)value;
        }
        private ref float AttackTimer => ref Projectile.ai[0];
        private ref float DirectionBias => ref Projectile.localAI[1];
        private Player Target => Main.player[Player.FindClosest(Projectile.Center, 1, 1)];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Attacker Guardian");
            Main.projFrames[Projectile.type] = 6;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 112;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = ProvidenceBehaviorOverride.GuardianApparationTime;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Time);
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Time = reader.ReadSingle();
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
        }

        internal void DecideValuesForNextAI()
        {
            switch (State)
            {
                case AttackState.TopLeftCharge:
                    Projectile.velocity = Projectile.SafeDirectionTo(Target.Center) * 22.5f;
                    Projectile.spriteDirection = (Projectile.velocity.X < 0).ToDirectionInt();
                    if (!Main.dayTime)
                        Projectile.velocity *= 1.4f;
                    break;
                case AttackState.HorizontalCharge:
                    Projectile.rotation = 0f;
                    Projectile.velocity = Vector2.UnitX * -DirectionBias * 28f;
                    if (!Main.dayTime)
                        Projectile.velocity *= 1.4f;

                    Projectile.spriteDirection = (Projectile.velocity.X < 0).ToDirectionInt();

                    // Fire a spear and a lot of fire.
                    if (Main.myPlayer == Projectile.owner && Projectile.alpha == 0)
                    {
                        Vector2 spawnPosition = Projectile.Center - Vector2.UnitX * Projectile.spriteDirection * 40f;
                        Utilities.NewProjectileBetter(spawnPosition, -Vector2.UnitX * Projectile.spriteDirection * 45f, ModContent.ProjectileType<ProfanedSpear2>(), 290, 0f);

                        for (int i = 0; i < 25; i++)
                        {
                            Vector2 shootVelocity = (MathHelper.TwoPi * i / 25f).ToRotationVector2() * 10f;
                            Utilities.NewProjectileBetter(Projectile.Center + shootVelocity * 3f, shootVelocity, ModContent.ProjectileType<HolyFireSpark>(), 270, 0f);
                        }
                    }

                    SoundEngine.PlaySound(SoundID.Item109, Target.Center);
                    break;
                case AttackState.TopLeftRedirect:
                    if (Main.myPlayer == Projectile.owner && Projectile.alpha == 0)
                    {
                        Vector2 baseShootDirection = Projectile.SafeDirectionTo(Target.Center);
                        for (int i = 0; i < 13; i++)
                        {
                            Vector2 spawnPosition = Projectile.Center;
                            Vector2 shootVelocity = baseShootDirection.RotatedBy(MathHelper.TwoPi * i / 13f) * 18f;
                            Utilities.NewProjectileBetter(spawnPosition, shootVelocity, ModContent.ProjectileType<ProfanedSpear2>(), 300, 0f);
                        }
                    }

                    SoundEngine.PlaySound(SoundID.Item109, Target.Center);
                    break;
            }
        }

        public override void AI()
        {
            void updateMyOthersState()
            {
                if (OtherGuardianIndex != -1)
                {
                    int otherGuardianUUID = Projectile.GetByUUID(Projectile.owner, OtherGuardianIndex);
                    if (otherGuardianUUID == -1)
                        return;

                    if (AttackTimer == 0f)
                        Main.projectile[otherGuardianUUID].ai[0] = AttackTimer;
                    Main.projectile[otherGuardianUUID].ai[1] = Projectile.ai[1];
                    (Main.projectile[otherGuardianUUID].ModProjectile as AttackerApparation).DecideValuesForNextAI();
                    Main.projectile[otherGuardianUUID].netUpdate = true;
                }
            }

            if (Time < 90)
                Projectile.alpha = Utils.Clamp(Projectile.alpha - 16, 0, 255);
            if (Projectile.timeLeft < 45)
                Projectile.alpha = Utils.Clamp(Projectile.alpha + 16, 0, 255);

            // Reset the direction bias if it's uninitialized for some reason.
            if (DirectionBias == 0f)
            {
                DirectionBias = Main.rand.NextBool(2).ToDirectionInt();
                Projectile.netUpdate = true;
            }

            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 7 % Main.projFrames[Projectile.type];

            switch (State)
            {
                case AttackState.TopLeftRedirect:
                    ref float yOffset = ref Projectile.localAI[0];
                    if (yOffset == 0f)
                    {
                        yOffset = Math.Abs(Target.velocity.Y) < 0.1f ? -1 : Math.Sign(Target.velocity.Y);
                        yOffset *= 385f;

                        Projectile.netUpdate = true;
                    }

                    Vector2 destination = Target.Center + new Vector2(DirectionBias * 500f, yOffset);
                    Projectile.spriteDirection = (Projectile.velocity.X < 0).ToDirectionInt();
                    Projectile.rotation *= 0.9f;

                    // Redirect towards the destination.
                    if ((Projectile.Distance(destination) >= 38f + Target.velocity.Length() && AttackTimer < 75f) || Time <= 90f)
                    {
                        Projectile.velocity *= 0.3f;
                        Projectile.velocity += (destination - Projectile.Center) * 0.065f;
                    }
                    else
                    {
                        // Charge at the target.
                        State = AttackState.TopLeftCharge;

                        yOffset = 0f;
                        AttackTimer = 0f;
                        DecideValuesForNextAI();
                        Projectile.netUpdate = true;

                        updateMyOthersState();
                    }
                    break;

                case AttackState.TopLeftCharge:
                    // For a small period of time, adjust the direction of the velocity to have it arc towards the player.
                    // The magnitude is reset after directional charges are completed.
                    if (AttackTimer < 45f)
                    {
                        float initialSpeed = Projectile.velocity.Length();
                        Projectile.velocity += Projectile.SafeDirectionTo(Target.Center).RotatedBy((float)Math.Cos(Projectile.timeLeft / 30f) * 0.14f);
                        Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * initialSpeed;
                    }

                    Projectile.rotation = Projectile.velocity.ToRotation();
                    if (Projectile.spriteDirection == 1)
                        Projectile.rotation += MathHelper.Pi;
                    if (AttackTimer >= 75f)
                    {
                        AttackTimer = 0f;
                        State = AttackState.HorizontalRedirect;
                        Projectile.netUpdate = true;

                        updateMyOthersState();
                    }
                    break;

                case AttackState.HorizontalRedirect:
                    ref float xOffset = ref Projectile.localAI[0];
                    if (xOffset == 0f)
                    {
                        xOffset = DirectionBias * 530f;
                        Projectile.netUpdate = true;
                    }

                    destination = Target.Center + Vector2.UnitX * xOffset;
                    Projectile.spriteDirection = (Projectile.velocity.X < 0).ToDirectionInt();
                    Projectile.rotation *= 0.7f;

                    // Redirect towards the destination.
                    if (Projectile.Distance(destination) >= 38f + Target.velocity.Length() && AttackTimer < 75f)
                    {
                        Projectile.velocity *= 0.3f;
                        Projectile.velocity += (destination - Projectile.Center) * 0.14f;
                    }
                    else if (AttackTimer >= 40f)
                    {
                        // Charge at the target.
                        State = AttackState.HorizontalCharge;

                        xOffset = 0f;
                        AttackTimer = 0f;
                        DecideValuesForNextAI();
                        Projectile.netUpdate = true;

                        updateMyOthersState();
                    }
                    break;
                case AttackState.HorizontalCharge:
                    Projectile.velocity.X *= 1.005f;
                    if (!Main.dayTime)
                        Projectile.velocity *= 1.003f;

                    if (AttackTimer >= 45f)
                        Projectile.velocity.X *= 0.9f;

                    if (AttackTimer >= 70f)
                    {
                        State = AttackState.HorizontalRedirect;

                        AttackTimer = 0f;
                        DecideValuesForNextAI();
                        Projectile.netUpdate = true;

                        updateMyOthersState();
                    }
                    break;
            }

            AttackTimer++;
            Time++;
        }

        public override bool? CanDamage() => Projectile.alpha <= 40 && State != AttackState.HorizontalRedirect && State != AttackState.TopLeftRedirect ? null : false;

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            target.Calamity().lastProjectileHit = Projectile;
        }
    }
}
