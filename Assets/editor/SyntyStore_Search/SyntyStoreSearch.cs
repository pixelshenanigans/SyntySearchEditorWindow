using PixelShenanigans.EditorUtilities;
using PixelShenanigans.FileUtilities;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;

using UnityEditor;

using UnityEngine;

namespace PixelShenanigans.SyntyStoreSearch
{
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
        const string PrefabFileExt = ".prefab";
        const string ImageSizeEditorPrefsKey = "ImageSize";
        const string CurrentAppVersion = "1.2";
        const string CurrentCacheVersion = "1.1";
        const int WindowWidth = 500;
        const int WindowHeight = 700;
        const int BannerHeight = 130;
        const int ImageMargin = 5;
        const int imageWidthMin = 40;
        const int imageWidthMax = 200;
        const int fontSizeMin = 8;
        const int fontSizeMax = 14;

        bool cacheIsDirty = false;
        int imageSize = 120;
        int fontSize = 15;
        int ownedPackageCount = 0;
        int importedPackageCount = 0;
        string searchMessage = "";
        string searchTerm = "";
        GUIStyle imageStyle;
        GUIStyle labelStyle;
        GUIStyle nameStyle;
        GUIStyle buttonStyle;
        GUIStyle iconStyle;
        GUIStyle iconLabelStyle;
        GUIStyle textBoxStyle;
        GUIStyle sliderIconStyle;
        GUIStyle sliderStyle;
        //GUIStyle sliderLabelStyle;
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
        List<FileSearchTermData> fileSearchTermDataList = new List<FileSearchTermData>();
        Dictionary<string, PackageModel> ownedPackages = new Dictionary<string, PackageModel>();

        [MenuItem("Synty Tools/" + EditorTitle)]
        private static void OpenWindow()
        {
            SyntyStoreSearch window = GetWindow<SyntyStoreSearch>();
            window.titleContent = new GUIContent(EditorTitle, GetImageFromUrlAsTexture(IconTexture));
            window.minSize = new Vector2(WindowWidth, WindowHeight);
        }

        private void OnEnable()
        {
            searchMessage = "";
            AssetDatabase.importPackageCompleted += OnImportPackageCompleted;

            if (EditorPrefs.HasKey(ImageSizeEditorPrefsKey))
            {
                imageSize = EditorPrefs.GetInt(ImageSizeEditorPrefsKey);
            }

            var locationsFilePath = Path.Combine(GetCacheLocation(), "locations.json");
            var cacheFilePath = Path.Combine(GetCacheLocation(), "cache.json");

            if (File.Exists(cacheFilePath))
            {
                string cacheJson = File.ReadAllText(cacheFilePath);
                var packagesDto = JsonUtility.FromJson<OwnedPackagesDto>(cacheJson);
                var packageDtos = packagesDto.packageDtos;

                if (packagesDto.Version != CurrentCacheVersion
                 || packageDtos.Count == 0)
                {
                    searchMessage = "Cache files need to be updated - please set package location to re-scan";
                    File.Delete(cacheFilePath);
                    File.Delete(locationsFilePath);
                    return;
                }

                ownedPackages = ToModels(packageDtos);
                CalculatePackageStats();            }

            if (File.Exists(locationsFilePath))
            {
                string locationsJson = File.ReadAllText(locationsFilePath);
                var folderPathsDto = JsonUtility.FromJson<PackageFolderPathsDto>(locationsJson);
                packageFolderPaths = folderPathsDto.folderPaths;
            }
        }

        private void OnImportPackageCompleted(string packageName)
        {
            Debug.Log($"Imported {packageName}");

            if (ownedPackages.TryGetValue(packageName, out var package))
            {
                package.IsCurrentlyImported = true;
            }
        }

