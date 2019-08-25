using System;

namespace DigitalFilter
{
    /// <summary>
    /// Designing an IIR digital filter takes place in three steps:
    /// 1. Set parameters: each filter type has a set of parameters that may be set; parameters which are
    /// "unset" must be set <=0 (number of poles), null (HP/LP filter type), or to NaN for real-valued parameters
    /// 2. Call ValidateDesign: this evaluates the paramters that have been "set" and determines that a valid
    /// design is possible; it does some preliminary calculations and, in particular, sets the values determined
    /// for the "unset" parameters
    /// 3. Call CompleteDesign: this does the final calculations and completes the filter design; it can only be 
    /// called once without changing the value of one of the parameters
    /// The IIR digital filter is now ready for use by calling a Filter method.
    /// Step 2 may be skipped, but if an invalid filter design is attemped an exception is thrown
    /// </summary>
    public abstract class IIRFilter: DFilter, IFilterDesign
    {
        protected Cascade c;

        protected bool designValidated = false;
        protected bool designCompleted = false;

        public abstract bool ValidateDesign();
        public abstract void CompleteDesign();
        public abstract Tuple<string, int, double[]> Description { get; }

        protected bool? _hp = null;
        public bool? HP
        {
            get { return _hp; }
            set
            {
                if (_hp == value) return;
                designValidated = false;
                designCompleted = false;
                _hp = value;
            }
        }

        protected double _passF = double.NaN;
        public double PassF
        {
            get { return _passF; }
            set
            {
                if (_passF.Equals(value)) return; //NOTE: use Equals so NaN==NaN
                designValidated = false;
                designCompleted = false;
                _passF = value;
            }
        }

        protected double _sr = double.NaN;
        public double SR
        {
            get { return _sr; }
            set
            {
                if (_sr.Equals(value)) return;
                designValidated = false;
                designCompleted = false; 
                _sr = value;
            }
        }

        protected int _np = 0;
        public int NP
        {
            get { return _np; }
            set
            {
                if (_np == value) return;
                designValidated = false;
                designCompleted = false;
                _np = value;
            }
        }

        protected double _stopF = double.NaN;
        public double StopF
        {
            set
            {
                if (_stopF.Equals(value)) return;
                designValidated = false;
                designCompleted = false;
                _stopF = value;
            }
            get { return _stopF; }
        }

        protected double _stopA = double.NaN;
        public double StopA
        {
            get { return _stopA; }
            set
            {
                if (Math.Abs(_stopA).Equals(Math.Abs(value))) return;
                designValidated = false;
                designCompleted = false;
                _stopA = value;
            }
        }

        public bool IsValid { get { return designValidated; } }
        public bool IsCompleted { get { return designCompleted; } }

        public override double Filter(double x0)
        {
            if (!designCompleted)
                throw new Exception("In IIRFilter.Filter(double): attempt to filter without completed design.");
            return c.Filter(x0);
        }

        public override void Reset()
        {
            if (!designCompleted)
                throw new Exception("In IIRFilter.Reset(): attempt to reset filter without completed design.");
            c.Reset();
        }

        public override string ToString(string format)
        {
            if (designCompleted)
                return c.ToString(format);
            return "Incomplete design";
        }

        public override string ToString()
        {
            if (designCompleted)
                return c.ToString();
            return "Incomplete design";
        }
    }

    public class Butterworth : IIRFilter
    {
        double omegaP;
        double omegaS;

        public Butterworth()
        {

        }

        public Butterworth(bool HP, double cutoff, double samplingRate)
        {
            _hp = HP;
            if (samplingRate < 0D)
                throw new ArgumentException("In Butterworth.cotr: invalid sampling frequency");
            _sr = samplingRate;
            Nyquist = _sr / 2D;
            if (cutoff <= 0D || cutoff >= Nyquist)
                throw new ArgumentException("In Butterworth.cotr: invalid cutoff frequency");
            _passF = cutoff;
            _stopF = HP ? cutoff * 0.1 : Math.Min(Nyquist, cutoff * 10D); //preliminary stop frequency
        }

