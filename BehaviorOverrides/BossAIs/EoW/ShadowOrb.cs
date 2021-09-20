using CalamityMod.NPCs.HiveMind;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace InfernumMode.BehaviorOverrides.BossAIs.EoW
{
    public class ShadowOrb : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Shadow Orb");

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 30;
            projectile.hostile = true;
            projectile.ignoreWater = true;
            projectile.tileCollide = false;
            projectile.penetrate = -1;
            projectile.timeLeft = 120;
        }

        public override void AI()
        {
            // Make the nearby light more dim.
            Lighting.AddLight(projectile.Center, Color.DarkGray.ToVector3() * projectile.Opacity * 0.5f);

            // Fade in and out.
            projectile.Opacity = Utils.InverseLerp(0f, 30f, projectile.timeLeft, true) * Utils.InverseLerp(120f, 90f, projectile.timeLeft, true);
        }

        // Summon a random enemy after disappearing.
        public override void Kill(int timeLeft)
        {
            Main.PlaySound(SoundID.Item8, projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            WeightedRandom<int> enemySelector = new WeightedRandom<int>(Main.rand);
            enemySelector.Add(NPCID.EaterofSouls);
            enemySelector.Add(NPCID.DevourerHead, 0.4);
            enemySelector.Add(ModContent.NPCType<DarkHeart>(), 0.65);
            enemySelector.Add(ModContent.NPCType<DankCreeper>(), 0.4);
            NPC.NewNPC((int)projectile.Center.X, (int)projectile.Center.Y, enemySelector.Get(), 1);
        }
    }
}
