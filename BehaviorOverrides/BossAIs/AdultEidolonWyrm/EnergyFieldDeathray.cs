using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class EnergyFieldDeathray : BaseLaserbeamProjectile
    {
        public int LocalLifetime = 160;
        public int OwnerIndex => (int)Projectile.ai[1];
        public override float Lifetime => LocalLifetime;
        public override Color LaserOverlayColor => new(79, 174, 255, 32);
        public override Color LightCastColor => Color.Cyan;
        public override Texture2D LaserBeginTexture => Utilities.ProjTexture(Projectile.type);
        public override Texture2D LaserMiddleTexture => Main.extraTexture[21];
        public override Texture2D LaserEndTexture => Main.extraTexture[22];
        public override float MaxLaserLength => 20f;
        public override float MaxScale => 0.5f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Eidolic Energy Ray");

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.hostile = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = (int)Lifetime;
            Projectile.hide = true;
            Projectile.Calamity().canBreakPlayerDefense = true;
            CooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
        }
        public override void AttachToSomething()
        {
            if (!Main.npc.IndexInRange(OwnerIndex) || !Main.npc.IndexInRange((int)Main.npc[OwnerIndex].ai[1]))
            {
                Projectile.Kill();
                return;
            }

            if (!Main.npc[OwnerIndex].active || !Main.npc[(int)Main.npc[OwnerIndex].ai[1]].active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = Main.npc[OwnerIndex].Center;
            Projectile.velocity = (Main.npc[(int)Main.npc[OwnerIndex].ai[1]].Center - Main.npc[OwnerIndex].Center).SafeNormalize(Vector2.UnitY);

            // Die if the wyrm collides with the lasers.
            if (CalamityGlobalNPC.adultEidolonWyrmHead >= 0)
            {
                NPC wyrm = Main.npc[CalamityGlobalNPC.adultEidolonWyrmHead];
                if (Projectile.Colliding(Projectile.Hitbox, wyrm.Hitbox))
                {
                    SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/PlasmaGrenadeExplosion"), Projectile.Center);
                    Projectile.Kill();
                }
            }

            Projectile.timeLeft = 900;
            if (Time > 300f)
                Time = 300f;
        }

        public override float DetermineLaserLength()
        {
            float fuck = Main.npc[OwnerIndex].Distance(Main.npc[(int)Main.npc[OwnerIndex].ai[1]].Center);
            return fuck;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = Projectile;

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            drawCacheProjsBehindNPCs.Add(index);
        }
    }
}
