using CalamityMod;
using CalamityMod.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Deerclops
{
    public class LightSnuffingHand : ModNPC
    {
        public Player Target => Main.player[NPC.target];

        public ref float Timer => ref NPC.ai[0];

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.InsanityShadowHostile}";

        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            // DisplayName.SetDefault("Light Snuffing Shadow Hand");
            Main.npcFrameCount[NPC.type] = 5;
        }

        public override void SetDefaults()
        {
            NPC.damage = 78;
            NPC.npcSlots = 0f;
            NPC.width = NPC.height = 40;
            NPC.defense = 0;
            NPC.lifeMax = BossRushEvent.BossRushActive ? 62000 : 196;
            NPC.aiStyle = AIType = -1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.canGhostHeal = false;
            NPC.hide = true;
            NPC.HitSound = SoundID.NPCHit54;
            NPC.DeathSound = SoundID.NPCDeath33;
            NPC.Calamity().canBreakPlayerDefense = true;
        }

        public override void AI()
        {
            int deerclopsIndex = NPC.FindFirstNPC(NPCID.Deerclops);
            if (deerclopsIndex < 0 || Main.npc[deerclopsIndex].ai[0] != (int)DeerclopsBehaviorOverride.DeerclopsAttackState.DyingBeaconOfLight)
            {
                if (deerclopsIndex >= 0 && Main.npc[deerclopsIndex].ai[0] != (int)DeerclopsBehaviorOverride.DeerclopsAttackState.DyingBeaconOfLight && NPC.Opacity > 0f)
                {
                    NPC.Opacity -= 0.1f;
                    NPC.velocity = Vector2.Zero;
                    return;
                }

                NPC.active = false;
                return;
            }

            // Begin dying if the player is really close to the hand, preferring to attack them instead of hovering near deerclops.
            float flySpeed = 8.5f;
            Player target = Main.player[Player.FindClosest(NPC.Center, 1, 1)];
            bool chasePlayer = NPC.WithinRange(target.Center, 300f) && Main.npc[deerclopsIndex].WithinRange(target.Center, 480f);
            if (BossRushEvent.BossRushActive)
                flySpeed *= 1.75f;

            // Fade in.
            NPC.Opacity = Utils.GetLerpValue(0f, 16f, Timer, true) * Utils.GetLerpValue(-36f, 120f, NPC.life, true);
            NPC.damage = Timer >= 45f ? NPC.defDamage : 0;

            // Hover near Deerclops' eye/the player.
            Vector2 hoverDestination = DeerclopsBehaviorOverride.GetEyePosition(Main.npc[deerclopsIndex]);
            if (chasePlayer)
            {
                hoverDestination = target.Center;
                if (Main.rand.NextBool(10))
                {
                    NPC.life -= 8;
                    if (NPC.life <= 0)
                        NPC.active = false;
                }
            }

            NPC.velocity = (NPC.velocity * 29f + NPC.SafeDirectionTo(hoverDestination) * flySpeed) / 30f;
            if (NPC.velocity.Length() > 3f)
                NPC.rotation = NPC.rotation.AngleTowards(NPC.AngleTo(hoverDestination), 0.084f);
            Timer++;
        }

        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCsOverPlayers.Add(index);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = NPC.Center - Main.screenPosition;
            Vector2 origin = tex.Size() * 0.5f;

            float rotation = NPC.rotation;
            Color backglowColor = Color.DarkViolet * NPC.Opacity * 0.5f;
            for (int j = 0; j < 4; j++)
            {
                Vector2 offsetDirection = rotation.ToRotationVector2();
                double spin = Main.GlobalTimeWrappedHourly * TwoPi / 24f + TwoPi * j / 4f;
                Main.EntitySpriteDraw(tex, drawPosition + offsetDirection.RotatedBy(spin) * 6f, null, backglowColor, rotation, origin, NPC.scale, 0, 0);
            }
            Main.spriteBatch.Draw(tex, drawPosition, null, NPC.GetAlpha(Color.Black), rotation, origin, NPC.scale, 0, 0f);
            return false;
        }
    }
}
