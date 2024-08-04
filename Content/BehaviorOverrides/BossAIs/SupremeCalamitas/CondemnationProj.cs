using System.IO;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas
{
    public class CondemnationProj : ModProjectile
    {
        public ref float Time => ref Projectile.ai[0];

        public Vector2 TipPosition => Projectile.Center + Projectile.rotation.ToRotationVector2() * Projectile.width * 0.5f;

        public override string Texture => "CalamityMod/Items/Weapons/Ranged/Condemnation";
        // public override void SetStaticDefaults() => DisplayName.SetDefault("Condemnation");

        public override void SetDefaults()
        {
            Projectile.width = 130;
            Projectile.height = 42;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90000;
            Projectile.Opacity = 0f;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        // Ensure that rotation is synced. It is very important for SCal's attacks.
        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Projectile.rotation);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.rotation = reader.ReadSingle();

        // Projectile spawning and rotation code are done in SCal's AI.
        public override void AI()
        {
            // Die if SCal is gone.
            if (CalamityGlobalNPC.SCal == -1 || !Main.npc[CalamityGlobalNPC.SCal].active)
            {
                Projectile.Kill();
                return;
            }

            // Stay glued to SCal's hand.
            Vector2 handPosition = SupremeCalamitasBehaviorOverride.CalculateHandPosition();
            Projectile.Center = handPosition;

            // Fade in. While this happens the projectile emits large amounts of flames.
            int flameCount = (int)((1f - Projectile.Opacity) * 12f);
            Projectile.Opacity = Clamp(Projectile.Opacity + 0.05f, 0f, 1f);

            // Create the fade-in dust.
            for (int i = 0; i < flameCount; i++)
            {
                Vector2 fireSpawnPosition = Projectile.Center;
                fireSpawnPosition += Vector2.UnitX.RotatedBy(Projectile.rotation) * Main.rand.NextFloatDirection() * Projectile.width * 0.5f;
                fireSpawnPosition += Vector2.UnitY.RotatedBy(Projectile.rotation) * Main.rand.NextFloatDirection() * Projectile.height * 0.5f;

                Dust fire = Dust.NewDustPerfect(fireSpawnPosition, 6);
                fire.velocity = -Vector2.UnitY.RotatedByRandom(0.44f) * Main.rand.NextFloat(2f, 4f);
                fire.scale = 1.4f;
                fire.fadeIn = 0.4f;
                fire.noGravity = true;
            }

            // Frequently sync.
            if (Main.netMode != NetmodeID.MultiplayerClient && Projectile.timeLeft % 12 == 11)
            {
                Projectile.netUpdate = true;
                Projectile.netSpam = 0;
            }

            // Define the direction.
            Projectile.direction = (Cos(Projectile.rotation) > 0f).ToDirectionInt();
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += Pi;
            Projectile.spriteDirection = Projectile.direction;

            Time++;
        }
    }
}
