using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlantillaChanchoV16;

namespace PlantillaChanchoV16.Utilities
{
    public class Images
    {
        private static Utils utilities = new Utils();

        private static string namespaceDefault = "PlantillaChanchoV16";
            
        // MAIN LOGO

        private Image _mainLogo;
        // Logo officiel PaiPai (fourni par l'utilisateur) : remplace l'ancien placeholder
        // logoEternal.jpg. Une seule source ici -> se propage automatiquement partout où
        // MainLogo est déjà utilisé (nav SpinningLogo, Login, écran de lancement ConfigLogo).
        public Image MainLogo => _mainLogo ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.Logo.png");

        private Image _homeImage;
        public Image HomeImage => _homeImage ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.homeImage3.jpg");
        private Image _homeBg;
        public Image HomeBg => _homeBg ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.gridAllGames.png");

        // ICONS


        private Image _ytIcon;
        public Image YtIcon => _ytIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.ytIcon.png");

        private Image _iconTabHome;
        public Image IconTabHome => _iconTabHome ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.iconTabHome.png");


        private Image _iconDetailProduct;
        public Image IconDetailProduct => _iconDetailProduct ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.iconDetailProduct.png");

        private Image _iconTabProducts;
        public Image IconTabProducts => _iconTabProducts ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.iconTabProducts.png");

        private Image _iconTabUserData;
        public Image IconTabUserData => _iconTabUserData ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.iconTabUserData.png");

        private Image _iconTabFav;
        public Image IconTabFav => _iconTabFav ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.iconTabFav.png");

        private Image _iconFavOff;
        public Image IconFavOff => _iconFavOff ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.favOff.png");

        private Image _iconFavOn;
        public Image IconFavOn => _iconFavOn ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.favOn.png");

        private Image _iconSignIn;
        public Image IconSignIn => _iconSignIn ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.iconSignIn.png");

        private Image _iconSignUp;
        public Image IconSignUp => _iconSignUp ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.iconSignUp.png");

        private Image _logOutIcon;
        public Image LogOutIcon => _logOutIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.logOutIcon.png");

        private Image _playVideoIcon;
        public Image PlayVideoIcon => _playVideoIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.playVideoIcon.png");

        private Image _dcIcon;
        public Image DcIcon => _dcIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.dcIcon.png");

        private Image _igIcon;
        public Image IgIcon => _igIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.igIcon.png");


        private Image _userIcon;
        public Image UserIcon => _userIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.txUserIcon.png");

        private Image _passIcon;
        public Image PassIcon => _passIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.txPassIcon.png");

        private Image _keyIcon;
        public Image KeyIcon => _keyIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.txKeyIcon.png");



        private Image _closeIcon;
        public Image CloseIcon => _closeIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.closeIcon.png");

        private Image _csIcon;
        public Image CsIcon => _csIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.csIcon.png");

        private Image _umIcon;
        public Image UmIcon => _umIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.umIcon.png");




        private Image _xIcon;
        public Image XIcon => _xIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.xIcon.png");

        private Image _keyLicenseIcon;
        public Image KeyLicenseIcon => _keyLicenseIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.key_1.png");

        private Image _checkIcon;
        public Image CheckIcon => _checkIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.checkIcon.png");

        private Image _downloadIcon;
        public Image DownloadIcon => _downloadIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.downloadIcon.png");

        private Image _iconXF;
        public Image IconXF => _iconXF ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.iconXF.png");

        private Image _iconCheckF;
        public Image IconCheckF => _iconCheckF ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.iconCheckF.png");

        private static Image _arrowIcon;
        public static Image ArrowIcon => _arrowIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.arrow.png");

        private static Image _arrowRIcon;
        public static Image ArrowRIcon => _arrowRIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.arrowRight.png");

        private Image _iconOpenDetails;
        public Image IconOpenDetails => _iconOpenDetails ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.iconOpenDetails.png");

        private Image _playIcon;
        public Image PlayIcon => _playIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.playIcon.png");

        private static Image _homeIcon;
        public static Image HomeIcon => _homeIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.homeIcon.png");

        private static Image _backIcon;
        public static Image BackIcon => _backIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.arrowBack.png");

        private static Image _expandedIcon;
        public static Image ExpandedIcon => _expandedIcon ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.expandedIcon.png");

        private Image _imgUser;
        public Image ImgUser => _imgUser ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.imgUser.jpg");
        private Image _iconPrevSpecial;
        public Image IconPrevSpecial => _iconPrevSpecial ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Icons.iconPreviewSpecial.png");





        // ANYDESK TEST

        private Image _img1Anydesk;
        public Image Img1Anydesk => _img1Anydesk ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.woofer2.png");

        private Image _img2Anydesk;
        public Image Img2Anydesk => _img2Anydesk ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.woofer1.png");

