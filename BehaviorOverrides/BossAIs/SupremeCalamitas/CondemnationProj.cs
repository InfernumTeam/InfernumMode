using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class CondemnationProj : ModProjectile
    {
        public ref float Time => ref projectile.ai[0];

        public Vector2 TipPosition => projectile.Center + projectile.rotation.ToRotationVector2() * projectile.width * 0.5f;

        public override string Texture => "CalamityMod/Items/Weapons/Ranged/Condemnation";
        public override void SetStaticDefaults() => DisplayName.SetDefault("Condemnation");

        public override void SetDefaults()
        {
            projectile.width = 130;
            projectile.height = 42;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.netImportant = true;
            projectile.penetrate = -1;
            projectile.timeLeft = 90000;
            projectile.Opacity = 0f;
            cooldownSlot = 1;
        }

        // Ensure that rotation is synced. It is very important for SCal's attacks.
        public override void SendExtraAI(BinaryWriter writer) => writer.Write(projectile.rotation);

        public override void ReceiveExtraAI(BinaryReader reader) => projectile.rotation = reader.ReadSingle();

        // Projectile spawning and rotation code are done in SCal's AI.
        public override void AI()
        {
            // Die if SCal is gone.
            if (CalamityGlobalNPC.SCal == -1)
            {
                projectile.Kill();
                return;
            }

            // Stay glued to SCal's hand.
            Vector2 handPosition = SupremeCalamitasBehaviorOverride.CalculateHandPosition();
            projectile.Center = handPosition;

            // Fade in. While this happens the projectile emits large amounts of flames.
            int flameCount = (int)((1f - projectile.Opacity) * 12f);
            projectile.Opacity = MathHelper.Clamp(projectile.Opacity + 0.05f, 0f, 1f);

            // Create the fade-in dust.
            for (int i = 0; i < flameCount; i++)
            {
                Vector2 fireSpawnPosition = projectile.Center;
                fireSpawnPosition += Vector2.UnitX.RotatedBy(projectile.rotation) * Main.rand.NextFloatDirection() * projectile.width * 0.5f;
                fireSpawnPosition += Vector2.UnitY.RotatedBy(projectile.rotation) * Main.rand.NextFloatDirection() * projectile.height * 0.5f;

                Dust fire = Dust.NewDustPerfect(fireSpawnPosition, 6);
                fire.velocity = -Vector2.UnitY.RotatedByRandom(0.44f) * Main.rand.NextFloat(2f, 4f);
                fire.scale = 1.4f;
                fire.fadeIn = 0.4f;
                fire.noGravity = true;
            }

            // Frequently sync.
            if (Main.netMode != NetmodeID.MultiplayerClient && projectile.timeLeft % 12 == 11)
            {
                projectile.netUpdate = true;
                projectile.netSpam = 0;
            }

            // Define the direction.
            projectile.direction = ((float)Math.Cos(projectile.rotation) > 0f).ToDirectionInt();
            if (projectile.spriteDirection == -1)
                projectile.rotation += MathHelper.Pi;
            projectile.spriteDirection = projectile.direction;

            Time++;
        }
    }
}
