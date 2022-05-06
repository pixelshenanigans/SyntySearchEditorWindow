using PixelShenanigans.EditorUtilities;
using PixelShenanigans.FileUtilities;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using UnityEditor;

using UnityEngine;

namespace PixelShenanigans.SyntyStoreSearch
{
    [EditorWindowTitle(icon="synty_search_icon.png", title=EditorTitle)]
    public class SyntyStoreSearch : EditorWindow
    {
        const string HeaderTexture = "Assets/Editor/SyntyStore_Search/synty_search_header.png";
        const string IconTexture = "Assets/Editor/SyntyStore_Search/synty_search_icon.png";
        const string ImageResizeIconTexture = "Assets/Editor/SyntyStore_Search/resize_image_icon.png";
        const string EditorTitle = "Synty Store Prefab Search";
        const string StoreUrl = "https://syntystore.com/";
        const string SiteUrl = "https://www.syntysearch.com";
        const string ApiPart = "/api/";
        const string SearchPart = "prefab/search/";
        const string SetLocationText = "Set package location";
        const string AssetPathPrefix = "Assets/";
        const int WindowWidth = 500;
        const int WindowHeight = 700;
        const int BannerHeight = 180;
        const int ImageMargin = 5;
        const int imageWidthMin = 40;
        const int imageWidthMax = 200;
        const int fontSizeMin = 8;
        const int fontSizeMax = 14;

        bool cacheIsDirty = false;
        int imageSize = 120;
        int fontSize = 15;
        string searchMessage = "";
        string searchTerm = "";
        GUIStyle imageStyle;
        GUIStyle labelStyle;
        GUIStyle nameStyle;
        GUIStyle buttonStyle;
        GUIStyle iconStyle;
        GUIStyle iconLabelStyle;
        GUIStyle textBoxStyle;
        GUIStyle sliderStyle;
        GUIStyle sliderLabelStyle;
        GUIStyle containerStyle;
        Texture2D buttonTexture;
        Texture2D iconTexture;
        Texture2D imageResizeIconTexture;
        GUIContent buttonContent;
        GUIContent iconContent;
        GUIContent imageResizeIconContent;
        GUIContent setLocationContent;
        Vector2 scrollPos;
        List<PackModel> sortedSearchData;
        List<string> packageFolderPaths = new List<string>();
        Dictionary<string, PackageModel> ownedPackages = new Dictionary<string, PackageModel>();

        [MenuItem("Synty Tools/" + EditorTitle)]
        private static void OpenWindow()
        {
            SyntyStoreSearch window = GetWindow<SyntyStoreSearch>();
            window.titleContent = new GUIContent(EditorTitle);
            window.minSize = new Vector2(WindowWidth, WindowHeight);
        }

        private void OnEnable()
        {
            searchMessage = "";

            var locationsFilePath = Path.Combine(GetCacheLocation(), "locations.json");
            if (File.Exists(locationsFilePath))
            {
                string locationsJson = File.ReadAllText(locationsFilePath);
                var folderPathsDto =  JsonUtility.FromJson<PackageFolderPathsDto>(locationsJson);
                packageFolderPaths = folderPathsDto.folderPaths;
            }

            var cacheFilePath = Path.Combine(GetCacheLocation(), "cache.json");
            if (File.Exists(cacheFilePath))
            {
                string cacheJson = File.ReadAllText(cacheFilePath);
                var packagesDto = JsonUtility.FromJson<OwnedPackagesDto>(cacheJson);
                var packageDtos = packagesDto.packageDtos;
                ownedPackages = ToModels(packageDtos);
            }
        }

