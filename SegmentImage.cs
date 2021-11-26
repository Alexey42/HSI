using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSI
{
    struct Edge {
        public float w;
        public int a, b;
    }

    struct UElement
    {
        public int rank, p, size;
    }

    class Universe
    {
        int num;
        UElement[] elements;

        public Universe(int size)
        {
            elements = new UElement[size];
            num = size;
            for (int i = 0; i < size; i++)
            {
                elements[i].rank = 0;
                elements[i].size = 1;
                elements[i].p = i;
            }
        }

        public int Find(int x)
        {
            int y = x;
            while (y != elements[y].p)
                y = elements[y].p;
            elements[x].p = y;
            return y;
        }

        public void Join(int x, int y)
        {
            if (elements[x].rank > elements[y].rank)
            {
                elements[y].p = x;
                elements[x].size += elements[y].size;
            }
            else
            {
                elements[x].p = y;
                elements[y].size += elements[x].size;
                if (elements[x].rank == elements[y].rank)
                    elements[y].rank++;
            }
            num--;
        }

        public int GetSize(int x) { return elements[x].size; }

        public int GetNumSets() { return num; }
}

    public static class SegmentImage
    {

        static float GetAngle(Vec3b a, Vec3b b)
        {
            float scalar = a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
            float k1 = (float)Math.Sqrt(Math.Pow(a[0], 2) + Math.Pow(a[1], 2) + Math.Pow(a[2], 2));
            float k2 = (float)Math.Sqrt(Math.Pow(b[0], 2) + Math.Pow(b[1], 2) + Math.Pow(b[2], 2));
            float cos = scalar / (k1 * k2);

            return (float)(Math.Acos(cos) * 180 / Math.PI);
        }

        /*
            Efficient Graph-Based Image Segmentation
            P. Felzenszwalb, D. Huttenlocher
            International Journal of Computer Vision, Vol. 59, No. 2, September 2004 
        */
        public static Mat SegmentAngle(Mat image, float sigma, float c, int min_size, out int ccsNum, BackgroundWorker backgroundWorker)
        {
            int width = image.Cols;
            int height = image.Rows;
            int verticesNum = width * height;
            
            Mat smoothed = image.GaussianBlur(Size.Zero, sigma);
            Vec3b[] array;
            smoothed.GetArray(out array);
            smoothed.Dispose();
            //int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Edge));
            //List<Edge> edges = new List<Edge>(width * height * 4);
            Edge[] edges = new Edge[verticesNum * 4];
            int edgesNum = 0;
            backgroundWorker.ReportProgress(5);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x < width - 1)
                    {
                        //var s = new Edge();
                        edges[edgesNum].a = y * width + x;
                        edges[edgesNum].b = y * width + (x + 1);
                        edges[edgesNum].w = GetAngle(array[y * width + x], array[y * width + x + 1]);
                        //edges.Add(s);
                        edgesNum++;
                    }
                    if (y < height - 1)
                    {
                        //var s = new Edge();
                        edges[edgesNum].a = y * width + x;
                        edges[edgesNum].b = (y + 1) * width + x;
                        edges[edgesNum].w = GetAngle(array[y * width + x], array[(y + 1) * width + x]);
                        //edges.Add(s);
                        edgesNum++;
                    }
                    if ((x < width - 1) && (y < height - 1))
                    {
                        //var s = new Edge();
                        edges[edgesNum].a = y * width + x;
                        edges[edgesNum].b = (y + 1) * width + (x + 1);
                        edges[edgesNum].w = GetAngle(array[y * width + x], array[(y + 1) * width + x + 1]);
                        //edges.Add(s);
                        edgesNum++;
                    }
                    if ((x < width - 1) && (y > 0))
                    {
                        //var s = new Edge();
                        edges[edgesNum].a = y * width + x;
                        edges[edgesNum].b = (y - 1) * width + (x + 1);
                        edges[edgesNum].w = GetAngle(array[y * width + x], array[(y - 1) * width + x + 1]);
                        //edges.Add(s);
                        edgesNum++;
                    }
                }
            }

            backgroundWorker.ReportProgress(30);
            //edges.Sort((x, y) => x.w.CompareTo(y.w));
            Array.Sort(edges, (x, y) => x.w.CompareTo(y.w));

            Universe u = new Universe(verticesNum);
            float[] thresholds = new float[verticesNum];

            for (int i = 0; i < verticesNum; i++)
                thresholds[i] = c;

            for (int i = 0; i < edgesNum; i++)
            {
                Edge pedge = edges[i];
                int a = u.Find(pedge.a);
                int b = u.Find(pedge.b);
                if (a != b)
                {
                    if ((pedge.w <= thresholds[a]) && (pedge.w <= thresholds[b]))
                    {
                        u.Join(a, b);
                        a = u.Find(a);
                        thresholds[a] = pedge.w + (c / u.GetSize(a));
                    }
                }
            }
            backgroundWorker.ReportProgress(60);

            // post process small components
            for (int i = 0; i < edgesNum; i++)
            {
                int a = u.Find(edges[i].a);
                int b = u.Find(edges[i].b);
                if ((a != b) && ((u.GetSize(a) < min_size) || (u.GetSize(b) < min_size)))
                    u.Join(a, b);
            }

            ccsNum = u.GetNumSets(); // number of connected components in the segmentation

            Vec3i[] colors = new Vec3i[verticesNum]; // or sparse arrays if necessary
            int[] div = new int[verticesNum];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int comp = u.Find(y * width + x);
                    div[comp]++;
                    colors[comp].Item0 += array[y * width + x].Item0;
                    colors[comp].Item1 += array[y * width + x].Item1;
                    colors[comp].Item2 += array[y * width + x].Item2;
                }
            }
            backgroundWorker.ReportProgress(80);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int comp = u.Find(y * width + x);
                    array[y * width + x].Item0 = (byte)(colors[comp].Item0 / div[comp]);
                    array[y * width + x].Item1 = (byte)(colors[comp].Item1 / div[comp]);
                    array[y * width + x].Item2 = (byte)(colors[comp].Item2 / div[comp]);
                }
            }
            backgroundWorker.ReportProgress(100);

            Mat result = new Mat(height, width, MatType.CV_8UC3, array);
            return result;
        }
    }
}
