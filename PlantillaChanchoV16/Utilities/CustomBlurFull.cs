using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class BlurPanelFull : Guna2Panel
{
    private Bitmap cachedBitmap;
    private float blurAmount = 5.0f;

    private static List<BlurPanelFull> activeBlurPanels = new List<BlurPanelFull>();
    // Suivi par-panneau (voir CustomBlur.cs / BlurPanel pour l'explication complète) : un
    // compteur monotone comparé à une liste qui rétrécit/grandit se désynchronise
    // définitivement après une reconstruction de fenêtre.
    private static readonly HashSet<BlurPanelFull> paintedPanels = new HashSet<BlurPanelFull>();
    public static event Action AllBlurPanelsProcessed;

    public Color BlurColor { get; set; } = Color.Transparent;
    public float BlurAmount
    {
        get { return blurAmount; }
        set
        {
            if (value > 0)
            {
                blurAmount = value;
                Invalidate();
            }
        }
    }

    public float CornerRadius { get; set; } = 10.0f;

    public BlurPanelFull()
    {
        this.Resize += (s, e) => Invalidate();
        this.Paint += BlurPanel_Paint;

        lock (activeBlurPanels)
        {
            activeBlurPanels.Add(this);
        }
    }

    private void BlurPanel_Paint(object sender, PaintEventArgs e)
    {
        if (cachedBitmap == null || cachedBitmap.Width != this.Width || cachedBitmap.Height != this.Height)
        {
            cachedBitmap?.Dispose();

            cachedBitmap = new Bitmap(this.Width, this.Height);

            using (Graphics g = Graphics.FromImage(cachedBitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                try
                {
                    this.DrawToBitmap(cachedBitmap, new Rectangle(0, 0, this.Width, this.Height));

                    ApplyGaussianBlur(cachedBitmap, BlurAmount, BlurColor);
                }
                catch (Exception ex)
                {
                    PlantillaChanchoV16.Template.SakuraMessageBox.Show($"Error al aplicar desenfoque: {ex.Message}");
                }
            }

            lock (activeBlurPanels)
            {
                paintedPanels.Add(this);
                bool allDone = activeBlurPanels.Count > 0;
                if (allDone)
                    foreach (var p in activeBlurPanels)
                        if (!paintedPanels.Contains(p)) { allDone = false; break; }

                if (allDone)
                {
                    paintedPanels.Clear();
                    AllBlurPanelsProcessed?.Invoke();
                }
            }
        }

        if (cachedBitmap != null)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                float diameter = CornerRadius * 2;
                RectangleF rect = new RectangleF(0, 0, this.Width, this.Height);
                RectangleF arcRect = new RectangleF(rect.Location, new SizeF(diameter, diameter));

                path.AddArc(arcRect, 180, 90);

                arcRect.X = rect.Right - diameter;
                path.AddArc(arcRect, 270, 90);

                arcRect.Y = rect.Bottom - diameter;
                path.AddArc(arcRect, 0, 90);

                arcRect.X = rect.Left;
                path.AddArc(arcRect, 90, 90);

                path.CloseFigure();

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.SetClip(path);

                e.Graphics.DrawImage(cachedBitmap, 0, 0);
            }
        }
    }

    // Flou gaussien RAPIDE (LockBits + noyau SÉPARABLE en 2 passes 1D) — voir CustomBlur.cs
    // pour l'explication complète. Remplace la version pixel-par-pixel (GetPixel/SetPixel)
    // qui prenait plusieurs secondes = onglet produits qui "ramait" au 1er affichage.
    private void ApplyGaussianBlur(Bitmap bitmap, float blurAmount, Color blurColor)
    {
        if (blurAmount <= 0)
            return;

        int width = bitmap.Width;
        int height = bitmap.Height;
        if (width <= 0 || height <= 0)
            return;

        int radius = (int)Math.Ceiling(blurAmount);
        int kernelSize = 2 * radius + 1;
        float[] kernel = new float[kernelSize];
        double sigma = blurAmount / 2.0;
        double sum = 0.0;
        for (int i = 0; i < kernelSize; i++)
        {
            int d = i - radius;
            kernel[i] = (float)Math.Exp(-(d * d) / (2 * sigma * sigma));
            sum += kernel[i];
        }
        for (int i = 0; i < kernelSize; i++)
            kernel[i] = (float)(kernel[i] / sum);

        Rectangle rect = new Rectangle(0, 0, width, height);
        BitmapData srcData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        int stride = srcData.Stride;
        byte[] src = new byte[stride * height];
        Marshal.Copy(srcData.Scan0, src, 0, src.Length);
        bitmap.UnlockBits(srcData);

        int n = width * height;
        float[] tmpR = new float[n];
        float[] tmpG = new float[n];
        float[] tmpB = new float[n];
        for (int y = 0; y < height; y++)
        {
            int rowBase = y * stride;
            for (int x = 0; x < width; x++)
            {
                float r = 0, g = 0, b = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int xx = x + k;
                    if (xx < 0) xx = 0; else if (xx >= width) xx = width - 1;
                    int idx = rowBase + xx * 4;
                    float w = kernel[k + radius];
                    b += src[idx] * w;
                    g += src[idx + 1] * w;
                    r += src[idx + 2] * w;
                }
                int o = y * width + x;
                tmpR[o] = r; tmpG[o] = g; tmpB[o] = b;
            }
        }

        byte[] dst = new byte[stride * height];
        const float blend = 0.5f;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float r = 0, g = 0, b = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    int yy = y + k;
                    if (yy < 0) yy = 0; else if (yy >= height) yy = height - 1;
                    int o = yy * width + x;
                    float w = kernel[k + radius];
                    r += tmpR[o] * w;
                    g += tmpG[o] * w;
                    b += tmpB[o] * w;
                }
                int idx = y * stride + x * 4;
                dst[idx] = ClampByte(b * (1 - blend) + blurColor.B * blend);
                dst[idx + 1] = ClampByte(g * (1 - blend) + blurColor.G * blend);
                dst[idx + 2] = ClampByte(r * (1 - blend) + blurColor.R * blend);
                dst[idx + 3] = 255;
            }
        }

        Bitmap blurredBitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        BitmapData dstData = blurredBitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(dst, 0, dstData.Scan0, dst.Length);
        blurredBitmap.UnlockBits(dstData);

        bitmap.Dispose();
        cachedBitmap = blurredBitmap;
    }

    private static byte ClampByte(float v)
    {
        if (v < 0) return 0;
        if (v > 255) return 255;
        return (byte)v;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            cachedBitmap?.Dispose();

            lock (activeBlurPanels)
            {
                activeBlurPanels.Remove(this);
                paintedPanels.Remove(this);
            }
        }
        base.Dispose(disposing);
    }
}

