using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class EnergyFieldLaserProjector : ModNPC
    {
        public int SpinDirection = 1;
        public float MoveIncrement = 0;
        public Vector2 InitialTargetPosition;
        public int LaserAttackTime => 180;
        public float CloseInInterpolant => Utils.GetLerpValue(LaserAttackTime - 60f, LaserAttackTime, AttackTimer, true);
        public Player Target => Main.player[NPC.target];
        public ref float AttackTimer => ref NPC.ai[0];
        public ref float NextProjectorIndex => ref NPC.ai[1];
        public ref float OffsetDirection => ref NPC.ai[2];
        public ref float MoveOffset => ref NPC.ai[3];

        public override string Texture => "InfernumMode/BehaviorOverrides/BossAIs/AdultEidolonWyrm/PsychicEnergyField";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Energy Field");
        }

        public override void SetDefaults()
        {
            NPC.damage = 0;
            NPC.npcSlots = 0f;
            NPC.width = NPC.height = 16;
            NPC.defense = 15;
            NPC.lifeMax = 5000;
            NPC.aiStyle = aiType = -1;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.dontTakeDamage = true;
        }

        public override void AI()
        {
            Lighting.AddLight(NPC.Center, 0.03f, 0.2f, 0.2f);

            // Handle despawn stuff.
            if (!Target.active || Target.dead)
            {
                NPC.TargetClosest(false);
                if (!Target.active || Target.dead)
                {
                    if (NPC.timeLeft > 10)
                        NPC.timeLeft = 10;
                    return;
                }
            }
            else if (NPC.timeLeft > 600)
                NPC.timeLeft = 600;

            if (InitialTargetPosition == Vector2.Zero)
                InitialTargetPosition = Target.Center + Target.velocity;

            MoveOffset = MathHelper.Lerp(0f, MoveIncrement * 100f + 1300f, 1f - CloseInInterpolant);
            NPC.Center = InitialTargetPosition + OffsetDirection.ToRotationVector2() * MoveOffset;

            if (AttackTimer == 0f)
            {
                SoundEngine.PlaySound(SoundLoader.GetLegacySoundSlot(InfernumMode.CalamityMod, "Sounds/Item/MechGaussRifle"), NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 laserDirection = NPC.SafeDirectionTo(Main.npc[(int)NextProjectorIndex].Center, Vector2.UnitY);
                    int laser = Utilities.NewProjectileBetter(NPC.Center, laserDirection, ModContent.ProjectileType<EnergyFieldDeathray>(), 1000, 0f);
                    if (Main.projectile.IndexInRange(laser))
                    {
                        Main.projectile[laser].ModProjectile<EnergyFieldDeathray>().LocalLifetime = 1200;
                        Main.projectile[laser].ai[1] = NPC.whoAmI;
                    }
                }
            }
            AttackTimer++;

            // Explode if enough time has passed.
            if (AttackTimer >= LaserAttackTime)
            {
                NPC.life = 0;
                NPC.checkDead();
                NPC.active = false;
            }
        }

        public override bool PreNPCLoot() => false;

        public override bool CheckDead()
        {
            SoundEngine.PlaySound(SoundID.DD2_KoboldExplosion, NPC.position);

            NPC.position = NPC.Center;
            NPC.width = NPC.height = 84;
            NPC.Center = NPC.position;

            for (int i = 0; i < 15; i++)
            {
                Dust waterMagic = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 267, 0f, 0f, 100, default, 1.4f);
                if (Main.rand.NextBool(2))
                {
                    waterMagic.scale = 0.5f;
                    waterMagic.fadeIn = Main.rand.NextFloat(1f, 2f);
                }
                waterMagic.color = Color.Lerp(Color.Cyan, Color.SkyBlue, Main.rand.NextFloat());
                waterMagic.velocity *= 3f;
                waterMagic.noGravity = true;
            }

            for (int i = 0; i < 30; i++)
            {
                Dust waterMagic = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 267, 0f, 0f, 100, default, 1.85f);
                waterMagic.color = Color.Lerp(Color.Cyan, Color.SkyBlue, Main.rand.NextFloat());
                waterMagic.noGravity = true;
                waterMagic.velocity *= 5f;

                waterMagic = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, 267, 0f, 0f, 100, default, 2f);
                waterMagic.color = Color.Lerp(Color.Cyan, Color.SkyBlue, Main.rand.NextFloat());
                waterMagic.velocity *= 2f;
                waterMagic.noGravity = true;
            }
            return true;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            Main.spriteBatch.EnterShaderRegion();
            Texture2D noiseTexture = TextureAssets.Npc[npc.type].Value;
            Vector2 drawPosition2 = NPC.Center - Main.screenPosition;
            Vector2 origin = noiseTexture.Size() * 0.5f;
            GameShaders.Misc["Infernum:AEWPsychicEnergy"].UseOpacity(NPC.Opacity);
            GameShaders.Misc["Infernum:AEWPsychicEnergy"].UseColor(Color.Cyan);
            GameShaders.Misc["Infernum:AEWPsychicEnergy"].UseSecondaryColor(Color.Lerp(Color.Purple, Color.Black, 0.25f));
            GameShaders.Misc["Infernum:AEWPsychicEnergy"].Apply();

            Main.spriteBatch.Draw(noiseTexture, drawPosition2, null, Color.White, 0f, origin, 0.4f, SpriteEffects.None, 0f);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
