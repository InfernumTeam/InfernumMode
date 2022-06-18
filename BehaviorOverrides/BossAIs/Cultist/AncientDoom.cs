using CalamityMod;
using CalamityMod.Events;
using InfernumMode.BehaviorOverrides.BossAIs.HiveMind;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Cultist
{
    public class AncientDoom : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];
        public Player Target => Main.player[(int)projectile.ai[1]];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Doomer");
            Main.projFrames[projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 50;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.netImportant = true;
            projectile.hostile = true;
            projectile.timeLeft = 90;
            projectile.Opacity = 0f;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            // Initialize a target.
            if (Main.netMode != NetmodeID.MultiplayerClient && projectile.localAI[0] == 0f)
            {
                projectile.ai[1] = Player.FindClosest(projectile.Center, 1, 1);
                projectile.localAI[0] = 1f;

                projectile.netUpdate = true;
            }

            projectile.Opacity = Utils.InverseLerp(0f, 20f, Time, true) * Utils.InverseLerp(4f, 35f, projectile.timeLeft, true);
            projectile.scale = Utils.InverseLerp(0f, 15f, Time, true) * Utils.InverseLerp(4f, 35f, projectile.timeLeft, true) * 1.35f;
            projectile.frameCounter++;
            if (projectile.frameCounter % 5 == 4)
                projectile.frame = (projectile.frame + 1) % Main.projFrames[projectile.type];

            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.SetBlendState(BlendState.Additive);

            Texture2D doomTexture = Main.projectileTexture[projectile.type];
            Rectangle frame = doomTexture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
            for (int i = 0; i < 10; i++)
            {
                Color drawColor = Color.Magenta * projectile.Opacity * 0.18f;
                Vector2 drawPosition = projectile.Center + (MathHelper.TwoPi * i / 10f + Main.GlobalTime * 4.4f).ToRotationVector2() * 5f;
                spriteBatch.Draw(doomTexture, drawPosition, frame, drawColor, projectile.rotation, frame.Size() * 0.5f, projectile.scale, SpriteEffects.None, 0f);
            }

            spriteBatch.ResetBlendState();

            // Make line telegraphs.
            if (projectile.timeLeft < 40f)
            {
                for (int i = 0; i < 9; i++)
                {
                    Vector2 beamDirection = (MathHelper.TwoPi * i / 9f).ToRotationVector2();
                    if (projectile.localAI[1] == 1f)
                        beamDirection = beamDirection.RotatedBy(MathHelper.TwoPi / 18f);
                    spriteBatch.DrawLineBetter(projectile.Center, projectile.Center + beamDirection * DoomBeam.LaserLength, Color.Purple, (float)Math.Sin(MathHelper.Pi * Utils.InverseLerp(0f, 40f, projectile.timeLeft, true)) * 2f);
                }
            }

            return true;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override void Kill(int timeLeft)
        {
            // Make some strong sounds.
            var sound = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/FlareSound"), Target.Center);
            if (sound != null)
                sound.Volume = MathHelper.Clamp(sound.Volume * 1.61f, -1f, 1f);
            sound = Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), Target.Center);
            if (sound != null)
                sound.Pitch = -0.21f;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            // And explode into a bunch of powerful projectiles.
            Utilities.NewProjectileBetter(projectile.Center, Vector2.Zero, ModContent.ProjectileType<HiveMindWave>(), 0, 0f);
            for (int i = 0; i < 35; i++)
            {
                Vector2 doomVelocity = (MathHelper.TwoPi * i / 35f).ToRotationVector2() * 3.15f;
                if (BossRushEvent.BossRushActive)
                    doomVelocity *= 1.5f;

                Utilities.NewProjectileBetter(projectile.Center, doomVelocity, ModContent.ProjectileType<DarkPulse>(), 170, 0f);
                Utilities.NewProjectileBetter(projectile.Center, doomVelocity * 0.25f, ModContent.ProjectileType<DarkPulse>(), 170, 0f);
            }
            for (int i = 0; i < 9; i++)
            {
                Vector2 beamDirection = (MathHelper.TwoPi * i / 9f).ToRotationVector2();
                if (projectile.localAI[1] == 1f)
                    beamDirection = beamDirection.RotatedBy(MathHelper.TwoPi / 18f);
                Utilities.NewProjectileBetter(projectile.Center, beamDirection, ModContent.ProjectileType<DoomBeam>(), 240, 0f);
            }
        }
    }
}
