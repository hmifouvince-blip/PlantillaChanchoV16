using Guna.UI2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class BlurPanel : Guna2Panel
{
    private Bitmap cachedBitmap;
    private float blurAmount = 5.0f;

    private static List<BlurPanel> activeBlurPanels = new List<BlurPanel>();
    private static int processedBlurCount = 0;
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
                    MessageBox.Show($"Error al aplicar desenfoque: {ex.Message}");
                }
            }

            lock (activeBlurPanels)
            {
                processedBlurCount++;
                if (processedBlurCount == activeBlurPanels.Count)
                {
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

    private void ApplyGaussianBlur(Bitmap bitmap, float blurAmount, Color blurColor)
    {
        if (blurAmount <= 0)
            return;

        int width = bitmap.Width;
        int height = bitmap.Height;
        Bitmap blurredBitmap = new Bitmap(width, height);
        int radius = (int)Math.Ceiling(blurAmount);

        int kernelSize = 2 * radius + 1;
        double[,] kernel = new double[kernelSize, kernelSize];
        double sigma = blurAmount / 2.0;
        double sum = 0.0;

        for (int y = 0; y < kernelSize; y++)
        {
            for (int x = 0; x < kernelSize; x++)
            {
                int dx = x - radius;
                int dy = y - radius;
                kernel[x, y] = Math.Exp(-(dx * dx + dy * dy) / (2 * sigma * sigma)) / (2 * Math.PI * sigma * sigma);
                sum += kernel[x, y];
            }
        }

        for (int y = 0; y < kernelSize; y++)
        {
            for (int x = 0; x < kernelSize; x++)
            {
                kernel[x, y] /= sum;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double red = 0, green = 0, blue = 0;

                for (int ky = -radius; ky <= radius; ky++)
                {
                    for (int kx = -radius; kx <= radius; kx++)
                    {
                        int pixelX = Math.Min(Math.Max(x + kx, 0), width - 1);
                        int pixelY = Math.Min(Math.Max(y + ky, 0), height - 1);
                        Color pixelColor = bitmap.GetPixel(pixelX, pixelY);

                        double weight = kernel[kx + radius, ky + radius];

                        red += pixelColor.R * weight;
                        green += pixelColor.G * weight;
                        blue += pixelColor.B * weight;
                    }
                }

                double blendFactor = 0.5;
                int finalRed = (int)(red * (1 - blendFactor) + blurColor.R * blendFactor);
                int finalGreen = (int)(green * (1 - blendFactor) + blurColor.G * blendFactor);
                int finalBlue = (int)(blue * (1 - blendFactor) + blurColor.B * blendFactor);

                Color newColor = Color.FromArgb(
                    Math.Min(Math.Max(finalRed, 0), 255),
                    Math.Min(Math.Max(finalGreen, 0), 255),
                    Math.Min(Math.Max(finalBlue, 0), 255)
                );

                blurredBitmap.SetPixel(x, y, newColor);
            }
        }

        bitmap.Dispose();
        cachedBitmap = blurredBitmap;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            cachedBitmap?.Dispose();

            lock (activeBlurPanels)
            {
                activeBlurPanels.Remove(this);
            }
        }
        base.Dispose(disposing);
    }
}
