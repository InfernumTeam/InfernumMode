using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Magic
{
    public class IllusionersReverieProj : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];

        public float ShootIntensity => MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(0f, 275f, Time, true));

        public ref float Time => ref Projectile.ai[0];

        public ref float AttackTimer => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Illusioner's Reverie");
            Main.projFrames[Projectile.type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            // If the player is no longer able to hold the book, kill it.
            if (!Owner.channel || Owner.noItems || Owner.CCed)
            {
                Projectile.Kill();
                return;
            }

            if (AttackTimer >= Main.rand.Next(36, 50) - (int)MathHelper.Lerp(0f, 20f, Utils.GetLerpValue(0f, 120f, Time, true)))
                ReleaseThings();

            // Switch frames at a linearly increasing rate to make it look like the player is flipping pages quickly.
            Projectile.localAI[0] += Utils.Remap(Time, 0f, 180f, 1f, 5f);
            Projectile.frame = (int)Math.Round(Projectile.localAI[0] / 10f) % Main.projFrames[Projectile.type];
            if (Projectile.localAI[0] >= Main.projFrames[Projectile.type] * 10f)
                Projectile.localAI[0] = 0f;

            Owner.ChangeDir(Projectile.velocity.X.DirectionalSign());
            AdjustPlayerValues();
            Projectile.Center = Owner.Center + (Owner.compositeFrontArm.rotation + MathHelper.PiOver2).ToRotationVector2() * 14f - Vector2.UnitY * 4f;
            Projectile.timeLeft = 2;
            AttackTimer++;
            Time++;
        }

        public void ReleaseThings()
        {
            SoundEngine.PlaySound(BossRushEvent.TeleportSound with { Pitch = 0.2f, Volume = 0.6f });
            if (Main.myPlayer != Projectile.owner)
                return;

            // If the owner has sufficient mana, consume it.
            // Otherwise, delete the book and don't bother summoning anything.
            if (!Owner.CheckMana(Owner.ActiveItem().mana, true, false))
            {
                Projectile.Kill();
                return;
            }

            Vector2 spawnPosition = Owner.Center - Vector2.UnitX.RotatedByRandom(0.82f) * Owner.direction * Main.rand.NextFloat(50f, 150f);
            Vector2 shootVelocity = (Main.MouseWorld - spawnPosition).SafeNormalize(Vector2.UnitY) * 4f;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPosition, shootVelocity, ModContent.ProjectileType<ShadowIllusion>(), Projectile.damage, Projectile.knockBack, Projectile.owner, ShootIntensity);

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
            Owner.itemRotation = (Projectile.direction * Projectile.velocity).ToRotation();

            // Update the player's arm directions to make it look as though they're flipping through the book.
            float frontArmRotation = (MathHelper.PiOver2 - 0.46f) * -Owner.direction;
            float backArmRotation = frontArmRotation + MathHelper.Lerp(0.23f, 0.97f, CalamityUtils.Convert01To010(Projectile.localAI[0] / Main.projFrames[Projectile.type] / 10f)) * -Owner.direction;
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, backArmRotation);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float glowOutwardness = MathHelper.SmoothStep(0f, 6f, Utils.GetLerpValue(90f, 270f, Time, true));
            Texture2D bookTexture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = bookTexture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 drawPosition;
            Vector2 origin = frame.Size() * 0.5f;
            Color glowColor = Color.Lerp(Color.HotPink, Color.Blue, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 5f) * 0.5f + 0.5f);
            glowColor.A = 0;

            // Draw an ominous glowing version of the book after a bit of time.
            for (int i = 0; i < 12; i++)
            {
                drawPosition = Projectile.Center + (MathHelper.TwoPi * i / 12f + Main.GlobalTimeWrappedHourly * 4f).ToRotationVector2() * glowOutwardness - Main.screenPosition;
                Main.EntitySpriteDraw(bookTexture, drawPosition, frame, Projectile.GetAlpha(glowColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            drawPosition = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bookTexture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override bool? CanDamage() => false;
    }
}
