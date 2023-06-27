using CalamityMod;
using CalamityMod.Particles;
using InfernumMode.Common.Graphics.Particles;
using InfernumMode.Content.Items.Weapons.Rogue;
using InfernumMode.Core.GlobalInstances.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Rogue
{
    public class StormMaidensRetributionProj : ModProjectile
    {
        public enum BehaviorState
        {
            Aim,
            Fire
        }

        public bool CreatedByStealthStrike
        {
            get;
            set;
        }

        public BehaviorState CurrentState
        {
            get => (BehaviorState)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        public bool StealthStrikeEffects => Projectile.Calamity().stealthStrike || CreatedByStealthStrike;

        public Player Owner => Main.player[Projectile.owner];

        public Vector2 TipOfSpear => Projectile.Center + Projectile.velocity * Projectile.width * 0.45f;

        public ref float Time => ref Projectile.ai[1];

        public ref float PinkLightningFormInterpolant => ref Projectile.localAI[0];

        public override string Texture => "InfernumMode/Content/Items/Weapons/Rogue/StormMaidensRetribution";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Storm Maiden's Retribution");
            Main.projFrames[Type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = 144;
            Projectile.height = 144;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 1f;
            Projectile.timeLeft = 14400;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 7;
            Projectile.DamageType = RogueDamageClass.Instance;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(CreatedByStealthStrike);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            CreatedByStealthStrike = reader.ReadBoolean();
        }

        public override void AI()
        {
            switch (CurrentState)
            {
                case BehaviorState.Aim:
                    DoBehavior_Aim();
                    break;
                case BehaviorState.Fire:
                    DoBehavior_Fire();
                    break;
            }

            Time++;
        }

        public void DoBehavior_Aim()
        {
            int shootDelay = 54;
            float animationCompletion = Utils.GetLerpValue(0f, shootDelay, Time, true);
            float unsharpenedLightningInterpolant = Sin(Pi * animationCompletion * 2.5f);
            PinkLightningFormInterpolant = Pow(unsharpenedLightningInterpolant, 6f);

            // Play lightning crackle sounds and release sparks when the spear has reached a peak energy state.
            // This doesn't happen once it's fully charged.
            if (animationCompletion < 0.99f && PinkLightningFormInterpolant >= 0.97f)
                CreateCrackleWithSparks(true);

            // Play a magic sound when the spear is ready to fire.
            if (Time == shootDelay)
                SoundEngine.PlaySound(SoundID.Item29, Owner.Center);

            // Aim the spear.
            if (Main.myPlayer == Projectile.owner)
            {
                float aimInterpolant = Utils.GetLerpValue(5f, 25f, Owner.Distance(Main.MouseWorld), true);
                Vector2 oldVelocity = Projectile.velocity;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Owner.SafeDirectionTo(Main.MouseWorld), aimInterpolant);
                if (Projectile.velocity != oldVelocity)
                {
                    Projectile.netSpam = 0;
                    Projectile.netUpdate = true;
                }
            }

            // Stick to the player.
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
            Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());
            float frontArmRotation = Projectile.rotation - PiOver4 - animationCompletion * Owner.direction * 0.74f;
            if (Owner.direction == 1)
                frontArmRotation += Pi;

            Projectile.Center = Owner.Center + (frontArmRotation + PiOver2).ToRotationVector2() * Projectile.scale * 16f + Projectile.velocity * Projectile.scale * 40f;
            Owner.heldProj = Projectile.whoAmI;
            Owner.SetDummyItemTime(2);

            // Perform directioning.
            Projectile.spriteDirection = Owner.direction;
            if (Owner.direction == -1)
                Projectile.rotation += PiOver2;

            // Destroy the spear if the owner can no longer hold it.
            Item heldItem = Owner.ActiveItem();
            if (Owner.dead || !Owner.active || Owner.noItems || Owner.CCed || heldItem is null)
            {
                Projectile.Kill();
                return;
            }

            // Update the player's arm directions to make it look as though they're holding the spear.
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);

            // Check if the spear can be fired if the player has stopped channeling.
            // If it can, fire it. Otherwise, destroy the speaar.
            if (!Owner.channel)
            {
                Owner.SetCompositeArmFront(false, Player.CompositeArmStretchAmount.Full, 0f);
                if (Time >= shootDelay)
                {
                    SoundEngine.PlaySound(SoundID.Item73, Projectile.Center);
                    CreateCrackleWithSparks(false);
                    CurrentState = BehaviorState.Fire;
                    Time = 0f;
                    Projectile.netUpdate = true;

                    ShootLightningInDirection(TipOfSpear, Projectile.velocity.RotatedBy(-0.03f));
                    ShootLightningInDirection(TipOfSpear, Projectile.velocity.RotatedBy(0.03f));
                    Projectile.velocity *= heldItem.shootSpeed;

                    // Release side spears if this is a stealth strike.
                    if (Main.myPlayer == Projectile.owner && Projectile.Calamity().stealthStrike)
                    {
                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(spear =>
                        {
                            spear.ModProjectile<StormMaidensRetributionProj>().CreatedByStealthStrike = true;
                        });
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.RotatedBy(-0.09f), Projectile.type, Projectile.damage, Projectile.knockBack, Projectile.owner, (int)BehaviorState.Fire);

                        ProjectileSpawnManagementSystem.PrepareProjectileForSpawning(spear =>
                        {
                            spear.ModProjectile<StormMaidensRetributionProj>().CreatedByStealthStrike = true;
                        });
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.RotatedBy(0.09f), Projectile.type, Projectile.damage, Projectile.knockBack, Projectile.owner, (int)BehaviorState.Fire);
                    }

                    return;
                }

                Projectile.Kill();
            }

            // Update frames.
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 7 % Main.projFrames[Type];
        }

        public void DoBehavior_Fire()
        {
            Projectile.frame = 0;
            if (Projectile.timeLeft > 360)
                Projectile.timeLeft = 360;

            // Home in on targets if a stealth strike.
            NPC target = Projectile.Center.ClosestNPCAt(StormMaidensRetribution.StealthStrikeTargetingDistance, false);
            if (StealthStrikeEffects && target is not null)
                Projectile.velocity = Projectile.velocity.RotateTowards(Projectile.AngleTo(target.Center), 0.2f);

            // Allow single-enemy and tile interactions if a stealth strike.
            if (StealthStrikeEffects)
            {
                Projectile.tileCollide = Projectile.timeLeft < 300;
                Projectile.penetrate = 1;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
            Projectile.spriteDirection = 1;
        }

        public void ShootLightningInDirection(Vector2 lightningSpawnPosition, Vector2 direction)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            float aimDirection = direction.ToRotation();
            Vector2 lightningShootVelocity = direction * Main.rand.NextFloat(25f, 34f);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), lightningSpawnPosition, lightningShootVelocity, ModContent.ProjectileType<StormMaidensLightning>(), Projectile.damage / 2, 0f, Projectile.owner, aimDirection, Main.rand.Next(100));
        }

        public void CreateCrackleWithSparks(bool playZapSound)
        {
            if (playZapSound)
                SoundEngine.PlaySound(SoundID.DD2_LightningBugZap, TipOfSpear);
            for (int i = 0; i < 8; i++)
            {
                Vector2 sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 11f);
                Color sparkColor = Color.Lerp(Color.Orange, Color.IndianRed, Main.rand.NextFloat(0.4f, 1f));
                GeneralParticleHandler.SpawnParticle(new SparkParticle(TipOfSpear, sparkVelocity, false, 60, 2f, sparkColor));

                sparkVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 10f);
                Color arcColor = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.3f, 1f));
                GeneralParticleHandler.SpawnParticle(new ElectricArc(TipOfSpear, sparkVelocity, arcColor, 0.84f, 30));
            }
        }

        public void DrawBackglow()
        {
            float backglowWidth = PinkLightningFormInterpolant * 2f;
            if (backglowWidth <= 0.5f)
                backglowWidth = 0f;

            Color backglowColor = Color.IndianRed;
            backglowColor = Color.Lerp(backglowColor, Color.Wheat, Utils.GetLerpValue(0.7f, 1f, PinkLightningFormInterpolant, true) * 0.56f) * 0.4f;
            backglowColor.A = 20;

            Texture2D glowmaskTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Items/Weapons/Rogue/StormMaidensRetributionSpear").Value;
            Rectangle frame = glowmaskTexture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            for (int i = 0; i < 10; i++)
            {
                Vector2 drawOffset = (TwoPi * i / 10f).ToRotationVector2() * backglowWidth;
                Main.spriteBatch.Draw(glowmaskTexture, drawPosition + drawOffset, frame, backglowColor, Projectile.rotation, origin, Projectile.scale, direction, 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color baseColor = Projectile.GetAlpha(lightColor) * (1f - PinkLightningFormInterpolant);

            Texture2D spearTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Items/Weapons/Rogue/StormMaidensRetributionGlowmask").Value;
            Rectangle frame = spearTexture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;

            DrawBackglow();
            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.spriteBatch.Draw(spearTexture, drawPosition, frame, baseColor, Projectile.rotation, origin, Projectile.scale, direction, 0f);
            return false;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (!StealthStrikeEffects)
                return;

            Owner.Infernum_Camera().CurrentScreenShakePower = 6f;

            Color[] explosionColorPalette = (Color[])CalamityUtils.ExoPalette.Clone();
            for (int i = 0; i < explosionColorPalette.Length; i++)
                explosionColorPalette[i] = Color.Lerp(explosionColorPalette[i], Color.Red, 0.3f);

            for (int i = 0; i < 6; i++)
                GeneralParticleHandler.SpawnParticle(new ElectricExplosionRing(target.Center + Main.rand.NextVector2Circular(60f, 60f), Vector2.Zero, explosionColorPalette, 1.45f, 78, 0.3f));

            CreateCrackleWithSparks(false);
            for (int i = 0; i < 6; i++)
                ShootLightningInDirection(target.Center - Vector2.UnitY * 1100f + Main.rand.NextVector2Circular(60f, 60f), Vector2.UnitY);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!StealthStrikeEffects)
                return true;

            Color[] explosionColorPalette = (Color[])CalamityUtils.ExoPalette.Clone();
            for (int i = 0; i < explosionColorPalette.Length; i++)
                explosionColorPalette[i] = Color.Lerp(explosionColorPalette[i], Color.Red, 0.3f);

            for (int i = 0; i < 6; i++)
                GeneralParticleHandler.SpawnParticle(new ElectricExplosionRing(Projectile.Center + Main.rand.NextVector2Circular(60f, 60f), Vector2.Zero, explosionColorPalette, 1.45f, 78, 0.3f));
            CreateCrackleWithSparks(false);
            for (int i = 0; i < 5; i++)
                ShootLightningInDirection(Projectile.Center - Vector2.UnitY * 1100f + Main.rand.NextVector2Circular(50f, 50f), Vector2.UnitY);

            return true;
        }

        // Don't do damage while aiming, to prevent "poking" strats where massive damage is acquired from just sitting on top of enemies with the spear.
        public override bool? CanDamage() => CurrentState != BehaviorState.Aim;
    }
}
