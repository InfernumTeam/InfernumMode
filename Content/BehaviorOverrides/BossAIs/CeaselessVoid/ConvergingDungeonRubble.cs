using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.CeaselessVoid
{
    public class ConvergingDungeonRubble : ModProjectile
    {
        public ref float Variant => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Energized Rubble");
            Main.projFrames[Type] = 3;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.scale = 1.5f;
            Projectile.Calamity().DealsDefenseDamage = true;
            Projectile.Infernum().FadesAwayWhenManuallyKilled = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Projectile.timeLeft);

        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = reader.ReadInt32();

        public override void AI()
        {
            if (CalamityGlobalNPC.voidBoss != -1 && Projectile.WithinRange(Main.npc[CalamityGlobalNPC.voidBoss].Center, 300f))
                Projectile.velocity *= 1.04f;

            if (CalamityGlobalNPC.voidBoss != -1 && Projectile.WithinRange(Main.npc[CalamityGlobalNPC.voidBoss].Center, Projectile.velocity.Length() * 1.96f + 28f))
                Projectile.Kill();

            // Initialize frame data.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
                Projectile.localAI[0] = 1f;

                // Select the projectile variant based on which dungeon wall is behind the nearest player.
                Player closestPlayer = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
                ushort playerWall = CalamityUtils.ParanoidTileRetrieval((int)(closestPlayer.Center.X / 16f), (int)(closestPlayer.Center.Y / 16f)).WallType;
                bool pinkWall = playerWall is WallID.PinkDungeonUnsafe or WallID.PinkDungeonTileUnsafe or WallID.PinkDungeonSlabUnsafe;
                bool greenWall = playerWall is WallID.GreenDungeonUnsafe or WallID.GreenDungeonTileUnsafe or WallID.GreenDungeonSlabUnsafe;
                bool blueWall = playerWall is WallID.BlueDungeonUnsafe or WallID.BlueDungeonTileUnsafe or WallID.BlueDungeonSlabUnsafe;

                if (pinkWall)
                    Variant = 2f;
                else if (greenWall)
                    Variant = 1f;
                else if (blueWall)
                    Variant = 0f;

                Projectile.netUpdate = true;
            }

            Projectile.rotation += Projectile.velocity.X * 0.02f;

            if (Projectile.velocity.Length() < 12f)
                Projectile.velocity *= 1.02f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(3, Main.projFrames[Type], (int)Variant, Projectile.frame);
            Projectile.DrawProjectileWithBackglowTemp(Color.HotPink with { A = 0 }, Color.Lerp(lightColor, Color.White, 0.75f), 6f, frame);
            return false;
        }
    }
}
