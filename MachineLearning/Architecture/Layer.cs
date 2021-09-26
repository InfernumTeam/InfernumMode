using InfernumMode.MachineLearning.Optimizers;
using System;
using Terraria;

namespace InfernumMode.MachineLearning.Architecture
{
    [Serializable]
    public class Layer
    {
        public double LearningRate;
        public double[] Inputs;
        public double[,] Weights;
        public double[] Biases;
        public double[] Outputs;
        public double[] Deltas;
        public Func<double, double> ActivationFunction;
        public Func<double, double, double> CostFunction;

        public Layer(int inputCount, int neuronCount, double learningRate, Func<double, double> activationFunction, Func<double, double, double> costFunction)
        {
            LearningRate = learningRate;
            Inputs = new double[inputCount];
            Weights = Main.rand.GenerateRandomArray(neuronCount, inputCount, - 0.3, 0.3);
            Biases = new double[neuronCount];
            Outputs = new double[neuronCount];
            Deltas = new double[neuronCount];
            ActivationFunction = activationFunction;
            CostFunction = costFunction;

            UpdateOutputs();
        }

        /// <summary>
        /// Updates the outputs of the layer, allowing optional overriding of the outputs.
        /// </summary>
        /// <param name="newInputs">The new inputs to send. This does not need to be inputted.</param>
        public void UpdateOutputs(double[] newInputs = null)
        {
            // Attempt to change the inputs if necessary.
            if (newInputs != null)
            {
                if (newInputs.Length != Inputs.Length)
                    throw new InvalidOperationException("Input sizes do not match those already established by the layer.");
                Inputs = newInputs;
            }

            // Compute outputs.
            for (int i = 0; i < Outputs.Length; i++)
            {
                double newOutput = 0D;
                for (int j = 0; j < Inputs.Length; j++)
                    newOutput += Inputs[j] * Weights[i, j];
                newOutput += Biases[i];

                Outputs[i] = ActivationFunction(newOutput);
            }
        }

        /// <summary>
        /// Updates weights and biases of the network based on a list of expected values.
        /// </summary>
        /// <param name="expectedValues">The list of expected values.</param>
        /// <param name="optimizer">An optional optimizer to use.</param>
        public void Backpropagate(double[] expectedValues, BaseOptimizer optimizer = null)
        {
            for (int i = 0; i < Outputs.Length; i++)
            {
                Deltas[i] = CostFunction.ApproximatePartialDerivative(Outputs[i], expectedValues[i], 0) * ActivationFunction.ApproximateDerivative(Outputs[i]);
                double[] updatesValues = optimizer?.DecideUpdateValues(Deltas, LearningRate) ?? Deltas;

                for (int j = 0; j < Weights.GetLength(1); j++)
                    Weights[i, j] -= updatesValues[i] * Inputs[j];

                Biases[i] -= updatesValues[i];
            }
        }

        /// <summary>
        /// Updates weights and biases of the network based on an ahead layer.
        /// </summary>
        /// <param name="aheadLayer">The ahead layer.</param>
        /// <param name="optimizer">An optional optimizer to use.</param>
        public void Backpropagate(Layer aheadLayer, BaseOptimizer optimizer = null)
        {
            for (int i = 0; i < Outputs.Length; i++)
            {
                double delta = 0D;
                for (int j = 0; j < Weights.GetLength(1); j++)
                {
                    for (int k = 0; k < aheadLayer.Deltas.Length; k++)
                        delta += Weights[i, j] * aheadLayer.Deltas[k];
                }

                Deltas[i] = delta * ActivationFunction.ApproximateDerivative(Outputs[i]);
                double[] updatesValues = optimizer?.DecideUpdateValues(Deltas, LearningRate) ?? Deltas;

                for (int j = 0; j < Weights.GetLength(1); j++)
                    Weights[i, j] -= updatesValues[i] * Inputs[j];

                Biases[i] -= updatesValues[i];
            }
        }
    }
}
