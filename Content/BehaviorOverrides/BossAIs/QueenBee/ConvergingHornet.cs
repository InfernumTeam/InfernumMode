using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenBee
{
    public class ConvergingHornet : ModProjectile
    {
        public enum HornetAttackState
        {
            MoveTowardsQueen,
            HoverAroundQueen,
            FlyOutward
        }

        public HornetAttackState CurrentAttackState
        {
            get => (HornetAttackState)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        public ref float Time => ref Projectile.ai[1];

        public static NPC QueenBee
        {
            get
            {
                int queenIndex = NPC.FindFirstNPC(NPCID.QueenBee);
                if (queenIndex != -1)
                    return Main.npc[queenIndex];

                return null;
            }
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Hornet");
            Main.projFrames[Projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
            Projectile.scale = 1f;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 600;
            Projectile.Opacity = 0f;
            Projectile.Calamity().DealsDefenseDamage = true;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            // Disappear if the queen bee is not present.
            if (QueenBee is null)
                return;

            Projectile.rotation = Clamp(Projectile.velocity.X * 0.15f, -0.7f, 0.7f);
            Projectile.spriteDirection = (Projectile.velocity.X < 0f).ToDirectionInt();
            Projectile.frame = Projectile.timeLeft / 4 % Main.projFrames[Projectile.type];
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.03f, 0f, 1f);

            if (Projectile.localAI[0] == 0f)
            {
                SoundEngine.PlaySound(SoundID.Item17, Projectile.Center);
                Projectile.localAI[0] = 1f;
            }

            switch (CurrentAttackState)
            {
                case HornetAttackState.MoveTowardsQueen:
                    DoBehavior_MoveTowardsQueen();
                    break;
                case HornetAttackState.HoverAroundQueen:
                    DoBehavior_HoverAroundQueen();
                    break;
                case HornetAttackState.FlyOutward:
                    DoBehavior_FlyOutward();
                    break;
            }

            Time++;
        }

        public void DoBehavior_MoveTowardsQueen()
        {
            // Move with perfect homing towards the queen bee.
            Projectile.velocity = Projectile.SafeDirectionTo(QueenBee.Center) * 9.6f;

            if (Projectile.WithinRange(QueenBee.Center, 150f))
            {
                CurrentAttackState = HornetAttackState.HoverAroundQueen;
                Time = 0f;
                Projectile.netUpdate = true;
            }
        }

        public void DoBehavior_HoverAroundQueen()
        {
            float hoverOffset = Lerp(120f, 200f, Projectile.identity / 9f % 1f);
            Vector2 hoverDestination = QueenBee.Center + (TwoPi * Projectile.identity / 11f + Time / 50f).ToRotationVector2() * hoverOffset;
            Vector2 idealVelocity = Projectile.SafeDirectionTo(hoverDestination) * 9f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, idealVelocity, 0.1f);
            Projectile.spriteDirection = (QueenBee.Center.X > Projectile.Center.X).ToDirectionInt();
        }

        public void DoBehavior_FlyOutward()
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(QueenBee.Center) * -18f, 0.09f);
            Projectile.spriteDirection = (QueenBee.Center.X > Projectile.Center.X).ToDirectionInt();
        }

        public static void MakeAllBeesFlyOutward()
        {
            foreach (Projectile bee in Utilities.AllProjectilesByID(ModContent.ProjectileType<ConvergingHornet>()))
            {
                bee.ModProjectile<ConvergingHornet>().CurrentAttackState = HornetAttackState.FlyOutward;
                bee.netUpdate = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Projectile.DrawProjectileWithBackglowTemp(Color.White with { A = 0 } * Pow(Projectile.Opacity, 2f), lightColor, Projectile.Opacity * 6f);
            return false;
        }

        public override bool? CanDamage() => CurrentAttackState != HornetAttackState.HoverAroundQueen;
    }
}
