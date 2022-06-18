using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class EnergyFieldLaserProjector : ModNPC
    {
        public int SpinDirection = 1;
        public float MoveIncrement = 0;
        public Vector2 InitialTargetPosition;
        public int LaserAttackTime => 180;
        public float CloseInInterpolant => Utils.InverseLerp(LaserAttackTime - 60f, LaserAttackTime, AttackTimer, true);
        public Player Target => Main.player[npc.target];
        public ref float AttackTimer => ref npc.ai[0];
        public ref float NextProjectorIndex => ref npc.ai[1];
        public ref float OffsetDirection => ref npc.ai[2];
        public ref float MoveOffset => ref npc.ai[3];

        public override string Texture => "InfernumMode/BehaviorOverrides/BossAIs/AdultEidolonWyrm/PsychicEnergyField";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Energy Field");
        }

        public override void SetDefaults()
        {
            npc.damage = 0;
            npc.npcSlots = 0f;
            npc.width = npc.height = 16;
            npc.defense = 15;
            npc.lifeMax = 5000;
            npc.aiStyle = aiType = -1;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.dontTakeDamage = true;
        }

        public override void AI()
        {
            Lighting.AddLight(npc.Center, 0.03f, 0.2f, 0.2f);

            // Handle despawn stuff.
            if (!Target.active || Target.dead)
            {
                npc.TargetClosest(false);
                if (!Target.active || Target.dead)
                {
                    if (npc.timeLeft > 10)
                        npc.timeLeft = 10;
                    return;
                }
            }
            else if (npc.timeLeft > 600)
                npc.timeLeft = 600;

            if (InitialTargetPosition == Vector2.Zero)
                InitialTargetPosition = Target.Center + Target.velocity;

            MoveOffset = MathHelper.Lerp(0f, MoveIncrement * 100f + 1300f, 1f - CloseInInterpolant);
            npc.Center = InitialTargetPosition + OffsetDirection.ToRotationVector2() * MoveOffset;

            if (AttackTimer == 0f)
            {
                Main.PlaySound(InfernumMode.CalamityMod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/MechGaussRifle"), npc.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 laserDirection = npc.SafeDirectionTo(Main.npc[(int)NextProjectorIndex].Center, Vector2.UnitY);
                    int laser = Utilities.NewProjectileBetter(npc.Center, laserDirection, ModContent.ProjectileType<EnergyFieldDeathray>(), 1000, 0f);
                    if (Main.projectile.IndexInRange(laser))
                    {
                        Main.projectile[laser].ModProjectile<EnergyFieldDeathray>().LocalLifetime = 1200;
                        Main.projectile[laser].ai[1] = npc.whoAmI;
                    }
                }
            }
            AttackTimer++;

            // Explode if enough time has passed.
            if (AttackTimer >= LaserAttackTime)
            {
                npc.life = 0;
                npc.checkDead();
                npc.active = false;
            }
        }

        public override bool PreNPCLoot() => false;

        public override bool CheckDead()
        {
            Main.PlaySound(SoundID.DD2_KoboldExplosion, npc.position);

            npc.position = npc.Center;
            npc.width = npc.height = 84;
            npc.Center = npc.position;

            for (int i = 0; i < 15; i++)
            {
                Dust waterMagic = Dust.NewDustDirect(npc.position, npc.width, npc.height, 267, 0f, 0f, 100, default, 1.4f);
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
                Dust waterMagic = Dust.NewDustDirect(npc.position, npc.width, npc.height, 267, 0f, 0f, 100, default, 1.85f);
                waterMagic.color = Color.Lerp(Color.Cyan, Color.SkyBlue, Main.rand.NextFloat());
                waterMagic.noGravity = true;
                waterMagic.velocity *= 5f;

                waterMagic = Dust.NewDustDirect(npc.position, npc.width, npc.height, 267, 0f, 0f, 100, default, 2f);
                waterMagic.color = Color.Lerp(Color.Cyan, Color.SkyBlue, Main.rand.NextFloat());
                waterMagic.velocity *= 2f;
                waterMagic.noGravity = true;
            }
            return true;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
        {
            spriteBatch.EnterShaderRegion();
            Texture2D noiseTexture = Main.npcTexture[npc.type];
            Vector2 drawPosition2 = npc.Center - Main.screenPosition;
            Vector2 origin = noiseTexture.Size() * 0.5f;
            GameShaders.Misc["Infernum:AEWPsychicEnergy"].UseOpacity(npc.Opacity);
            GameShaders.Misc["Infernum:AEWPsychicEnergy"].UseColor(Color.Cyan);
            GameShaders.Misc["Infernum:AEWPsychicEnergy"].UseSecondaryColor(Color.Lerp(Color.Purple, Color.Black, 0.25f));
            GameShaders.Misc["Infernum:AEWPsychicEnergy"].Apply();

            spriteBatch.Draw(noiseTexture, drawPosition2, null, Color.White, 0f, origin, 0.4f, SpriteEffects.None, 0f);
            spriteBatch.ExitShaderRegion();
            return false;
        }
    }
}