        private Image _img3Anydesk;
        public Image Img3Anydesk => _img3Anydesk ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.woofer3.png");
        private Image _img4Anydesk;
        public Image Img4Anydesk => _img4Anydesk ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.woofer4.jpg");



        // PRODUCT GTA 5

        private Image _logoGta5;
        public Image LogoGta5 => _logoGta5 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.logo_gta5.png");

        private Image _bgGta5;
        public Image BgGta5 => _bgGta5 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.bg_gta5.jpg");



        private Image _img1Gta5;
        public Image Img1Gta5 => _img1Gta5 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img1_gta5.jpg");

        private Image _img2Gta5;
        public Image Img2Gta5 => _img2Gta5 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img2_gta5.jpg");

        private Image _img3Gta5;
        public Image Img3Gta5 => _img3Gta5 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img3_gta5.jpg");
        private Image _img4Gta5;
        public Image Img4Gta5 => _img4Gta5 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img4_gta5.png");
        private Image _img5Gta5;
        public Image Img5Gta5 => _img5Gta5 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img5_gta5.png");






        // PRODUCT CSGO

        private Image _logoCsgo;
        public Image LogoCsgo => _logoCsgo ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.logo_csgo.png");

        private Image _bgCsgo;
        public Image BgCsgo => _bgCsgo ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.bg_csgo.jpg");

        private Image _bgCsgo1;
        public Image BgCsgo1 => _bgCsgo1 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.bg_csgo_1.jpg");


        private Image _img1Csgo;
        public Image Img1Csgo => _img1Csgo ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img1_csgo.jpg");

        private Image _img2Csgo;
        public Image Img2Csgo => _img2Csgo ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img2_csgo.jpg");

        private Image _img3Csgo;
        public Image Img3Csgo => _img3Csgo ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img3_csgo.jpg");
        private Image _img4Csgo;
        public Image Img4Csgo => _img4Csgo ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img4_csgo.png");
        private Image _img5Csgo;
        public Image Img5Csgo => _img5Csgo ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img5_csgo.png");

        // PRODUCT PUBG
        private Image _logoPubg;
        public Image LogoPubg => _logoPubg ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.logo_pubg.jpg");

        private Image _bgPubg;
        public Image BgPubg => _bgPubg ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.bg_pubg.jpg");


        // PRODUCT FREE FIRE

        private Image _logoFreefire;
        public Image LogoFreefire => _logoFreefire ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.logo_freefire.jpg");

        private Image _bgFreefire;
        public Image BgFreefire => _bgFreefire ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.bg_freefire.jpeg");

        private Image _img1FreeFire;
        public Image Img1FreeFire => _img1FreeFire ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img1_freefire.jpg");

        private Image _img2FreeFire;
        public Image Img2FreeFire => _img2FreeFire ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img2_freefire.jpg");

        private Image _img3FreeFire;
        public Image Img3FreeFire => _img3FreeFire ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img3_freefire.jpg");
        private Image _img4FreeFire;
        public Image Img4FreeFire => _img4FreeFire ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img4_freefire.png");

        private Image _img5FreeFire;
        public Image Img5FreeFire => _img5FreeFire ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img5_freefire.png");

        // PRODUCT FORTNITE
        private Image _logoFortnite;
        public Image LogoFortnite => _logoFortnite ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.logo_fortnite.jpg");

        private Image _bgFortnite;
        public Image BgFortnite => _bgFortnite ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.bg_fortnite.jpg");

        // PRODUCT VALORANT

        private Image _logoValorant;
        public Image LogoValorant => _logoValorant ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.logo_valorant.jpg");

        private Image _bgValorant;
        public Image BgValorant => _bgValorant ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.bg_valorant.jpg");

        private Image _img1Valorant;
        public Image Img1Valorant => _img1Valorant ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img1_valorant.jpg");

        private Image _img2Valorant;
        public Image Img2Valorant => _img2Valorant ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img2_valorant.jpg");

        private Image _img3Valorant;
        public Image Img3Valorant => _img3Valorant ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img3_valorant.jpg");
        private Image _img4Valorant;
        public Image Img4Valorant => _img4Valorant ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img4_valorant.png");

        private Image _img5Valorant;
        public Image Img5Valorant => _img5Valorant ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img5_valorant.png");
        // PRODUCT WARZONE

        private Image _logoWarzone;
        public Image LogoWarzone => _logoWarzone ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.logo_warzone.png");

        private Image _logoWoofer;
        public Image LogoWoofer => _logoWoofer ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.testLogo.png");

        private Image _bgWarzone;
        public Image BgWarzone => _bgWarzone ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.bg_warzone.jpg");

        private Image _img1Warzone;
        public Image Img1Warzone => _img1Warzone ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img1_warzone.jpg");

