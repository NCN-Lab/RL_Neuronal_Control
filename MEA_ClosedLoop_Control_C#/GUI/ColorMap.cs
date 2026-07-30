using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace GUI
{
    class ColorMap
    {
        int nColors;
        string cmap_type;
        int[][] colormap;
        
        public ColorMap()
        {
            nColors = 100;
            colormap = new int[nColors][];
            cmap_type = "BlueRed";
            CreateColormap(cmap_type);
        }

        public ColorMap(int ncolors)
        {
            nColors = ncolors;
            colormap = new int[nColors][];
            cmap_type = "BlueRed";
            CreateColormap(cmap_type);
        }

        public ColorMap(int ncolors, string cMap)
        {
            nColors = ncolors;
            colormap = new int[nColors][];
            cmap_type = cMap;
            CreateColormap(cmap_type);
        }

        public void CreateColormap(string type)
        {
            colormap[0] = new int[] { 255, 255, 255 };
            int[] RGB = new int[3];

            for (int i = 1; i < nColors; i++)
            {
                if (type == "BlueRed")
                    RGB = BlueRed_colormap(i);

                else if (type == "WhiteBlack")
                    RGB = WhiteBlack_colormap(i);

                else if (type == "RedBlue")
                    RGB = RedBlue_colormap(i);

                else if (type == "RedGreen")
                    RGB = RedGreen_colormap(i);

                else if (type == "GreenRed")
                    RGB = GreenRed_colormap(i);

                else if (type == "Jet")
                    RGB = Jet_colormap(i);

                colormap[i] = new int[3];
                colormap[i][0] = RGB[0];
                colormap[i][1] = RGB[1];
                colormap[i][2] = RGB[2];
            }
        }

        private int[] BlueRed_colormap(int i)
        {
            double r;
            double g;
            double b;

            r = 255 * i / nColors;
            g = 0;
            b = 255 * (nColors - i) / nColors;

            int[] RGB = new int[3];
            RGB[0] = (int)Math.Round(r);
            RGB[1] = (int)Math.Round(g);
            RGB[2] = (int)Math.Round(b);

            return RGB;
        }


        private int[] RedGreen_colormap(int i)
        {
            double r;
            double g;
            double b;

            r = 255 * (nColors - i) / nColors; 
            g = 255 * i / nColors;
            b = 0;

            int[] RGB = new int[3];
            RGB[0] = (int)Math.Round(r);
            RGB[1] = (int)Math.Round(g);
            RGB[2] = (int)Math.Round(b);

            return RGB;
        }

        private int[] GreenRed_colormap(int i)
        {
            double r;
            double g;
            double b;

            r = 255 * i / nColors;
            g = 255 * (nColors - i) / nColors;
            b = 0;

            int[] RGB = new int[3];
            RGB[0] = (int)Math.Round(r);
            RGB[1] = (int)Math.Round(g);
            RGB[2] = (int)Math.Round(b);

            return RGB;
        }

        private int[] RedBlue_colormap(int i)
        {
            double r;
            double g;
            double b;

            r = 255 * (nColors - i) / nColors;
            b = 255 * i / nColors;
            g = 0;

            int[] RGB = new int[3];
            RGB[0] = (int)Math.Round(r);
            RGB[1] = (int)Math.Round(g);
            RGB[2] = (int)Math.Round(b);

            return RGB;
        }



        private int[] WhiteBlack_colormap(int i)
        {
            double r;
            double g;
            double b;

            r = 255 * (nColors - i) / nColors;
            g = 255 * (nColors - i) / nColors;
            b = 255 * (nColors - i) / nColors;

            int[] RGB = new int[3];
            RGB[0] = (int)Math.Round(r);
            RGB[1] = (int)Math.Round(g);
            RGB[2] = (int)Math.Round(b);

            return RGB;
        }


        private int[] Jet_colormap(int i)
        {
            double r;
            double g;
            double b;
            double n;

            n = 4 * (double)i / nColors;
            r = 255 * Math.Min(Math.Max(Math.Min(n - 1.5, -n + 4.5), 0), 1);
            g = 255 * Math.Min(Math.Max(Math.Min(n - 0.5, -n + 3.5), 0), 1);
            b = 255 * Math.Min(Math.Max(Math.Min(n + 0.5, -n + 2.5), 0), 1);

            int[] RGB = new int[3];
            RGB[0] = (int)Math.Round(r);
            RGB[1] = (int)Math.Round(g);
            RGB[2] = (int)Math.Round(b);

            return RGB;
        }



        public void Set_Colormap(string cMap)
        {
            cmap_type = cMap;
            CreateColormap(cmap_type);
        }


        public void Set_Colormap(int [][] newColormap)
        {
            colormap = newColormap;
        }


        public int[][] Get_Colormap()
        {
            return colormap;
        }


        public int Get_nColors()
        {
            return nColors;
        }

        /*
        public int[] Get_RGB_Color(int n)
        {
            return colormap[n];
        }
        */
        public Color Get_RGB_Color(int n)
        {
            int[] RGB = colormap[n];
            return Color.FromArgb(255, RGB[0], RGB[1], RGB[2]);
        }


        public Color Get_RGB_Color(double frac)
        {
            int i = (int)Math.Round(frac * (nColors-1));
            int[] RGB = colormap[i];
            return Color.FromArgb(255, RGB[0], RGB[1], RGB[2]);
        }
    }
}
