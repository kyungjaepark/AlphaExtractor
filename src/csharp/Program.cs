using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace TransparencyExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            Convert(@"white.png", @"black.png", @"result.png");
        }

        static void Convert(string whiteBackgroundImagePath, string blackBackgroundImagePath, string resultImagePath)
        {
            // prepare coloralpha map
            // key : [color_white][color_black] (byte)
            // value : [alpha][color_result] (byte)
            Dictionary<uint, uint> colorAlphaMap = new Dictionary<uint, uint>();
            for (uint c_white = 0; c_white < 256; c_white++)
            {
                for (uint c_black = 0; c_black < 256; c_black++)
                {
                    float alpha = 1.0f - ((float)c_white - (float)c_black) / 255.0f;
                    if (Math.Floor(alpha * 255.0f) <= 0)
                        alpha = 0.0f;

                    uint color = c_white;
                    if (alpha != 0)
                    {
                        uint color_candi_a = (uint)Math.Min(255.0f, Math.Max(0.0f, (255.0f + ((float)c_white - 255.0f) / alpha)));
                        uint color_candi_b = (uint)Math.Min(255.0f, Math.Max(0.0f, (float)c_black / alpha));
                        color = Math.Min(255, (color_candi_a + color_candi_b) / 2);
                    }

                    uint alpha_uint = (uint)(Math.Min(255.0f, alpha * 256.0f));
                    colorAlphaMap[(c_white << 8) | (c_black & 0xff)] = ((alpha_uint << 8) | (color & 0xff));
                }
            }

            Bitmap i_white = new Bitmap(whiteBackgroundImagePath);
            Bitmap i_black = new Bitmap(blackBackgroundImagePath);
            Bitmap i_result = new Bitmap(i_white.Width, i_white.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < i_white.Height; y++)
            {
                for (int x = 0; x < i_white.Width; x++)
                {
                    // calc pixel
                    uint whiteARGB = (uint)Color.White.ToArgb();
                    uint blackARGB = (uint)Color.Black.ToArgb();

                    uint argb_white = (uint)i_white.GetPixel(x, y).ToArgb();
                    uint argb_black = (uint)i_black.GetPixel(x, y).ToArgb();

                    Color c_result = Color.Transparent;
                    if (argb_white == whiteARGB && argb_black == blackARGB)
                    {
                        c_result = Color.Transparent;
                    }
                    else if (argb_white == argb_black)
                    {
                        c_result = Color.FromArgb((int)argb_white);
                    }
                    else
                    {
                        uint r_white = ((argb_white >> 16) & 0xff);
                        uint g_white = ((argb_white >> 8) & 0xff);
                        uint b_white = ((argb_white >> 0) & 0xff);
                        uint r_black = ((argb_black >> 16) & 0xff);
                        uint g_black = ((argb_black >> 8) & 0xff);
                        uint b_black = ((argb_black >> 0) & 0xff);

                        uint colorAlphaMap_r = colorAlphaMap[(r_white << 8) | (r_black)];
                        uint colorAlphaMap_g = colorAlphaMap[(g_white << 8) | (g_black)];
                        uint colorAlphaMap_b = colorAlphaMap[(b_white << 8) | (b_black)];
                        uint alpha = Math.Min(Math.Min(colorAlphaMap_r & 0xff00, colorAlphaMap_g & 0xff00), colorAlphaMap_b & 0xff00);
                        alpha = (alpha >> 8) & 0xff;
                        uint argb_result = ((alpha) << 24)
                            | ((colorAlphaMap_r & 0xff) << 16)
                            | ((colorAlphaMap_g & 0xff) << 8)
                            | ((colorAlphaMap_b & 0xff) << 0);

                        c_result = Color.FromArgb((int)argb_result);
                    }
                    i_result.SetPixel(x, y, c_result);
                }
            }
            i_result.Save(resultImagePath);
        }
    }
}
