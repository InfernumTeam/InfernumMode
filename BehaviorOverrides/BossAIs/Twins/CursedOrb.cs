using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Twins
{
    public class CursedOrb : ModNPC
    {
        public PrimitiveTrailCopy FireDrawer;
        public Player Target => Main.player[NPC.target];
        public NPC Owner => Main.npc[(int)NPC.ai[0]];
        public ref float Time => ref NPC.ai[2];
        public ref float StunTimer => ref NPC.ai[3];
        internal ref float OwnerAttackTimer => ref Owner.Infernum().ExtraAI[10];
        internal TwinsAttackSynchronizer.SpazmatismAttackState OwnerAttackState => (TwinsAttackSynchronizer.SpazmatismAttackState)(int)Owner.Infernum().ExtraAI[11];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Cursed Flame Orb");
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.TrailCacheLength[NPC.type] = 7;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 1f;
            NPC.aiStyle = AIType = -1;
            NPC.width = NPC.height = 22;
            NPC.damage = 5;
            NPC.lifeMax = BossRushEvent.BossRushActive ? 48500 : 1600;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.netAlways = true;
            NPC.scale = 1.15f;
        }

        public override void AI()
        {
            if (!Main.npc.IndexInRange((int)NPC.ai[0]) || !Owner.active)
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            NPC.timeLeft = 3600;
            NPC.target = Owner.target;

            if (StunTimer > 0f)
            {
                StunTimer--;
                NPC.velocity *= 0.9f;
                return;
            }

            Vector2 flyDestination = Target.Center;
            float flySpeed = MathHelper.SmoothStep(8f, 23f, Utils.GetLerpValue(400f, 1500f, NPC.Distance(flyDestination), true));
            if (BossRushEvent.BossRushActive)
                flySpeed *= 1.6f;

            if (!NPC.WithinRange(flyDestination, 105f))
                NPC.velocity = Vector2.Lerp(NPC.velocity, NPC.SafeDirectionTo(flyDestination) * flySpeed, 0.08f);

            NPC.rotation += MathHelper.ToRadians(Math.Sign(NPC.velocity.X) * 10f);

            float attackWrappedTimer = Time % 150f;
            if (attackWrappedTimer >= 85f && attackWrappedTimer <= 95f && attackWrappedTimer % 2f == 0f)
            {
                if (attackWrappedTimer == 86f)
                    SoundEngine.PlaySound(SoundID.Item125, NPC.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float offsetAngle = MathHelper.Lerp(-0.51f, 0.51f, Utils.GetLerpValue(85f, 95f, attackWrappedTimer, true));
                    Vector2 shootVelocity = NPC.SafeDirectionTo(Target.Center).RotatedBy(offsetAngle) * 10f;
                    if (BossRushEvent.BossRushActive)
                        shootVelocity *= 1.65f;

                    Utilities.NewProjectileBetter(NPC.Center, shootVelocity, ModContent.ProjectileType<CursedCinder>(), 120, 0f);
                }
            }

            Time++;
        }

        public float WidthFunction(float completionRatio)
        {
            float squeezeInterpolant = (float)Math.Pow(Utils.GetLerpValue(0f, 0.27f, completionRatio, true), 0.4f) * Utils.GetLerpValue(1f, 0.86f, completionRatio, true);
            return MathHelper.SmoothStep(3f, NPC.width * 0.6f, squeezeInterpolant);
        }

        public Color ColorFunction(float completionRatio)
        {
            Color color = Color.Lerp(Color.White, Color.LimeGreen, 0.25f);
            color *= 1f - 0.5f * (float)Math.Pow(completionRatio, 6D);
            return color * NPC.Opacity;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Vector2[] baseOldPositions = NPC.oldPos.Where(oldPos => oldPos != Vector2.Zero).ToArray();
            if (baseOldPositions.Length <= 2)
                return true;

            if (FireDrawer is null)
                FireDrawer = new PrimitiveTrailCopy(WidthFunction, ColorFunction, null, true, GameShaders.Misc["Infernum:Fire"]);

            GameShaders.Misc["Infernum:Fire"].UseSaturation(0.9f);
            GameShaders.Misc["Infernum:Fire"].UseImage("Images/Misc/Perlin");
            FireDrawer.Draw(NPC.oldPos, NPC.Size * 0.5f - Main.screenPosition + NPC.velocity * 1.6f, 47);

            Texture2D texture = TextureAssets.Npc[NPC.type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color afterimageColor = Color.White * 0.16f;
            afterimageColor.A = 0;

            for (int i = 0; i < 8; i++)
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 3f;
                Vector2 drawPosition = NPC.Center - Main.screenPosition + drawOffset;
                spriteBatch.Draw(texture, drawPosition, null, afterimageColor, 0f, origin, NPC.scale, SpriteEffects.None, 0f);
            }

            return false;
        }

        public override bool CheckDead()
        {
            NPC.life = NPC.lifeMax;
            StunTimer = 300f;
            NPC.netUpdate = true;
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.CursedInferno, 150);
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (projectile.timeLeft > 10 && projectile.damage > 0)
                projectile.Kill();
        }

        public override bool CheckActive() => false;
    }
}