        public override bool ValidateDesign()
        {
            if (designCompleted)
                throw new Exception("In Butterworth.ValidateDesign(): attempt to validate a completed design.");
            if (double.IsNaN(_sr) || _hp == null) { designValidated = false; return false; }

            int designCode = _np > 0 ? 1 : 0;
            designCode |= double.IsNaN(_passF) ? 0 : 0x02;
            designCode |= double.IsNaN(_stopF) ? 0 : 0x04;
            designCode |= double.IsNaN(_stopA) ? 0 : 0x08;

            if (designCode != 3 && designCode != 0x0D && designCode != 0x0E) //invalid parameter combination
            {
                designValidated = false;
                return false;
            }

            double nyquist = _sr / 2d;
            if (designCode == 3) //NP & passF
            {
                if (_passF >= nyquist) { designValidated = false; return false; }
                _stopA = 20D; //arbitrary 20dB down at stopF
                double A = Math.Log10(Math.Pow(10D, Math.Abs(_stopA) / 10D) - 1);
                omegaP = Math.Tan(Math.PI * _passF / _sr);
                omegaS = omegaP * Math.Pow(10D, ((bool)_hp ? -1D : 1D) * A / (2D * _np));
                _stopF = _sr * Math.Atan(omegaS) / Math.PI;
            }
            else
            {
                if (_stopF >= nyquist) { designValidated = false; return false; }
                omegaS = Math.Tan(Math.PI * _stopF / _sr);
                double A = Math.Log10(Math.Pow(10D, Math.Abs(_stopA) / 10D) - 1D);
                if (designCode == 0x0E) //stop band info + passF
                {
                    if (_passF >= nyquist || ((_passF < _stopF) == (bool)_hp) || _passF == _stopF)
                    {
                        designValidated = false;
                        return false;
                    }
                    omegaP = Math.Tan(Math.PI * _passF / _sr);
                    double t = Math.Log10((bool)_hp ? omegaP / omegaS : omegaS / omegaP);
                    _np = (int)Math.Ceiling(A / (2D * t));
                }
                else //designCode == 0x0D: stop band info + NP
                {
                    omegaP = omegaS * Math.Pow(10D, ((bool)_hp ? 1D : -1D) * A / (2D * _np));
                    _passF = _sr * Math.Atan(omegaP) / Math.PI;
                }
            }

            designValidated = true;
            return true;
        }

        public override void CompleteDesign()
        {
            if (designCompleted) return;
            if (!designValidated && !this.ValidateDesign())
                throw new Exception("In Butterworth.CompleteDesign(): attempt to complete an invalid filter design.");

            double tan2 = omegaP * omegaP;

            int odd = _np - ((_np >> 1) << 1);
            int r = (_np - odd) / 2;
            DFilter[] f = new DFilter[r + odd + 1];
            if (odd > 0)
            {
                double a = omegaP + 1D;
                f[0] = new Constant(((bool)_hp ? 1D : omegaP) / a);
                f[1] = new SinglePole((omegaP - 1) / a, 1D, (bool)_hp ? -1D : 1D);
            }
            else
                f[0] = new Constant(1D);

            double b1 = (bool)_hp ? -2D : 2D;
            for (int m = 1; m <= r; m++)
            {
                double sin = Math.Sin(Math.PI * (2D * m - 1D) / (2D * _np));
                double a0 = tan2 + 2D * omegaP * sin + 1D;
                double a1 = 2D * (tan2 - 1D) / a0;
                double a2 = (tan2 - 2D * omegaP * sin + 1D) / a0;
                f[m + odd] = new BiQuad(a1, a2, 1D, b1, 1D);
                ((Constant)f[0]).Update(((bool)_hp ? 1D : tan2) / a0);
            }
            this.c = new Cascade(f);
            designCompleted = true;
        }

        public override Tuple<string, int, double[]> Description
        {
            get
            {
                if (!designCompleted) throw new Exception("In Butterworth.Description.get: design not completed");
                Tuple<string, int, double[]> t = new Tuple<string, int, double[]>(
                    "Butterworth " + (((bool)_hp) ? "HP" : "LP"), _np, new double[] { _passF });
                return t;
            }
        }
    }

    public class Chebyshev : IIRFilter
    {
        protected double omegaC;
        protected double omegaS;

        protected int designCode;

        public double ActualStopA
        {
            get
            {
                if (_hp == null) return 0D;
                double a = ArcCosh((bool)_hp ? omegaC / omegaS : omegaS / omegaC);
                return 20D * Math.Log10(Math.Abs(1D + Math.Cosh(((double)_np) * a)));
            }
        }

        public Chebyshev()
        {

        }

        public override bool ValidateDesign()
        {
            if (designCompleted)
                throw new Exception("In Chebyshev.ValidateDesign(): attempt to validate a competed design");
            if (double.IsNaN(_sr) || _hp == null) { designValidated = false; return false; }

            designCode = getDesignCode();

            if (designCode <= 0)
            {
                designValidated = false;
                return false;
            }

            if((bool)_hp) return ValidateHPDesign();
            return ValidateLPDesign();
        }

