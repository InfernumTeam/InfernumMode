using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.QueenSlime
{
    public class QueenSlimeCrown : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Queen Slime's Crown");
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            projectile.width = 82;
            projectile.height = 56;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 90000;
        }

        public override void AI()
        {
            // Disappear if the queen slime is not present.
            int queenSlimeIndex = NPC.FindFirstNPC(ModContent.NPCType<QueenSlimeNPC>());
            if (queenSlimeIndex == -1)
            {
                projectile.active = false;
                return;
            }

            if (projectile.Calamity().defDamage == 0)
                projectile.Calamity().defDamage = projectile.damage;

            projectile.damage = 0;
            NPC queenSlime = Main.npc[queenSlimeIndex];
            Player target = Main.player[queenSlime.target];

            switch ((QueenSlimeNPC.QueenSlimeAttackType)queenSlime.ai[0])
            {
                case QueenSlimeNPC.QueenSlimeAttackType.CrownDashes:
                    DoBehavior_CrownDashes(queenSlime, target);
                    break;
                case QueenSlimeNPC.QueenSlimeAttackType.CrownLasers:
                    DoBehavior_CrownLasers(queenSlime, target);
                    break;
                default:
                    queenSlime.ModNPC<QueenSlimeNPC>().CrownIsAttached = true;
                    projectile.Kill();
                    break;
            }

            Time++;
        }

        public void DoBehavior_CrownDashes(NPC queenSlime, Player target)
        {
            // Return to the queen slime's head if it should.
            if (queenSlime.Infernum().ExtraAI[1] == 1f)
            {
                Vector2 destination = queenSlime.ModNPC<QueenSlimeNPC>().CrownPosition;
                projectile.velocity = projectile.SafeDirectionTo(destination) * 24f;
                if (projectile.WithinRange(destination, 28f))
                {
                    queenSlime.ModNPC<QueenSlimeNPC>().CrownIsAttached = true;
                    queenSlime.ModNPC<QueenSlimeNPC>().SelectNextAttack();
                    projectile.active = false;
                }
                return;
            }

            // Hover into position near the target.
            if (Time < 150f)
            {
                float flySpeed = MathHelper.Lerp(23f, 48f, Time / 150f);
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < projectile.Center.X).ToDirectionInt() * 400f, -200f);
                Vector2 idealVelocity = projectile.SafeDirectionTo(hoverDestination) * flySpeed;

                projectile.Center = projectile.Center.MoveTowards(hoverDestination, 3f);
                projectile.velocity = (projectile.velocity * 15f + idealVelocity) / 16f;
                projectile.velocity = projectile.velocity.MoveTowards(idealVelocity, 1f);

                if (Time >= 45f && projectile.WithinRange(hoverDestination, 50f))
                {
                    Time = 150f;
                    projectile.velocity *= 0.6f;
                    projectile.netUpdate = true;
                }
            }

            // Slow down before charging.
            else if (Time < 165f)
                projectile.velocity *= 0.95f;

            // Charge.
            else
            {
                float chargeSpeedInterpolant = Utils.GetLerpValue(165f, 172f, Time, true);
                float chargeSpeed = MathHelper.Lerp(8f, 28f, chargeSpeedInterpolant);
                if (queenSlime.ModNPC<QueenSlimeNPC>().InPhase2)
                    chargeSpeed *= 1.15f;

                if (chargeSpeedInterpolant < 1f)
                    projectile.velocity = projectile.SafeDirectionTo(target.Center) * chargeSpeed;

                if (Time >= 196f)
                {
                    Time = 0f;
                    projectile.netUpdate = true;
                }
                projectile.damage = projectile.Calamity().defDamage;
            }
        }

        public void DoBehavior_CrownLasers(NPC queenSlime, Player target)
        {
            // Return to the queen slime's head if it should.
            if (queenSlime.Infernum().ExtraAI[0] == 1f)
            {
                Vector2 destination = queenSlime.ModNPC<QueenSlimeNPC>().CrownPosition;
                projectile.velocity = projectile.SafeDirectionTo(destination) * 24f;
                if (projectile.WithinRange(destination, 28f))
                {
                    queenSlime.ModNPC<QueenSlimeNPC>().CrownIsAttached = true;
                    queenSlime.ModNPC<QueenSlimeNPC>().SelectNextAttack();
                    projectile.active = false;
                }
                return;
            }

            Vector2 hoverDestination = target.Center + (MathHelper.TwoPi * Time / 180f).ToRotationVector2() * 550f;
            if (Time % 90f > 75f)
                projectile.velocity *= 0.925f;
            else
                projectile.velocity = Vector2.Zero.MoveTowards(hoverDestination - projectile.Center, 16f);

            if (Time % 90f == 89f)
            {
                SoundEngine.PlaySound(SoundID.Item43, projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 boltSpawnPosition = projectile.Center + Vector2.UnitY * 8f;
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 shootVelocity = (target.Center - boltSpawnPosition).SafeNormalize(Vector2.UnitY) * 10f;
                        shootVelocity = shootVelocity.RotatedBy(MathHelper.Lerp(-0.49f, 0.49f, i / 4f));
                        Utilities.NewProjectileBetter(boltSpawnPosition, shootVelocity, ModContent.ProjectileType<CrownBeam>(), 125, 0f);
                    }
                }
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Utilities.DrawAfterimagesCentered(projectile, Color.White, ProjectileID.Sets.TrailingMode[projectile.type], 1);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(projectile.Center, projectile.scale * 17f, targetHitbox);
        }
    }
}
