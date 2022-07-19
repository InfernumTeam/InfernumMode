using CalamityMod;
using CalamityMod.NPCs.Bumblebirb;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.Dragonfolly
{
    public class RedPlasmaEnergy : ModNPC
    {
        public Player Target => Main.player[NPC.target];
        public ref float Time => ref NPC.ai[0];
        public ref float DestinationXOffset => ref NPC.ai[1];
        public bool HasReachedDestination
        {
            get => NPC.ai[2] == 1f;
            set => NPC.ai[2] = value.ToInt();
        }
        public override void SetStaticDefaults()
        {
            this.HideFromBestiary();
            DisplayName.SetDefault("Plasma Orb");
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.TrailCacheLength[NPC.type] = 7;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 1f;
            NPC.aiStyle = AIType = -1;
            NPC.width = NPC.height = 22;
            NPC.damage = 164;
            NPC.lifeMax = 900;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.netAlways = true;
            NPC.dontTakeDamage = true;
        }

        public override void AI()
        {
            NPC.TargetClosest(true);
            Lighting.AddLight(NPC.Center, Color.White.ToVector3());

            if (!NPC.AnyNPCs(ModContent.NPCType<Bumblefuck>()) || NPC.Opacity <= 0.01f)
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            if (Time % 240f < 180f)
            {
                if (!NPC.WithinRange(Target.Center, 300f))
                    NPC.velocity = (NPC.velocity * 26f + NPC.SafeDirectionTo(Target.Center) * 22f) / 27f;
                else if (NPC.velocity.Length() > 9f)
                    NPC.velocity *= 0.965f;
                NPC.damage = 0;
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity - 0.05f, 0.2f, 1f);
            }
            else
            {
                NPC.damage = HasReachedDestination ? NPC.defDamage : 0;
                if (Main.netMode != NetmodeID.MultiplayerClient && Time % 240f == 180f)
                {
                    DestinationXOffset = Target.velocity.X * 18f;
                    NPC.netUpdate = true;
                }

                if (!HasReachedDestination)
                {
                    Vector2 destination = Target.Center + new Vector2(DestinationXOffset, -400f);
                    NPC.velocity = (NPC.velocity * 11f + NPC.SafeDirectionTo(destination) * 24f) / 12f;
                    NPC.Center += NPC.SafeDirectionTo(destination) * 6f;
                    if (NPC.WithinRange(Target.Center, 250f))
                        NPC.Center -= NPC.SafeDirectionTo(Target.Center) * 4f;

                    if (Main.netMode != NetmodeID.MultiplayerClient && NPC.WithinRange(destination, 40f))
                    {
                        NPC.velocity = NPC.SafeDirectionTo(Target.Center);
                        NPC.velocity.X *= 0.4f;
                        NPC.velocity = NPC.velocity.SafeNormalize(Vector2.UnitY) * 10f;
                        HasReachedDestination = true;
                        NPC.netUpdate = true;
                    }
                    Time--;
                }
                else
                    NPC.velocity *= 1.018f;
                NPC.Opacity = MathHelper.Clamp(NPC.Opacity + 0.08f, 0.2f, 1f);
            }

            // Idly emit red electric dust.
            Dust redElectricity = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2CircularEdge(5f, 5f), 267);
            redElectricity.velocity = NPC.velocity + Main.rand.NextVector2Circular(1.8f, 1.8f);
            redElectricity.color = Color.Lerp(Color.White, Color.Red, Main.rand.NextFloat(0.4f, 1f));
            redElectricity.scale = Main.rand.NextFloat(0.8f, 1.2f);
            redElectricity.noGravity = true;

            NPC.velocity = NPC.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Max(0.5f, NPC.velocity.Length());
            NPC.Opacity *= 1f - Utils.GetLerpValue(550f, 610f, Time, true);
            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            drawColor = Color.Red * NPC.Opacity * 0.8f;
            drawColor.A = 0;
            Texture2D energyTexture = TextureAssets.Npc[NPC.type].Value;
            for (int i = 0; i < 12; i++)
            {
                Vector2 drawPosition = NPC.Center + (MathHelper.TwoPi * i / 12f + Main.GlobalTimeWrappedHourly * 4.1f).ToRotationVector2() * 4f - Main.screenPosition;
                Main.spriteBatch.Draw(energyTexture, drawPosition, null, drawColor, NPC.rotation, energyTexture.Size() * 0.5f, NPC.scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(BuffID.Electrified, 120);

        public override bool CheckActive() => false;
    }
}
