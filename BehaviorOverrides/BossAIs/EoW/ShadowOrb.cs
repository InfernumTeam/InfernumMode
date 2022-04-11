using CalamityMod.NPCs.HiveMind;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.EoW
{
    public class ShadowOrb : ModProjectile
    {
        public override void SetStaticDefaults() => DisplayName.SetDefault("Shadow Orb");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 30;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
        }

        public override void AI()
        {
            // Make the nearby light more dim.
            Lighting.AddLight(Projectile.Center, Color.DarkGray.ToVector3() * Projectile.Opacity * 0.5f);

            // Fade in and out.
            Projectile.Opacity = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true) * Utils.GetLerpValue(120f, 90f, Projectile.timeLeft, true);
        }

        // Summon a random enemy after disappearing.
        public override void Kill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item8, Projectile.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            WeightedRandom<int> enemySelector = new(Main.rand);
            enemySelector.Add(NPCID.EaterofSouls);
            enemySelector.Add(NPCID.DevourerHead, 0.4);
            enemySelector.Add(ModContent.NPCType<DarkHeart>(), 0.65);
            enemySelector.Add(ModContent.NPCType<DankCreeper>(), 0.4);
            NPC.NewNPC((int)Projectile.Center.X, (int)Projectile.Center.Y, enemySelector.Get(), 1);
        }
    }
}