        public override void CompleteDesign()
        {
            if (designCompleted) return;
            if (!designValidated && !this.ValidateDesign())
                throw new Exception("In Chebyshev.CompleteDesign(): attempt to complete an invalid design.");
            if ((bool)_hp) CompleteHPDesign();
            else CompleteLPDesign();
        }

        protected bool ValidateHPDesign()
        {
            double nyquist = _sr / 2D;
            if (designCode == 3) //stopF missing
            {
                double a = Math.Pow(10D, Math.Abs(_stopA) / 20D) - 1D;
                if (_passF <= 0D || _passF >= nyquist || a <= 1D) { designValidated = false; return false; }
                omegaC = Math.Tan(Math.PI * _passF / _sr);
                omegaS = omegaC / Math.Cosh(ArcCosh(a) / _np);
                _stopF = _sr * Math.Atan(omegaS) / Math.PI;
            }
            else if (designCode == 2) //passF missing
            {
                double a = Math.Pow(10D, Math.Abs(_stopA) / 20D) - 1D;
                if (_stopF <= 0D || _stopF >= nyquist || a <= 1D) { designValidated = false; return false; }
                omegaS = Math.Tan(Math.PI * _stopF / _sr);
                omegaC = omegaS * Math.Cosh(ArcCosh(a) / _np);
                _passF = _sr * Math.Atan(omegaC) / Math.PI;
            }
            else //both frequencies present
            {
                if (_stopF >= _passF || _stopF <= 0 || _passF >= nyquist)
                {
                    designValidated = false;
                    return false;
                }
                omegaC = Math.Tan(Math.PI * _passF / _sr);
                omegaS = Math.Tan(Math.PI * _stopF / _sr);
                if (designCode == 1) //NP missing
                {
                    double a = Math.Pow(10D, Math.Abs(_stopA) / 20D) - 1D;
                    if (a <= 1D) { designValidated = false; return false; }
                    _np = (int)(Math.Ceiling(ArcCosh(a) / ArcCosh(omegaC / omegaS)));
                }
                else //Atten missing
                    _stopA = 20D * Math.Log10(Math.Abs(1D + Math.Cosh(((double)_np) *
                        ArcCosh(omegaC / omegaS))));
            }

            designValidated = true;
            return true;
        }

        public void CompleteHPDesign()
        {
            double alpha = ArcSinh(Math.Cosh(_np * ArcCosh(omegaC / omegaS))) / _np;
            double cosh = Math.Cosh(2 * alpha);
            double sinh = Math.Sinh(alpha);
            double C = omegaS * omegaS;

            int odd = _np - ((_np >> 1) << 1);
            DFilter[] f = new DFilter[_np / 2 + odd];
            if (odd > 0)
            {
                double a = sinh * omegaS;
                f[0] = new SinglePole((a - 1) / (a + 1), 1 / (a + 1), -1 / (a + 1));
            }
            for (int m = 1; m <= _np / 2; m++)
            {
                double beta = Math.PI * (2D * m - 1D) / (2 * _np);
                double K = C * (Math.Cos(2D * beta) + cosh) / 2D;
                double cos = Math.Cos(beta);
                double sin = Math.Sin(beta);

                double a = K + 2D * omegaS * sin * sinh + 1D;
                double a1 = 2D * (K - 1D) / a;
                double a2 = (K - 2D * omegaS * sin * sinh + 1D) / a;
                double b0 = (C * cos * cos + 1D) / a;
                double b1 = 2D * (C * cos * cos - 1D) / a;
                f[m - 1 + odd] = new BiQuad(a1, a2, b0, b1, b0);
            }
            this.c = new Cascade(f);
            designCompleted = true;
        }

        protected bool ValidateLPDesign()
        {
            double nyquist = _sr / 2D;
            if (designCode == 3)
            {
                double a = Math.Pow(10D, Math.Abs(_stopA) / 20D) - 1D;
                if (_passF <= 0D || _passF >= nyquist || a <= 1D) { designValidated = false; return false; }
                omegaC = Math.Tan(Math.PI * _passF / _sr);
                omegaS = omegaC * Math.Cosh(ArcCosh(a) / _np);
                _stopF = _sr * Math.Atan(omegaS) / Math.PI;
            }
            else if (designCode == 2)
            {
                double a = Math.Pow(10D, Math.Abs(_stopA) / 20D) - 1D;
                if (_stopF <= 0D || _stopF >= nyquist || a <= 1D) { designValidated = false; return false; }
                omegaS = Math.Tan(Math.PI * _stopF / _sr);
                omegaC = omegaS / Math.Cosh(ArcCosh(a) / _np);
                _passF = _sr * Math.Atan(omegaC) / Math.PI;

            }
            else
            {
                if (_passF >= _stopF || _passF <= 0 || _stopF >= nyquist)
                {
                    designValidated = false;
                    return false;
                }
                omegaS = Math.Tan(Math.PI * _stopF / _sr);
                omegaC = Math.Tan(Math.PI * _passF / _sr);
                if (designCode == 1)
                {
                    double a = Math.Pow(10D, Math.Abs(_stopA) / 20D) - 1D;
                    if (a <= 1D) { designValidated = false; return false; }
                    _np = (int)(Math.Ceiling(ArcCosh(a) / ArcCosh(omegaS / omegaC)));
                }
                else
                    _stopA = 20D * Math.Log10(Math.Abs(1D + Math.Cosh(((double)_np) *
                        ArcCosh(omegaS / omegaC))));
            }
            designValidated = true;
            return true;
        }

