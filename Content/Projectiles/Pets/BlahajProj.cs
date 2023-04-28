using CalamityMod;
using InfernumMode.Core.GlobalInstances.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.Projectiles.Pets
{
    public class BlahajProj : ModProjectile
    {
        public Player Owner => Main.player[Projectile.owner];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blahaj");
            Main.projPet[Projectile.type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 98;
            Projectile.height = 56;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 90000;
            Projectile.tileCollide = false;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            if (!Owner.active)
            {
                Projectile.active = false;
                return;
            }

            Projectile.FloatingPetAI(false, 0.02f);
            Projectile.spriteDirection = (Owner.Center.X < Projectile.Center.X).ToDirectionInt();

            HandlePetVariables();
        }

        public void HandlePetVariables()
        {
            PetsPlayer modPlayer = Owner.Infernum_Pet();
            if (Owner.dead)
                modPlayer.BlahajPet = false;
            if (modPlayer.BlahajPet)
                Projectile.timeLeft = 2;
        }

        // Prevent dying when touching tiles.
        public override bool OnTileCollide(Vector2 oldVelocity) => false;
    }
}