        private void OnDestroy()
        {
            AssetDatabase.importPackageCompleted -= OnImportPackageCompleted;

            EditorPrefs.SetInt(ImageSizeEditorPrefsKey, imageSize);

            if (!cacheIsDirty) return;

            string cachePath = GetCacheLocation();
            DirectoryInfo directory = new DirectoryInfo(cachePath);
            if (!directory.Exists)
            {
                directory.Create();
            }

            var locationsFilePath = Path.Combine(GetCacheLocation(), "locations.json");
            var cacheFilePath = Path.Combine(GetCacheLocation(), "cache.json");

            var ownedPackagesDto = new OwnedPackagesDto()
            {
                packageDtos = FromModels(ownedPackages),
                Version = CurrentCacheVersion
            };
            string cacheJson = JsonUtility.ToJson(ownedPackagesDto);
            File.WriteAllText(cacheFilePath, cacheJson);

            var folderPathsDto = new PackageFolderPathsDto()
            {
                folderPaths = packageFolderPaths
            };
            string locationsJson = JsonUtility.ToJson(folderPathsDto);
            File.WriteAllText(locationsFilePath, locationsJson);

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
                fixedHeight = 20,
                fixedWidth = 20,
                margin = new RectOffset(0, 0, 8, 0)
            };

            iconLabelStyle = new GUIStyle()
            {
                fixedHeight = 20,
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(5, 5, 7, 5)
            };
            iconLabelStyle.normal.textColor = Color.white;

            textBoxStyle = new GUIStyle(GUI.skin.textField)
            {
                fixedHeight = 25,
                fontSize = 14,
                margin = new RectOffset(10, 10, 5, 5),
                padding = new RectOffset(10, 10, 2, 2),
                alignment = TextAnchor.MiddleLeft,
                stretchWidth = false
            };

            sliderIconStyle = new GUIStyle(GUI.skin.box)
            {
                fixedHeight = 20,
                fixedWidth = 20,
                margin = new RectOffset(0, 5, 8, 5)
            };

            //sliderLabelStyle = new GUIStyle()
            //{
            //    fixedHeight = 20,
            //    fontSize = 12,
            //    alignment = TextAnchor.MiddleLeft,
            //    margin = new RectOffset(5, 5, 7, 5)
            //};
            //sliderLabelStyle.normal.textColor = Color.white;

            sliderStyle = new GUIStyle(GUI.skin.horizontalSlider)
            {
                margin = new RectOffset(2, 2, 8, 0),
                alignment = TextAnchor.MiddleLeft
            };

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

            if (iconContent == null)
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

            GUILayout.Label($"Version {CurrentAppVersion} by PixelShenanigans");

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Visit the online Synty Search page", buttonStyle))
            {
                Application.OpenURL(SiteUrl);
            }