        protected void CompleteLPDesign()
        {
            double alpha = ArcSinh(Math.Cosh(_np * ArcCosh(omegaS / omegaC))) / _np;
            double cosh = Math.Cosh(2 * alpha);
            double sinh = Math.Sinh(alpha);
            double C = omegaS * omegaS;

            int odd = _np - ((_np >> 1) << 1);
            DFilter[] f = new DFilter[(_np >> 1) + odd];
            if (odd > 0) //odd number of poles
            {
                double a = sinh / omegaS;
                f[0] = new SinglePole((1 - a) / (1 + a), 1 / (1 + a), 1 / (1 + a));
            }
            for (int m = 1; m <= _np / 2; m++)
            {
                double beta = Math.PI * (2D * m - 1D) / (2 * _np);
                double K = Math.Cos(2D * beta) + cosh;
                double cos = Math.Cos(beta);
                double sin = Math.Sin(beta);

                double a = K + 2D * C + 4D * omegaS * sin * sinh;
                double a1 = -2D * (K - 2D * C) / a;
                double a2 = (K + 2D * C - 4D * omegaS * sin * sinh) / a;
                double b0 = 2D * (cos * cos + C) / a;
                double b1 = -4D * (cos * cos - C) / a;
                f[m - 1 + odd] = new BiQuad(a1, a2, b0, b1, b0);
            }
            this.c = new Cascade(f);
            designCompleted = true;
        }

        public override Tuple<string, int, double[]> Description
        {
            get
            {
                if (!designCompleted) throw new Exception("In Chebyshev.Description.get: design not completed");
                Tuple<string, int, double[]> t = new Tuple<string, int, double[]>(
                    "Chebyshev2 " + (((bool)_hp) ? "HP" : "LP"), _np, new double[] { _passF, _stopF, _stopA });
                return t;
            }
        }

        protected int getDesignCode()
        {
            int v = -1;
            if (_np <= 0) v = 1;
            if (double.IsNaN(_passF)) v = v >= 0 ? 0 : 2;
            if (double.IsNaN(_stopF)) v = v >= 0 ? 0 : 3;
            if (double.IsNaN(_stopA)) v = v >= 0 ? 0 : 4;
            return v;
        }
    }

    public class ChebyshevHP : Chebyshev
    {
        public ChebyshevHP()
        {
            _hp = true;
        }

        public ChebyshevHP(double passF, double samplingRate)
        {
            if (samplingRate <= 0D)
                throw new ArgumentException("In ChebyshevHP.cotr: invalid sampling frequency");
            _sr = samplingRate;
            Nyquist = _sr / 2D;
            if (passF <= 0 || passF >= Nyquist)
                throw new ArgumentException("In ChebyshevHP.Design: invalid cutoff frequncy");
            _passF = passF;
        }

        public override bool ValidateDesign()
        {
            if (designCompleted)
                throw new Exception("In ChebyshevHP.ValidateDesign(): attempt to validate a competed design");
            if (double.IsNaN(_sr)) { designValidated = false; return false; }

            designCode = getDesignCode();

            if (designCode <= 0)
            {
                designValidated = false;
                return false;
            }
            return ValidateHPDesign();
        }

        public override void CompleteDesign()
        {
            CompleteHPDesign();
        }
    }

    public class ChebyshevLP : Chebyshev
    {
        public ChebyshevLP()
        {
            _hp = false;
        }

        public ChebyshevLP(double passF, double samplingRate)
        {
            if (samplingRate < 0D)
                throw new ArgumentException("In ChebyshevLP.cotr: invalid sampling frequency");
            _sr = samplingRate;
            Nyquist = _sr / 2D;
            if (passF <= 0 || passF >= Nyquist)
                throw new ArgumentException("In ChebyshevLP.cotr: invalid cutoff frequncy");
            _passF = passF;
        }

