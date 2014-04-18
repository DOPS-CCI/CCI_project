using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Polhemus
{
    public class Projection
    {
        Triple Tx = new Triple(1, 0, 0);
        Triple Ty = new Triple(0, 1, 0);
        Triple Tz = new Triple(0, 0, 1);

        public double Eye;

        const double scaleFactor = 25D;
        const double horizonFactor = 0.75;

        //direction sines and cosines
        double sinPitch = 0D;
        double cosPitch = 1D;
        double sinRoll = 0D;
        double cosRoll = 1D;
        double sinYaw = 0D;
        double cosYaw = 1D;

        public Projection(double eye)
        {
            Eye = eye;
        }

        const double convertToRadians = Math.PI / 180D;
        public void ChangePitch(double theta)
        {
            sinPitch = Math.Sin(theta * convertToRadians);
            cosPitch = Math.Cos(theta * convertToRadians);
            calculateTy();
            calculateTz();
        }
        public void ChangeRoll(double theta)
        {
            sinRoll = Math.Sin(theta * convertToRadians);
            cosRoll = Math.Cos(theta * convertToRadians);
            calculateTx();
            calculateTy();
            calculateTz();
        }
        public void ChangeYaw(double theta)
        {
            sinYaw = Math.Sin(theta * convertToRadians);
            cosYaw = Math.Cos(theta * convertToRadians);
            calculateTx();
            calculateTy();
            calculateTz();
        }

        private void calculateTz()
        {
            Tz.v1 = -cosPitch * cosYaw * sinRoll + sinPitch * sinYaw;
            Tz.v2 = cosYaw * sinPitch + cosPitch * sinRoll * sinYaw;
            Tz.v3 = cosPitch * cosRoll;
        }

        private void calculateTy()
        {
            Ty.v1 = cosYaw * sinPitch * sinRoll + cosPitch * sinYaw;
            Ty.v2 = cosPitch * cosYaw - sinPitch * sinRoll * sinYaw;
            Ty.v3 = -cosRoll * sinPitch;
        }

        private void calculateTx()
        {
            Tx.v1 = cosRoll * cosYaw;
            Tx.v2 = -cosRoll * sinYaw;
            Tx.v3 = sinRoll;
        }
        public Triple Project(Triple point)
        {
            //used to calculate rotation of point as observed from and projected onto eye plane
            Triple p = new Triple(0, 0, Eye - Tz * point);
            if (p.v3 <= 0D) return p; //point behind eye plane: not going to show anyway
            double factor = scaleFactor / Math.Pow(p.v3, horizonFactor); //scale towards vanashing point
            p.v1 = factor * (Tx * point); //rotate and scale projected point
            p.v2 = factor * (Ty * point);
            return p;
        }

        double C22 = Math.Cos(Math.PI / 8D);
        double T67 = Math.Tan(3D * Math.PI / 8D);
        public string[] nameDirections()
        {
            double x, y;
            string[] d = new string[4];
            for (int i = 0; i < 4; i++) d[i] = "";
            if (Math.Abs(Tz.v1) < C22) //then R/L shows
            {
                x = Tx.v1;
                y = Ty.v1;
                if (x > 0 && Math.Abs(y / x) < T67) { d[1] = "R"; d[3] = "L"; }
                else
                    if (x < 0 && Math.Abs(y / x) < T67) { d[1] = "L"; d[3] = "R"; }
                if (y > 0 && Math.Abs(x / y) < T67) { d[0] = "R"; d[2] = "L"; }
                else
                    if (y < 0 && Math.Abs(x / y) < T67) { d[0] = "L"; d[2] = "R"; }

            }
            if (Math.Abs(Tz.v2) < C22) //then A/P shows
            {
                x = Tx.v2;
                y = Ty.v2;
                if (x > 0 && Math.Abs(y / x) < T67) { d[1] = d[1] + "A"; d[3] = d[3] + "P"; }
                else
                    if (x < 0 && Math.Abs(y / x) < T67) { d[1] = d[1] + "P"; d[3] = d[3] + "A"; }
                if (y > 0 && Math.Abs(x / y) < T67) { d[0] = d[0] + "A"; d[2] = d[2] + "P"; }
                else
                    if (y < 0 && Math.Abs(x / y) < T67) { d[0] = d[0] + "P"; d[2] = d[2] + "A"; }
            }
            if (Math.Abs(Tz.v3) < C22) //then S/I shows
            {
                x = Tx.v3;
                y = Ty.v3;
                if (x > 0 && Math.Abs(y / x) < T67) { d[1] = d[1] + "S"; d[3] = d[3] + "I"; }
                else
                    if (x < 0 && Math.Abs(y / x) < T67) { d[1] = d[1] + "I"; d[3] = d[3] + "S"; }
                if (y > 0 && Math.Abs(x / y) < T67) { d[0] = d[0] + "S"; d[2] = d[2] + "I"; }
                else
                    if (y < 0 && Math.Abs(x / y) < T67) { d[0] = d[0] + "I"; d[2] = d[2] + "S"; }
            }
            return d;
        }
    }
}
