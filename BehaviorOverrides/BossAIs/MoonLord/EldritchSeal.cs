using CalamityMod;
using CalamityMod.Projectiles.Summon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static InfernumMode.GlobalInstances.GlobalNPCOverrides;

namespace InfernumMode.BehaviorOverrides.BossAIs.MoonLord
{
    public class EldritchSeal : ModNPC
    {
        public int HitTimer = 0;
        public Vector2 TargetPosition = default;
        public ref float ShootTimer => ref npc.ai[1];
        public ref float SpinAngle => ref npc.Infernum().ExtraAI[0];
        public ref float HitDelay => ref npc.Infernum().ExtraAI[7];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Eldritch Seal");
        }

        public override void SetDefaults()
        {
            npc.damage = 0;
            npc.width = 78;
            npc.height = 78;
            npc.lifeMax = 13;
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
            {
                npc.active = false;
                return;
            }
            npc.alpha = Utils.Clamp(npc.alpha - 2, 0, 255);

            HitDelay++;
            ShootTimer++;
            if (HitTimer > 0)
                HitTimer--;

            bool anySpecialSeals = Main.npc.Any(npc => npc.type == this.npc.type && npc.active && npc.ai[0] == 3f);
            Lighting.AddLight(npc.Center, 1f, 1f, 1f);
            Player player = Main.player[Player.FindClosest(npc.Center, 1, 1)];
            npc.dontTakeDamage = HitTimer > 0 || HitDelay < 420f;

            switch ((int)npc.ai[0])
            {
                // Shooters Type A
                case 0:
                    float shootRate = anySpecialSeals ? 250f : 160f;
                    if (ShootTimer % shootRate < shootRate - 30f)
                    {
                        TargetPosition = player.Center;
                    }
                    if (ShootTimer % shootRate == shootRate - 30f)
                    {
                        Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(TargetPosition) * 4f, ProjectileID.PhantasmalBolt, 185, 0f);
                        TargetPosition = default;
                    }
                    break;
                // Shooters Type B
                case 1:
                    shootRate = anySpecialSeals ? 100f : 150f;
                    if (ShootTimer % shootRate == shootRate - 20f)
                    {
                        for (int i = -3; i <= 3; i++)
                        {
                            float angle = MathHelper.Pi / 3f * i;
                            Utilities.NewProjectileBetter(npc.Center, (npc.SafeDirectionTo(TargetPosition) * 1.4f).RotatedBy(angle), ProjectileID.PhantasmalBolt, 185, 0f);
                        }
                    }
                    break;
                // Sniper
                case 2:
                    shootRate = anySpecialSeals ? 300f : 225f;
                    if (ShootTimer % shootRate < shootRate - 130f)
                    {
                        TargetPosition = player.Center;
                    }
                    if (ShootTimer % shootRate == shootRate - 70f)
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
                        Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(TargetPosition) * 1.4f, ModContent.ProjectileType<PhantasmalBlast>(), 185, 0f);
                        TargetPosition = default;
                    }
                    break;
            }

            SpinAngle += MathHelper.ToRadians(0.6f);
            if (npc.ai[0] != 3f)
            {
                npc.position = Main.npc[(int)npc.ai[3]].Center + SpinAngle.ToRotationVector2() * 900f;
                Main.LocalPlayer.Calamity().adrenaline = 0;
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
                                Main.npc[i].Infernum().ExtraAI[0] = angle;
                            }
                            angle += MathHelper.TwoPi / 3f;
                        }
                    }
                    if (npc.ai[2] > 0f)
                    {
                        npc.SimpleFlyMovement(npc.SafeDirectionTo(player.Center + SpinAngle.ToRotationVector2() * 540f) * 16f, 0.5f);
                        if (npc.ai[2] == 1f)
                        {
                            npc.velocity = Vector2.Zero;
                            int idx = Utilities.NewProjectileBetter(npc.Center, npc.SafeDirectionTo(TargetPosition), ModContent.ProjectileType<MoonlordPendulum>(), 350, 0f, 255, 0f, npc.whoAmI);
                            Main.projectile[idx].ai[0] = 0f;
                            Main.projectile[idx].ai[1] = npc.whoAmI;
                            TargetPosition = default;
                            npc.ai[2] = 0f;
                        }
                        else if (npc.ai[2] > 20f)
                        {
                            TargetPosition = player.Center;
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
                        npc.Infernum().ExtraAI[5] = 1f;
                    }
                    if (npc.Center.X > player.Center.X + 300f)
                    {
                        npc.Infernum().ExtraAI[5] = -1f;
                    }
                    npc.velocity.X += npc.Infernum().ExtraAI[5] * acceleration;
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
                    if (ShootTimer % 80f == 60f)
                        Utilities.NewProjectileBetter(npc.Center, new Vector2(0f, -8f).RotatedByRandom(MathHelper.ToRadians(26f)), ProjectileID.PhantasmalEye, 205, 1f);
                }
                npc.dontTakeDamage = anyNonSpecialSeals;
            }
            npc.rotation += MathHelper.ToRadians(6f);
        }
        public override bool StrikeNPC(ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit)
        {
            HitTimer = 15;

            if (npc.ai[0] != 3f)
                damage = 1D;
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
            if (TargetPosition != default && (npc.ai[0] == 0f || npc.ai[0] == 3f))
                spriteBatch.DrawLineBetter(npc.Center, TargetPosition, Color.Cyan * 0.7f, 4f);

            return true;
        }
        public override bool CheckActive() => false;
    }
}