        private void OnDestroy()
        {
            if (!cacheIsDirty) return;

            string cachePath = GetCacheLocation();
            DirectoryInfo directory = new DirectoryInfo(cachePath);
            if (!directory.Exists)
            {
                directory.Create();
            }

            var locationsFilePath = Path.Combine(GetCacheLocation(), "locations.json");
            var folderPathsDto = new PackageFolderPathsDto()
            {
                folderPaths = packageFolderPaths
            };
            string locationsJson = JsonUtility.ToJson(folderPathsDto);
            File.WriteAllText(locationsFilePath, locationsJson);

            var cacheFilePath = Path.Combine(GetCacheLocation(), "cache.json");
            var ownedPackagesDto = new OwnedPackagesDto()
            {
                packageDtos = FromModels(ownedPackages)
            };
            string cacheJson = JsonUtility.ToJson(ownedPackagesDto);
            File.WriteAllText(cacheFilePath, cacheJson);

            cacheIsDirty = false;
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void OnGUI()
        {
            InitializeStyles();

            GUILayout.BeginVertical(containerStyle);

            DisplayHeaderContent();

            DisplaySearchContent();

            if (sortedSearchData != null)
            {
                DisplaySearchResults();
            }

            GUILayout.EndVertical();
        }

        private void InitializeStyles()
        {
            imageStyle = new GUIStyle(GUI.skin.box)
            {
                margin = new RectOffset(ImageMargin, ImageMargin, ImageMargin, ImageMargin)
            };
            imageStyle.normal.background = new Texture2D(imageSize, imageSize);

            nameStyle = new GUIStyle(GUI.skin.textArea)
            {
                fontSize = fontSize
            };

            labelStyle = new GUIStyle()
            {
                fixedHeight = 25,
                fontSize = 15,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(10, 10, 5, 5),
                padding = new RectOffset(10, 10, 2, 2)
            };
            labelStyle.normal.textColor = Color.yellow;

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 25,
                fontSize = 14,
                margin = new RectOffset(10, 10, 5, 5),
                padding = new RectOffset(10, 10, 2, 2),
                stretchWidth = false
            };

            iconStyle = new GUIStyle(GUI.skin.box)
            {
                fixedHeight = 25,
                fixedWidth = 25,
                margin = new RectOffset(ImageMargin, ImageMargin, ImageMargin, ImageMargin)
            };

            iconLabelStyle = new GUIStyle()
            {
                fixedHeight = 25,
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(10, 0, 5, 0)
            };
            iconLabelStyle.normal.textColor = Color.green;

            textBoxStyle = new GUIStyle(GUI.skin.textField)
            {
                fixedHeight = 25,
                fontSize = 14,
                margin = new RectOffset(10, 10, 5, 5),
                padding = new RectOffset(10, 10, 2, 2),
                alignment = TextAnchor.MiddleLeft,
                stretchWidth = false
            };

            sliderStyle = new GUIStyle(GUI.skin.horizontalSlider)
            {
                margin = new RectOffset(2, 2, 10, 0),
                alignment = TextAnchor.MiddleLeft
            };

            sliderLabelStyle = new GUIStyle()
            {
                fixedHeight = 25,
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(10, 0, 5, 0)
            };
            sliderLabelStyle.normal.textColor = Color.white;

            containerStyle = new GUIStyle()
            {
                padding = new RectOffset(10, 10, 2, 2)
            };
        }

        private void InitializeContent()
        {
            if (buttonTexture == null)
            {
                buttonTexture = GetImageFromUrlAsTexture(HeaderTexture);
                buttonContent = new GUIContent(buttonTexture, "Visit the online Synty Store");
            }

            if (iconTexture == null)
            {
                iconTexture = GetImageFromUrlAsTexture(IconTexture);
                iconContent = new GUIContent(iconTexture, "Package Count");
            }

            if (imageResizeIconTexture == null)
            {
                imageResizeIconTexture = GetImageFromUrlAsTexture(ImageResizeIconTexture);
                imageResizeIconContent = new GUIContent(imageResizeIconTexture, "Resize Images");
            }

            setLocationContent = new GUIContent(
                SetLocationText,
                packageFolderPaths.Count == 0
                    ? "Specify the location of your Synty unitypackage files (optional)"
                    : string.Join("\n", packageFolderPaths));
        }

        private void DisplayHeaderContent()
        {
            InitializeContent();

            if (GUILayout.Button(buttonContent, GUILayout.Width(WindowWidth - 20), GUILayout.Height(BannerHeight)))
            {
                Application.OpenURL(StoreUrl);
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Visit the online Synty Search page", buttonStyle))
            {
                Application.OpenURL(SiteUrl);
            }

            if (GUILayout.Button(setLocationContent, buttonStyle))
            {
                var path = EditorUtility.OpenFolderPanel(SetLocationText, "", "");
#if UNITY_EDITOR_WIN
                path = path.Replace("/", "\\");
#endif
                FetchPackageFiles(path);
            }

            GUILayout.Button(iconContent, iconStyle);
            GUILayout.Label(ownedPackages.Count.ToString(), iconLabelStyle);

            GUILayout.EndHorizontal();
        }

        private void DisplaySearchContent()
        {
            GUILayout.BeginHorizontal();

            GUI.SetNextControlName("search");
            searchTerm = GUILayout.TextField(searchTerm, this.textBoxStyle, GUILayout.MinWidth(200));

            bool performedSearch = GUILayout.Button("Search", this.buttonStyle);

            DisplayImageSizeSlider();

            if (!string.IsNullOrEmpty(searchTerm)
              && Event.current.type == EventType.KeyUp
              && Event.current.keyCode == KeyCode.Return
              && GUI.GetNameOfFocusedControl() == "search")
            {
                performedSearch = true;
            }

            if (performedSearch)
            {
                GetSearchResults(searchTerm);
            }

            GUILayout.EndHorizontal();

            GUILayout.Label(searchMessage, labelStyle);
        }

        private void DisplaySearchResults()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, true, true);

