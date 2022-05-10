using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Plantera
{
    public class SporeFlower : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Flower");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 22;
            projectile.hostile = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 150;
            projectile.penetrate = -1;
        }

        public override void AI()
        {
            projectile.scale = Utils.InverseLerp(-5f, 20f, projectile.timeLeft, true) * Utils.InverseLerp(150f, 130f, projectile.timeLeft, true);
        }

        public override void Kill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int spore = NPC.NewNPC((int)projectile.Center.X, (int)projectile.Center.Y, NPCID.Spore);
            if (Main.npc.IndexInRange(spore))
                Main.npc[spore].velocity = -Vector2.UnitY.RotatedByRandom(0.67f) * Main.rand.NextFloat(3f, 6f);
        }

        public override Color? GetAlpha(Color lightColor) => Color.Red * 0.6f;
    }
}
