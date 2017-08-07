using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCIUtilities
{
    public class RQuaternion
    {
        double[] q;

        public double this[int i]
        {
            get { return q[i]; }

            set { q[i] = value; }
        }

        public RQuaternion()
        {
            q = new double[] { 0, 0, 0, 0 };
        }

        public RQuaternion(double a, double b, double c, double d)
            : this()
        {
            q[0] = a;
            q[1] = b;
            q[2] = c;
            q[3] = d;
        }

        public RQuaternion(double theta, NVector v)
            : this()
        {
            if (v.N != 3)
                throw new Exception("NVector must be 3-vector in RQuarternion(double, NVector) constructor");
            double s = Math.Sqrt(v.Norm2());
            q[0] = Math.Cos(theta / 2);
            double s2 = Math.Sin(theta / 2);
            q[1] = v[0] * s2 / s;
            q[2] = v[1] * s2 / s;
            q[3] = v[2] * s2 / s;
        }

        public RQuaternion(NVector v)
            : this()
        {
            if (v.N != 3)
                throw new Exception("NVector must be 3-vector in RQuarternion(NVector) constructor");
            q[1] = v[0];
            q[2] = v[1];
            q[3] = v[2];
        }

        public RQuaternion(RQuaternion Q) //copy constructor
            : this()
        {
            for (int i = 0; i < 4; i++) q[i] = Q[i];
        }

        public static RQuaternion operator *(double d, RQuaternion Q)
        {
            RQuaternion R = new RQuaternion();
            for (int i = 0; i < 4; i++) R[i] = d * Q[i];
            return R;
        }

        public static RQuaternion operator +(RQuaternion Q, RQuaternion R)
        {
            RQuaternion S = new RQuaternion();
            for (int i = 0; i < 4; i++) S[i] = Q[i] + R[i];
            return S;
        }

        public static RQuaternion operator *(RQuaternion Q, RQuaternion R)
        {
            RQuaternion S = new RQuaternion();
            S[0] = Q[0] * R[0] - Q[1] * R[1] - Q[2] * R[2] - Q[3] * R[3];
            S[1] = Q[0] * R[1] + Q[1] * R[0] + Q[2] * R[3] - Q[3] * R[2];
            S[2] = Q[0] * R[2] - Q[1] * R[3] + Q[2] * R[0] + Q[3] * R[1];
            S[3] = Q[0] * R[3] + Q[1] * R[2] - Q[2] * R[1] + Q[3] * R[0];
            return S;
        }

        public static RQuaternion operator /(RQuaternion Q, double s)
        {
            RQuaternion R = new RQuaternion();
            for (int i = 0; i < 4; i++) R[i] = Q[i] / s;
            return R;
        }

        public RQuaternion Conjugate()
        {
            RQuaternion R = new RQuaternion();
            R[0] = q[0];
            for (int i = 1; i < 4; i++) R[i] = - q[i];
            return R;
        }

        public RQuaternion Inverse()
        {
            return this.Conjugate() / Math.Pow(this.Norm(), 2);
        }

        public double Norm()
        {
            double s = 0D;
            for (int i = 0; i < 4; i++) s += q[i] * q[i];
            return Math.Sqrt(s);
        }

        public RQuaternion Normalize()
        {
            double s = Norm();
            RQuaternion Q = new RQuaternion();
            for (int i = 0; i < 4; i++) Q[i] = q[i] / s;
            return Q;
        }

        public NVector ExtractV()
        {
            return new NVector(new double[] { q[1], q[2], q[3] });
        }

        public override string ToString()
        {
            return this.ToString("G");
        }

        public string ToString(string format)
        {
            return "[" + q[0].ToString(format) + ", " + q[1].ToString(format) + ", "
                + q[2].ToString(format) + ", " + q[3].ToString(format) + "]";
        }
    }
}
