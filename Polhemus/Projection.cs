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

        const double horizonFactor = 90D;

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
            //used to calculate rotation of point as observed from and projected onto eye
            Triple p = new Triple(0, 0, Eye - Tz * point);
            if (p.v3 <= 0D) return p; //point behind plane: not going to show anyway
            double factor = horizonFactor / p.v3;
            p.v1 = factor * (Tx * point);
            p.v2 = factor * (Ty * point);
//            double theta = Math.Acos(Ty * d.Norm()) / (FOV * r1); //displacement from origin,
                //as observed from e as angular displacement
            return p;
        }
    }
}
