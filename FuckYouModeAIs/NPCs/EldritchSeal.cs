using CalamityMod;
using CalamityMod.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using InfernumMode.FuckYouModeAIs.MoonLord;
using static InfernumMode.FuckYouModeAIs.MainAI.FuckYouModeAIsGlobal;
using static InfernumMode.FuckYouModeAIs.MoonLord.MoonLordAIClass;

namespace InfernumMode.FuckYouModeAIs.NPCs
{
    public class EldritchSeal : ModNPC
    {
        Vector2 targetPosition = default;
        int hitCounter = 0;
        const int secondsOfInvinicibility = 7;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Eldritch Seal");
        }

        public override void SetDefaults()
        {
            npc.damage = 0;
            npc.width = 78;
            npc.height = 78;
            npc.lifeMax = 20;
            npc.noTileCollide = false;
            npc.noGravity = true;
            npc.netAlways = true;
            npc.aiStyle = -1;
            aiType = -1;
            npc.HitSound = SoundID.NPCHit5;
            npc.DeathSound = SoundID.NPCDeath3;
            npc.scale = 1f;
            npc.chaseable = true;
            npc.knockBackResist = 0f;
            for (int k = 0; k < npc.buffImmune.Length; k++)
            {
                npc.buffImmune[k] = true;
            }
            npc.alpha = 254;
            npc.noTileCollide = true;
        }
        public override void AI()
        {
            if (!NPC.AnyNPCs(NPCID.MoonLordCore))
                npc.active = false;
            if (npc.alpha > 0)
                npc.alpha -= 2;
            bool anySpecialSeals = Main.npc.Any(npc => npc.type == this.npc.type && npc.active && npc.ai[0] == 3f);
            Lighting.AddLight(npc.Center, 1f, 1f, 1f);
            Player player = Main.player[Player.FindClosest(npc.Center, 1, 1)];
            npc.GetGlobalNPC<MainAI.FuckYouModeAIsGlobal>().ExtraAI[7] += 1f;
            npc.dontTakeDamage = hitCounter > 0 || npc.GetGlobalNPC<MainAI.FuckYouModeAIsGlobal>().ExtraAI[7] < 60f * secondsOfInvinicibility;
            if (hitCounter > 0)
                hitCounter--;
            npc.ai[1] += 1f;
            switch ((int)npc.ai[0])
            {
                // Shooters Type A
                case 0:
                    float modulo = anySpecialSeals ? 250f : 160f;
                    if (npc.ai[1] % modulo < modulo - 30f)
                    {
                        targetPosition = player.Center;
                    }
                    if (npc.ai[1] % modulo == modulo - 30f)
                    {
                        Utilities.NewProjectileBetter(npc.Center, npc.DirectionTo(targetPosition) * 2.8f, ProjectileID.PhantasmalBolt, BoltDamage, 0f);
                        targetPosition = default;
                    }
                    break;
                // Shooters Type B
                case 1:
                    modulo = anySpecialSeals ? 120f : 210f;
                    if (npc.ai[1] % modulo == modulo - 20f)
                    {
                        for (int i = -3; i <= 3; i++)
                        {
                            float angle = MathHelper.Pi / 3f * i;
                            Utilities.NewProjectileBetter(npc.Center, (npc.DirectionTo(targetPosition) * 1.4f).RotatedBy(angle), ProjectileID.PhantasmalBolt, BoltDamage, 0f);
                        }
                    }
                    break;
                // Sniper
                case 2:
                    modulo = anySpecialSeals ? 330f : 250f;
                    if (npc.ai[1] % modulo < modulo - 130f)
                    {
                        targetPosition = player.Center;
                    }
                    if (npc.ai[1] % modulo == modulo - 70f)
                    {
                        const int dustCount = 50;
                        const int dustType = 229;
                        for (var i = 0; i < dustCount; i++)
                        {
                            float theta = MathHelper.TwoPi / dustCount * i;
                            Vector2 v2 = theta.ToRotationVector2() * 8f;
                            v2 = v2.RotatedBy(MathHelper.ToRadians(360f / dustCount));
                            int idx = Dust.NewDust(npc.Center, 1, 1, dustType, v2.X, v2.Y, 0,
                                default, 1f);
                            Main.dust[idx].noGravity = true;
                        }
                        Utilities.NewProjectileBetter(npc.Center, npc.DirectionTo(targetPosition) * 1.4f, ModContent.ProjectileType<PhantasmalBlast>(), BlastDamage, 0f);
                        targetPosition = default;
                    }
                    break;
            }
            const float radius = 900f;
            npc.GetGlobalNPC<MainAI.FuckYouModeAIsGlobal>().ExtraAI[0] += MathHelper.ToRadians(0.6f);
            if (npc.ai[0] != 3f)
            {
                npc.position = Main.npc[(int)npc.ai[3]].Center + npc.GetGlobalNPC<MainAI.FuckYouModeAIsGlobal>().ExtraAI[0].ToRotationVector2() * radius;
                Main.LocalPlayer.Calamity().adrenaline = 0;
                npc.Calamity().DR = 0.999999f;
            }
            else
            {
                npc.Calamity().DR = 0.3f;
                bool anyNonSpecialSeals = Main.npc.Any(npc => npc.type == this.npc.type && npc.active && npc.ai[0] != 3f);
                if (!anyNonSpecialSeals)
                {
                    if (MLSealTeleport)
                    {
                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            if (Main.projectile[i].type == ModContent.ProjectileType<MoonlordPendulum>())
                            {
                                Main.projectile[i].active = false;
                            }
                        }
                        MLSealTeleport = false;
                        float angle = 0f;
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].type == npc.type && Main.npc[i].ai[0] == 3f && Main.npc[i].active)
                            {
                                Main.npc[i].ai[2] = 60f * 3f;
                                Main.npc[i].GetGlobalNPC<MainAI.FuckYouModeAIsGlobal>().ExtraAI[0] = angle;
                            }
                            angle += MathHelper.TwoPi / 3f;
                        }
                    }
                    if (npc.ai[2] > 0f)
                    {
                        npc.SimpleFlyMovement(npc.DirectionTo(player.Center + npc.GetGlobalNPC<MainAI.FuckYouModeAIsGlobal>().ExtraAI[0].ToRotationVector2() * 540f) * 16f, 0.5f);
                        if (npc.ai[2] == 1f)
                        {
                            npc.velocity = Vector2.Zero;
                            int idx = Utilities.NewProjectileBetter(npc.Center, npc.DirectionTo(targetPosition), ModContent.ProjectileType<MoonlordPendulum>(), LaserDamage, 0f, 255, 0f, npc.whoAmI);
                            Main.projectile[idx].ai[0] = 0f;
                            Main.projectile[idx].ai[1] = npc.whoAmI;
                            targetPosition = default;
                            npc.ai[2] = 0f;
                        }
                        else if (npc.ai[2] > 20f)
                        {
                            targetPosition = player.Center;
                        }
                        npc.ai[2] -= 1f;
                    }
                }
                else
                {
                    npc.TargetClosest(false);
                    const float acceleration = 1.3f;
                    const float velocity = 18f;
                    if (npc.Center.X < player.Center.X - 300f)
                    {
                        npc.GetGlobalNPC<MainAI.FuckYouModeAIsGlobal>().ExtraAI[5] = 1f;
                    }
                    if (npc.Center.X > player.Center.X + 300f)
                    {
                        npc.GetGlobalNPC<MainAI.FuckYouModeAIsGlobal>().ExtraAI[5] = -1f;
                    }
                    npc.velocity.X += npc.GetGlobalNPC<MainAI.FuckYouModeAIsGlobal>().ExtraAI[5] * acceleration;
                    if (npc.velocity.X > velocity)
                        npc.velocity.X = velocity;
                    if (npc.velocity.X < -velocity)
                        npc.velocity.X = -velocity;

                    float distPlayerY = player.position.Y - npc.Center.Y;
                    if (distPlayerY < 200f) // 150
                        npc.velocity.Y -= 0.2f;
                    if (distPlayerY > 250f) // 200
                        npc.velocity.Y += 0.2f;

                    if (npc.velocity.Y > 6f)
                        npc.velocity.Y = 6f;
                    if (npc.velocity.Y < -6f)
                        npc.velocity.Y = -6f;
                    if (npc.ai[1] % 80f == 60f)
                        Utilities.NewProjectileBetter(npc.Center, new Vector2(0f, -8f).RotatedByRandom(MathHelper.ToRadians(26f)), ProjectileID.PhantasmalEye, EyeDamage, 1f);
                }
                npc.dontTakeDamage = anyNonSpecialSeals;
            }
            npc.rotation += MathHelper.ToRadians(6f);
        }
        public override bool StrikeNPC(ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit)
        {
            hitCounter = 15;
            crit = false;
            return base.StrikeNPC(ref damage, defense, ref knockback, hitDirection, ref crit);
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (npc.ai[0] == 3)
            {
                if (projectile.type == ModContent.ProjectileType<SakuraBullet>() ||
                    projectile.type == ModContent.ProjectileType<PurpleButterfly>() ||
                    (projectile.type >= ProjectileID.StardustDragon1 && projectile.type <= ProjectileID.StardustDragon4))
                {
                    damage = (int)(damage * 0.55);
                }
            }
        }
        public override Color? GetAlpha(Color drawColor)
        {
            return new Color(7, 171, 171, npc.alpha);
        }
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position) => false;
        public override void HitEffect(int hitDirection, double damage)
        {
            if (npc.life <= 0)
            {
                int dustType = 229;
                const int dustCount = 50;
                for (var i = 0; i < dustCount; i++)
                {
                    float theta = MathHelper.TwoPi / 20f * i;
                    Vector2 velocity = theta.ToRotationVector2() * Main.rand.NextFloat(7f, 39f);
                    velocity = velocity.RotatedBy(MathHelper.ToRadians(360f / dustCount));
                    int idx = Dust.NewDust(npc.Center, 1, 1, dustType, velocity.X, velocity.Y, 0,
                        default, 1f);
                    Main.dust[idx].noGravity = true;
                }
            }
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            if (targetPosition != default && (npc.ai[0] == 0f || npc.ai[0] == 3f))
            {
                Utils.DrawLine(spriteBatch, npc.Center, npc.Center + npc.AngleTo(targetPosition).ToRotationVector2() * npc.Distance(targetPosition), Color.Cyan, Color.Transparent, 4f);
            }
            return true;
        }
        public override bool CheckActive() => false;
    }
}
