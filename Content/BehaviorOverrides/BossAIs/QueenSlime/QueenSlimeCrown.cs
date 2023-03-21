using CalamityMod.Events;
using CalamityMod;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using InfernumMode.Assets.ExtraTextures;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.QueenSlime
{
    public class QueenSlimeCrown : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public ref float ChargeGlowTelegraphInterpolant => ref Projectile.localAI[0];

        public override string Texture => InfernumTextureRegistry.InvisPath;

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
            CooldownSlot = ImmunityCooldownID.Bosses;
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

            // Reset the glow interpolant.
            ChargeGlowTelegraphInterpolant = 0f;

            Projectile.damage = 0;
            NPC queenSlime = Main.npc[queenSlimeIndex];
            Player target = Main.player[queenSlime.target];

            switch ((QueenSlimeBehaviorOverride.QueenSlimeAttackType)queenSlime.ai[0])
            {
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

        public void DoBehavior_CrownLasers(NPC queenSlime, Player target)
        {
            // Return to the queen slime's head if it should.
            if (queenSlime.Infernum().ExtraAI[0] == 1f)
            {
                Vector2 destination = QueenSlimeBehaviorOverride.CrownPosition(queenSlime);
                Projectile.velocity = Projectile.SafeDirectionTo(destination) * 24f;
                if (Projectile.WithinRange(destination, 28f))
                {
                    queenSlime.ai[3] = 1f;
                    queenSlime.rotation = 0f;
                    QueenSlimeBehaviorOverride.SelectNextAttack(queenSlime);
                    Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<QueenJewelBeam>(), ModContent.ProjectileType<FallingGel>());
                    Projectile.active = false;
                }
                return;
            }

            int attackCycleTime = (int)queenSlime.Infernum().ExtraAI[2];
            float wrappedAttackTimer = Time % attackCycleTime;
            Vector2 hoverDestination = target.Center + (MathHelper.TwoPi * Time / 180f).ToRotationVector2() * 680f;
            if (wrappedAttackTimer > 75f)
                Projectile.velocity *= 0.925f;
            else
                Projectile.velocity = Vector2.Zero.MoveTowards(hoverDestination - Projectile.Center, 28f);

            ChargeGlowTelegraphInterpolant = Utils.GetLerpValue(attackCycleTime - 25f, attackCycleTime - 5f, wrappedAttackTimer, true);

            // Periodically release lasers.
            if (wrappedAttackTimer == attackCycleTime - 1f)
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

                        Utilities.NewProjectileBetter(boltSpawnPosition, shootVelocity, ModContent.ProjectileType<QueenJewelBeam>(), 140, 0f);
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Extra[177].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = 0; i < 10; i++)
            {
                Color backglowColor = new Color(1f, 0.24f, 1f, 0f) * Projectile.Opacity * ChargeGlowTelegraphInterpolant * 0.65f;
                Vector2 drawOffset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * ChargeGlowTelegraphInterpolant * 10f;
                Main.spriteBatch.Draw(texture, drawPosition + drawOffset, null, backglowColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            }
            Utilities.DrawAfterimagesCentered(Projectile, Color.White, ProjectileID.Sets.TrailingMode[Projectile.type], 1, texture);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.scale * 17f, targetHitbox);
        }
    }
}