using CalamityMod;
using CalamityMod.Events;
using InfernumMode.Assets.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Magic
{
    public class EyeOfMadnessProj : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];

        public ref float Time => ref Projectile.ai[0];

        public ref float AttackTimer => ref Projectile.ai[1];

        public override void SetStaticDefaults() => DisplayName.SetDefault("Eye of Madness");

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.Infernum().DrawAsShadow = true;
        }

        public override void AI()
        {
            // If the player is no longer able to hold the eye, kill it.
            if (!Owner.channel || Owner.noItems || Owner.CCed)
            {
                Projectile.Kill();
                return;
            }

            AdjustPlayerValues();

            if (Time % 6f == 5f)
                ReleaseThings();

            Owner.ChangeDir(Projectile.velocity.X.DirectionalSign());
            Projectile.spriteDirection = Owner.direction;
            Projectile.Center = (Owner.Center + Vector2.UnitX * Owner.direction * 12f).Floor();
            Projectile.timeLeft = 2;
            AttackTimer++;
            Time++;
        }

        public void ReleaseThings()
        {
            SoundEngine.PlaySound(InfernumSoundRegistry.CeaselessVoidSwirlSound with { Volume = 0.5f }, Projectile.Center);
            if (Main.myPlayer != Projectile.owner)
                return;

            // If the owner has sufficient mana, consume it.
            // Otherwise, delete the eye and don't bother summoning anything.
            if (!Owner.CheckMana(Owner.ActiveItem().mana, true, false))
            {
                Projectile.Kill();
                return;
            }

            Vector2 spawnPosition = Projectile.Center + Main.rand.NextVector2Circular(5f, 5f);
            Vector2 shootVelocity = (Main.MouseWorld - spawnPosition).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.48f) * Main.rand.NextFloat(4.5f, 6f);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPosition, shootVelocity, ModContent.ProjectileType<ShadowTendril>(), Projectile.damage, Projectile.knockBack, Projectile.owner);

            AttackTimer = 0f;
            Projectile.velocity = Projectile.SafeDirectionTo(Main.MouseWorld) * 0.01f;
            Projectile.netUpdate = true;
        }

        public void AdjustPlayerValues()
        {
            Projectile.spriteDirection = Projectile.direction = Owner.direction;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = 2;
            Owner.itemAnimation = 2;
            Owner.itemRotation = CalamityUtils.WrapAngle90Degrees((Projectile.direction * Projectile.velocity).ToRotation());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bookTexture = ModContent.Request<Texture2D>(Texture).Value;
            SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle frame = bookTexture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;

            Main.EntitySpriteDraw(bookTexture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, direction, 0);
            return false;
        }

        public override bool? CanDamage() => false;
    }
}
