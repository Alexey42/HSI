using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace HSI
{
    public class Model
    {
        public string name;
        public System.Windows.Media.Color color;
        public System.Windows.Media.Color userColor;
        public int[,] data;
        public double coverPercentage;
        public int coverPixels;
        public long coverMetres;

        public Model()
        {

        }

    }
}
