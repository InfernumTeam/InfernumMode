using CalamityMod.Events;
using CalamityMod.NPCs.Crabulon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Crabulon
{
    public class FungalClump : ModNPC
    {
        public PrimitiveTrailCopy FireDrawer;
        public Player Target => Main.player[NPC.target];
        public NPC Owner => Main.npc[(int)NPC.ai[0]];
        public float MainBossLifeRatio => Owner.life / (float)Owner.lifeMax;
        public ref float Time => ref NPC.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fungal Clump");
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.TrailCacheLength[NPC.type] = 7;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 1f;
            NPC.aiStyle = AIType = -1;
            NPC.width = NPC.height = 50;
            NPC.damage = 45;
            NPC.lifeMax = 5000;
            NPC.knockBackResist = 0f;
            NPC.dontTakeDamage = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.netAlways = true;
        }

        public override void AI()
        {
            // Die if the main boss is not present.
            if (!Main.npc.IndexInRange((int)NPC.ai[0]) || !Owner.active || !NPC.AnyNPCs(ModContent.NPCType<CrabulonIdle>()))
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            NPC.target = Owner.target;
            float hoverSpeed = MathHelper.Lerp(7f, 11f, 1f - MainBossLifeRatio);
            if (BossRushEvent.BossRushActive)
                hoverSpeed *= 2.15f;
            HomeTowardsTarget(hoverSpeed);

            if (Main.netMode != NetmodeID.MultiplayerClient && MainBossLifeRatio < CrabulonBehaviorOverride.Phase3LifeRatio && Time % 90f == 89f)
            {
                ReleaseSpores();
                NPC.netUpdate = true;
            }

            Time++;
        }

        public void HomeTowardsTarget(float hoverSpeed)
        {
            // Home more quickly if close to the target.
            // However, if really close to the target, stop homing and simply go in the
            // current direction.
            Vector2 hoverDestination = (Target.Center + (NPC.whoAmI * 10.81f).ToRotationVector2() * 24f);
            if (!NPC.WithinRange(hoverDestination, 180f))
                NPC.velocity = (NPC.velocity * 115f + NPC.SafeDirectionTo(hoverDestination) * hoverSpeed) / 116f;
            else if (!NPC.WithinRange(hoverDestination, 90f))
                NPC.velocity = (NPC.velocity * 90f + NPC.SafeDirectionTo(hoverDestination) * hoverSpeed * 0.8f) / 91f;
        }

        public void ReleaseSpores()
        {
            int spore = Utilities.NewProjectileBetter(NPC.Center, Vector2.UnitY.RotatedBy(-0.45f) * -6f, ModContent.ProjectileType<HomingSpore>(), 45, 0f);
            Main.projectile[spore].ai[0] = Utils.GetLerpValue(0.45f, 0.1f, MainBossLifeRatio);

            spore = Utilities.NewProjectileBetter(NPC.Center, Vector2.UnitY.RotatedBy(0.45f) * -6f, ModContent.ProjectileType<HomingSpore>(), 45, 0f);
            Main.projectile[spore].ai[0] = Utils.GetLerpValue(0.45f, 0.1f, MainBossLifeRatio);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            int healAmount = damage;
            Owner.HealEffect(healAmount);
            Owner.life += healAmount;
            if (Owner.life > Owner.lifeMax)
                Owner.life = Owner.lifeMax;
        }

        // Draw a blue glowmask for Crabulon's fungal clumps.
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glowmask = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/Crabulon/FungalClump_Glowmask").Value;
            Vector2 drawPosition = NPC.Center - Main.screenPosition;
            Vector2 origin = NPC.frame.Size() * 0.5f;
            Color color = NPC.GetAlpha(drawColor);
            spriteBatch.Draw(texture, drawPosition, NPC.frame, color, NPC.rotation, origin, NPC.scale, 0, 0f);
            spriteBatch.Draw(glowmask, drawPosition, NPC.frame, NPC.GetAlpha(Color.White), NPC.rotation, origin, NPC.scale, 0, 0f);
            return false;
        }

        public override bool CheckActive() => false;
    }
}
