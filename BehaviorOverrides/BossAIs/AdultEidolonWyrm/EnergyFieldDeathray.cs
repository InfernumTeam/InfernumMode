using CalamityMod;
using CalamityMod.NPCs;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class EnergyFieldDeathray : BaseLaserbeamProjectile
    {
        public int LocalLifetime = 160;
        public int OwnerIndex => (int)projectile.ai[1];
        public override float Lifetime => LocalLifetime;
        public override Color LaserOverlayColor => new Color(79, 174, 255, 32);
        public override Color LightCastColor => Color.Cyan;
        public override Texture2D LaserBeginTexture => Main.projectileTexture[projectile.type];
        public override Texture2D LaserMiddleTexture => Main.extraTexture[21];
        public override Texture2D LaserEndTexture => Main.extraTexture[22];
        public override float MaxLaserLength => 20f;
        public override float MaxScale => 0.5f;
        public override void SetStaticDefaults() => DisplayName.SetDefault("Eidolic Energy Ray");

        public override void SetDefaults()
        {
            projectile.width = 48;
            projectile.height = 48;
            projectile.hostile = true;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.timeLeft = (int)Lifetime;
            projectile.hide = true;
            projectile.Calamity().canBreakPlayerDefense = true;
            cooldownSlot = 1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(projectile.localAI[0]);
            writer.Write(projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            projectile.localAI[0] = reader.ReadSingle();
            projectile.localAI[1] = reader.ReadSingle();
        }
        public override void AttachToSomething()
        {
            if (!Main.npc.IndexInRange(OwnerIndex) || !Main.npc.IndexInRange((int)Main.npc[OwnerIndex].ai[1]))
            {
                projectile.Kill();
                return;
            }

            if (!Main.npc[OwnerIndex].active || !Main.npc[(int)Main.npc[OwnerIndex].ai[1]].active)
            {
                projectile.Kill();
                return;
            }

            projectile.Center = Main.npc[OwnerIndex].Center;
            projectile.velocity = (Main.npc[(int)Main.npc[OwnerIndex].ai[1]].Center - Main.npc[OwnerIndex].Center).SafeNormalize(Vector2.UnitY);

            // Die if the wyrm collides with the lasers.
            if (CalamityGlobalNPC.adultEidolonWyrmHead >= 0)
            {
                NPC wyrm = Main.npc[CalamityGlobalNPC.adultEidolonWyrmHead];
                if (projectile.Colliding(projectile.Hitbox, wyrm.Hitbox))
                {
                    Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PlasmaGrenadeExplosion"), projectile.Center);
                    projectile.Kill();
                }
            }

            projectile.timeLeft = 900;
            if (Time > 300f)
                Time = 300f;
        }

        public override float DetermineLaserLength()
        {
            float fuck = Main.npc[OwnerIndex].Distance(Main.npc[(int)Main.npc[OwnerIndex].ai[1]].Center);
            return fuck;
        }

        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit) => target.Calamity().lastProjectileHit = projectile;

        public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
        {
            drawCacheProjsBehindNPCs.Add(index);
        }
    }
}
