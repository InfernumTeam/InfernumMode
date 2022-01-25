using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace InfernumMode.MachineLearning
{
    public class MLAttackSelector
    {
        public float[] Weights;
        public string Name;
        public MLAttackSelector(int totalPossibilities, string name)
        {
            Weights = new float[totalPossibilities];
            for (int i = 0; i < Weights.Length; i++)
                Weights[i] = 0.5f;
            Name = name;
        }

        public void BiasInFavorOf(int index, float biasFactor = 0.1f)
        {
            for (int i = 0; i < Weights.Length; i++)
            {
                Weights[i] = MathHelper.Lerp(Weights[i], i == index ? 1f : 0f, biasFactor);
                Weights[i] = MathHelper.Clamp(Weights[i], 0.005f, 0.995f);
            }
        }

        public void BiasAwayFrom(int index, float biasFactor = 0.056f)
        {
            for (int i = 0; i < Weights.Length; i++)
            {
                Weights[i] = MathHelper.Lerp(Weights[i], i == index ? 0f : 0.5f, biasFactor);
                Weights[i] = MathHelper.Clamp(Weights[i], 0.005f, 0.995f);
            }
        }

        public int MakeSelection()
        {
            WeightedRandom<int> rng = new WeightedRandom<int>(Main.rand);
            for (int i = 0; i < Weights.Length; i++)
                rng.Add(i, Weights[i]);

            return rng.Get();
        }

        public static MLAttackSelector Load(TagCompound tag, string name)
        {
            if (!tag.ContainsKey($"SelectorWeights_{name}"))
                return null;

            byte[] weightBytes = (byte[])tag[$"SelectorWeights_{name}"];
            float[] weights = new float[weightBytes.Length / 4];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = BitConverter.ToSingle(weightBytes, i * 4);

            return new MLAttackSelector(1, name)
            {
                Weights = weights
            };
        }

        public void Save(TagCompound tag)
        {
            byte[] weightByteArray = new byte[Weights.Length * 4];
            for (int i = 0; i < Weights.Length; i++)
            {
                byte[] weightBytes = BitConverter.GetBytes(Weights[i]);
                for (int j = 0; j < 4; j++)
                    weightByteArray[i * 4 + j] = weightBytes[j];
            }
            tag.Add($"SelectorWeights_{Name}", weightByteArray);
        }
    }
}