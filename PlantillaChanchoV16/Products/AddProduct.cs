using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using PlantillaChanchoV16.Utilities;
using System.ComponentModel;
using PlantillaChanchoV16.Template;
using PlantillaChanchoV16;

public class AddProduct
{
    private Guna2Panel containerSpecialProduct;

    private Dictionary<string, Guna2Panel> cachedViews = new Dictionary<string, Guna2Panel>();
    private Guna2Panel containerMain;
    private Guna2Panel containerFavProducts;
    private List<Guna2Panel> views = new List<Guna2Panel>();
    private int currentViewIndex = 0;
    private Action<string> openGameDetails;
    private Images images;
    private bool expandedViewProducts;
    private Guna2HtmlLabel _countAllProducts;
    public AddProduct(Guna2Panel containerMain, Guna2Panel containerSpecialProduct, Guna2Panel containerFavProducts, Guna2HtmlLabel _countAllProducts, Action<string> openGameDetails, Images images)
    {
        this.containerMain = containerMain;
        this.containerSpecialProduct = containerSpecialProduct;
        this.containerFavProducts = containerFavProducts;
        this.openGameDetails = openGameDetails;
        this.images = images;
        this._countAllProducts = _countAllProducts;

       
    }

    public Guna2Panel GetOrCreateProductContainer(string key, bool visible = true)
    {
        if (cachedViews.TryGetValue(key, out var cachedContainer))
        {
            cachedContainer.Visible = visible;
            return cachedContainer;
        }

        var newContainer = CreateProductContainer(visible);
        cachedViews[key] = newContainer;
        views.Add(newContainer);
        return newContainer;
    }

    private Guna2Panel CreateProductContainer(bool visible)
    {
        var containerForAllProducts = new Guna2Panel
        {
            BackColor = Color.Transparent,
            FillColor = Color.Transparent,
            AutoScroll = false,
            BorderThickness = 0,
            Visible = visible,
            BorderColor = Color.White,
            Width = containerMain.Width,
            Height = containerMain.Height - 85,
            AutoSize = false,
        };

        containerForAllProducts.Location = new Point(-4, 52);

        containerForAllProducts.GetType()
            .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(containerForAllProducts, true, null);

        containerForAllProducts.GetType()
            .GetMethod("SetStyle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.Invoke(containerForAllProducts, new object[] {
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer, true
            });

