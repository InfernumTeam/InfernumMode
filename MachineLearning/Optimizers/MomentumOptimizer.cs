using System;

namespace InfernumMode.MachineLearning.Optimizers
{
    [Serializable]
    public class MomentumOptimizer : BaseOptimizer
    {
        public double Momentum;
        public double[] PreviousValues = null;

        public MomentumOptimizer(double momentum) => Momentum = momentum;

        public override double[] DecideUpdateValues(double[] updateValues, double learningRate)
        {
            if (PreviousValues is null)
            {
                PreviousValues = updateValues;
                return updateValues;
            }

            for (int i = 0; i < updateValues.Length; i++)
                updateValues[i] = Momentum * PreviousValues[i] + learningRate * updateValues[i];

            PreviousValues = updateValues;
            return updateValues;
        }
    }
}
