using System;

namespace InfernumMode.MachineLearning.Optimizers
{
    [Serializable]
    public class AdamOptimizer : BaseOptimizer
    {
        public double Beta1;
        public double Beta2;
        public double[] PreviousValuesFirstMoment = null;
        public double[] PreviousValuesSecondMoment = null;

        public AdamOptimizer(double beta1, double beta2)
        {
            Beta1 = beta1;
            Beta2 = beta2;
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
                PreviousValuesFirstMoment[i] = Beta1 * PreviousValuesFirstMoment[i] + (1D - Beta1) * updateValues[i];
                PreviousValuesSecondMoment[i] = Beta2 * PreviousValuesSecondMoment[i] + (1D - Beta2) * updateValues[i] * updateValues[i];

                double biasCorrectedFirstMoment = PreviousValuesFirstMoment[i] / (1D - Beta1);
                double biasCorrectedSecondMoment = PreviousValuesSecondMoment[i] / (1D - Beta2);
                updateValues[i] = learningRate / (Math.Sqrt(biasCorrectedSecondMoment) + 1e-8) * biasCorrectedFirstMoment;
            }

            return updateValues;
        }
    }
}
