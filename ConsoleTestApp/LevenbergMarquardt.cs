using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinearAlgebra;

namespace ConsoleTestApp
{
    public delegate NVector Function(NVector t, NVector p);

    public class LevenbergMarquardt
    {

        const double lambda_DN_fac = 9D;
        const double lambda_UP_fac = 11D;
        Function func;
        NVector p;
        NVector t;
        NVector y_dat;
        NVector dp;
        NVector p_min;
        NVector p_max;
        double[] eps;
        UpdateType updateType;
        int MaxIter;
        int n;
        int m;

        NMMatrix J;
        NMMatrix JtWJ;
        NVector JtWdy;
        NVector weight_sq;
        double lambda;
        double X2;
        double X2_old;
        double dX2;
        NVector p_old;
        NVector y_old;
        NVector y_hat;
        double alpha;
        double nu;
        int iteration;

        int _result = 0;
        public int Result
        {
            get { return _result; }
        }

        public LevenbergMarquardt(Function func,
            NVector dp, NVector p_min, NVector p_max, double[] eps, UpdateType updateType)
        {
            n = p_min.N;
            MaxIter = 10 * n;
            this.func = func;
            if (dp.N != n) throw new Exception("LevenbergMarquardt: size mismatch dp");
            this.dp = dp;
            if (p_min.N != n) throw new Exception("LevenbergMarquardt: size mismatch p_min");
            this.p_min = p_min;
            if (p_max.N != n) throw new Exception("LevenbergMarquardt: size mismatch p_max");
            this.p_max = p_max;
            this.eps = eps;
            this.updateType = updateType;
        }

        public NVector Calculate(NVector par_initial, NVector t, NVector y_dat)
        {
            _result = 0;
            m = t.N;
            if (par_initial.N != n) throw new Exception("LevenbergMarquardt.Calculate: size mismatch parms");
            this.p = par_initial;
            this.t = t;
            if (y_dat.N != m) throw new Exception("LevenbergMarquardt.Calculate: size mismatch t-y");
            this.y_dat = y_dat;

            weight_sq = NVector.Uniform((m - n + 1) / y_dat.Dot(y_dat), m);

            LM_matrix(); //initalize Jacobian and related matrices

            if (JtWdy.Abs().Max() < eps[0]) return p; //Good guess!!!
            if (updateType == UpdateType.Marquardt)
                lambda = 0.01D;
            else
                lambda = 0.01D * JtWJ.Diag().Max();

            X2_old = double.MaxValue;

            iteration = 0;
            bool stop = false;

            /************************** Begin Main loop ***********************/
            while (!stop)
            {
                iteration++;

                NVector h;
                if (updateType == UpdateType.Marquardt)
                    h = JtWdy / (JtWJ + lambda * JtWJ.Diag().Diag());
                else
                    h = JtWdy / (JtWJ + lambda * NMMatrix.I(n));

                NVector p_try = (p + h).Max(p_min).Min(p_max);

                NVector delta_y = y_dat - func(t, p_try);

                double X2_try = (delta_y * weight_sq).Dot(delta_y);

                if (updateType == UpdateType.Quadratic)
                {
                    alpha = JtWdy.Dot(h) / ((X2_try - X2) / 2D + 2D * JtWdy.Dot(h));
                    h = h * alpha;
                    p_try = (p_try + h).Max(p_min).Min(p_max);
                    delta_y = y_dat - func(t, p_try);
                    X2_try = (delta_y * weight_sq).Dot(delta_y);
                }

                double rho = (X2 - X2_try) / (2D * (lambda * h + JtWdy).Dot(h));

                if (rho > eps[3]) //found a better estimate
                {
                    dX2 = X2 - X2_old;
                    X2_old = X2;
                    p_old = p;
                    y_old = y_hat;
                    p = p_try;

                    LM_matrix();
                    switch (updateType)
                    {
                        case UpdateType.Marquardt:
                            lambda = Math.Max(lambda / lambda_DN_fac, 1E-7);
                            break;
                        case UpdateType.Quadratic:
                            lambda = Math.Max(lambda / (1 + alpha), 1E-7);
                            break;
                        case UpdateType.Nielsen:
                            lambda = lambda * Math.Max(1D / 3D, 1D - Math.Pow(2D * rho - 1D, 3));
                            nu = 2D;
                            break;
                    }

                    if (JtWdy.Abs().Max() < eps[0] && iteration > 2)
                    {
                        _result = 1;
                        stop = true;
                    }
                    else if ((h / p).Abs().Max() < eps[1] && iteration > 2)
                    {
                        _result = 2;
                        stop = true;
                    }
                    else if (X2 / (m - n + 1) < eps[2] && iteration > 2)
                    {
                        _result = 3;
                        stop = true;
                    }
                }
                else //Not a better estimate
                {
                    X2 = X2_old;
                    if (iteration % (2 * n) == 0)
                        LM_matrix();
                    switch (updateType)
                    {
                        case UpdateType.Marquardt:
                            lambda = Math.Min(lambda * lambda_UP_fac, 1E7);
                            break;
                        case UpdateType.Quadratic:
                            lambda = lambda + Math.Abs((X2_try - X2) / 2D / alpha);
                            break;
                        case UpdateType.Nielsen:
                            lambda = lambda * nu;
                            nu *= 2D;
                            break;
                    }
                }

                if (iteration > MaxIter && !stop)
                {
                    _result = -1;
                    stop = true;
                }
            }
            /************************** End Main loop ************************/

            return p;
        }

        private NMMatrix Jacobian(NVector p, NVector y)
        {
            NVector ps = new NVector(p); //save a copy
            NMMatrix J = new NMMatrix(m, n);
            NVector del = new NVector(n);
            for (int j = 0; j < n; j++)
            {
                del[j] = dp[j] * (1 + Math.Abs(p[j]));
                p[j] = ps[j] + del[j];
                NVector y1 = func(t, p);
                if (dp[j] != 0D) //forward or backward difference
                {
                    J.ReplaceColumn(j, (y1 - y) / del[j]);
                }
                else //central difference
                {
                    p[j] = ps[j] - del[j];
                    J.ReplaceColumn(j, (y1 - func(t, p)) / (2D * del[j]));
                }
            }
            p = ps;
            return J;
        }

        private NMMatrix Broyden(NVector p_old, NVector y_old, NMMatrix J, NVector p, NVector y)
        {
            NVector h = p - p_old;
            J = J + (y - y_old - J * h).Cross(h) / h.Dot(h);
            return J;
        }

        private void LM_matrix()
        {
            JtWdy = new NVector(m);
            JtWJ = new NMMatrix(n, n);

            y_hat = func(t, p);

            if (iteration % 2 * n == 0)
                J = Jacobian(p, y_hat);
            else
                J = Broyden(p_old, y_old, J, p, y_hat);
        }

        public enum UpdateType { Marquardt, Quadratic, Nielsen };
    }
}