        public override bool ValidateDesign()
        {
            if (designValidated)
                throw new Exception("In ChebyshevLP.Design: attempt to redesign filter");
            if (double.IsNaN(_sr)) { designValidated = false; return false; }

            designCode = getDesignCode();

            if (designCode <= 0)
            {
                designValidated = false;
                return false;
            }
            return ValidateLPDesign();
        }

        public override void CompleteDesign()
        {
            CompleteLPDesign();
        }
    }

    public class Elliptical : IIRFilter
    {
        protected double omegaC;
        protected double omegaS;

        protected double q;
        protected double p0;
        protected double W;

        protected double _pr = double.NaN;
        protected double _passA = double.NaN;
        public double Ripple
        {
            set
            {
                if (_pr.Equals(value)) return;
                designValidated = false;
                designCompleted = false;
                _pr = value;
                if (double.IsNaN(_pr))
                    _passA = value;
                else
                    _passA = -20D * Math.Log10(1 - _pr);
            }
            get { return _pr; }
        }

        public double ActualStopA
        {
            get
            {
                if (designValidated)
                {
                    if (_hp == null) return 0D;
                    double k = (bool)_hp ? omegaS / omegaC : omegaC / omegaS;
                    return 10D * Math.Log10(1D + (Math.Pow(1 - _pr, -2) - 1D) /
                        Math.Pow(D(k), 2)); //get newly calculated attenuation
                }
                throw new Exception("In Elliptical.ActualStopA: invalid design.");
            }
        }

        public double PassA
        {
            set
            {
                if (Math.Abs(_passA).Equals(Math.Abs(value))) return;
                designValidated = false;
                designCompleted = false;
                _passA = value;
                if (double.IsNaN(_passA))
                    _pr = value;
                else
                    _pr = 1D - Math.Pow(10D, -Math.Abs(_passA) / 20);
            }
            get { return _passA; }
        }

        protected int designCode;

        public Elliptical()
        {

        }

        public override bool ValidateDesign()
        {
            if (designCompleted)
                throw new Exception("In Elliptical.ValidateDesign(): attempt to validate a competed design");
            if (double.IsNaN(_sr) || _hp == null) { designValidated = false; return false; }

            designCode = getDesignCode();

            if (designCode <= 0)
            {
                designValidated = false;
                return false;
            }

            if ((bool)_hp) return ValidateHPDesign();
            return ValidateLPDesign();
        }

        protected bool ValidateHPDesign()
        {
            if (designCode != 2 && (_pr <= 0D || _pr >= 1D)) { designValidated = false; return false; }
            double nyquist = _sr / 2D;
            if (designCode == 1) //missing PassF
            {
                if (_stopF <= 0D || _stopF >= nyquist ||
                    Math.Abs(_stopA) <= 20D * Math.Log10(1 / (1 - _pr)))
                {
                    designValidated = false;
                    return false;
                }
                omegaS = Math.Tan(Math.PI * _stopF / _sr);
                _passF = _stopF / selectivity;
                omegaC = Math.Tan(Math.PI * _passF / _sr);
            }
            else if (designCode == 3) //missing StopF
            {
                if (_passF <= 0D || _passF >= nyquist ||
                    Math.Abs(_stopA) <= 20D * Math.Log10(1 / (1 - _pr)))
                {
                    designValidated = false;
                    return false;
                }
                omegaC = Math.Tan(Math.PI * _passF / _sr);
                _stopF = _passF * selectivity;
                omegaS = Math.Tan(Math.PI * _stopF / _sr);
            }
            else //have both frequencies
            {
                if (_passF <= _stopF || _stopF <= 0 || _passF >= nyquist)
                {
                    designValidated = false;
                    return false;
                }
                omegaC = Math.Tan(Math.PI * _passF / _sr);
                omegaS = Math.Tan(Math.PI * _stopF / _sr);
                if (designCode == 2) //missing PassA/Ripple
                {
                    _pr = 1D - 1D / Math.Sqrt(Math.Pow(D(omegaS / omegaC), 2) * Math.Pow(10D, Math.Abs(_stopA) / 10D) + 1D);
                    _passA = -20D * Math.Log10(1 - _pr);
                }
                else if (designCode == 4) //missing StopA
                    _stopA = 10D * Math.Log10(1D + (Math.Pow(1 - _pr, -2) - 1D) / Math.Pow(D(omegaS / omegaC), 2));
                else //missing NP
                {
                    if (Math.Abs(_stopA) <= 20D * Math.Log10(1 / (1 - _pr)))
                    {
                        designValidated = false;
                        return false;
                    }
                    _np = np(omegaS / omegaC);
                }
            }
            designValidated = true;
            return true;
        }

