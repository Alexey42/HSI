using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace HSI
{
    class VectorOperations
    {
        public int numOfVectors = 2;

        public float Mean(float[] arr, int n)
        {
            float sum = 0;

            for (int i = 0; i < n; i++)
                sum = sum + arr[i];

            return sum / n;
        }

        public float Covariance(float[] arr1, float[] arr2, int n)
        {
            float sum = 0;

            for (int i = 0; i < n; i++)
                sum = sum + (arr1[i] - Mean(arr1, n)) * (arr2[i] - Mean(arr2, n));

            return sum / (n - 1);
        }

        public float[,] CovarianceMatrix(float[] arr1, float[] arr2, int n)
        {
            float[,] res = new float[n, n];

            res[0, 0] = Covariance(arr1, arr1, n);
            res[0, 1] = Covariance(arr1, arr2, n);
            res[1, 0] = Covariance(arr2, arr1, n);
            res[1, 1] = Covariance(arr2, arr2, n);

            return res;
        }

        public float[] SubstractVectors(float[] arr1, float[] arr2, int n)
        {
            float[] res = new float[n];

            for (int i = 0; i < n; i++)
                res[i] = arr1[i] - arr2[i];

            return res;
        }

        public float[,] TransposeMatrix(float[,] mat)
        {
            int size = numOfVectors;
            float[,] res = new float[size, size];
            
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    res[j, i] = mat[i, j];
                }
            }

            return res;
        }

        float[,] MatrixDuplicate(float[,] matrix)
        {
            int size = numOfVectors;
            float[,] result = new float[size, size];
            for (int i = 0; i < size; ++i) // copy the values
                for (int j = 0; j < size; ++j)
                    result[i, j] = matrix[i, j];
            return result;
        }

        float[] HelperSolve(float[,] luMatrix, float[] b)
        {
            // before calling this helper, permute b using the perm array
            // from MatrixDecompose that generated luMatrix
            int n = numOfVectors;
            float[] x = new float[n];
            b.CopyTo(x, 0);

            for (int i = 1; i < n; ++i)
            {
                float sum = x[i];
                for (int j = 0; j < i; ++j)
                    sum -= luMatrix[i, j] * x[j];
                x[i] = sum;
            }

            x[n - 1] /= luMatrix[n - 1, n - 1];
            for (int i = n - 2; i >= 0; --i)
            {
                float sum = x[i];
                for (int j = i + 1; j < n; ++j)
                    sum -= luMatrix[i, j] * x[j];
                x[i] = sum / luMatrix[i, i];
            }

            return x;
        }

        float[,] MatrixDecompose(float[,] matrix, out int[] perm, out int toggle)
        {
            // Doolittle LUP decomposition with partial pivoting.
            // rerturns: result is L (with 1s on diagonal) and U;
            // perm holds row permutations; toggle is +1 or -1 (even or odd)
            int rows = numOfVectors;
            int cols = numOfVectors; // assume square
            if (rows != cols)
                throw new Exception("Attempt to decompose a non-square m");

            int n = rows; // convenience

            float[,] result = MatrixDuplicate(matrix);

            perm = new int[n]; // set up row permutation result
            for (int i = 0; i < n; ++i) { perm[i] = i; }

            toggle = 1; // toggle tracks row swaps.
                        // +1 -greater-than even, -1 -greater-than odd. used by MatrixDeterminant

            for (int j = 0; j < n - 1; ++j) // each column
            {
                float colMax = Math.Abs(result[j, j]); // find largest val in col
                int pRow = j;
                //for (int i = j + 1; i less-than n; ++i)
                //{
                //  if (result[i][j] greater-than colMax)
                //  {
                //    colMax = result[i][j];
                //    pRow = i;
                //  }
                //}

                // reader Matt V needed this:
                for (int i = j + 1; i < n; ++i)
                {
                    if (Math.Abs(result[i, j]) > colMax)
                    {
                        colMax = Math.Abs(result[i, j]);
                        pRow = i;
                    }
                }
                // Not sure if this approach is needed always, or not.

                if (pRow != j) // if largest value not on pivot, swap rows
                {
                    float[] rowPtr = new float[numOfVectors];
                    for (int k = 0; k < numOfVectors; k++)
                        rowPtr[k] = result[pRow, k];
                    for (int k = 0; k < numOfVectors; k++)
                        result[pRow, k] = result[j, k];
                    for (int k = 0; k < numOfVectors; k++)
                        result[j, k] = rowPtr[k];                  

                    int tmp = perm[pRow]; // and swap perm info
                    perm[pRow] = perm[j];
                    perm[j] = tmp;

                    toggle = -toggle; // adjust the row-swap toggle
                }

                // --------------------------------------------------
                // This part added later (not in original)
                // and replaces the 'return null' below.
                // if there is a 0 on the diagonal, find a good row
                // from i = j+1 down that doesn't have
                // a 0 in column j, and swap that good row with row j
                // --------------------------------------------------

                if (result[j, j] == 0.0)
                {
                    // find a good row to swap
                    int goodRow = -1;
                    for (int row = j + 1; row < n; ++row)
                    {
                        if (result[row, j] != 0.0)
                            goodRow = row;
                    }

                    if (goodRow == -1)
                        throw new Exception("Cannot use Doolittle's method");

                    // swap rows so 0.0 no longer on diagonal
                    float[] rowPtr = new float[numOfVectors];
                    for (int k = 0; k < numOfVectors; k++)
                        rowPtr[k] = result[goodRow, k];
                    for (int k = 0; k < numOfVectors; k++)
                        result[goodRow, k] = result[j, k];
                    for (int k = 0; k < numOfVectors; k++)
                        result[j, k] = rowPtr[k];                 

                    int tmp = perm[goodRow]; // and swap perm info
                    perm[goodRow] = perm[j];
                    perm[j] = tmp;

                    toggle = -toggle; // adjust the row-swap toggle
                }
                // --------------------------------------------------
                // if diagonal after swap is zero . .
                //if (Math.Abs(result[j][j]) less-than 1.0E-20) 
                //  return null; // consider a throw

                for (int i = j + 1; i < n; ++i)
                {
                    result[i, j] /= result[j, j];
                    for (int k = j + 1; k < n; ++k)
                    {
                        result[i, k] -= result[i, j] * result[j, k];
                    }
                }


            } // main j column loop

            return result;
        }

        float[,] MatrixInverse(float[,] matrix)
        {
            int n = matrix.Length;
            float[,] result = MatrixDuplicate(matrix);

            int[] perm;
            int toggle;
            float[,] lum = MatrixDecompose(matrix, out perm,
              out toggle);
            if (lum == null)
                throw new Exception("Unable to compute inverse");

            float[] b = new float[n];
            for (int i = 0; i < n; ++i)
            {
                for (int j = 0; j < n; ++j)
                {
                    if (i == perm[j])
                        b[j] = 1.0f;
                    else
                        b[j] = 0.0f;
                }

                float[] x = HelperSolve(lum, b);

                for (int j = 0; j < n; ++j)
                    result[j, i] = x[j];
            }
            return result;
        }

        public float MahalanobisDistance(float[] arr1, float[] arr2)
        {
            int n = arr1.Length;
            float[,] cov = CovarianceMatrix(arr1, arr2, n);
            var ic = MatrixInverse(cov);
            float res = 0;


            return res;
        }
    }   
}
