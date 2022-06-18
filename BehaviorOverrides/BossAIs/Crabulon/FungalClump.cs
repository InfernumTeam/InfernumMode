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
        public Player Target => Main.player[npc.target];
        public NPC Owner => Main.npc[(int)npc.ai[0]];
        public float MainBossLifeRatio => Owner.life / (float)Owner.lifeMax;
        public ref float Time => ref npc.ai[1];
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Fungal Clump");
            NPCID.Sets.TrailingMode[npc.type] = 0;
            NPCID.Sets.TrailCacheLength[npc.type] = 7;
        }

        public override void SetDefaults()
        {
            npc.npcSlots = 1f;
            npc.aiStyle = aiType = -1;
            npc.width = npc.height = 50;
            npc.damage = 45;
            npc.lifeMax = 5000;
            npc.knockBackResist = 0f;
            npc.dontTakeDamage = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.netAlways = true;
        }

        public override void AI()
        {
            // Die if the main boss is not present.
            if (!Main.npc.IndexInRange((int)npc.ai[0]) || !Owner.active || !NPC.AnyNPCs(ModContent.NPCType<CrabulonIdle>()))
            {
                npc.active = false;
                npc.netUpdate = true;
                return;
            }

            npc.target = Owner.target;
            float hoverSpeed = MathHelper.Lerp(7f, 11f, 1f - MainBossLifeRatio);
            if (BossRushEvent.BossRushActive)
                hoverSpeed *= 2.15f;
            HomeTowardsTarget(hoverSpeed);

            if (Main.netMode != NetmodeID.MultiplayerClient && MainBossLifeRatio < CrabulonBehaviorOverride.Phase3LifeRatio && Time % 90f == 89f)
            {
                ReleaseSpores();
                npc.netUpdate = true;
            }

            Time++;
        }

        public void HomeTowardsTarget(float hoverSpeed)
        {
            // Home more quickly if close to the target.
            // However, if really close to the target, stop homing and simply go in the
            // current direction.
            Vector2 hoverDestination = (Target.Center + (npc.whoAmI * 10.81f).ToRotationVector2() * 24f);
            if (!npc.WithinRange(hoverDestination, 180f))
                npc.velocity = (npc.velocity * 115f + npc.SafeDirectionTo(hoverDestination) * hoverSpeed) / 116f;
            else if (!npc.WithinRange(hoverDestination, 90f))
                npc.velocity = (npc.velocity * 90f + npc.SafeDirectionTo(hoverDestination) * hoverSpeed * 0.8f) / 91f;
        }

        public void ReleaseSpores()
        {
            int spore = Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY.RotatedBy(-0.45f) * -6f, ModContent.ProjectileType<HomingSpore>(), 45, 0f);
            Main.projectile[spore].ai[0] = Utils.InverseLerp(0.45f, 0.1f, MainBossLifeRatio);

            spore = Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY.RotatedBy(0.45f) * -6f, ModContent.ProjectileType<HomingSpore>(), 45, 0f);
            Main.projectile[spore].ai[0] = Utils.InverseLerp(0.45f, 0.1f, MainBossLifeRatio);
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
        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            Texture2D texture = ModContent.GetTexture(Texture);
            Texture2D glowmask = ModContent.GetTexture("InfernumMode/BehaviorOverrides/BossAIs/Crabulon/FungalClump_Glowmask");
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Vector2 origin = npc.frame.Size() * 0.5f;
            Color color = npc.GetAlpha(drawColor);
            spriteBatch.Draw(texture, drawPosition, npc.frame, color, npc.rotation, origin, npc.scale, 0, 0f);
            spriteBatch.Draw(glowmask, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, origin, npc.scale, 0, 0f);
            return false;
        }

        public override bool CheckActive() => false;
    }
}