        protected bool ValidateLPDesign()
        {
            double nyquist = _sr/2D;
            if (designCode == 1) //missing PassF
            {
                if (_stopF <= 0D || _stopF >= nyquist ||
                    Math.Abs(_stopA) <= 20D * Math.Log10(1 / (1 - _pr)))
                {
                    designValidated = false;
                    return false;
                }
                omegaS = Math.Tan(Math.PI * _stopF / _sr);
                _passF = _sr * Math.Atan(omegaS * selectivity) / Math.PI;
                omegaC = Math.Tan(Math.PI * _passF / _sr);
            }
            else if (designCode == 3) //missing StopF
            {
                if (_stopF <= 0D || _stopF >= nyquist ||
                    Math.Abs(_stopA) <= 20D * Math.Log10(1 / (1 - _pr)))
                {
                    designValidated = false;
                    return false;
                }
                omegaC = Math.Tan(Math.PI * _passF / _sr);
                _stopF = _sr * Math.Atan(omegaC / selectivity) / Math.PI;
                omegaS = Math.Tan(Math.PI * _stopF / _sr);
            }
            else //have both frequencies
            {
                if (_passF >= _stopF || _passF <= 0 || _stopF >= nyquist)
                {
                    designValidated = false;
                    return false;
                }
                omegaC = Math.Tan(Math.PI * _passF / _sr);
                omegaS = Math.Tan(Math.PI * _stopF / _sr);
                if (designCode == 2) //missing PassA/Ripple
                {
                    _pr = 1D - 1D / Math.Sqrt(Math.Pow(D(omegaC / omegaS), 2) * Math.Pow(10D, Math.Abs(_stopA) / 10D) + 1D);
                    _passA = -20D * Math.Log10(1 - _pr);
                }
                else if (designCode == 4) //missing StopA
                {
                    double d = Math.Pow(D(omegaC / omegaS), 2);
                    _stopA = 10D * Math.Log10(1D + (Math.Pow(1 - _pr, -2) - 1D) / d);
                }
                else //missing NP
                {
                    if (Math.Abs(_stopA) <= 20D * Math.Log10(1 / (1 - _pr)))
                    {
                        designValidated = false;
                        return false;
                    }
                    _np = np(omegaC / omegaS);
                }
            }
                designValidated = true;
                return true;
        }

        public override void CompleteDesign()
        {
            if (designCompleted) return;
            if (!designValidated && !this.ValidateDesign())
                throw new Exception("In Elliptical.CompleteDesign(): attempt to complete an invalid design.");
            if ((bool)_hp) CompleteHPDesign();
            else CompleteLPDesign();
        }

        protected void CompleteHPDesign()
        {
            double k = omegaS / omegaC;
            calculate(k);
            double alpha = Math.Sqrt(omegaS * omegaC);
            int r = _np >> 1;
            DFilter[] df;
            int odd = NP - (r << 1);
            if (odd == 0)
            {
                df = new DFilter[r + 1];
                df[0] = new Constant(1 - _pr);
            }
            else
            {
                df = new DFilter[r + 2];
                df[0] = new Constant(p0 / (p0 + alpha));
                df[1] = new SinglePole((alpha - p0) / (alpha + p0), 1D, -1D);
            }
            for (int i = 1; i <= r; i++)
            {
                double mu = Math.PI * ((double)i + 0.5 * (odd - 1)) / _np;
                double temp = 0;
                for (double m = 0; m <= 10; m++)
                    temp += Math.Pow(-1D, m) * Math.Pow(q, m * (m + 1)) * Math.Sin((2D * m + 1) * mu);
                double temp1 = 0.5;
                for (double m = 1D; m <= 10; m++)
                    temp1 += Math.Pow(-1D, m) * Math.Pow(q, m * m) * Math.Cos(2D * m * mu);
                double X = Math.Abs(Math.Pow(q, 0.25) * temp / temp1);
                double Y = Math.Sqrt((1D - X * X / k) * (1D - k * X * X));
                double a = alpha * alpha * X * X;
                temp = Math.Pow(W * X, 2) + Math.Pow(p0 * Y, 2);
                double b = 2D * alpha * p0 * Y * (1D + Math.Pow(p0 * X, 2)) / temp;
                double c = alpha * alpha * Math.Pow(1D + Math.Pow(p0 * X, 2), 2) / temp;
                double a0 = 1D + b + c;
                double a1 = 2D * (c - 1D) / a0;
                double a2 = (1D - b + c) / a0;
                double b1 = 2D * (a - 1D) / (a + 1D);
                BiQuad d = new BiQuad(a1, a2, 1D, b1, 1D);
                df[i + odd] = d;
                ((Constant)df[0]).Update((1 - a1 + a2) / (2 - b1));
            }
            this.c = new Cascade(df);
            designCompleted = true;
        }