            for (int i = 0; i < sortedSearchData.Count; i++)
            {
                PackModel pack = sortedSearchData[i];
                DisplayPackContent(pack);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DisplayPackContent(PackModel pack)
        {
            GUIContent foldoutContent = new GUIContent();

            foldoutContent.text = "- ";
            if (ownedPackages.Count > 0)
            {
                if (ownedPackages.ContainsKey(pack.Name))
                {
                    if (ownedPackages[pack.Name].IsCurrentlyImported)
                    {
                        foldoutContent.text += "Imported";
                        foldoutContent.tooltip = "Click an item to view it in your project!";
                    }
                    else
                    {
                        foldoutContent.text += "Owned";
                        foldoutContent.tooltip = "Try the Synty Importer tool!";
                    }
                }
                else
                {
                    foldoutContent.text += "Not owned";
                    foldoutContent.tooltip = "Click an item to buy!";
                }
            }

            foldoutContent.text = $"{pack.Name} ({pack.SearchItems.Count}) {foldoutContent.text}";

            pack.FoldoutIsExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(pack.FoldoutIsExpanded, foldoutContent);

            if (pack.FoldoutIsExpanded)
            {
                DisplayFoldoutContent(pack);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DisplayImageSizeSlider()
        {
            GUILayout.Button(imageResizeIconContent, iconStyle);
            
            float size = GUILayout.HorizontalSlider(imageSize, imageWidthMin, imageWidthMax, sliderStyle,
                GUI.skin.horizontalSliderThumb, GUILayout.Width(120));

            imageSize = Mathf.RoundToInt(size);
            fontSize = MapRange(imageSize, imageWidthMin, imageWidthMax, fontSizeMin, fontSizeMax);

            GUILayout.Label(imageSize.ToString(), sliderLabelStyle);
        }

        private int MapRange(int value, int minA, int maxA, int minB, int maxB)
        {
            float aValue = Mathf.InverseLerp(minA, maxA, value);
            float bValue = Mathf.Lerp(minB, maxB, aValue);
            return Mathf.RoundToInt(bValue);
        }

        private void DisplayFoldoutContent(PackModel pack)
        {
            int horizontalImageSpace = 0;
            EditorGUILayout.BeginHorizontal();

            foreach (var item in pack.SearchItems)
            {
                if (item.ImageTexture == null)
                {
                    item.ImageTexture = GetImageFromUrlAsTexture(SiteUrl + ApiPart + item.ImagePath);
                }

                horizontalImageSpace = CalculateImagePosition(horizontalImageSpace);

                GUILayout.BeginVertical();

                if (GUILayout.Button(new GUIContent(item.ImageTexture, item.Name),
                                     imageStyle,
                                     GUILayout.Width(imageSize),
                                     GUILayout.Height(imageSize),
                                     GUILayout.ExpandWidth(false)))
                {
                    if (ownedPackages.ContainsKey(pack.Name))
                    {
                        if (ownedPackages.ContainsKey(pack.Name))
                        {
                            var package = ownedPackages[pack.Name];
                            if (package.IsCurrentlyImported)
                            {
                                if (package.Assets.ContainsKey(item.Name))
                                {
                                    EditorGUIUtility.PingObject(
                                        AssetDatabase.LoadMainAssetAtPath(
                                            Path.Combine(AssetPathPrefix, package.Name, package.Assets[item.Name])));
                                }
                            }
                        }
                    }
                    else
                    {
                        Application.OpenURL(pack.StoreUrl);
                    }
                }

                GUILayout.TextArea(item.Name, nameStyle, GUILayout.Width(imageSize));
                GUILayout.EndVertical();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void FetchPackageFiles(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            cacheIsDirty = true;

            searchMessage = "Processing Unity package files...";

            CoroutineEditorUtility.StartCoroutine(LoadUnityPackagesRoutine(path));
        }

        private IEnumerator LoadUnityPackagesRoutine(string path)
        {
            string defaultPackageLocation = GetDefaultUnityPackageLocation();

            if (!packageFolderPaths.Contains(defaultPackageLocation))
            {
                CoroutineEditorUtility.StartCoroutine(GetUnityPackagesRoutine(defaultPackageLocation));
                packageFolderPaths.Add(defaultPackageLocation);
            }

            if (!packageFolderPaths.Contains(path))
            {
                CoroutineEditorUtility.StartCoroutine(GetUnityPackagesRoutine(path));
                packageFolderPaths.Add(path);
            }

            yield return null;

            searchMessage = "";
        }

        private IEnumerator GetUnityPackagesRoutine(string path)
        {
            string[] files = Directory.GetFiles(path, "polygon*.unitypackage", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                Debug.Log(file);
                
                var package = GetUnityPackageContents(file);
                if (!ownedPackages.ContainsKey(package.Name))
                {
                    ownedPackages.Add(package.Name, package);
                }

                yield return null;
            }
        }

        private PackageModel GetUnityPackageContents(string filePath)
        {
            var assets = new Dictionary<string, string>();
            string firstAssetPath = null;

            using (var fileStream = File.Open(filePath, FileMode.Open))
            {
                MemoryStream memoryStream = TarFileUtility.DecompressGzFile(fileStream);
                using (MemoryStream stream = new MemoryStream(memoryStream.ToArray()))
                {
                    foreach (var (fileName, fileData) in TarFileUtility.ReadFiles(stream))
                    {
                        if (fileName.EndsWith("pathname"))
                        {
                            string assetPath = Encoding.ASCII.GetString(fileData).Trim('\n', '0');

                            if (assetPath.EndsWith(".prefab"))
                            {
                                if (assetPath.StartsWith(AssetPathPrefix))
                                {
                                    if (firstAssetPath == null)
                                    {
                                        firstAssetPath = assetPath;
                                    }
                                    assetPath = ReduceAssetPath(assetPath);
                                }

                                string assetName = GetAssetName(assetPath);
                                if (!assets.ContainsKey(assetName))
                                {
                                    assets.Add(assetName, assetPath);
                                }
                            }
                        }
                    }
                }
            }

            return new PackageModel()
            {
                Name = GetPackageName(firstAssetPath),
                Path = filePath,
                Assets = assets,
                IsCurrentlyImported = IsCurrentlyImported(GetPackageName(firstAssetPath), firstAssetPath)
            };
        }

        private string ReduceAssetPath(string assetPath)
        {
            string path = assetPath.Remove(0, AssetPathPrefix.Length);
            int index = path.IndexOf("/");
            return path.Remove(0, index + 1);
        }

        private string GetAssetName(string assetPath)
        {
            int startIndex = assetPath.LastIndexOf("/") + 1;
            int endIndex = assetPath.LastIndexOf(".");
            return assetPath.Substring(startIndex, endIndex - startIndex);
        }

        private string GetPackageName(string assetPath)
        {
            string path = assetPath.Remove(0, AssetPathPrefix.Length);
            int endIndex = path.IndexOf("/");
            return path.Substring(0, endIndex);
        }

        private bool IsCurrentlyImported(string name, string assetPath)
        {
            string importedAssetPath = Path.Combine(Application.dataPath, name, assetPath);
            return File.Exists(importedAssetPath);
        }

        private List<PackModel> GetSearchResults(string term)
        {
            searchMessage = "";
            if (searchTerm.Length >= 3)
            {
                string url = SiteUrl + ApiPart + SearchPart + term;
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("Referer", SiteUrl);
                    byte[] data = client.DownloadData(url);
                    if (data != null)
                    {
                        var searchData = JsonUtility.FromJson<SearchData>(Encoding.ASCII.GetString(data));
                        if (searchData.success == true)
                        {
                            sortedSearchData = SortSearchResults(searchData);
                            searchMessage = $"Search returned {sortedSearchData.Sum(x => x.SearchItems.Count)} results.";
                            return sortedSearchData;
                        }
                    }
                }
                searchMessage = "Unable to get search results, please try later.";
            }
            else
            {
                searchMessage = "Search term must have at least 3 characters.";
            }

            return null;
        }

        private bool IsPackOwned(string packName)
        {
            return ownedPackages.ContainsKey(packName);
        }

        private int CalculateImagePosition(int position)
        {
            position += imageSize + ImageMargin;
            if (position > (base.position.width - imageWidthMin - 1))
            {
                position = imageSize + ImageMargin;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }

            return position;
        }

        private Texture2D GetImageFromUrlAsTexture(string url)
        {
            Texture2D texture = new Texture2D(2, 2);

            using (WebClient client = new WebClient())
            {
                byte[] data = client.DownloadData(url);
                texture.LoadImage(data);
            }

            return texture;
        }

        private List<PackModel> SortSearchResults(SearchData results)
        {
            List<PackModel> packs = new List<PackModel>();

            var packNames = results.data
                .Select(x => x.pack)
                .Distinct()
                .ToList();

            foreach (var packName in packNames)
            {
                packs.Add(new PackModel()
                {
                    Name = packName,
                    StoreUrl = results.data
                        .Where(x => x.pack == packName)
                        .Select(x => x.packStoreUrl)
                        .First(),
                    FoldoutIsExpanded = false,
                    SearchItems = results.data
                        .Where(x => x.pack == packName)
                        .Select(x => new SearchItemModel()
                        {
                            Name = x.name,
                            ImagePath = x.imagePath,
                            ImageTexture = null
                        })
                        .ToList()
                });
            }

            return packs;
        }

        private string GetDefaultUnityPackageLocation()
        {
#if UNITY_EDITOR_WIN
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, @"Unity\Asset Store-5.x\Synty Studios");
#elif UNITY_EDITOR_OSX
            return "~/Library/Unity/Asset Store-5.x/Synty Studios";
#else
            return "~/.local/share/unity3d/Asset Store-5.x/Synty Studios";
#endif
        }

        public string GetCacheLocation()
        {
#if UNITY_EDITOR_OSX
            return Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"/Library/SyntySearch/";
#else
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, "SyntySearch");
#endif
        }

        private Dictionary<string, PackageModel> ToModels(List<PackageDto> packageDtos)
        {
            Dictionary<string, PackageModel> packages = new Dictionary<string, PackageModel>();

            foreach (var packageDto in packageDtos)
            {
                packages.Add(packageDto.Name, ToModel(packageDto));
            }

            return packages;
        }

        private PackageModel ToModel(PackageDto packageDto)
        {
            var package = new PackageModel()
            {
                Name = packageDto.Name,
                Path = packageDto.Path,
                Assets = packageDto.Assets.Select(x => x.Split('|')).ToDictionary(s => s[0], s => s[1])
            };

            package.IsCurrentlyImported = IsCurrentlyImported(package.Name, package.Assets.Values.First());
            
            return package;
        }

        private List<PackageDto> FromModels(Dictionary<string,PackageModel> packages)
        {
            List<PackageDto> packageDtos = new List<PackageDto>();

            foreach (var package in packages.Values)
            {
                packageDtos.Add(FromModel(package));
            }

            return packageDtos;
        }

        private PackageDto FromModel(PackageModel package)
        {
            return new PackageDto()
            {
                Name = package.Name,
                Path = package.Path,
                Assets = package.Assets.Select(x => string.Join("|", x.Key, x.Value)).ToList()
            };
        }
    }

    [Serializable]
    public class SearchData
    {
        public bool success;
        public SearchItem[] data;
    }

    [Serializable]
    public class SearchItem
    {
        public string name;
        public string pack;
        public string packStoreUrl;
        public string imagePath;
    }

    public class PackModel
    {
        public string Name;
        public string StoreUrl;
        public bool FoldoutIsExpanded;
        public List<SearchItemModel> SearchItems;
    }

    public class SearchItemModel
    {
        public string Name;
        public string ImagePath;
        public Texture2D ImageTexture;
    }

    public class PackageModel
    {
        public string Name;
        public string Path;
        public Dictionary<string, string> Assets;
        public bool IsCurrentlyImported;
    }

    [Serializable]
    public class PackageDto
    {
        public string Name;
        public string Path;
        public List<string> Assets;
    }

    [Serializable]
    public class PackageFolderPathsDto
    {
        public List<string> folderPaths;
    }

    [Serializable]
    public class OwnedPackagesDto
    {
        public List<PackageDto> packageDtos;
    }
}