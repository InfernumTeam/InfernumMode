using System;

namespace InfernumMode.MachineLearning.Optimizers
{
    [Serializable]
    public class AdamOptimizer : BaseOptimizer
    {
        public double FirstMomentDecayFactor;
        public double SecondMomentDecayFactor;
        public double[] PreviousValuesFirstMoment = null;
        public double[] PreviousValuesSecondMoment = null;

        public AdamOptimizer(double firstMomentDecayFactor, double secondMomentDecayFactor)
        {
            FirstMomentDecayFactor = firstMomentDecayFactor;
            SecondMomentDecayFactor = secondMomentDecayFactor;
        }

        public override double[] DecideUpdateValues(double[] updateValues, double learningRate)
        {
            if (PreviousValuesFirstMoment is null)
            {
                PreviousValuesFirstMoment = new double[updateValues.Length];
                PreviousValuesSecondMoment = new double[updateValues.Length];
                return updateValues;
            }

            for (int i = 0; i < updateValues.Length; i++)
            {
                PreviousValuesFirstMoment[i] = FirstMomentDecayFactor * PreviousValuesFirstMoment[i] + (1D - FirstMomentDecayFactor) * updateValues[i];
                PreviousValuesSecondMoment[i] = SecondMomentDecayFactor * PreviousValuesSecondMoment[i] + (1D - SecondMomentDecayFactor) * updateValues[i] * updateValues[i];

                double biasCorrectedFirstMoment = PreviousValuesFirstMoment[i] / (1D - FirstMomentDecayFactor);
                double biasCorrectedSecondMoment = PreviousValuesSecondMoment[i] / (1D - SecondMomentDecayFactor);
                updateValues[i] = learningRate / (Math.Sqrt(biasCorrectedSecondMoment) + 1e-8) * biasCorrectedFirstMoment;
            }

            return updateValues;
        }
    }
}
