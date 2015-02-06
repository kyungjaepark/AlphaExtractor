using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

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

            Rectangle imageRect = new Rectangle(0, 0, i_white.Width, i_white.Height);

            BitmapData bd_white = i_white.LockBits(imageRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData bd_black = i_black.LockBits(imageRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData bd_result = i_result.LockBits(imageRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            long ptr_white = bd_white.Scan0.ToInt64();
            long ptr_black = bd_black.Scan0.ToInt64();
            long ptr_result = bd_result.Scan0.ToInt64();

            byte[] buf_white = new byte[imageRect.Width * 3];
            byte[] buf_black = new byte[imageRect.Width * 3];
            byte[] buf_result = new byte[imageRect.Width * 4];

            for (int y = 0; y < imageRect.Height; y++)
            {
                Marshal.Copy(new IntPtr(ptr_white + y * bd_white.Stride), buf_white, 0, imageRect.Width * 3);
                Marshal.Copy(new IntPtr(ptr_black + y * bd_black.Stride), buf_black, 0, imageRect.Width * 3);
                for (int x = 0; x < imageRect.Width; x++)
                {
                    // calc pixel
                    byte r_white = buf_white[x * 3 + 2];
                    byte g_white = buf_white[x * 3 + 1];
                    byte b_white = buf_white[x * 3 + 0];
                    byte r_black = buf_black[x * 3 + 2];
                    byte g_black = buf_black[x * 3 + 1];
                    byte b_black = buf_black[x * 3 + 0];

                    Color c_result = Color.Transparent;
                    if (r_white == 0xff && g_white == 0xff && b_white == 0xff && r_black == 0 && g_black == 0 && b_black == 0)
                    {
                        buf_result[x * 4 + 3] = 0;
                        buf_result[x * 4 + 2] = 0;
                        buf_result[x * 4 + 1] = 0;
                        buf_result[x * 4 + 0] = 0;
                    }
                    else if (r_white == r_black && g_white == g_black && b_white == b_black)
                    {
                        buf_result[x * 4 + 3] = 0xff;
                        buf_result[x * 4 + 2] = r_white;
                        buf_result[x * 4 + 1] = g_white;
                        buf_result[x * 4 + 0] = b_white;
                    }
                    else
                    {
                        uint colorAlphaMap_r = colorAlphaMap[(((uint)r_white) << 8) | (r_black)];
                        uint colorAlphaMap_g = colorAlphaMap[(((uint)g_white) << 8) | (g_black)];
                        uint colorAlphaMap_b = colorAlphaMap[(((uint)b_white) << 8) | (b_black)];
                        uint alpha = Math.Min(Math.Min(colorAlphaMap_r & 0xff00, colorAlphaMap_g & 0xff00), colorAlphaMap_b & 0xff00);
                        alpha = (alpha >> 8) & 0xff;

                        buf_result[x * 4 + 3] = (byte)alpha;
                        buf_result[x * 4 + 2] = (byte)(colorAlphaMap_r & 0xff);
                        buf_result[x * 4 + 1] = (byte)(colorAlphaMap_g & 0xff);
                        buf_result[x * 4 + 0] = (byte)(colorAlphaMap_b & 0xff);

                        uint argb_result = ((alpha) << 24)
                            | ((colorAlphaMap_r & 0xff) << 16)
                            | ((colorAlphaMap_g & 0xff) << 8)
                            | ((colorAlphaMap_b & 0xff) << 0);

                        c_result = Color.FromArgb((int)argb_result);
                    }
                }
                Marshal.Copy(buf_result, 0, new IntPtr(ptr_result + y * bd_result.Stride), imageRect.Width * 4);
            }

            i_white.UnlockBits(bd_white);
            i_black.UnlockBits(bd_black);
            i_result.UnlockBits(bd_result);

            i_result.Save(resultImagePath);
        }
    }
}
