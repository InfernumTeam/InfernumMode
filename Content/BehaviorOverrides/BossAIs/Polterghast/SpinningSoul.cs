﻿using System.IO;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Polterghast
{
    public class SpinningSoul : ModProjectile
    {
        public bool CounterclockwiseSpin;

        public bool Cyan => Projectile.ai[0] == 1f;

        public ref float SpinOffsetAngle => ref Projectile.ai[1];

        public ref float SpinSpeedFactor => ref Projectile.localAI[0];

        public ref float Radius => ref Projectile.localAI[1];

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Soul");
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 270;
            Projectile.penetrate = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        // Sync local AI values.
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(CounterclockwiseSpin);
            writer.Write(SpinSpeedFactor);
            writer.Write(Radius);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            CounterclockwiseSpin = reader.ReadBoolean();
            SpinSpeedFactor = reader.ReadSingle();
            Radius = reader.ReadSingle();
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.ghostBoss))
            {
                Projectile.Kill();
                return;
            }

            NPC polterghast = Main.npc[CalamityGlobalNPC.ghostBoss];
            Player target = Main.player[polterghast.target];

            // Spin around the Polterghast.
            if (Radius == 0f)
            {
                Radius = 1520f;
                if (!target.WithinRange(polterghast.Center, Radius))
                    Radius = target.Distance(polterghast.Center) + 100f;

                Projectile.netUpdate = true;
            }
            Radius -= SpinSpeedFactor * 6f;
            SpinOffsetAngle -= ToRadians(Lerp(SpinSpeedFactor, 1f, 0.3f) * 1.5f) * CounterclockwiseSpin.ToDirectionInt();
            Projectile.Center = polterghast.Center + SpinOffsetAngle.ToRotationVector2() * Radius;
            if (Radius <= 20f)
                Projectile.Kill();

            // Handle fade effects and rotate.
            Projectile.Opacity = Utils.GetLerpValue(270f, 260f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 25f, Projectile.timeLeft, true);
            Projectile.rotation = (Projectile.position - Projectile.oldPosition).ToRotation() - PiOver2;

            // Determine frames.
            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5 == 4)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];

            if (Projectile.timeLeft % 18 == 17)
            {
                // Release a circle of dust every so often.
                for (int i = 0; i < 16; i++)
                {
                    Vector2 dustOffset = Vector2.UnitY.RotatedBy(TwoPi * i / 16f) * new Vector2(4f, 1f);
                    dustOffset = dustOffset.RotatedBy(Projectile.velocity.ToRotation());

                    Dust ectoplasm = Dust.NewDustDirect(Projectile.Center, 0, 0, DustID.SpectreStaff, 0f, 0f);
                    ectoplasm.position = Projectile.Center + dustOffset;
                    ectoplasm.velocity = dustOffset.SafeNormalize(Vector2.Zero) * 1.5f;
                    ectoplasm.color = Color.Lerp(Color.Purple, Color.White, 0.5f);
                    ectoplasm.scale = 1.5f;
                    ectoplasm.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Polterghast/SoulLarge" + (Cyan ? "Cyan" : string.Empty)).Value;
            if (Projectile.whoAmI % 2 == 0)
                texture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/Polterghast/SoulMedium" + (Cyan ? "Cyan" : string.Empty)).Value;

            Utilities.DrawAfterimagesCentered(Projectile, lightColor, ProjectileID.Sets.TrailingMode[Projectile.type], 2, texture);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.npc.IndexInRange(CalamityGlobalNPC.ghostBoss) && Main.npc[CalamityGlobalNPC.ghostBoss].active)
            {
                Main.npc[CalamityGlobalNPC.ghostBoss].ai[2] = Clamp(Main.npc[CalamityGlobalNPC.ghostBoss].ai[2] - 1f, 0f, 500f);
                Main.npc[CalamityGlobalNPC.ghostBoss].netUpdate = true;
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            Color color = Color.White;
            color.A = 0;
            return color * Projectile.Opacity;
        }
    }
}