        private Image _img2Warzone;
        public Image Img2Warzone => _img2Warzone ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img2_warzone.jpg");

        private Image _img3Warzone;
        public Image Img3Warzone => _img3Warzone ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img3_warzone.jpg");
        private Image _img4Warzone;
        public Image Img4Warzone => _img4Warzone ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img4_warzone.png");

        private Image _img5Warzone;
        public Image Img5Warzone => _img5Warzone ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img5_warzone.png");

        // PRODUCT MINECRAFT 

        private Image _logoMinecraft;
        public Image LogoMinecraft => _logoMinecraft ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.logo_minecraft.png");

        private Image _bgMinecraft;
        public Image BgMinecraft => _bgMinecraft ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.bg_minecraft.jpg");

        private Image _img1Minecraft;
        public Image Img1Minecraft => _img1Minecraft ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img1_minecraft.jpg");

        private Image _img2Minecraft;
        public Image Img2Minecraft => _img2Minecraft ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img2_minecraft.jpg");

        private Image _img3Minecraft;
        public Image Img3Minecraft => _img3Minecraft ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img3_minecraft.jpg");
        private Image _img4Minecraft;
        public Image Img4Minecraft => _img4Minecraft ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img4_minecraft.png");

        private Image _img5Minecraft;
        public Image Img5Minecraft => _img5Minecraft ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img5_minecraft.png");

        // PRODUCT ROBLOX

        private Image _logoRoblox;
        public Image LogoRoblox => _logoRoblox ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.logo_roblox.jpg");

        private Image _bgRoblox;
        public Image BgRoblox => _bgRoblox ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.bg_roblox.jpg");

        private Image _img1Roblox;
        public Image Img1Roblox => _img1Roblox ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img1_roblox.jpg");

        private Image _img2Roblox;
        public Image Img2Roblox => _img2Roblox ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img2_roblox.jpg");

        private Image _img3Roblox;
        public Image Img3Roblox => _img3Roblox ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img3_roblox.jpg");
        private Image _img4Roblox;
        public Image Img4Roblox => _img4Roblox ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img4_roblox.png");

        private Image _img5Roblox;
        public Image Img5Roblox => _img5Roblox ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img5_roblox.png");

        // PRODUCT RED DEAD REDEMPTION 2 (RD2)

        private Image _logoRd2;
        public Image LogoRd2 => _logoRd2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.logo_rd2.png");

        private Image _bgRd2;
        public Image BgRd2 => _bgRd2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.bg_rd2.jpg");

        private Image _img1Rd2;
        public Image Img1Rd2 => _img1Rd2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img1_rd2.jpg");

        private Image _img2Rd2;
        public Image Img2Rd2 => _img2Rd2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img2_rd2.jpg");

        private Image _img3Rd2;
        public Image Img3Rd2 => _img3Rd2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img3_rd2.jpg");
        private Image _img4Rd2;
        public Image Img4Rd2 => _img4Rd2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img4_rd2.png");

        private Image _img5Rd2;
        public Image Img5Rd2 => _img5Rd2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img5_rd2.png");

        // PRODUCT DOTA 2

        private Image _logoDota2;
        public Image LogoDota2 => _logoDota2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.logo_dota2.png");

        private Image _bgDota2;
        public Image BgDota2 => _bgDota2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.bg_dota2.jpg");

        private Image _img1Dota2;
        public Image Img1Dota2 => _img1Dota2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img1_dota2.jpg");

        private Image _img2Dota2;
        public Image Img2Dota2 => _img2Dota2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img2_dota2.jpg");

        private Image _img3Dota2;
        public Image Img3Dota2 => _img3Dota2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img3_dota2.jpg");
        private Image _img4Dota2;
        public Image Img4Dota2 => _img4Dota2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img4_dota2.png");

        private Image _img5Dota2;
        public Image Img5Dota2 => _img5Dota2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img5_dota2.png");

        // PRODUCT TEAM FORTRESS 2 (TF2)

        private Image _logoTf2;
        public Image LogoTf2 => _logoTf2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.logo_tf2.png");

        private Image _bgTf2;
        public Image BgTf2 => _bgTf2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.bg_tf2.jpg");

        private Image _img1Tf2;
        public Image Img1Tf2 => _img1Tf2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img1_tf2.jpg");

        private Image _img2Tf2;
        public Image Img2Tf2 => _img2Tf2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img2_tf2.jpg");

        private Image _img3Tf2;
        public Image Img3Tf2 => _img3Tf2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img3_tf2.jpg");
        private Image _img4Tf2;
        public Image Img4Tf2 => _img4Tf2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img4_tf2.png");

        private Image _img5Tf2;
        public Image Img5Tf2 => _img5Tf2 ??= utilities.LoadEmbeddedImage($"{namespaceDefault}.Images.img5_tf2.png");


    }
}
