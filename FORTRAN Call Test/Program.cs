using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Security;

namespace FORTRAN_Call_Test
{
    class Program
    {
        [DllImport("FORTRANDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void FOOADD(ref int A, ref int B, out int c);
        [DllImport("FORTRANDLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern float TSAT12(float[] p, int n);
        static void Main(string[] args)
        {
            int a = 2;
            int b = 3;
            int c;
            FOOADD(ref a, ref b, out c);
            float s = TSAT12(new float[] { 2.3F, -1.1F, 12.2F, 9.1F }, 4);
            Console.WriteLine("s = " + s.ToString("G"));
            const int n = 2;
            const int nrhs = 1;
            int[] ipiv = new int[n];
            double[] A = new double[] { 2, 3, 5, -2, 1, 4, 1, 1, -4 };
            double[] B = new double[] { -5, 2, 9 };
            int lda = 3;
            int ldb = 3;
            int result = LAPACK.dgesv(n, nrhs, A, lda, ipiv, B, ldb);
            Console.ReadKey();
        }
    }

    [SuppressUnmanagedCodeSecurity]
    internal sealed unsafe class CNative
    {
        private CNative() { }

        [DllImport("mkl.dll", CallingConvention=CallingConvention.Cdecl,
            ExactSpelling=true,SetLastError=false)]
        internal static extern int LAPACKE_dgesv(
            int matrix_layout,
            int n,
            int nrhs,
            double* A,
            int lda,
            int[] ipiv,
            double* B,
            int ldb
            );
    }
    public unsafe sealed class LAPACK
    {
        public static int dgesv(int n, int nrhs,
            double[] A, int lda, int[] ipiv,
            double[] B, int ldb)
        {
            fixed (double* pA = &A[0])
            fixed (double* pB = &B[0])
                return CNative.LAPACKE_dgesv(101, n, nrhs, pA, lda, ipiv, pB, ldb);
        }
    }
}
