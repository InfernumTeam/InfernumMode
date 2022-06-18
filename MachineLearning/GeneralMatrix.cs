using System;

namespace InfernumMode.MachineLearning
{
    public struct GeneralMatrix
    {
        private readonly double[,] _values;
        public int TotalRows => _values.GetLength(0);
        public int TotalColumns => _values.GetLength(1);

        public GeneralMatrix(int size) : this(size, size) { }
        public GeneralMatrix(int width, int height)
        {
            _values = new double[width, height];
        }

        public double this[int i, int j]
        {
            get => _values[i, j];
            set => _values[i, j] = value;
        }

        public double Sum
        {
            get
            {
                double sum = 0D;
                for (int i = 0; i < TotalRows; i++)
                {
                    for (int j = 0; j < TotalColumns; j++)
                        sum += this[i, j];
                }
                return sum;
            }
        }

        public double Length
        {
            get
            {
                double length = 0D;
                for (int i = 0; i < TotalRows; i++)
                {
                    for (int j = 0; j < TotalColumns; j++)
                        length += this[i, j] * this[i, j];
                }
                return Math.Sqrt(length);
            }
        }

        public static GeneralMatrix operator -(GeneralMatrix GeneralMatrix)
        {
            GeneralMatrix result = new GeneralMatrix(GeneralMatrix.TotalRows, GeneralMatrix.TotalColumns);
            for (int i = 0; i < GeneralMatrix.TotalRows; i++)
            {
                for (int j = 0; j < GeneralMatrix.TotalColumns; j++)
                    result[i, j] = GeneralMatrix[i, j] * -1D;
            }

            return GeneralMatrix;
        }

        public static GeneralMatrix operator +(double scalar, GeneralMatrix GeneralMatrix)
        {
            GeneralMatrix result = new GeneralMatrix(GeneralMatrix.TotalRows, GeneralMatrix.TotalColumns);
            for (int i = 0; i < result.TotalRows; i++)
            {
                for (int j = 0; j < result.TotalColumns; j++)
                    result[i, j] = GeneralMatrix[i, j] + scalar;
            }

            return result;
        }

        public static GeneralMatrix operator +(GeneralMatrix GeneralMatrix, double scalar) => scalar + GeneralMatrix;

        public static GeneralMatrix operator +(GeneralMatrix m1, GeneralMatrix m2)
        {
            if (m1.TotalRows != m2.TotalRows || m1.TotalColumns != m2.TotalColumns)
                throw new InvalidOperationException("Two matrices may only be added if they have equal sizes.");

            GeneralMatrix result = new GeneralMatrix(m1.TotalRows, m1.TotalColumns);
            for (int i = 0; i < result.TotalRows; i++)
            {
                for (int j = 0; j < result.TotalColumns; j++)
                    result[i, j] = m1[i, j] + m2[i, j];
            }

            return result;
        }

        public static GeneralMatrix operator -(GeneralMatrix m1, GeneralMatrix m2)
        {
            if (m1.TotalRows != m2.TotalRows || m1.TotalColumns != m2.TotalColumns)
                throw new InvalidOperationException("Two matrices may only be added if they have equal sizes.");

            GeneralMatrix result = new GeneralMatrix(m1.TotalRows, m1.TotalColumns);
            for (int i = 0; i < result.TotalRows; i++)
            {
                for (int j = 0; j < result.TotalColumns; j++)
                    result[i, j] = m1[i, j] - m2[i, j];
            }

            return result;
        }

        public static GeneralMatrix operator *(GeneralMatrix m1, GeneralMatrix m2)
        {
            // A generalized GeneralMatrix multiplication formula. Based on wikipedia's mathematical definition.
            int m1Rows = m1.TotalRows;
            int m2Cols = m2.TotalColumns;
            int m1Cols = m1.TotalColumns;

            if (m2.TotalRows != m1Cols)
                throw new InvalidOperationException("Non-matching GeneralMatrix dimensions.");

            GeneralMatrix result = new GeneralMatrix(m1Rows, m2Cols);
            for (int i = 0; i < m1Rows; i++)
            {
                for (int j = 0; j < m2Cols; j++)
                {
                    double indexValue = 0D;
                    for (int k = 0; k < m1Cols; k++)
                        indexValue += m1[i, k] * m2[k, j];

                    result[i, j] = indexValue;
                }
            }

            return result;
        }

        public static GeneralMatrix operator *(double scalar, GeneralMatrix GeneralMatrix)
        {
            GeneralMatrix result = new GeneralMatrix(GeneralMatrix.TotalRows, GeneralMatrix.TotalColumns);
            for (int i = 0; i < result.TotalRows; i++)
            {
                for (int j = 0; j < result.TotalColumns; j++)
                    result[i, j] = GeneralMatrix[i, j] * scalar;
            }

            return result;
        }

        public static GeneralMatrix operator *(GeneralMatrix GeneralMatrix, double scalar) => scalar * GeneralMatrix;

        public GeneralMatrix Transpose()
        {
            GeneralMatrix result = new GeneralMatrix(TotalColumns, TotalRows);
            for (int i = 0; i < TotalRows; i++)
            {
                for (int j = 0; j < TotalColumns; j++)
                {
                    result[j, i] = this[i, j];
                }
            }
            return result;
        }

        public GeneralMatrix ElementwiseMultiplication(GeneralMatrix otherGeneralMatrix)
        {
            if (TotalRows != otherGeneralMatrix.TotalRows || TotalColumns != otherGeneralMatrix.TotalColumns)
                throw new InvalidOperationException("Two matrices may only be multiplied element-wise if they have equal sizes.");

            GeneralMatrix result = new GeneralMatrix(TotalRows, TotalColumns);
            for (int i = 0; i < TotalRows; i++)
            {
                for (int j = 0; j < TotalColumns; j++)
                    result[i, j] = this[i, j] * otherGeneralMatrix[i, j];
            }
            return result;
        }

        public override string ToString()
        {
            string result = string.Empty;
            for (int i = 0; i < TotalRows; i++)
            {
                result += "[";
                for (int j = 0; j < TotalColumns; j++)
                {
                    result += $"{this[i, j]}";
                    if (j != TotalColumns - 1)
                        result += ", ";
                }

                result += "]\n";
            }
            return result;
        }
    }
}
