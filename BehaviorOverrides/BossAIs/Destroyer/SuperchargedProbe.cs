using CalamityMod;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.Destroyer
{
    public class SuperchargedProbe : ModNPC
    {
        public enum SuperchargedProbeAttackState
        {
            ChargePreparation,
            Charge
        }

        public Player Target => Main.player[NPC.target];
        public SuperchargedProbeAttackState AttackState
        {
            get => (SuperchargedProbeAttackState)(int)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }
        public ref float AttackTimer => ref NPC.ai[1];
        public ref float GeneralTimer => ref NPC.ai[2];
        public bool SoundCreator => NPC.ai[3] == 1f;
        public const int Lifetime = 480;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Supercharged Probe");
        }

        public override void SetDefaults()
        {
            NPC.damage = 120;
            NPC.npcSlots = 0f;
            NPC.width = NPC.height = 34;
            NPC.defense = 15;
            NPC.lifeMax = 1113;
            NPC.aiStyle = aiType = -1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.canGhostHeal = false;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
        }

        public override void AI()
        {
            Lighting.AddLight(NPC.Center, 0.07f, 0.4f, 0.07f);

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

            // Explode if enough time has passed.
            GeneralTimer++;

            // Have a brief moment of no damage.
            NPC.damage = GeneralTimer > 60f ? NPC.defDamage : 0;

            if (GeneralTimer > Lifetime)
            {
                NPC.life = 0;
                NPC.checkDead();
                NPC.active = false;
            }

            NPC.Calamity().canBreakPlayerDefense = false;
            switch (AttackState)
            {
                case SuperchargedProbeAttackState.ChargePreparation:
                    DoBehavior_ChargePreparation();
                    break;
                case SuperchargedProbeAttackState.Charge:
                    DoBehavior_Charge();
                    NPC.Calamity().canBreakPlayerDefense = true;
                    break;
            }

            AttackTimer++;
        }

        public void DoBehavior_ChargePreparation()
        {
            // Negate damage.
            NPC.damage = 0;

            ref float horizontalHoverOffset = ref NPC.Infernum().ExtraAI[0];

            // Hover near the target.
            if (horizontalHoverOffset == 0f)
                horizontalHoverOffset = Math.Sign(Target.Center.X - NPC.Center.X) * 500f;
            Vector2 hoverDestination = Target.Center + new Vector2(horizontalHoverOffset, -350f) + (NPC.whoAmI * MathHelper.TwoPi / 4f).ToRotationVector2() * 100f - NPC.velocity;
            NPC.SimpleFlyMovement(NPC.SafeDirectionTo(hoverDestination) * 22f, 1.05f);

            // Look at the target.
            NPC.spriteDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();
            NPC.rotation = NPC.AngleTo(Target.Center + Target.velocity * 30f);

            if (NPC.spriteDirection == 1)
                NPC.rotation += MathHelper.Pi;

            if (AttackTimer > 40f)
            {
                horizontalHoverOffset = 0f;
                AttackState = SuperchargedProbeAttackState.Charge;
                AttackTimer = 0f;
            }
        }

        public void DoBehavior_Charge()
        {
            NPC.damage = NPC.defDamage;

            // Do the charge on the first frame.
            if (AttackTimer == 1f)
            {
                if (SoundCreator)
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/ELRFire"), NPC.Center);

                int chargeDirection = (Target.Center.X < NPC.Center.X).ToDirectionInt();
                float chargeSpeed = 16.5f;
                NPC.velocity = NPC.SafeDirectionTo(Target.Center + Target.velocity * 35f) * chargeSpeed;
                NPC.spriteDirection = chargeDirection;

                NPC.rotation = NPC.velocity.ToRotation();
                if (NPC.spriteDirection == 1)
                    NPC.rotation += MathHelper.Pi;
                NPC.netUpdate = true;

                return;
            }

            // Otherwise accelerate and emit laser dust.
            NPC.velocity *= 1.01f;

            // Spawn laser dust.
            int dustCount = 3;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 dustSpawnPosition = NPC.Center + (Vector2.Normalize(NPC.velocity) * new Vector2((NPC.width + 10) / 2f, NPC.height) * 0.3f).RotatedBy(MathHelper.TwoPi * i / dustCount);
                Vector2 dustVelocity = (Main.rand.NextFloatDirection() * MathHelper.PiOver2).ToRotationVector2() * Main.rand.NextFloat(3f, 8f);
                Dust laser = Dust.NewDustPerfect(dustSpawnPosition + dustVelocity, 245, dustVelocity);
                laser.scale *= 1.2f;
                laser.velocity *= 0.25f;
                laser.velocity -= NPC.velocity;
                laser.color = Color.Green;
                laser.noGravity = true;
            }

            if (AttackTimer >= 40f)
            {
                AttackState = SuperchargedProbeAttackState.ChargePreparation;
                AttackTimer = 0f;
            }
        }

        public override bool PreNPCLoot() => false;

        public override bool CheckDead()
        {
            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, NPC.position);

            NPC.position = NPC.Center;
            NPC.width = NPC.height = 84;
            NPC.Center = NPC.position;

            for (int i = 0; i < 15; i++)
            {
                Dust laserDust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 245, 0f, 0f, 100, default, 1.4f);
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
                Dust laserDust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 245, 0f, 0f, 100, default, 1.85f);
                laserDust.noGravity = true;
                laserDust.velocity *= 5f;

                laserDust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 245, 0f, 0f, 100, default, 2f);
                laserDust.velocity *= 2f;
                laserDust.noGravity = true;
            }
            return true;
        }
    }
}
