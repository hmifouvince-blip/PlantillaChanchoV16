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

    // Un "viewport" = une zone à taille FIXE qui affiche une portion défilable de son
    // contenu (Content, en AutoSize) via une scrollbar -> permet d'atteindre les produits
    // qui ne rentrent pas dans la fenêtre (façon liste de produits déroulante).
    private class GridViewport
    {
        public Guna2Panel Content;
        public Guna2VScrollBar ScrollBar;
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

        // Contenu réel (les cartes) : AutoSize -> sa hauteur reflète le nombre de lignes.
        // Il est décalé verticalement (Top) pour "défiler" à l'intérieur du viewport fixe.
        var gridContent = new Guna2Panel
        {
            BackColor = Color.Transparent,
            FillColor = Color.Transparent,
            BorderThickness = 0,
            AutoSize = true,
            Location = new Point(0, 0),
        };
        containerForAllProducts.Controls.Add(gridContent);

        var scrollBar = new Guna2VScrollBar
        {
            Width = 8,
            FillColor = ColorTranslator.FromHtml("#23242D"),
            BorderColor = Colors.bgColor,
            ThumbColor = Colors.mainColor,
            Minimum = 0,
            Maximum = 0,
            Visible = false,
            AutoRoundedCorners = true,
            Height = containerForAllProducts.Height,
        };
        scrollBar.Location = new Point(containerForAllProducts.Width - scrollBar.Width - 2, 0);
        containerForAllProducts.Controls.Add(scrollBar);
        scrollBar.BringToFront();

        scrollBar.ValueChanged += (s, e) => gridContent.Top = -scrollBar.Value;

        containerForAllProducts.MouseWheel += (s, e) => ScrollBy(scrollBar, -e.Delta / 3);
        gridContent.MouseWheel += (s, e) => ScrollBy(scrollBar, -e.Delta / 3);

        containerForAllProducts.Tag = new GridViewport { Content = gridContent, ScrollBar = scrollBar };

        containerMain.Controls.Add(containerForAllProducts);
        return containerForAllProducts;
    }

    private static void ScrollBy(Guna2VScrollBar scrollBar, int delta)
    {
        int newValue = scrollBar.Value + delta;
        scrollBar.Value = Math.Max(scrollBar.Minimum, Math.Min(scrollBar.Maximum, newValue));
    }

    // Redimensionne le viewport actuellement affiché à la hauteur RÉELLEMENT visible dans
    // la fenêtre (différente entre "Home", sous la bannière, et "Products", sous la nav) et
    // met à jour l'étendue de la scrollbar en conséquence. À appeler après avoir positionné
    // containerMain pour la vue courante.
    public void SetViewportHeight(int viewportHeight)
    {
        if (currentViewIndex < 0 || currentViewIndex >= views.Count) return;
        ApplyViewportHeight(views[currentViewIndex], viewportHeight);
    }

    private void ApplyViewportHeight(Guna2Panel viewport, int viewportHeight)
    {
        if (!(viewport.Tag is GridViewport vp)) return;

        viewportHeight = Math.Max(60, viewportHeight);
        viewport.Height = viewportHeight;

        vp.ScrollBar.Height = viewportHeight;
        vp.ScrollBar.Location = new Point(viewport.Width - vp.ScrollBar.Width - 2, 0);

        int overflow = Math.Max(0, vp.Content.Height - viewportHeight);
        vp.ScrollBar.Maximum = overflow;
        vp.ScrollBar.Visible = overflow > 0;

        if (vp.Content.Top < -overflow) vp.Content.Top = -overflow;
        if (overflow == 0) vp.Content.Top = 0;
    }




    private void AddProductsToContainer(Guna2Panel container, ProductView[] products)
    {
        int cols = 3;
        // Espacement plus large entre les cartes -> lecture "dashboard pro" plutôt que
        // grille tassée façon template.
        int spacing = 16;

        int sizeIncrement = 0;

        int productWidth = Default.widthProduct - sizeIncrement;
        int productHeight = Default.heightProduct - sizeIncrement;

        var target = (container.Tag is GridViewport vp) ? vp.Content : container;

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

            target.Controls.Add(products[i]);
        }

        // Les cartes empilent image + voile + libelles : sans double tampon, le
        // defilement de la grille scintille et accroche a chaque survol.
        UiStyle.EnableDoubleBuffer(target);
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

    // Fait apparaître les produits de la vue courante en cascade (slide-in décalé).
    public void PlayProductsEntrance()
    {
        if (currentViewIndex < 0 || currentViewIndex >= views.Count) return;
        var viewport = views[currentViewIndex];
        var content = (viewport.Tag is GridViewport vp) ? vp.Content : viewport;

        int i = 0;
        foreach (Control c in content.Controls)
        {
            if (c is PlantillaChanchoV16.Template.ProductView pv)
                pv.PlayEntrance((i++) * 70);
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
            var content = (view.Tag is GridViewport vp) ? vp.Content : view;
            totalProductCount += content.Controls.Count;
        }

        _countAllProducts.Text = totalProductCount == 1 ? "1 PRODUCT" : $"{totalProductCount} PRODUCTS";
    }

    public void InitializeProducts()
    {
        // Les cartes vont DIRECTEMENT dans leur conteneur final (view1) : pas de conteneur
        // "initial" intermédiaire ni d'attente sur BlurPanel.AllBlurPanelsProcessed (ancien
        // design). Ce gate était fragile car _containerMain peut être caché (Visible=false,
        // ex. ActivateHomeTab() appelé en fin de constructeur de Main) AVANT que la fenêtre
        // ne soit jamais affichée à l'écran -> un contrôle invisible ne reçoit jamais de
        // WM_PAINT, donc BlurPanel.Paint ne se déclenche jamais, l'événement ne fire jamais,
        // et les produits restent bloqués dans le conteneur intermédiaire pour toujours
        // (onglet Products vide, aucun compte affiché). Ajouter les cartes directement dans
        // la vue finale évite complètement cette dépendance : chaque carte se peindra/se
        // flloutera normalement dès que sa vue deviendra visible, comme n'importe quel autre
        // contrôle.
        var container1 = GetOrCreateProductContainer("view1", true);

        var productsView1 = GetProductsView1();
        var productsView2 = GetProductsView2();

        AddProductsToContainer(container1, productsView1);
        AddProductsToContainer(container1, productsView2);

        currentViewIndex = 0;
        ShowTotalProductCount();
        ShowView("view1");
        containerSpecialProduct.Visible = true;
        containerMain.Location = new Point(containerMain.Location.X, containerSpecialProduct.Bottom);

        expandedViewProducts = false;
    }




    private ProductView[] GetProductsView1() // VIEW 1 : MAX 9 PRODUCTS
    {
        return new[]
        {
            // Bannieres : ProductArt genere un visuel DEDIE pour les produits sans
            // affiche officielle. Avant, Spoofer et Windows PaiPai partageaient la
            // capture generique Img2Anydesk (l'ecran "ASUS / OTHERS"), et Windows
            // PaiPai s'en servait meme comme vignette -> illisible une fois reduite.
            new ProductView(productActive: true, productUnderMaintenance: false, "Spoofer", "12.01.2025", images.LogoWoofer, ProductArt.Spoofer, () => openGameDetails("anydesk")),
            //new ProductView(productActive: true, productUnderMaintenance: false, "CS:GO 2", "12.01.2025", images.LogoCsgo, images.BgCsgo, () => openGameDetails("csgo")), // CODE FOR REMOTE UPDATE DATE --> $"{Login.KeyAuthApp.var("variable_name")}"
            //new ProductView(productActive: true, productUnderMaintenance: false, "GTA V", "12.01.2025", images.LogoGta5, images.BgGta5, () => openGameDetails("gta5")),
            //new ProductView(productActive: true, productUnderMaintenance: false, "MINECRAFT", "12.01.2025", images.LogoMinecraft, images.BgMinecraft, () => openGameDetails("minecraft")),
            new ProductView(productActive: true, productUnderMaintenance: false, "ROBLOX", "12.01.2025", images.LogoRoblox, images.BgRoblox, () => openGameDetails("roblox")),
            //new ProductView(productActive: true, productUnderMaintenance: false, "RED DEAD REDEMPTION 2", "12.01.2025", images.LogoRd2, images.BgRd2, () => openGameDetails("rd2")),
            new ProductView(productActive: true, productUnderMaintenance: false, "PaiPai Val + Emulator", "12.01.2025", images.LogoValorant, images.BgValorant, () => openGameDetails("valorant")),
            new ProductView(productActive: true, productUnderMaintenance: false, "Windows PaiPai", "12.01.2025", ProductArt.WindowsPaiIcon, ProductArt.WindowsPai, () => openGameDetails("windowspai")),
            // Bot Manager manquait completement de la grille alors qu'il figure dans
            // ProductCatalog (et donc dans les licences) -> l'onglet Products
            // annoncait "4 PRODUCTS" pour un catalogue de 5.
            new ProductView(productActive: true, productUnderMaintenance: false, "Bot Manager", "12.01.2025", ProductArt.BotManagerIcon, ProductArt.BotManager, () => openGameDetails("botmanager")),
            //new ProductView(productActive: true, productUnderMaintenance: false, "FREE FIRE", "12.01.2025", images.LogoFreefire, images.BgFreefire, () => openGameDetails("freefire")),
            //new ProductView(productActive: false, productUnderMaintenance: false, "PUBG", "12.01.2025", images.LogoPubg, images.BgPubg, () => openGameDetails("pubg")),
            //new ProductView(productActive: true, productUnderMaintenance: false, "FORTNITE", "12.01.2025", images.LogoFortnite, images.BgFortnite, () => openGameDetails("fortnite")),
        };
    }

    private ProductView[] GetProductsView2() // VIEW 2 : désactivée (plus de 2e page)
    {
        return System.Array.Empty<ProductView>();
    }




    public int CurrentViewIndex => currentViewIndex;
    public int ViewCount => views.Count;
}
