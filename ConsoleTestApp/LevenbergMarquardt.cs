using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinearAlgebra;

namespace ConsoleTestApp
{

    public class LevenbergMarquardt
    {
        public delegate NVector Function(NVector t, NVector p);
        public delegate NMMatrix JFunc(NVector t, NVector p);

        const double lambda_DN_fac = 2D;
        const double lambda_UP_fac = 3D;
        Function func;
        JFunc Jfunc;
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
        NMMatrix JtJ;
        NVector Jtdy;
        double DOF;
        double lambda;
        double X2;
        double dX2;
        NVector p_old;
        NVector y_old;
        NVector y_hat;
        double alpha;
        double nu;
        int iteration;

        int _result = 0;
        public ResultType Result
        {
            get { return (ResultType)_result; }
        }

        public int Iterations
        {
            get { return iteration; }
        }

        public double ChiSquare
        {
            get { return X2 / DOF; }
        }

        public double normalizedStandardErrorOfFit
        {
            get
            {
                return (X2 / DOF - DOF) / Math.Sqrt(2 * DOF);
            }
        }

        public NMMatrix parameterCovariance
        {
            get
            {
                NMMatrix Vp = NMMatrix.I(n) / JtJ;
                return Vp;
            }
        }

        public NVector parameterStandardError
        {
            get
            {
                NVector Sp = (DOF * (NMMatrix.I(n) / JtJ).Diag()).Apply((LinearAlgebra.F)Math.Sqrt);
                return Sp;
            }
        }

        public LevenbergMarquardt(Function func, JFunc Jfunc, NVector p_min, NVector p_max, NVector dp, double[] eps, UpdateType updateType)
        {
            this.func = func;
            this.Jfunc = Jfunc;
            n = p_min.N;
            this.p_min = p_min;
            if (p_max.N != n) throw new Exception("LevenbergMarquardt: size mismatch p_max");
            this.p_max = p_max;
            if (Jfunc == null)
            {
                if (dp.N != n) throw new Exception("LevenbergMarquardt: size mismatch dp");
                this.dp = dp;
            }
            MaxIter = 50 * n;
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

//            weight_sq = (m - n + 1) / y_dat.Dot(y_dat);
            DOF = (double)(m - n + 1);

            //initalize Jacobian and related matrices
            y_hat = func(t, p);
            y_old = y_hat;
            if (Jfunc == null)
                J = Jacobian(p, y_hat);
            else
                J = Jfunc(t, p);
            NVector delta_y = y_dat - y_hat;
            X2 = delta_y.Dot(delta_y);
            JtJ = J.Transpose() * J;
            Jtdy = J.Transpose() * delta_y;

            iteration = 0;

            if (Jtdy.Abs().Max() < eps[0])
            {
                _result = 1;
                return p; //Good guess!!!
            }
            if (updateType == UpdateType.Marquardt)
                lambda = 0.01D;
            else
                lambda = 0.01D * JtJ.Diag().Max();

            bool stop = false;

            /************************** Begin Main loop ***********************/
            // y_hat = vector of y estimates for current value of parameters
            // y_try = vector of y estimates for current trial value of parameters
            // y_dat = given dependent values (fixed)
            // y_old = vector of y estimates for previous value of parameters (used in Broyden estimate of J)
            // t = given independent values (fixed)
            // p = current accepted estimate of parameters
            // h = last calculated (trial) increment for the parameters
            // p_try = current trial value for the parameters
            // p_old = previous accepted value of parameters (used in Broyden estimate of J)
            // X2 = chi^2 of last accepted estimate
            // X2_try = chi^2 of current trial estimate
            // J = current estimate of Jacobian at p

            while (!stop)
            {
                iteration++;

                NVector h;
                if (updateType == UpdateType.Marquardt)
                    h = Jtdy / (JtJ + lambda * JtJ.Diag().Diag());
                else
                    h = Jtdy / (JtJ + lambda * NMMatrix.I(n));

                NVector p_try = (p + h).Max(p_min).Min(p_max);

                NVector y_try = func(t, p_try);
                delta_y = y_dat - y_try;

                double X2_try = delta_y.Dot(delta_y);

                if (updateType == UpdateType.Quadratic)
                {
                    alpha = Jtdy.Dot(h) / ((X2_try - X2) / 2D + 2D * Jtdy.Dot(h));
                    h = h * alpha;
                    p_try = (p_try + h).Max(p_min).Min(p_max);
                    delta_y = y_dat - func(t, p_try);
                    X2_try = delta_y .Dot(delta_y);
                }
                dX2 = X2_try - X2;

                double rho = -dX2 / (2D * (lambda * h + Jtdy).Dot(h));

                if (dX2 < 0D) //found a better estimate
                {
                    X2 = X2_try;
                    p_old = p;
                    p = p_try;
                    y_old = y_hat;
                    y_hat = y_try;

                    if (iteration % (2 * n) == 0) //|| dX2 > 0 or is it rho > ep[3] ?
                        if (Jfunc == null)
                            J = Jacobian(p, y_hat);
                        else
                            J = Jfunc(t, p);
                    else
                        J = J + (y_hat - y_old - J * h).Cross(h) / h.Dot(h); //Broyden rank-1 update of J

                    JtJ = J.Transpose() * J;
                    Jtdy = J.Transpose() * delta_y;

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

                    if (Jtdy.Abs().Max() < eps[0] && iteration > 2)
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
                    if (iteration % (2 * n) == 0) //update J every 2n th no matter what
                    {
                        if (Jfunc == null)
                            J = Jacobian(p, y_hat);
                        else
                            J = Jfunc(t, p);
                        JtJ = J.Transpose() * J;
                        Jtdy = J.Transpose() * (y_dat - y_hat);
                    }

                    switch (updateType)
                    {
                        case UpdateType.Marquardt:
                            lambda = Math.Min(lambda * lambda_UP_fac, 1E7);
                            break;
                        case UpdateType.Quadratic:
                            lambda = lambda + Math.Abs(dX2 / (2D * alpha));
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
                    return p;
                }
            }
            /************************** End Main loop ************************/

            return p;
        }

        private NMMatrix Jacobian(NVector p, NVector y)
        {
            NVector ps = new NVector(p); //save a copy
            NMMatrix J = new NMMatrix(m, n); //creating a new J from scratch
            double del_p;
            for (int j = 0; j < n; j++)
            {
                del_p = Math.Max(dp[j] * Math.Abs(p[j]), dp[j]);
                p[j] = ps[j] + del_p;
                NVector y1 = func(t, p);
                if (dp[j] != 0D) //forward or backward difference
                    J.ReplaceColumn(j, (y1 - y) / del_p);
                else //central difference
                {
                    p[j] = ps[j] - del_p;
                    J.ReplaceColumn(j, (y1 - func(t, p)) / (2D * del_p));
                }
                p[j] = ps[j]; //restore this value
            }
            return J;
        }

        private NMMatrix Broyden(NVector p_old, NVector y_old, NMMatrix J, NVector p, NVector y)
        {
            NVector h = p - p_old;
            J = J + (y - y_old - J * h).Cross(h) / h.Dot(h);
            return J;
        }

        public enum UpdateType { Marquardt, Quadratic, Nielsen };

        public enum ResultType { MaximumIterations = -1, NoResult = 0, Jacobian = 1, ParameterChange = 2, ChiSquare = 3 };
    }
}
