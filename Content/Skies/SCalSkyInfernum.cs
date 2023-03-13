using CalamityMod.Events;
using CalamityMod.NPCs;
using CalamityMod.NPCs.SupremeCalamitas;
using InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ModLoader;

namespace InfernumMode.Content.Skies
{
    public class SCalSkyScene : ModSceneEffect
    {
        public override SceneEffectPriority Priority => (SceneEffectPriority)9;

        public override bool IsSceneEffectActive(Player player) => !Main.gameMenu && NPC.AnyNPCs(ModContent.NPCType<SupremeCalamitas>()) && InfernumMode.CanUseCustomAIs;

        public override void SpecialVisuals(Player player, bool isActive)
        {
            player.ManageSpecialBiomeVisuals("InfernumMode:SCal", isActive);

            if (isActive)
                SkyManager.Instance["InfernumMode:SCal"].Activate(Main.LocalPlayer.Center);
            else
                SkyManager.Instance["InfernumMode:SCal"].Deactivate();
        }
    }

    public class SCalSkyInfernum : CustomSky
    {
        public class Cinder
        {
            public int Time;
            public int Lifetime;
            public int IdentityIndex;
            public float Scale;
            public float Depth;
            public Color DrawColor;
            public Vector2 Velocity;
            public Vector2 Center;
            public Cinder(int lifetime, int identity, float depth, Color color, Vector2 startingPosition, Vector2 startingVelocity)
            {
                Lifetime = lifetime;
                IdentityIndex = identity;
                Depth = depth;
                DrawColor = color;
                Center = startingPosition;
                Velocity = startingVelocity;
            }
        }

        private bool isActive = false;

        private float intensity = 0f;

        private int SCalIndex = -1;

        public List<Cinder> Cinders = new();

        public static bool RitualDramaProjectileIsPresent
        {
            get;
            internal set;
        }

        public static int CinderReleaseChance
        {
            get
            {
                if (!Main.npc.IndexInRange(CalamityGlobalNPC.SCal) || Main.npc[CalamityGlobalNPC.SCal].type != ModContent.NPCType<SupremeCalamitas>())
                    return int.MaxValue;

                return 9;
            }
        }
        public static float CinderSpeed
        {
            get
            {
                if (!Main.npc.IndexInRange(CalamityGlobalNPC.SCal) || Main.npc[CalamityGlobalNPC.SCal].type != ModContent.NPCType<SupremeCalamitas>())
                    return 0f;

                // Move a little quickly while brothers or Sepulcher are alive.
                if (NPC.AnyNPCs(ModContent.NPCType<SupremeCataclysm>()) || NPC.AnyNPCs(ModContent.NPCType<SupremeCatastrophe>()) || NPC.AnyNPCs(ModContent.NPCType<SepulcherHead>()))
                    return 17f;

                // Move moderately quickly usually.
                return 12.5f;
            }
        }

