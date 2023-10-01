using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Plantera
{
    public class SporeFlower : ModProjectile
    {
        // public override void SetStaticDefaults() => DisplayName.SetDefault("Flower");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 150;
            Projectile.penetrate = -1;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void AI()
        {
            Projectile.scale = Utils.GetLerpValue(-5f, 20f, Projectile.timeLeft, true) * Utils.GetLerpValue(150f, 130f, Projectile.timeLeft, true);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int spore = NPC.NewNPC(Projectile.GetSource_FromAI(), (int)Projectile.Center.X, (int)Projectile.Center.Y, NPCID.Spore);
            if (Main.npc.IndexInRange(spore))
                Main.npc[spore].velocity = -Vector2.UnitY.RotatedByRandom(0.67f) * Main.rand.NextFloat(3f, 6f);
        }

        public override Color? GetAlpha(Color lightColor) => Color.Red * 0.6f;
    }
}