        protected void CompleteLPDesign()
        {
            double k = omegaC / omegaS;
            calculate(k);
            double alpha = Math.Sqrt(omegaC * omegaS);
            double ap0 = alpha * p0;
            DFilter[] df;
            int r = NP >> 1;
            int odd = NP - (r << 1);
            if (odd == 0)
            {
                df = new DFilter[r + 1];
                df[0] = new Constant(1 - _pr);
            }
            else //odd pole
            {
                df = new DFilter[r + 2];
                df[0] = new Constant(ap0 / (ap0 + 1D));
                df[1] = new SinglePole((ap0 - 1D) / (ap0 + 1D), 1D, 1D);
                odd = 1;
            }
            for (int i = 1; i <= r; i++) //each biquad section
            {
                double mu = Math.PI * ((double)i + 0.5 * (odd - 1)) / _np;
                double temp = 0;
                for (double m = 0; m <= 10; m++)
                    temp += Math.Pow(-1D, m) * Math.Pow(q, m * (m + 1)) * Math.Sin((2D * m + 1) * mu);
                double temp1 = 0.5;
                for (double m = 1D; m <= 10; m++)
                    temp1 += Math.Pow(-1D, m) * Math.Pow(q, m * m) * Math.Cos(2D * m * mu);
                double X = Math.Abs(Math.Pow(q, 0.25) * temp / temp1);
                double Y = Math.Sqrt((1D - X * X / k) * (1D - k * X * X));
                double a = alpha * alpha / (X * X);
                double b = 2D * ap0 * Y / (1 + Math.Pow(p0 * X, 2));
                double c = alpha * alpha * (Math.Pow(p0 * Y, 2) + Math.Pow(W * X, 2)) /
                    Math.Pow(1D + Math.Pow(p0 * X, 2), 2);
                double a0 = 1D + b + c;
                double a1 = 2d * (c - 1D) / a0;
                double a2 = (1D - b + c) / a0;
                double b1 = 2D * (a - 1D) / (a + 1D);
                BiQuad d = new BiQuad(a1, a2, 1, b1, 1);
                df[i + odd] = d;
                ((Constant)df[0]).Update((1 + a1 + a2) / (2D + b1));
            }
            this.c = new Cascade(df);
            designCompleted = true;
        }

        protected void calculate(double k)
        {
            double V = Math.Log((2D - _pr) / _pr) / (2D * _np);
            double temp = 0;
            for (int m = 0; m <= 10; m++)
                temp += Math.Pow(-1D, m) * Math.Pow(q, m * (m + 1)) * Math.Sinh((2 * m + 1) * V);
            double temp1 = 0.5;
            for (int m = 1; m <= 10; m++)
                temp1 += Math.Pow(-1D, m) * Math.Pow(q, m * m) * Math.Cosh(2D * m * V);
            p0 = Math.Abs(Math.Pow(q, 0.25) * temp / temp1);
            W = Math.Sqrt((1D + p0 * p0 / k) * (1D + k * p0 * p0));
        }

        protected double HFromK(double k)
        {
            double kp = Math.Pow(1D - k * k, 0.25);
            return 0.5 * (1D - kp) / (1D + kp);
        }

        protected double QFromH(double h)
        {
            double t = Math.Pow(h, 4);
            return h * (1D + t * (2D + t * (15D + t * (150D + t * (1707D + t * (20910D +
                t * (268616D + t * (3567400D + t * 48555069D))))))));
        }

        protected double HFromQ(double q)
        {
            return q * (1D + Math.Pow(q, 8) * (1D + Math.Pow(q, 16) * (1D + Math.Pow(q, 24)))) /
                (1D + 2 * Math.Pow(q, 4) * (1D + Math.Pow(q, 12) * (1D + Math.Pow(q, 20))));
        }
        protected double KFromH(double h)
        {
            return Math.Sqrt(16D * h * (1D + 4D * h * h) / Math.Pow(1D + 2D * h, 4));
        }

        public override Tuple<string, int, double[]> Description
        {
            get
            {
                if (!designCompleted) throw new Exception("In Elliptic.Description.get: design not completed");
                Tuple<string, int, double[]> t = new Tuple<string, int, double[]>(
                    "Elliptic " + (((bool)_hp) ? "HP" : "LP"), _np, new double[] { _passF, _stopF, ActualStopA, Ripple });
                return t;
            }
        }

