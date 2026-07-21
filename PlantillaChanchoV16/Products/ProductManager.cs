using PlantillaChanchoV16.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantillaChanchoV16.Products
{
    public class ProductManager
    {
        Utils utils = new Utils();
        Images images = new Images();
        private Dictionary<string, DetailsProduct> cachedForms = new Dictionary<string, DetailsProduct>();

        public async Task<DetailsProduct> GetGameDetails(Form form, string gameName)
        {
            if (cachedForms.TryGetValue(gameName, out var cachedForm))
            {
                return cachedForm;
            }

            DetailsProduct newForm = await CreateGameDetails(form, gameName);
            cachedForms[gameName] = newForm;
            return newForm;
        }

        private async Task<DetailsProduct> CreateGameDetails(Form form, string gameName)
        {
            switch (gameName.ToLower())
            {
                case "warzone":
                    return new DetailsProduct(
                        __subscriptionName: "1BASIC",
                        logoRounded: false,
                        productName: "Call Of Duty:\nWarzone 2.0",
                        productDescription: "Take control in Warzone with improved accuracy, faster reactions, and powerful features to outplay your opponents and dominate the battlefield.",
                        __versionProduct: "26.78.1",
                        __lastUpdate: "16.12.2024",
                        productVideoURL: "https://youtu.be/VGuh4DfpaFY",
                        productLogo: images.LogoWarzone,
                        image1: images.Img1Warzone,
                        image2: images.Img2Warzone,
                        image3: images.Img3Warzone,
                        image4: images.Img4Warzone,
                        video1: images.Img5Warzone,
                        requirements: GetRequirementsWarzone(),
                        features: GetFeaturesWarzone(),
                        linkDiscord: "https://discord.gg/eternalproject",
                        openProduct: () => LoadGame(form, "warzone")
                        );
                case "anydesk":
                    return new DetailsProduct(
                        __subscriptionName: "forAnydesk",
                        logoRounded: false,
                        productName: "Spoofer",
                        productDescription: "- Supports All Windows 10 & Windows 11 \r\n- Supports All Intel/AMD Processors\r\n- Supports EAC/BE/Ricochet/Vanguard\r\n- Fixes \"HWID Identifiers\" Tournament Kick\r\n- Support all motherboard & laptop.\r\n- Ready TPM Bypass Valorant.",
                        __versionProduct: "26.78.1",
                        __lastUpdate: "16.12.2024",
                        productVideoURL: "https://youtu.be/VGuh4DfpaFY",
                        productLogo: images.LogoWoofer,
                        image1: images.Img1Anydesk,
                        image2: images.Img2Anydesk,
                        image3: images.Img3Anydesk,
                        image4: images.Img4Anydesk,
                        video1: images.Img4Anydesk,
                        requirements: GetRequirementsAnydesk(),
                        features: GetFeaturesAnydesk(),
                        linkDiscord: "https://discord.gg/eternalproject",
                        openProduct: () => LoadGame(form, "anydesk")
                        );
                case "csgo":
                    return new DetailsProduct(
                        __subscriptionName: "1BASIC",
                        logoRounded: false,
                        productName: "Counter-Strike:\nGlobal Offensive",
                        productDescription: "Our CS:GO cheat panel combines intuitive design, flawless performance, and features like customizable aimbot, wallhack, and advanced anti-detection tools.",
                        __versionProduct: "12.0.2",
                        __lastUpdate: "01.04.2025",
                        productVideoURL: "https://youtu.be/VGuh4DfpaFY",
                        productLogo: images.LogoCsgo,
                        image1: images.Img1Csgo,
                        image2: images.Img2Csgo,
                        image3: images.Img3Csgo,
                        image4: images.Img4Csgo,
                        video1: images.Img5Csgo,
                        requirements: GetRequirements(),
                        features: GetFeatures(),
                        linkDiscord: "https://discord.gg/eternalproject",
                        openProduct: () => LoadGame(form, "CS:GO")
                    );
                case "gta5":
                    return new DetailsProduct(
                        __subscriptionName: "Lgta5",
                        logoRounded: false,
                        productName: "Rockstar: Grand\nTheft Auto V",
                        productDescription: "Unlock unlimited potential in GTA 5 with enhanced speed, precision, and powerful tools for dominating the streets and missions.",
                        __versionProduct: "1.8.2",
                        __lastUpdate: "20.12.2024",
                        productVideoURL: "https://youtu.be/VGuh4DfpaFY",
                        productLogo: images.LogoGta5,
                        image1: images.Img1Gta5,
                        image2: images.Img2Gta5,
                        image3: images.Img3Gta5,
                        image4: images.Img4Gta5,
                        video1: images.Img5Gta5,
                        requirements: GetRequirements(),
                        features: GetFeatures(),
                        linkDiscord: "https://discord.gg/eternalproject",
                        openProduct: () => LoadGame(form, "GTA 5")
                    );
                case "minecraft":
                    return new DetailsProduct(
                        __subscriptionName: "Lminecraft",
                        productName: "Mojang:\nMinecraft",
                        productDescription: "Unleash creativity and adventure in Minecraft with innovative tools, visual enhancements, and custom features for all your building and survival needs.",
                        __versionProduct: "1.8.2",
                        __lastUpdate: "20.12.2024",
                        productVideoURL: "https://www.youtube.com/watch?v=MmB9b5njVbA",
                        logoRounded: false,
                        productLogo: images.LogoMinecraft,
                        image1: images.Img1Minecraft,
                        image2: images.Img2Minecraft,
                        image3: images.Img3Minecraft,
                        image4: images.Img4Minecraft,
                        video1: images.Img5Minecraft,
                         requirements: GetRequirements(),
                        features: GetFeatures(),
                        linkDiscord: "https://discord.gg/eternalproject",
                        openProduct: () => LoadGame(form, "minecraft")
                        );
                case "rd2":
                    return new DetailsProduct(
                        __subscriptionName: "1BASIC",
                        productName: "Red Dead\nRedemption 2",
                        productDescription: "Immerse yourself in RD2 with enhanced precision tools, realistic environment manipulation, and advanced gameplay customization.",
                        __versionProduct: "1.8.2",
                        __lastUpdate: "20.12.2024",
                        productVideoURL: "https://www.youtube.com/watch?v=KRD9q0toAb8",
                        logoRounded: false,
                        productLogo: images.LogoRd2,
                        image1: images.Img1Rd2,
                        image2: images.Img2Rd2,
                        image3: images.Img3Rd2,
                        image4: images.Img4Rd2,
                        video1: images.Img5Rd2,
                        requirements: GetRequirements(),
                        features: GetFeatures(),
                        linkDiscord: "https://discord.gg/eternalproject",
                        openProduct: () => LoadGame(form, "rd2")
                        );
                case "roblox":
                    return new DetailsProduct(
                        __subscriptionName: "1BASIC",
                        productName: "Roblox Co:\nRoblox",
                        productDescription: "Maximize your creativity in Roblox with advanced scripting tools, visual enhancements, and custom exploitations for diverse experiences.",
                        __versionProduct: "1.8.2",
                        __lastUpdate: "20.12.2024",
                        productVideoURL: "https://www.youtube.com/watch?v=_gZCUxRKqsM",
                        logoRounded: false,
                        productLogo: images.LogoRoblox,
                        image1: images.Img1Roblox,
                        image2: images.Img2Roblox,
                        image3: images.Img3Roblox,
                        image4: images.Img4Roblox,
                        video1: images.Img5Roblox,
                        requirements: GetRequirements(),
                        features: GetFeatures(),
                        linkDiscord: "https://discord.gg/eternalproject",
                        openProduct: () => LoadGame(form, "roblox")
                    );
                case "valorant":
                    return new DetailsProduct(
                        __subscriptionName: "Eternal",
                        logoRounded: true,
                        productName: "Riot Games:\nValorant",
                        productDescription: "Conquer Valorant with precise aim, faster reflexes, and advanced features to dominate every match.",
                        __versionProduct: "1.8.2",
                        __lastUpdate: "20.12.2024",
                        productVideoURL: "https://images.savi.wtf/u/4Gn5w5.mp4",
                        productLogo: images.LogoValorant,
                        image1: images.Img1Valorant,
                        image2: images.Img2Valorant,
                        image3: images.Img3Valorant,
                        image4: images.Img4Valorant,
                        video1: images.Img5Valorant,
                        requirements: GetRequirements(),
                        features: GetFeatures(),
                        linkDiscord: "https://discord.gg/eternalproject",
                        openProduct: () => LoadGame(form, "valorant")
                    );
                case "freefire":
                    return new DetailsProduct(
                        __subscriptionName: "Lfreefire",
                        productName: "Garena Online:\nFree Fire",
                        productDescription: "Master Garena Free Fire with enhanced precision, unmatched speed, and advanced tools to elevate your gameplay.",
                        __versionProduct: "1.8.2",
                        __lastUpdate: "20.12.2024",
                        productVideoURL: "https://www.youtube.com/watch?v=-NvgEsuINzA",
                        logoRounded: false,
                        productLogo: images.LogoFreefire,
                        image1: images.Img1FreeFire,
                        image2: images.Img2FreeFire,
                        image3: images.Img3FreeFire,
                        image4: images.Img4FreeFire,
                        video1: images.Img5FreeFire,
                        requirements: GetRequirements(),
                        features: GetFeatures(),
                        linkDiscord: "https://discord.gg/eternalproject",
                        openProduct: () => LoadGame(form, "freefire")
                        );
                case "dota2":
                    return new DetailsProduct(
                        __subscriptionName: "2BASIC",
                        productName: "Valve Corp:\nDota 2",
                        productDescription: "Take your MOBA gameplay to the next level with advanced vision control, precision enhancements, and strategic tools for Dota 2.",
                        __versionProduct: "1.8.2",
                        __lastUpdate: "20.12.2024",
                        productVideoURL: "https://www.youtube.com/watch?v=rvxGB87TdGA",
                        logoRounded: false,
                        productLogo: images.LogoDota2,
                        image1: images.Img1Dota2,
                        image2: images.Img2Dota2,
                        image3: images.Img3Dota2,
                        image4: images.Img4Dota2,
                        video1: images.Img5Dota2,
                        requirements: GetRequirements(),
                        features: GetFeatures(),
                        linkDiscord: "https://discord.gg/eternalproject",
                        openProduct: () => LoadGame(form, "dota2")
                        );
                case "tf2":
                    return new DetailsProduct(
                        __subscriptionName: "1BASIC",
                        productName: "Valve:\nTeam Fortress 2",
                        productDescription: "Enhance your gameplay in TF2 with advanced aim assistance, customizable loadouts, and tactical visual enhancements.",
                        __versionProduct: "1.8.2",
                        __lastUpdate: "20.12.2024",
                        productVideoURL: "https://www.youtube.com/watch?v=SliwAS92G7g",
                        logoRounded: false,
                        productLogo: images.LogoTf2,
                        image1: images.Img1Tf2,
                        image2: images.Img2Tf2,
                        image3: images.Img3Tf2,
                        image4: images.Img4Tf2,
                        video1: images.Img5Tf2,
                        requirements: GetRequirements(),
                        features: GetFeatures(),
                        linkDiscord: "https://discord.gg/eternalproject",
                        openProduct: () => LoadGame(form, "tf2")
                        );
                default:
                    throw new ArgumentException("Unknown game", nameof(gameName));
            }
        }


        private List<(string, Image, Color, string)> GetRequirements() // GENERAL
        {
            return new List<(string, Image, Color, string)>
            {
                (".NET Framework", images.DownloadIcon, Color.White, "https://docs.microsoft.com/en-us/cpp/c-runtime-library/ucrt-library-reference"),
                ("Universal C Runtime (UCRT)", images.DownloadIcon, Color.White, "https://docs.microsoft.com/en-us/cpp/c-runtime-library/ucrt-library-reference"),
                ("Vulkan Runtime Libraries", images.DownloadIcon, Color.White, "https://www.khronos.org/vulkan/"),
                ("Microsoft Visual C++ Redistributable", images.DownloadIcon, Color.White, "https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads"),
                ("DirectX Runtime", images.DownloadIcon, Color.White, "https://www.microsoft.com/en-us/download/details.aspx?id=35"),
                ("OpenGL Driver", images.DownloadIcon, Color.White, "https://www.opengl.org/"),
                ("Python Runtime", images.DownloadIcon, Color.White, "https://www.python.org/downloads/"),
                ("Java Runtime Environment (JRE)", images.DownloadIcon, Color.White, "https://www.oracle.com/java/technologies/javase-jre8-downloads.html"),
                ("Windows Media Feature Pack", images.DownloadIcon, Color.White, "https://support.microsoft.com/en-us/topic/media-feature-pack-list-for-windows-n-editions-418a12f5-71cf-7ec2-f8b7-fc56b1cf15a3"),
                ("Node.js", images.DownloadIcon, Color.White, "https://nodejs.org/en/"),
            };
        }


        private List<(string, Image, Color)> GetFeatures() // GENERAL
        {
            return new List<(string, Image, Color)>
            {
                ("Aimbot", images.IconCheckF, Colors.mainColor),
                ("Triggerbot", images.IconCheckF,Colors.mainColor),
                ("Spinbot", images.IconCheckF,Colors.mainColor),
                ("Radar Hack", images.IconCheckF,Colors.mainColor),
                ("Wallhack (ESP)", images.IconXF, Color.White),
                ("Speed x100", images.IconXF, Color.White),
                ("No Recoil", images.IconCheckF,Colors.mainColor),
                ("Unlimited Ammo", images.IconCheckF,Colors.mainColor),
                ("Invisible Mode", images.IconCheckF,Colors.mainColor),
                ("Teleportation", images.IconXF, Color.White),
                ("Auto Heal", images.IconCheckF,Colors.mainColor),
                ("One Hit Kill", images.IconXF, Color.White),
                ("Anti AFK", images.IconCheckF,Colors.mainColor),
                ("Rapid Fire", images.IconCheckF,Colors.mainColor),
                ("Infinite Money", images.IconCheckF,Colors.mainColor),
                ("Fly Mode", images.IconXF, Color.White),
                ("Ghost Mode", images.IconCheckF,Colors.mainColor),
            };
        }




        private List<(string, Image, Color, string)> GetRequirementsWarzone() // ONLY FOR WARZONE
        {
            return new List<(string, Image, Color, string)>
            {
                ("OpenGL Driver", images.DownloadIcon, Color.White, "https://www.opengl.org/"),
                ("Python Runtime", images.DownloadIcon, Color.White, "https://www.python.org/downloads/"),
                ("Java Runtime Environment (JRE)", images.DownloadIcon, Color.White, "https://www.oracle.com/java/technologies/javase-jre8-downloads.html"),
                ("Windows Media Feature Pack", images.DownloadIcon, Color.White, "https://support.microsoft.com/en-us/topic/media-feature-pack-list-for-windows-n-editions-418a12f5-71cf-7ec2-f8b7-fc56b1cf15a3"),
                ("Node.js", images.DownloadIcon, Color.White, "https://nodejs.org/en/"),
            };
        }


        private List<(string, Image, Color)> GetFeaturesWarzone() // ONLY FOR WARZONE
        {
            return new List<(string, Image, Color)>
            {
                
                ("Invisible Mode", images.IconCheckF,Colors.mainColor),
                ("Teleportation", images.IconXF, Color.White),
                ("Auto Heal", images.IconCheckF,Colors.mainColor),
                ("One Hit Kill", images.IconXF, Color.White),
                ("Anti AFK", images.IconCheckF,Colors.mainColor),
                ("Rapid Fire", images.IconCheckF,Colors.mainColor),
                ("Infinite Money", images.IconCheckF,Colors.mainColor)
            };
        }




        private List<(string, Image, Color, string)> GetRequirementsAnydesk() // ANYDESK
        {
            return new List<(string, Image, Color, string)>
            {
                ("Direct X", images.DownloadIcon, Color.White, "https://www.microsoft.com/en-us/download/details.aspx?id=35"),
                ("Virtual C++", images.DownloadIcon, Color.White, "https://www.techpowerup.com/download/visual-c-redistributable-runtime-package-all-in-one/"),
                ("Turn off Anti Virus", images.DownloadIcon, Color.White, ""),


            };
        }


        private List<(string, Image, Color)> GetFeaturesAnydesk() // ANYDESK
        {
            return new List<(string, Image, Color)>
            {
                ("SSD/NVME Virtualization", images.IconCheckF, Colors.mainColor),
                 ("CPU", images.IconCheckF, Colors.mainColor),
                  ("GPU", images.IconCheckF, Colors.mainColor),
                   ("MOBO (Motherboard)", images.IconCheckF, Colors.mainColor),
                    ("TPM (Trusted Platform Module)", images.IconCheckF, Colors.mainColor),
                    ("MAC (Media Access Control)", images.IconCheckF, Colors.mainColor),
            };
        }



        private async Task<string> Get__versionProduct()
        {
            return Login.KeyAuthApp.var("___versionProduct");
        }

        private async Task<string> Get__lastUpdate()
        {
            return Login.KeyAuthApp.var("___lastUpdate");
        }

        private async Task LoadGame(Form form, string gameName)
        {
            await Task.Delay(200);

            LoadingProduct loadingProduct;
            utils.ApplyFadeOutAnimation(form, () =>
            {
                form.Hide();
            });

            switch (gameName.ToLower())
            {
                case "anydesk":
                    loadingProduct = new LoadingProduct(form,
                        logoProduct: images.LogoWoofer,
                        logoSize: new Size(50, 50),
                        bgImage: images.Img4Anydesk,
                        productName: "Eternal Woofer",
                        productDescription: "This software avoids game bans by virtualizing hardware identifiers, making your old hardware untraceable. It secures SSD/NVME, CPU, GPU, motherboard, TPM, and MAC addresses for uninterrupted gaming",
                        downloadURL: $"{Login.KeyAuthApp.var("woofer")}", // DOWNLOAD LINK CODE
                        zipFileName: "HSP.zip",
                        exeFileNames: new List<string> {"HSP.exe","Woofer.exe" } // test
                    );
                    break;

                case "valorant":
                    loadingProduct = new LoadingProduct(form,
                        logoProduct: images.LogoMinecraft,
                        logoSize: new Size(50, 50),
                        bgImage: images.BgMinecraft,
                        productName: "Eternal Valorant",
                        productDescription: "Unleash creativity and adventure in Minecraft with innovative tools, visual enhancements, and custom features for all your building and survival needs.",
                        downloadURL: $"{Login.KeyAuthApp.var("Eternal")}", // DOWNLOAD LINK CODE
                        zipFileName: "Eternal.zip",
                        exeFileNames: new List<string> { "Eternal.bat" }
                    );
                    break;

                case "gta 5":
                    loadingProduct = new LoadingProduct(form,
                        logoProduct: images.LogoGta5,
                        logoSize: new Size(50, 50),
                        bgImage: images.BgGta5,
                        productName: "Grand Theft Auto V",
                        productDescription: "Unlock unlimited potential in GTA 5 with enhanced speed, precision, and powerful tools for dominating the streets and missions.",
                        downloadURL: $"{Login.KeyAuthApp.var("_productTest")}",  // DOWNLOAD LINK CODE
                        zipFileName: "PlantillaChanchoV5.zip",
                        exeFileNames: new List<string> { "PlantillaChanchoV5.exe" }
                    );
                    break;
                case "freefire":
                    loadingProduct = new LoadingProduct(form,
                         logoProduct: images.LogoFreefire,
                         logoSize: new Size(50, 50),
                         bgImage: images.Img4FreeFire,
                         productName: "Garena Online: Free Fire",
                         productDescription: "Master Garena Free Fire with enhanced precision, unmatched speed, and advanced tools to elevate your gameplay.",
                         downloadURL: $"{Login.KeyAuthApp.var("_productTest")}", // DOWNLOAD LINK CODE
                         zipFileName: "PlantillaChanchoV5.zip",
                         exeFileNames: new List<string> { "PlantillaChanchoV5.exe" }
                    );
                    break;

                default:
                    throw new ArgumentException($"Unknown game: {gameName}", nameof(gameName));
            }


            loadingProduct.Location = form.Location;

            loadingProduct.LocationChanged += (s, e) => form.Location = loadingProduct.Location;
            form.LocationChanged += (s, e) => loadingProduct.Location = form.Location;

        }


    }
}
