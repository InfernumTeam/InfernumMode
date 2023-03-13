using CalamityMod;
using CalamityMod.NPCs.AdultEidolonWyrm;
using CalamityMod.Sounds;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AEWIllusionTelegraphLine : ModProjectile, IAboveWaterProjectileDrawer
    {
        public bool CreatesRealAEW
        {
            get => Projectile.ai[0] == 1f;
            set => Projectile.ai[0] = value.ToInt();
        }

        public ref float Time => ref Projectile.localAI[0];

        public ref float Lifetime => ref Projectile.ai[1];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => DisplayName.SetDefault("Telegraph");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Time);

        public override void ReceiveExtraAI(BinaryReader reader) => Time = reader.ReadSingle();

        public override void AI()
        {
            Projectile.Opacity = CalamityUtils.Convert01To010(Time / Lifetime) * 3f;
            if (Projectile.Opacity > 1f)
                Projectile.Opacity = 1f;

            if (Time >= Lifetime)
                Projectile.Kill();

            Time++;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor) => false;

        public void DrawAboveWater(SpriteBatch spriteBatch)
        {
            float telegraphWidth = MathHelper.Lerp(0.3f, 5f, CalamityUtils.Convert01To010(Time / Lifetime));

            // Draw a telegraph line outward.
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 3000f;
            spriteBatch.DrawLineBetter(start, end, CreatesRealAEW ? Color.HotPink : Color.DarkGray, telegraphWidth);
        }

        public override void Kill(int timeLeft)
        {
            int aewIndex = NPC.FindFirstNPC(ModContent.NPCType<AdultEidolonWyrmHead>());
            int aewBodyID = ModContent.NPCType<AdultEidolonWyrmBody>();
            int aewBody2ID = ModContent.NPCType<AdultEidolonWyrmBodyAlt>();
            int aewTailID = ModContent.NPCType<AdultEidolonWyrmTail>();
            Player target = Main.player[Player.FindClosest(Projectile.Center, 1, 1)];
            Vector2 wyrmSpawnPosition = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 1000f;
            if (CreatesRealAEW && aewIndex != -1)
            {
                if (Projectile.WithinRange(target.Center, 3200f))
                    SoundEngine.PlaySound(CommonCalamitySounds.WyrmScreamSound, target.Center);

                NPC aew = Main.npc[aewIndex];
                aew.Center = wyrmSpawnPosition;
                aew.velocity = aew.SafeDirectionTo(target.Center) * 120f;
                aew.Infernum().ExtraAI[2] = 1f;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC n = Main.npc[i];
                    if (n.active && (n.type == aewBodyID || n.type == aewBody2ID || n.type == aewTailID))
                    {
                        n.Center = aew.Center;
                        n.netUpdate = true;
                    }
                }

                aew.netUpdate = true;
            }
            else if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 splitFormVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * 15f;
                Utilities.NewProjectileBetter(wyrmSpawnPosition, splitFormVelocity, ModContent.ProjectileType<AEWSplitForm>(), 0, 0f, -1, 1f);
            }
        }
    }
}