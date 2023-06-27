using CalamityMod;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Particles;
using InfernumMode.Assets.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Rogue
{
    public class DreamtasticProj : ModProjectile
    {
        public enum BehaviorState
        {
            SummonDorks,
            ReleaseEnergy
        }

        public float SummoningCircleOpacity
        {
            get;
            set;
        }

        public float SummoningCircleScale
        {
            get;
            set;
        }

        public BehaviorState CurrentState
        {
            get => (BehaviorState)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        public Vector2 PikyPosition =>
            Owner.Center + new Vector2(Cos(Time / 19f), Sin(Time / 29f) * 0.4f - 1.48f) * DorkHoverOffset;

        public Vector2 DunkerPosition =>
            Owner.Center + new Vector2(-Cos(Time / 19f), Sin(Time / 29f + Pi * 0.66f) * 0.4f - 1.48f) * DorkHoverOffset;

        public Player Owner => Main.player[Projectile.owner];

        public ref float Time => ref Projectile.ai[1];

        public ref float DorkOpacity => ref Projectile.localAI[0];

        public ref float DorkHoverOffset => ref Projectile.localAI[1];

        public override string Texture => "InfernumMode/Content/Items/Weapons/Rogue/Dreamtastic";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Dreamtastic");

        public override void SetDefaults()
        {
            Projectile.width = 54;
            Projectile.height = 54;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.Opacity = 1f;
            Projectile.timeLeft = 14400;
            Projectile.penetrate = -1;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            // Die if no longer holding the click button or otherwise cannot use the item.
            if (!Owner.channel || Owner.dead || !Owner.active || Owner.noItems || Owner.CCed)
            {
                Projectile.Kill();
                return;
            }

            // Stick to the owner.
            AdjustPlayerValues();

            switch (CurrentState)
            {
                case BehaviorState.SummonDorks:
                    DoBehavior_SummonDorks();
                    break;
                case BehaviorState.ReleaseEnergy:
                    DoBehavior_ReleaseEnergy();
                    break;
            }

            Time++;
        }

        public void AdjustPlayerValues()
        {
            Projectile.timeLeft = 2;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Projectile.spriteDirection = Owner.direction;

            Projectile.Center = Owner.MountedCenter + new Vector2(Owner.direction * 13f, Owner.gfxOffY);
            Projectile.velocity = Vector2.Zero;

            // Update the player's arm directions to make it look as though they're holding the spear.
            float frontArmRotation = Owner.direction * -0.86f;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);
        }

        public void DoBehavior_SummonDorks()
        {
            int energyChargeUpTime = 90;
            int animationTime = 210;

            // Charge up energy.
            float chargeUpInterpolant = Utils.GetLerpValue(0f, animationTime * 0.7f, Time, true);
            if (Time <= energyChargeUpTime)
            {
                SummoningCircleOpacity = Sqrt(chargeUpInterpolant);
                SummoningCircleScale = chargeUpInterpolant;

                // Create energy pulses.
                if (Time % 15f == 8f)
                {
                    SoundEngine.PlaySound(InfernumSoundRegistry.SizzleSound with { Pitch = 0.6f }, Projectile.Center);

                    Color energyColor = Color.Lerp(Color.Fuchsia, Color.Cyan, SmoothStep(0f, 1f, chargeUpInterpolant));
                    PulseRing inwardPulse = new(Projectile.Center, Vector2.Zero, energyColor, 2.9f, 0f, 45);
                    GeneralParticleHandler.SpawnParticle(inwardPulse);

                    for (int i = 0; i < 36; i++)
                    {
                        Vector2 magicOffset = (TwoPi * i / 36f).ToRotationVector2() * (270f - Abs(Cos(Pi * 6f * i / 20f)) * 100f) + Main.rand.NextVector2Circular(15f, 15f);

                        Dust magic = Dust.NewDustPerfect(Projectile.Center + magicOffset, 267);
                        magic.color = Color.Lerp(Color.Fuchsia, Color.DeepSkyBlue, Main.rand.NextFloat());
                        magic.velocity = (Projectile.Center - magic.position) * 0.092f;
                        magic.scale = Main.rand.NextFloat(1.1f, 1.8f);
                        magic.rotation = Main.rand.NextFloat(TwoPi);
                        magic.noGravity = true;
                    }
                }
            }

            // Emit idle magic particles.
            for (int i = 0; i < 3; i++)
            {
                if (Main.rand.NextFloat() <= Pow(chargeUpInterpolant, 2.4f))
                {
                    Dust magic = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(100f, 100f) * SummoningCircleScale, 267);
                    magic.color = Color.Lerp(Color.Fuchsia, Color.DeepSkyBlue, Main.rand.NextFloat());
                    magic.velocity = Projectile.SafeDirectionTo(magic.position).RotatedBy(PiOver2) * -Main.rand.NextFloat(1f, 3f);
                    magic.scale = Main.rand.NextFloat(0.9f, 1.05f);
                    magic.rotation = Main.rand.NextFloat(TwoPi);
                    magic.noGravity = true;
                }
            }

            // Make the two dorks appear near the end of the animation.
            DorkHoverOffset = Utils.GetLerpValue(animationTime - 105f, animationTime - 25f, Time, true) * 84f;

            if (Time >= animationTime)
            {
                CurrentState = BehaviorState.ReleaseEnergy;
                Projectile.netUpdate = true;
            }
        }

        public void DoBehavior_ReleaseEnergy()
        {
            // Release a lot of energy in the direction of the mouse.
            int energyReleaseRate = Owner.ActiveItem().useTime;
            if (Time % energyReleaseRate == energyReleaseRate - 1f)
            {
                SoundEngine.PlaySound(ArtAttack.UseSound with { Pitch = 0.67f }, Projectile.Center);

                // Release one big bolt from the book.
                if (Main.myPlayer == Projectile.owner)
                {
                    int damage = (int)Owner.GetDamage<RogueDamageClass>().ApplyTo(Owner.ActiveItem().damage);
                    Vector2 energyBoltVelocity = Projectile.SafeDirectionTo(Main.MouseWorld) * Main.rand.NextFloat(0.7f, 1.1f) * Owner.ActiveItem().shootSpeed * 0.5f + Main.rand.NextVector2Circular(3f, 3f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, energyBoltVelocity, ModContent.ProjectileType<DreamtasticEnergyBolt>(), damage, Projectile.knockBack, Projectile.owner, 0f, 75f);

                    Owner.ChangeDir(energyBoltVelocity.X.DirectionalSign());
                }

                // Release two minor bolts from the dorks.
                if (Main.myPlayer == Projectile.owner)
                {
                    int damage = (int)Owner.GetDamage<RogueDamageClass>().ApplyTo(Owner.ActiveItem().damage / 3);
                    Vector2 energyBoltVelocity = (Main.MouseWorld - PikyPosition).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.45f) * Main.rand.NextFloat(3f, 9f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), PikyPosition, energyBoltVelocity, ModContent.ProjectileType<DreamtasticEnergyBolt>(), damage, Projectile.knockBack, Projectile.owner, 0f, 27f);

                    energyBoltVelocity = (Main.MouseWorld - DunkerPosition).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.45f) * Main.rand.NextFloat(3f, 9f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), DunkerPosition, energyBoltVelocity, ModContent.ProjectileType<DreamtasticEnergyBolt>(), damage, Projectile.knockBack, Projectile.owner, 0f, 27f);
                }
                Owner.Calamity().ConsumeStealthByAttacking();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw the summoning circle if necessary.
            if (SummoningCircleScale > 0f && SummoningCircleOpacity > 0f)
            {
                Projectile.rotation += Projectile.direction * 0.03f;
                Main.spriteBatch.SetBlendState(BlendState.Additive);
                DrawSummoningCircle();
                Main.spriteBatch.ResetBlendState();
            }
            DrawDorks();
            DrawBook();

            return false;
        }

        public void DrawSummoningCircle()
        {
            float scale = SummoningCircleScale * Projectile.scale * 2f;
            Vector2 drawPosition = Owner.Center + Vector2.UnitY * Owner.gfxOffY - Main.screenPosition;
            Texture2D magicCircleTexture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/RancorMagicCircle").Value;
            Texture2D magicCircleTextureBlurred = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/RancorMagicCircleGlowmask").Value;

            // Calculate the colors for the magic circle. The main ones are considerly brighter and less vibrant, while the blurred backglow variants retain said vibrancy.
            Color mainCircleColor = CalamityUtils.ColorSwap(Color.LightPink, Color.LightSkyBlue, 1.5f) * SummoningCircleOpacity * 0.6f;
            Color blurredCircleColor = CalamityUtils.ColorSwap(Color.Fuchsia, Color.DeepSkyBlue, 3f) * SummoningCircleOpacity * 0.4f;

            Main.EntitySpriteDraw(magicCircleTextureBlurred, drawPosition, null, Projectile.GetAlpha(blurredCircleColor), Projectile.rotation, magicCircleTextureBlurred.Size() * 0.5f, scale, 0, 0);
            Main.EntitySpriteDraw(magicCircleTextureBlurred, drawPosition, null, Projectile.GetAlpha(blurredCircleColor), -Projectile.rotation, magicCircleTextureBlurred.Size() * 0.5f, scale, 0, 0);
            Main.EntitySpriteDraw(magicCircleTexture, drawPosition, null, Projectile.GetAlpha(mainCircleColor), Projectile.rotation, magicCircleTexture.Size() * 0.5f, scale, 0, 0);
            Main.EntitySpriteDraw(magicCircleTexture, drawPosition, null, Projectile.GetAlpha(mainCircleColor), -Projectile.rotation, magicCircleTexture.Size() * 0.5f, scale, 0, 0);
        }

        public void DrawBook()
        {
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Texture2D bookTexture = ModContent.Request<Texture2D>(Texture).Value;
            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Main.EntitySpriteDraw(bookTexture, drawPosition, null, Projectile.GetAlpha(Color.White), 0f, bookTexture.Size() * 0.5f, Projectile.scale * 0.5f, direction, 0);
        }

        public void DrawDorks()
        {
            Color dorkDrawColor = Color.Lerp(Color.Transparent, Color.White, Utils.GetLerpValue(24f, 75f, DorkHoverOffset, true));
            Vector2 baseDrawPosition = Projectile.Center - Main.screenPosition;
            Vector2 pikyDrawPosition = PikyPosition - Main.screenPosition;
            Vector2 dunkerDrawPosition = DunkerPosition - Main.screenPosition;

            Texture2D pikyTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Projectiles/Rogue/Piky").Value;
            Texture2D dunkerTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Projectiles/Rogue/Dunker").Value;
            SpriteEffects pikyDirection = pikyDrawPosition.X > baseDrawPosition.X ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            SpriteEffects dunkerDirection = dunkerDrawPosition.X < baseDrawPosition.X ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Main.EntitySpriteDraw(pikyTexture, pikyDrawPosition, null, Projectile.GetAlpha(dorkDrawColor), 0f, pikyTexture.Size() * 0.5f, Projectile.scale, pikyDirection, 0);
            Main.EntitySpriteDraw(dunkerTexture, dunkerDrawPosition, null, Projectile.GetAlpha(dorkDrawColor), 0f, pikyTexture.Size() * 0.5f, Projectile.scale, dunkerDirection, 0);
        }
    }
}
