using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class QueenSlimeCrown : ModProjectile
    {
        public int DefaultDamage;

        public ref float Time => ref Projectile.ai[0];

        public ref float ChargeGlowTelegraphInterpolant => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Queen Slime's Crown");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 82;
            Projectile.height = 56;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90000;
        }

        public override void AI()
        {
            // Disappear if the queen slime is not present.
            int queenSlimeIndex = NPC.FindFirstNPC(NPCID.QueenSlimeBoss);
            if (queenSlimeIndex == -1)
            {
                Projectile.active = false;
                return;
            }

            // Reset damage and the charge glow interpolant.
            ChargeGlowTelegraphInterpolant = 0f;
            if (DefaultDamage == 0)
                DefaultDamage = Projectile.damage;

            Projectile.damage = 0;
            NPC queenSlime = Main.npc[queenSlimeIndex];
            Player target = Main.player[queenSlime.target];

            switch ((QueenSlimeBehaviorOverride.QueenSlimeAttackType)queenSlime.ai[0])
            {
                case QueenSlimeBehaviorOverride.QueenSlimeAttackType.CrownDashes:
                    DoBehavior_CrownDashes(queenSlime, target);
                    break;
                case QueenSlimeBehaviorOverride.QueenSlimeAttackType.CrownLasers:
                    DoBehavior_CrownLasers(queenSlime, target);
                    break;
                default:
                    queenSlime.ai[1] = 1f;
                    Projectile.Kill();
                    break;
            }

            Time++;
        }

        public void DoBehavior_CrownDashes(NPC queenSlime, Player target)
        {
            // Return to the queen slime's head if it should.
            if (queenSlime.Infernum().ExtraAI[1] == 1f)
            {
                Vector2 destination = QueenSlimeBehaviorOverride.CrownPosition(queenSlime);
                Projectile.velocity = Projectile.SafeDirectionTo(destination) * 24f;
                if (Projectile.WithinRange(destination, 28f))
                {
                    queenSlime.ai[2] = 1f;
                    QueenSlimeBehaviorOverride.SelectNextAttack(queenSlime);
                    Projectile.active = false;
                }
                return;
            }

            // Glow before charging.
            ChargeGlowTelegraphInterpolant = Utils.GetLerpValue(100f, 165f, Time, true);
            ChargeGlowTelegraphInterpolant = Utils.GetLerpValue(0f, 0.4f, ChargeGlowTelegraphInterpolant, true) * Utils.GetLerpValue(1f, 0.8f, ChargeGlowTelegraphInterpolant, true);

            // Hover into position near the target.
            if (Time < 150f)
            {
                float flySpeed = MathHelper.Lerp(23f, 48f, Time / 150f);
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < Projectile.Center.X).ToDirectionInt() * 400f, -200f);
                Vector2 idealVelocity = Projectile.SafeDirectionTo(hoverDestination) * flySpeed;

                Projectile.Center = Projectile.Center.MoveTowards(hoverDestination, 3f);
                Projectile.velocity = (Projectile.velocity * 15f + idealVelocity) / 16f;
                Projectile.velocity = Projectile.velocity.MoveTowards(idealVelocity, 1f);

                if (Time >= 45f && Projectile.WithinRange(hoverDestination, 50f))
                {
                    Time = 150f;
                    Projectile.velocity *= 0.6f;
                    Projectile.netUpdate = true;
                }
            }

            // Slow down before charging.
            else if (Time < 165f)
                Projectile.velocity *= 0.95f;

            // Charge.
            else
            {
                float chargeSpeedInterpolant = Utils.GetLerpValue(165f, 172f, Time, true);
                float chargeSpeed = MathHelper.Lerp(8f, 28f, chargeSpeedInterpolant);
                if (QueenSlimeBehaviorOverride.InPhase2(queenSlime))
                    chargeSpeed *= 1.3f;
                if (BossRushEvent.BossRushActive)
                    chargeSpeed *= 1.8f;

                if (chargeSpeedInterpolant < 1f)
                    Projectile.velocity = Projectile.SafeDirectionTo(target.Center) * chargeSpeed;

                if (Time >= 196f)
                {
                    Time = 0f;
                    Projectile.netUpdate = true;
                }
                Projectile.damage = DefaultDamage;
            }
        }

        public void DoBehavior_CrownLasers(NPC queenSlime, Player target)
        {
            // Return to the queen slime's head if it should.
            if (queenSlime.Infernum().ExtraAI[0] == 1f)
            {
                Vector2 destination = QueenSlimeBehaviorOverride.CrownPosition(queenSlime);
                Projectile.velocity = Projectile.SafeDirectionTo(destination) * 24f;
                if (Projectile.WithinRange(destination, 28f))
                {
                    queenSlime.ai[2] = 1f;
                    QueenSlimeBehaviorOverride.SelectNextAttack(queenSlime);
                    Projectile.active = false;
                }
                return;
            }

            Vector2 hoverDestination = target.Center + (MathHelper.TwoPi * Time / 180f).ToRotationVector2() * 680f;
            if (Time % 90f > 75f)
                Projectile.velocity *= 0.925f;
            else
                Projectile.velocity = Vector2.Zero.MoveTowards(hoverDestination - Projectile.Center, 16f);

            if (Time % 90f == 89f)
            {
                SoundEngine.PlaySound(SoundID.Item43, Projectile.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 boltSpawnPosition = Projectile.Center + Vector2.UnitY * 8f;
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 shootVelocity = (target.Center - boltSpawnPosition).SafeNormalize(Vector2.UnitY) * 10f;
                        shootVelocity = shootVelocity.RotatedBy(MathHelper.Lerp(-0.49f, 0.49f, i / 4f));

                        if (BossRushEvent.BossRushActive)
                            shootVelocity *= 1.7f;

                        Utilities.NewProjectileBetter(boltSpawnPosition, shootVelocity, ModContent.ProjectileType<CrownBeam>(), 125, 0f);
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < 10; i++)
            {
                Color backglowColor = new Color(1f, 0.24f, 1f, 0f) * Projectile.Opacity * ChargeGlowTelegraphInterpolant * 0.65f;
                Vector2 drawOffset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * ChargeGlowTelegraphInterpolant * 10f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, backglowColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            Utilities.DrawAfterimagesCentered(Projectile, Color.White, ProjectileID.Sets.TrailingMode[Projectile.type], 1);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.scale * 17f, targetHitbox);
        }
    }
}
