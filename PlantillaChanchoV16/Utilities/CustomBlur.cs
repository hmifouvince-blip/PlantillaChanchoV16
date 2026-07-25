using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class BlurPanel : Guna2Panel
{
    private Bitmap cachedBitmap;
    private float blurAmount = 5.0f;

    private static List<BlurPanel> activeBlurPanels = new List<BlurPanel>();
    // Suivi par-panneau (au lieu d'un compteur monotone comparé à une liste qui rétrécit/
    // grandit) : sinon, après une reconstruction (changement de thème/langue), le compteur
    // "dépasse" définitivement la taille de la nouvelle liste et l'événement ne se déclenche
    // plus jamais -> les produits ne sont jamais réassignés à leur vue = accueil vide.
    private static readonly HashSet<BlurPanel> paintedPanels = new HashSet<BlurPanel>();
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

    public float BottomRadius { get; set; } = 10.0f;

    public BlurPanel()
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
                    paintedPanels.Clear(); // repart à zéro pour le prochain lot (prochaine vue/reconstruction)
                    AllBlurPanelsProcessed?.Invoke();
                }
            }
        }

        if (cachedBitmap != null)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddLine(0, 0, this.Width, 0);
                path.AddLine(this.Width, 0, this.Width, this.Height - BottomRadius);
                path.AddArc(this.Width - BottomRadius * 2, this.Height - BottomRadius * 2, BottomRadius * 2, BottomRadius * 2, 0, 90);
                path.AddLine(this.Width, this.Height - BottomRadius, this.Width - BottomRadius, this.Height);
                path.AddLine(this.Width - BottomRadius, this.Height, BottomRadius, this.Height);
                path.AddArc(0, this.Height - BottomRadius * 2, BottomRadius * 2, BottomRadius * 2, 90, 90);
                path.AddLine(0, this.Height - BottomRadius, 0, 0);

                path.CloseFigure();

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.SetClip(path);

                e.Graphics.DrawImage(cachedBitmap, 0, 0);
            }
        }
    }

    // Flou gaussien RAPIDE (LockBits + noyau SÉPARABLE en 2 passes 1D). Le flou gaussien 2D
    // est séparable -> appliquer un noyau 1D horizontalement puis verticalement donne un
    // résultat IDENTIQUE au noyau 2D, mais en O(W*H*K) au lieu de O(W*H*K^2), et sans le
    // moindre GetPixel/SetPixel (accès direct au buffer). L'ancienne version pixel-par-pixel
    // prenait plusieurs SECONDES pour l'ensemble des cartes -> l'onglet produits "ramait" au
    // 1er affichage et après chaque changement de thème/langue (le flou est recalculé).
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

        // Passe horizontale : src -> buffers float (précision).
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

        // Passe verticale : buffers -> dst, avec mélange (blend 0.5) vers blurColor.
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
                dst[idx + 3] = 255; // opaque, comme l'ancienne version (Color.FromArgb(r,g,b))
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