            if (GUILayout.Button(setLocationContent, buttonStyle))
            {
                var path = EditorUtility.OpenFolderPanel(SetLocationText, "", "");
#if UNITY_EDITOR_WIN
                path = path.Replace("/", @"\");
#endif
                FetchPackageFiles(path);
            }

            GUILayout.Button(iconContent, iconStyle);
            GUILayout.Label(
                new GUIContent($"{importedPackageCount}/{ownedPackageCount}", "Imported/Owned"),
                iconLabelStyle);

            GUILayout.FlexibleSpace();

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
                DisplayPackContent(sortedSearchData[i]);
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
                        foldoutContent.tooltip = "Click an item to import the package!";
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
            GUILayout.Button(imageResizeIconContent, sliderIconStyle);

            //GUILayout.Label(imageSize.ToString(), sliderLabelStyle);
            
            float size = GUILayout.HorizontalSlider(imageSize, imageWidthMin, imageWidthMax, sliderStyle,
                GUI.skin.horizontalSliderThumb, GUILayout.Width(150));

            imageSize = Mathf.RoundToInt(size);
            fontSize = MapRange(imageSize, imageWidthMin, imageWidthMax, fontSizeMin, fontSizeMax);

            GUILayout.FlexibleSpace();
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

                if (GUILayout.Button(new GUIContent(item.ImageTexture/*, item.Name*/),
                                     imageStyle,
                                     GUILayout.Width(imageSize),
                                     GUILayout.Height(imageSize),
                                     GUILayout.ExpandWidth(false)))
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
                        else
                        {
                            AssetDatabase.ImportPackage(package.Path, true);
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

            CoroutineEditorUtility.StartCoroutine(GetUnityPackagesRoutine(defaultPackageLocation));

            if (!packageFolderPaths.Contains(defaultPackageLocation))
            {
                packageFolderPaths.Add(defaultPackageLocation);
            }

            CoroutineEditorUtility.StartCoroutine(GetUnityPackagesRoutine(path));
            CoroutineEditorUtility.StartCoroutine(GetZipFilesRoutine(path));

            if (!packageFolderPaths.Contains(path))
            {
                packageFolderPaths.Add(path);
            }

            yield return null;

            searchMessage = "";
        }

        private IEnumerator GetUnityPackagesRoutine(string path)
        {
            string[] polygonFiles = Directory.GetFiles(path, "polygon*.unitypackage", SearchOption.AllDirectories);
            string[] simpleFiles = Directory.GetFiles(path, "simple*.unitypackage", SearchOption.AllDirectories);
            List<string> files = polygonFiles.ToList();
            files.AddRange(simpleFiles);

            foreach (var file in files)
            {
                Debug.Log($"Scanning {file}");
                
                var package = GetUnityPackageContents(file);
                if (!ownedPackages.ContainsKey(package.Name))
                {
                    ownedPackages.Add(package.Name, package);
                    CalculatePackageStats();
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

                            if (assetPath.EndsWith(PrefabFileExt))
                            {
                                //TODO: Contains
                                if (assetPath.StartsWith(AssetPathPrefix))
                                {
                                    assetPath = assetPath.Remove(0, AssetPathPrefix.Length);
                                    if (firstAssetPath == null)
                                    {
                                        firstAssetPath = assetPath;
                                    }
                                    assetPath = RemovePackageName(assetPath);
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
                IsCurrentlyImported = IsCurrentlyImported(firstAssetPath)
            };
        }

        private IEnumerator GetZipFilesRoutine(string path)
        {
            string[] files = Directory.GetFiles(path, "simple*.zip", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                Debug.Log($"Scanning {file}");

                var package = GetZipFileContents(file);
                if (!ownedPackages.ContainsKey(package.Name))
                {
                    ownedPackages.Add(package.Name, package);
                    CalculatePackageStats();
                }

                yield return null;
            }
        }

        private PackageModel GetZipFileContents(string filePath)
        {
            var assets = new Dictionary<string, string>();
            string firstAssetPath = null;

            using (var zip = ZipFile.Open(filePath, ZipArchiveMode.Read))
            {
                var fileSearchTermData = new FileSearchTermData()
                {
                    SearchTerm_Assets = new Dictionary<string, List<int>>()
                };

                int assetIndex = 0;

                foreach (var entry in zip.Entries)
                {
                    string assetPath = entry.FullName;

                    if (assetPath.EndsWith(PrefabFileExt))
                    {
                        assetPath = GetPathUnderAssets(assetPath);

                        if (string.IsNullOrWhiteSpace(fileSearchTermData.PackageName))
                        {
                            firstAssetPath = assetPath;
                            fileSearchTermData.PackageName = GetPackageName(assetPath);
                        }

                        assetPath = RemovePackageName(assetPath);

                        string assetName = GetAssetName(assetPath);
                        if (!assets.ContainsKey(assetName))
                        {
                            assets.Add(assetName, assetPath);
                        }

                        UpdateFullTextSearch(fileSearchTermData, assetName, assetIndex);
                        assetIndex++;
                    }
                }
            }

            return new PackageModel()
            {
                Name = GetPackageName(firstAssetPath),
                Path = filePath,
                Assets = assets,
                IsCurrentlyImported = IsCurrentlyImported(firstAssetPath)
            };
        }

        private void UpdateFullTextSearch(FileSearchTermData fileSearchTermData, string assetName, int assetIndex)
        {
            var words = assetName.SplitCamelCase();

            foreach (var word in words)
            {
                if (word.Length > 2)
                {
                    string wordToAdd = word.ToLower();

                    if (fileSearchTermData.SearchTerm_Assets.TryGetValue(wordToAdd, out var assetList))
                    {
                        assetList.Add(assetIndex);
                    }
                    else
                    {
                        assetList = new List<int>();
                        assetList.Add(assetIndex);
                        fileSearchTermData.SearchTerm_Assets.Add(wordToAdd, assetList);
                    }
                }
            }

            fileSearchTermDataList.Add(fileSearchTermData);
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
                        var searchData = JsonUtility.FromJson<WebSearchData>(Encoding.ASCII.GetString(data));
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

        private string GetPathUnderAssets(string assetPath)
        {
            int index = assetPath.IndexOf(AssetPathPrefix) + AssetPathPrefix.Length;
            return assetPath.Substring(index);
        }

        private string RemovePackageName(string  assetPath)
        {
            int index = assetPath.IndexOf("/");
            return assetPath.Remove(0, index + 1);
        }

        private string GetAssetName(string assetPath)
        {
            int startIndex = assetPath.LastIndexOf("/") + 1;
            int endIndex = assetPath.LastIndexOf(".");
            return assetPath.Substring(startIndex, endIndex - startIndex);
        }

        private string GetPackageName(string assetPath)
        {
            int endIndex = assetPath.IndexOf("/");
            return assetPath.Substring(0, endIndex);
        }

        private bool IsCurrentlyImported(string assetPath)
        {
            string importedAssetPath = Path.Combine(Application.dataPath, assetPath);
            return File.Exists(importedAssetPath);
        }

        private bool IsCurrentlyImported(string name, string assetPath)
        {
            string importedAssetPath = Path.Combine(Application.dataPath, name, assetPath);
            return File.Exists(importedAssetPath);
        }

        private void CalculatePackageStats()
        {
            ownedPackageCount = ownedPackages.Count;
            importedPackageCount = ownedPackages.Values.Count(x => x.IsCurrentlyImported);
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

        private static Texture2D GetImageFromUrlAsTexture(string url)
        {
            Texture2D texture = new Texture2D(2, 2);

            using (WebClient client = new WebClient())
            {
                byte[] data = client.DownloadData(url);
                texture.LoadImage(data);
            }

            return texture;
        }

        private List<PackModel> SortSearchResults(WebSearchData results)
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
#if UNITY_EDITOR_OSX
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            return Path.Combine(appDataPath, @"Library/Unity/Asset Store-5.x/Synty Studios";
#else
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, @"Unity\Asset Store-5.x\Synty Studios");
#endif
        }

        public string GetCacheLocation()
        {
#if UNITY_EDITOR_OSX
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            return Path.Combine(appDataPath, @"Library/SyntySearch/";
#else
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appDataPath, @"SyntySearch");
#endif
        }

        private int MapRange(int value, int minA, int maxA, int minB, int maxB)
        {
            float aValue = Mathf.InverseLerp(minA, maxA, value);
            float bValue = Mathf.Lerp(minB, maxB, aValue);
            return Mathf.RoundToInt(bValue);
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
    public class WebSearchData
    {
        public bool success;
        public WebSearchItem[] data;
    }

    [Serializable]
    public class WebSearchItem
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
        public string Version;
        public List<PackageDto> packageDtos;
    }

    [Serializable]
    public class FileSearchTermData
    {
        public string PackageName;
        public Dictionary<string, List<int>> SearchTerm_Assets;
    }

    [Serializable]
    public class FileSearchData
    {
        public string Name;
        public string PackageName;
    }
}