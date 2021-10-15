using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Destroyer
{
    public class SuperchargedProbe : ModNPC
    {
        public enum SuperchargedProbeAttackState
		{
            ChargePreparation,
            Charge
		}

        public Player Target => Main.player[npc.target];
        public SuperchargedProbeAttackState AttackState
		{
            get => (SuperchargedProbeAttackState)(int)npc.ai[0];
            set => npc.ai[0] = (int)value;
        }
        public ref float AttackTimer => ref npc.ai[1];
        public ref float GeneralTimer => ref npc.ai[2];
        public const int Lifetime = 360;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Supercharged Probe");
        }

        public override void SetDefaults()
        {
            npc.damage = 140;
            npc.npcSlots = 0f;
            npc.width = npc.height = 34;
            npc.defense = 15;
            npc.lifeMax = 1113;
            npc.aiStyle = aiType = -1;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.canGhostHeal = false;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath14;
        }

        public override void AI()
        {
            Lighting.AddLight(npc.Center, 0.07f, 0.4f, 0.07f);

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

            // Explode if enough time has passed.
            GeneralTimer++;
            if (GeneralTimer > Lifetime)
            {
                npc.life = 0;
                npc.checkDead();
                npc.active = false;
            }

            switch (AttackState)
			{
                case SuperchargedProbeAttackState.ChargePreparation:
                    DoBehavior_ChargePreparation();
                    break;
                case SuperchargedProbeAttackState.Charge:
                    DoBehavior_Charge();
                    break;
			}

            AttackTimer++;
        }

        public void DoBehavior_ChargePreparation()
        {
            // Negate damage.
            npc.damage = 0;

            ref float horizontalHoverOffset = ref npc.Infernum().ExtraAI[0];

            // Hover near the target.
            if (horizontalHoverOffset == 0f)
                horizontalHoverOffset = Math.Sign(Target.Center.X - npc.Center.X) * 500f;
            Vector2 hoverDestination = Target.Center + new Vector2(horizontalHoverOffset, -350f) + (npc.whoAmI * MathHelper.TwoPi / 4f).ToRotationVector2() * 100f - npc.velocity;
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * 22f, 1.05f);

            // Look at the target.
            npc.spriteDirection = (Target.Center.X < npc.Center.X).ToDirectionInt();
            npc.rotation = npc.AngleTo(Target.Center + Target.velocity * 30f);

            if (npc.spriteDirection == 1)
                npc.rotation += MathHelper.Pi;

            if (AttackTimer > 50f)
			{
                horizontalHoverOffset = 0f;
                AttackState = SuperchargedProbeAttackState.Charge;
                AttackTimer = 0f;
            }
        }

        public void DoBehavior_Charge()
        {
            // Do the charge on the first frame.
            if (AttackTimer == 1f)
            {
                int chargeDirection = (Target.Center.X < npc.Center.X).ToDirectionInt();
                float chargeSpeed = 18f;
                npc.velocity = npc.SafeDirectionTo(Target.Center + Target.velocity * 35f) * chargeSpeed;
                npc.spriteDirection = chargeDirection;

                npc.rotation = npc.velocity.ToRotation();
                if (npc.spriteDirection == 1)
                    npc.rotation += MathHelper.Pi;
                npc.netUpdate = true;

                return;
            }

            // Otherwise accelerate and emit laser dust.
            npc.velocity *= 1.01f;

            // Spawn laser dust.
            int dustCount = 3;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 dustSpawnPosition = npc.Center + (Vector2.Normalize(npc.velocity) * new Vector2((npc.width + 10) / 2f, npc.height) * 0.3f).RotatedBy(MathHelper.TwoPi * i / dustCount);
                Vector2 dustVelocity = (Main.rand.NextFloatDirection() * MathHelper.PiOver2).ToRotationVector2() * Main.rand.NextFloat(3f, 8f);
                Dust laser = Dust.NewDustPerfect(dustSpawnPosition + dustVelocity, 245, dustVelocity);
                laser.scale *= 1.2f;
                laser.velocity *= 0.25f;
                laser.velocity -= npc.velocity;
                laser.color = Color.Green;
                laser.noGravity = true;
            }

            if (AttackTimer >= 45f)
            {
                AttackState = SuperchargedProbeAttackState.ChargePreparation;
                AttackTimer = 0f;
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
                Dust laserDust = Dust.NewDustDirect(npc.position, npc.width, npc.height, 245, 0f, 0f, 100, default, 1.4f);
                if (Main.rand.NextBool(2))
                {
                    laserDust.scale = 0.5f;
                    laserDust.fadeIn = Main.rand.NextFloat(1f, 2f);
                }
                laserDust.velocity *= 3f;
                laserDust.noGravity = true;
            }

            for (int i = 0; i < 30; i++)
            {
                Dust laserDust = Dust.NewDustDirect(npc.position, npc.width, npc.height, 245, 0f, 0f, 100, default, 1.85f);
                laserDust.noGravity = true;
                laserDust.velocity *= 5f;

                laserDust = Dust.NewDustDirect(npc.position, npc.width, npc.height, 245, 0f, 0f, 100, default, 2f);
                laserDust.velocity *= 2f;
                laserDust.noGravity = true;
            }
            return true;
        }
    }
}
