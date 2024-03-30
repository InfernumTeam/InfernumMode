using InfernumMode.Assets.Effects;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Dragonfolly
{
    public class LightningSuperchargeTelegraph : ModProjectile
    {
        public Vector2[] ChargePositions = new Vector2[1];
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public NPC ThingToAttachTo => Main.npc.IndexInRange((int)Projectile.ai[1]) ? Main.npc[(int)Projectile.ai[1]] : null;

        public const int Lifetime = 60;

        public const float TelegraphFadeTime = 15f;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Charge Telegraph");
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(ChargePositions.Length);
            for (int i = 0; i < ChargePositions.Length; i++)
                writer.WriteVector2(ChargePositions[i]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            ChargePositions = new Vector2[reader.ReadInt32()];
            for (int i = 0; i < ChargePositions.Length; i++)
                ChargePositions[i] = reader.ReadVector2();
        }

        public override void AI()
        {
            // Determine the relative opacities for each player based on their distance.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.netUpdate = true;
            }

            // Die if the thing to attach to disappears.
            if (ThingToAttachTo is null || !ThingToAttachTo.active)
            {
                Projectile.Kill();
                return;
            }

            // Determine opacity.
            Projectile.Opacity = Utils.GetLerpValue(0f, 6f, Projectile.timeLeft, true) * Utils.GetLerpValue(Lifetime, Lifetime - 6f, Projectile.timeLeft, true);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 255, 255, Projectile.alpha);
        }

        public Color TelegraphPrimitiveColor(float completionRatio)
        {
            float opacity = Lerp(0.38f, 1.2f, Projectile.Opacity);
            opacity *= LumUtils.Convert01To010(completionRatio);
            opacity *= Lerp(0.9f, 0.2f, Projectile.ai[0] / (ChargePositions.Length - 1f));
            if (completionRatio > 0.95f)
                opacity = 0.0000001f;
            return Color.Red * opacity;
        }

        public float TelegraphPrimitiveWidth(float completionRatio)
        {
            return Projectile.Opacity * 15f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var flame = InfernumEffectsRegistry.FlameVertexShader;
            flame.TrySetParameter("uSaturation", 0.36f);
            Main.instance.GraphicsDevice.Textures[1] = ModContent.Request<Texture2D>("Terraria/Images/Misc/Perlin").Value;

            for (int i = ChargePositions.Length - 2; i >= 0; i--)
            {
                Vector2[] positions =
                [
                    ChargePositions[i],
                    ChargePositions[i + 1]
                ];

                // Stand-in variable used to differentiate between the beams.
                // It is not used anywhere else.
                Projectile.ai[0] = i;

                PrimitiveRenderer.RenderTrail(positions, new(TelegraphPrimitiveWidth, TelegraphPrimitiveColor, _ => Projectile.Size * 0.5f, false, Shader: flame), 55);
            }
            return false;
        }
    }
}
