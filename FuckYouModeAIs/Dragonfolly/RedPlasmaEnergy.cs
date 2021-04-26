using CalamityMod.NPCs.Bumblebirb;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.FuckYouModeAIs.Dragonfolly
{
	public class RedPlasmaEnergy : ModNPC
    {
        public Player Target => Main.player[npc.target];
        public ref float Time => ref npc.ai[0];
        public ref float DestinationXOffset => ref npc.ai[1];
        public bool HasReachedDestination
        {
            get => npc.ai[2] == 1f;
            set => npc.ai[2] = value.ToInt();
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Plasma Orb");
			NPCID.Sets.TrailingMode[npc.type] = 0;
            NPCID.Sets.TrailCacheLength[npc.type] = 7;
        }

        public override void SetDefaults()
        {
			npc.npcSlots = 1f;
            npc.aiStyle = aiType = -1;
            npc.width = npc.height = 22;
            npc.damage = 185;
            npc.lifeMax = 900;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.netAlways = true;
            npc.dontTakeDamage = true;
        }

		public override void AI()
        {
            npc.TargetClosest(true);
            Lighting.AddLight(npc.Center, Color.White.ToVector3());

            if (!NPC.AnyNPCs(ModContent.NPCType<Bumblefuck>()) || npc.Opacity <= 0.01f)
            {
                npc.active = false;
                npc.netUpdate = true;
                return;
            }

            if (Time % 300f < 240f)
            {
                if (!npc.WithinRange(Target.Center, 300f))
                    npc.velocity = (npc.velocity * 26f + npc.SafeDirectionTo(Target.Center) * 22f) / 27f;
                else if (npc.velocity.Length() > 9f)
                    npc.velocity *= 0.965f;
                npc.damage = 0;
                npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.05f, 0.2f, 1f);
            }
            else
            {
                npc.damage = npc.defDamage;
                if (Main.netMode != NetmodeID.MultiplayerClient && Time % 300f == 240f)
                {
                    DestinationXOffset = Target.velocity.X * 18f;
                    npc.netUpdate = true;
                }

                if (!HasReachedDestination)
                {
                    Vector2 destination = Target.Center + new Vector2(DestinationXOffset, -400f);
                    npc.velocity = (npc.velocity * 11f + npc.SafeDirectionTo(destination) * 24f) / 12f;
                    npc.Center += npc.SafeDirectionTo(destination) * 6f;
                    if (npc.WithinRange(Target.Center, 250f))
                        npc.Center -= npc.SafeDirectionTo(Target.Center) * 4f;

                    if (Main.netMode != NetmodeID.MultiplayerClient && npc.WithinRange(destination, 40f))
                    {
                        npc.velocity = npc.SafeDirectionTo(Target.Center);
                        npc.velocity.X *= 0.4f;
                        npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * 8f;
                        HasReachedDestination = true;
                        npc.netUpdate = true;
                    }
                    Time--;
                }
                else
                    npc.velocity *= 1.018f;
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.08f, 0.2f, 1f);
            }

            // Idly emit red electric dust.
            Dust redElectricity = Dust.NewDustPerfect(npc.Center + Main.rand.NextVector2CircularEdge(5f, 5f), 267);
            redElectricity.velocity = npc.velocity + Main.rand.NextVector2Circular(1.8f, 1.8f);
            redElectricity.color = Color.Lerp(Color.White, Color.Red, Main.rand.NextFloat(0.4f, 1f));
            redElectricity.scale = Main.rand.NextFloat(0.8f, 1.2f);
            redElectricity.noGravity = true;

            npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Max(0.5f, npc.velocity.Length());
            npc.Opacity *= 1f - Utils.InverseLerp(550f, 610f, Time, true);
            Time++;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            drawColor = Color.Red * npc.Opacity * 0.2f;
            drawColor.A = 0;
            Texture2D energyTexture = Main.npcTexture[npc.type];
            for (int i = 0; i < 7; i++)
            {
                Vector2 drawPosition = npc.Center + (MathHelper.TwoPi * i / 7f + Main.GlobalTime * 4.1f).ToRotationVector2() * 4f - Main.screenPosition;
                spriteBatch.Draw(energyTexture, drawPosition, null, drawColor, npc.rotation, energyTexture.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) => target.AddBuff(BuffID.Electrified, 120);

        public override bool CheckActive() => false;
    }
}
