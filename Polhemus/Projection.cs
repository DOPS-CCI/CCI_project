using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Polhemus
{
    public class Projection
    {
        Triple Tx;
        Triple Tz;
        Triple _eye;
        public Triple Eye
        {
            get { return _eye; }
            set
            {
                Triple Ty = (-1D) * value.Norm();
                Tx = (new Triple(Ty[1], -Ty[0], 0)).Norm();
                Tz = Triple.Cross(Tx, Ty);
                _eye = value;
            }
        }
        public double FOV;

        public Projection(Triple eye, double fieldOfView)
        {
            Triple Ty = (-1D) * eye.Norm();
            Tx = (new Triple(Ty[1], -Ty[0], 0)).Norm();
            Tz = Triple.Cross(Tx, Ty);
            _eye = eye;
            FOV = fieldOfView;
        }

        public Triple Project(Triple point)
        {
            //used to calculate rotation of point as observed from and projected onto e
            double y1 = Tz * point;
            double x1 = Tx * point;
            double r1 = Math.Sqrt(x1 * x1 + y1 * y1);
            Triple d = Eye - point;
            double distance = d.Length();
            double theta = Math.Acos(Tz * d.Norm()) / (FOV * r1); //displacement from origin,
                //as observed from e as angular displacement
            Triple p = new Triple(x1 * theta, y1 * theta, distance); //NB: not a true vector!!
            return p;
        }
    }
}