        containerMain.Controls.Add(containerForAllProducts);
        return containerForAllProducts;
    }







    private void AddProductsToContainer(Guna2Panel container, ProductView[] products)
    {
        int rows = 3;
        int cols = 3;
        int spacing = 8;

        int sizeIncrement = 0;

        int productWidth = Default.widthProduct - sizeIncrement;
        int productHeight = Default.heightProduct - sizeIncrement;

        for (int i = 0; i < products.Length; i++)
        {
            int row = i / cols;
            int col = i % cols;

            products[i].Width = productWidth;
            products[i].Height = productHeight;

            products[i].Location = new Point(
                col * (productWidth + spacing) + container.Padding.Left,
                row * (productHeight + spacing) + container.Padding.Top
            );

            container.Controls.Add(products[i]);
        }
    }

    private void ReassignProductsToFinalContainers(ProductView[] products, Guna2Panel targetContainer)
    {
        foreach (var product in products)
        {
            targetContainer.Controls.Add(product);
        }
        targetContainer.Visible = true;
    }

    public void ShowView(string key)
    {
        foreach (var view in views)
        {
            view.Visible = false;
        }

        if (cachedViews.TryGetValue(key, out var viewToShow))
        {
            viewToShow.Visible = true;
            viewToShow.BringToFront();
            currentViewIndex = views.IndexOf(viewToShow);
        }
    }

    public void NavigateToNextView()
    {
        if (views.Count <= 1) return;
        currentViewIndex = (currentViewIndex + 1) % views.Count;
        views[currentViewIndex].Visible = true;
        views[currentViewIndex].BringToFront();
    }

    public void NavigateToPreviousView()
    {
        if (views.Count <= 1) return;
        currentViewIndex = (currentViewIndex - 1 + views.Count) % views.Count;
        views[currentViewIndex].Visible = true;
        views[currentViewIndex].BringToFront();
    }


    public void ShowTotalProductCount()
    {
        int totalProductCount = 0;

        foreach (var view in views)
        {
            totalProductCount += view.Controls.Count;
        }

        _countAllProducts.Text = $"({totalProductCount})";
    }

    public void InitializeProducts()
    {
        // Contenedor inicial donde estarán todos los productos
        var initialContainer = GetOrCreateProductContainer("initial", true);

        // Contenedores finales [ IMPORANT ]
        var container1 = GetOrCreateProductContainer("view1", true);
        var container2 = GetOrCreateProductContainer("view2", true);

        // Colecciones de productos [ IMPORANT ]
        var productsView1 = GetProductsView1();
        var productsView2 = GetProductsView2();

        // Añadir TODOS los productos al contenedor inicial [ IMPORANT ]
        AddProductsToContainer(initialContainer, productsView1);
        AddProductsToContainer(initialContainer, productsView2);

        views.Clear();
        BlurPanel.AllBlurPanelsProcessed += async () =>
        {
            // Redistribuir los productos a sus vistas correspondientes [ IMPORANT ]
            ReassignProductsToFinalContainers(productsView1, container1);
            ReassignProductsToFinalContainers(productsView2, container2);

            containerMain.Controls.Remove(initialContainer);
            initialContainer.Dispose();

            // Agregar vistas finales [ IMPORANT ]
            views.Add(container1);
            views.Add(container2);

            currentViewIndex = 0;
            views[currentViewIndex].Visible = true;
            views[currentViewIndex].BringToFront();
            ShowTotalProductCount();
            ShowView("view1");
            containerSpecialProduct.Visible = true;
            containerMain.Location = new Point(containerMain.Location.X, containerSpecialProduct.Bottom);

            expandedViewProducts = false;

            await Task.Delay(100);
        };


    }


   

    private ProductView[] GetProductsView1() // VIEW 1 : MAX 9 PRODUCTS
    {
        return new[]
        {
            new ProductView(productActive: true, productUnderMaintenance: false, "Spoofer", "12.01.2025", images.LogoWoofer, images.Img2Anydesk, () => openGameDetails("anydesk")),
            //new ProductView(productActive: true, productUnderMaintenance: false, "CS:GO 2", "12.01.2025", images.LogoCsgo, images.BgCsgo, () => openGameDetails("csgo")), // CODE FOR REMOTE UPDATE DATE --> $"{Login.KeyAuthApp.var("variable_name")}"
            //new ProductView(productActive: true, productUnderMaintenance: false, "GTA V", "12.01.2025", images.LogoGta5, images.BgGta5, () => openGameDetails("gta5")),
            //new ProductView(productActive: true, productUnderMaintenance: false, "MINECRAFT", "12.01.2025", images.LogoMinecraft, images.BgMinecraft, () => openGameDetails("minecraft")),
            new ProductView(productActive: true, productUnderMaintenance: true, "ROBLOX", "12.01.2025", images.LogoRoblox, images.BgRoblox, () => openGameDetails("roblox")),
            //new ProductView(productActive: true, productUnderMaintenance: false, "RED DEAD REDEMPTION 2", "12.01.2025", images.LogoRd2, images.BgRd2, () => openGameDetails("rd2")),
            new ProductView(productActive: true, productUnderMaintenance: false, "VALORANT", "12.01.2025", images.LogoValorant, images.BgValorant, () => openGameDetails("valorant")),
            //new ProductView(productActive: true, productUnderMaintenance: false, "FREE FIRE", "12.01.2025", images.LogoFreefire, images.BgFreefire, () => openGameDetails("freefire")),
            //new ProductView(productActive: false, productUnderMaintenance: false, "PUBG", "12.01.2025", images.LogoPubg, images.BgPubg, () => openGameDetails("pubg")),
            //new ProductView(productActive: true, productUnderMaintenance: false, "FORTNITE", "12.01.2025", images.LogoFortnite, images.BgFortnite, () => openGameDetails("fortnite")),
        };
    }

    private ProductView[] GetProductsView2() // VIEW 2 : MAX 9 PRODUCTS
    {
        return new[]
        {
            new ProductView(productActive: true, productUnderMaintenance: false, "DOTA 2", "03.12.2024", images.BgDota2, images.BgDota2, () => openGameDetails("dota2")),
            //new ProductView(productActive: true, productUnderMaintenance: false, "TEAM FORTRESS 2", "03.12.2024", images.LogoTf2, images.BgTf2, () => openGameDetails("tf2"))
        };
    }




    public int CurrentViewIndex => currentViewIndex;
    public int ViewCount => views.Count;
}