        protected int getDesignCode()
        {
                int v = -1;
                if (double.IsNaN(_passF)) v = 1;
                if (double.IsNaN(_passA)) v = v >= 0 ? 0 : 2;
                if (double.IsNaN(_stopF)) v = v >= 0 ? 0 : 3;
                if (double.IsNaN(_stopA)) v = v >= 0 ? 0 : 4;
                if (_np == 0) v = v >= 0 ? 0 : 5;
                return v;
        }

        /// <summary>
        /// Calculate k, the selectivity, for given attenuation, pass band ripple and number of poles
        /// </summary>
        protected double selectivity
        {
            get
            {
                double k1 = Math.Sqrt((Math.Pow(1D - _pr, -2) - 1D) / (Math.Pow(10D, Math.Abs(_stopA) / 10D) - 1D));
                double q1 = QFromH(HFromK(k1));
                q = Math.Pow(q1, 1D / _np);
                return KFromH(HFromQ(q));
            }
        }

        /// <summary>
        /// Calculate the discrimination factor, basically the ratio between passband ripple and attenuation (k1)
        /// </summary>
        /// <param name="k">Selectivity</param>
        /// <returns></returns>
        protected double D(double k) //discrimination factor
        {
            q = QFromH(HFromK(k));
            double q1 = Math.Pow(q, _np);
            return KFromH(HFromQ(q1));
        }

        /// <summary>
        /// Calculate number of poles needed for given k and ripple, with at least the amount of attentuation; sets attentuation to the 
        /// new value
        /// </summary>
        /// <param name="k">Selectivity</param>
        /// <returns>Number of poles required</returns>
        protected int np(double k)
        {
            q = QFromH(HFromK(k));
            double D = Math.Sqrt((Math.Pow(1D - _pr, -2) - 1D) / (Math.Pow(10D, Math.Abs(_stopA) / 10D) - 1D));
            double q1 = QFromH(HFromK(D));
            return (int)Math.Ceiling(Math.Log(q1) / Math.Log(q));
        }
    }

    public class EllipticalLP : Elliptical
    {
        public EllipticalLP(double passF, double samplingRate)
        {
            if (samplingRate <= 0D)
                throw new Exception("In EllipticalLP.cotr: invalid sampling rate");
            _sr = samplingRate;
            Nyquist = _sr / 2D;
            if (passF <= 0 || passF >= Nyquist)
                throw new ArgumentException("In EllipticalLP.cotr: invalid cutoff frequncy");
            _passF = passF;
        }

        public EllipticalLP()
        {
            _hp = false;
        }

        public override bool ValidateDesign()
        {
            if (designCompleted)
                throw new Exception("In EllipticalLP.Design: attempt to validate completed design");
            if (double.IsNaN(_sr)) { designValidated = false; return false; }

            designCode = getDesignCode();
            if (designCode <= 0) { designValidated = false; return false; }
            return ValidateLPDesign();
        }

        public override void CompleteDesign()
        {
            if (designCompleted) return;
            if (!designValidated && !this.ValidateDesign())
                throw new Exception("In EllipticalLP.CompleteDesign(): attempt to complete an invalid design.");
            CompleteLPDesign();
        }
    }

    public class EllipticalHP : Elliptical
    {
        public EllipticalHP(double passF, double samplingRate)
        {
            if (samplingRate <= 0D)
                throw new Exception("In EllipticalHP.cotr: invalid sampling rate");
            _sr = samplingRate;
            Nyquist = _sr / 2D;
            if (passF <= 0 || passF >= Nyquist)
                throw new ArgumentException("In EllipticalHP.cotr: invalid cutoff frequncy");
            _passF = passF;
        }

        public EllipticalHP()
        {
            _hp = true;
        }

        public override bool ValidateDesign()
        {
            if (designValidated)
                throw new Exception("In EllipticalHP.Design: attempt to redesign filter");
            if (double.IsNaN(_sr)) { designValidated = false; return false; }

            designCode = getDesignCode();

            if (designCode <= 0)
            {
                designValidated = false;
                return false;
            }

            return ValidateHPDesign();
        }

        public override void CompleteDesign()
        {
            if (designCompleted) return;
            if (!designValidated && !this.ValidateDesign())
                throw new Exception("In EllipticalHP.CompleteDesign(): attempt to complete an invalid design.");
            CompleteHPDesign();
        }
    }

    public interface IFilterDesign
    {
        bool ValidateDesign();

        void CompleteDesign();
    }
}