        public override void Update(GameTime gameTime)
        {
            if (SCalIndex == -1)
            {
                UpdateSCalIndex();
                if (SCalIndex == -1)
                    isActive = false;
            }

            if (!Main.npc.IndexInRange(CalamityGlobalNPC.SCal) || Main.npc[CalamityGlobalNPC.SCal].type != ModContent.NPCType<SupremeCalamitas>())
                isActive = false;

            static Color selectCinderColor()
            {
                if (!Main.npc.IndexInRange(CalamityGlobalNPC.SCal) || Main.npc[CalamityGlobalNPC.SCal].type != ModContent.NPCType<SupremeCalamitas>())
                    return Color.Transparent;

                NPC scal = Main.npc[CalamityGlobalNPC.SCal];
                float lifeRatio = scal.life / (float)scal.lifeMax;
                if (lifeRatio > SupremeCalamitasBehaviorOverride.Phase3LifeRatio)
                    return Color.Lerp(Color.Red, Color.Orange, Main.rand.NextFloat(0.8f));
                else if (lifeRatio > SupremeCalamitasBehaviorOverride.Phase4LifeRatio)
                    return Color.Lerp(Color.Blue, Color.Cyan, Main.rand.NextFloat() * 0.65f);
                else if (lifeRatio > 0.01f)
                    return Color.Lerp(Color.Red, Color.Yellow, Main.rand.NextFloat(0.2f, 0.9f));
                else
                    return Color.Gray;
            }

            // Randomly add cinders.
            if (Main.rand.NextBool(CinderReleaseChance))
            {
                int lifetime = Main.rand.Next(285, 445);
                float depth = Main.rand.NextFloat(1.8f, 5f);
                Vector2 startingPosition = Main.screenPosition + new Vector2(Main.screenWidth * Main.rand.NextFloat(-0.1f, 1.1f), Main.screenHeight * 1.05f);
                Vector2 startingVelocity = -Vector2.UnitY.RotatedBy(Main.rand.NextFloat(0.93f)) * 3f;
                Cinders.Add(new Cinder(lifetime, Cinders.Count, depth, selectCinderColor(), startingPosition, startingVelocity));
            }

            // Update all cinders.
            for (int i = 0; i < Cinders.Count; i++)
            {
                Cinders[i].Scale = Utils.GetLerpValue(Cinders[i].Lifetime, Cinders[i].Lifetime / 3, Cinders[i].Time, true);
                Cinders[i].Scale *= MathHelper.Lerp(0.9f, 1.3f, Cinders[i].IdentityIndex % 6f / 6f);
                Cinders[i].Velocity = Cinders[i].Velocity.SafeNormalize(-Vector2.UnitY) * CinderSpeed;
                Cinders[i].Time++;

                Cinders[i].Center += Cinders[i].Velocity;
            }

            // Clear away all dead cinders.
            Cinders.RemoveAll(c => c.Time >= c.Lifetime);
        }

        private float GetIntensity()
        {
            if (UpdateSCalIndex())
            {
                float x = 0f;
                if (SCalIndex != -1)
                    x = Vector2.Distance(Main.player[Main.myPlayer].Center, Main.npc[this.SCalIndex].Center);
                float intensityFactor = BossRushEvent.BossRushActive ? -0.2f : 1f;

                return (1f - Utils.SmoothStep(3000f, 6000f, x)) * intensityFactor;
            }
            return 0f;
        }

        private bool UpdateSCalIndex()
        {
            int SCalType = ModContent.NPCType<SupremeCalamitas>();
            if (SCalIndex >= 0 && Main.npc[SCalIndex].active && Main.npc[SCalIndex].type == SCalType)
            {
                return true;
            }
            SCalIndex = -1;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == SCalType)
                {
                    SCalIndex = i;
                    break;
                }
            }
            return SCalIndex != -1;
        }

        public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
        {
            if (maxDepth >= 0 && minDepth < 0)
            {
                float intensity = GetIntensity();
                spriteBatch.Draw(TextureAssets.BlackTile.Value, new Rectangle(0, 0, Main.screenWidth * 2, Main.screenHeight * 2), Color.Black * intensity);
            }

            // Draw cinders.
            Texture2D cinderTexture = ModContent.Request<Texture2D>("CalamityMod/Skies/CalamitasCinder").Value;
            for (int i = 0; i < Cinders.Count; i++)
            {
                Vector2 drawPosition = Cinders[i].Center - Main.screenPosition;
                spriteBatch.Draw(cinderTexture, drawPosition, null, Cinders[i].DrawColor, 0f, cinderTexture.Size() * 0.5f, Cinders[i].Scale, SpriteEffects.None, 0f);
            }
        }

        public override float GetCloudAlpha()
        {
            return 0f;
        }

        public override void Activate(Vector2 position, params object[] args)
        {
            isActive = true;
        }

        public override void Deactivate(params object[] args)
        {
            isActive = false;
        }

        public override void Reset()
        {
            isActive = false;
        }

        public override bool IsActive()
        {
            return isActive || intensity > 0f;
        }
    }
}
