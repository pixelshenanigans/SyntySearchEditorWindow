using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using UnityEditor;

using UnityEngine;

namespace SyntyTools
{
    public class SyntyStoreSearch : EditorWindow
    {
        const string headerTexture = "Assets/Editor/SyntyStore_Search/synty_search_header.png";
        const string editorTitle = "Synty Store Search";
        const string storeUrl = "https://syntystore.com/";
        const string siteUrl = "https://www.syntysearch.com";
        const string apiPart = "/api/";
        const string searchPart = "prefab/search/";
        const string setLocationText = "Set package location";
        const int windowWidth = 700;
        const int windowHeight = 500;
        const int bannerHeight = 180;
        int imageWidth = 120;
        int imageHeight = 120;
        const int imageMargin = 5;

        string searchMessage = "";
        string searchTerm = "";
        string packageFolderPath = "";
        GUIStyle imageStyle;
        GUIStyle labelStyle;
        GUIStyle buttonStyle;
        GUIStyle textBoxStyle;
        GUIStyle sliderStyle;
        GUIStyle containerStyle;
        Texture2D buttonTexture;
        GUIContent buttonContent;
        Vector2 scrollPos;
        List<PackModel> sortedSearchData;
        Dictionary<string, string> ownedPackages;

        [MenuItem("Tools/Synty Tools/" + editorTitle)]
        private static void OpenWindow()
        {
            SyntyStoreSearch window = GetWindow<SyntyStoreSearch>();
            window.titleContent = new GUIContent(editorTitle);
            window.minSize = new Vector2(windowWidth, windowHeight);
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void OnGUI()
        {
            InitializeStyles();

            GUILayout.BeginVertical(this.containerStyle);

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
                margin = new RectOffset(imageMargin, imageMargin, imageMargin, imageMargin)
            };
            imageStyle.normal.background = new Texture2D(imageWidth, imageHeight);

            labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.yellow;
            labelStyle.fontSize = 15;
            labelStyle.alignment = TextAnchor.MiddleLeft;
            labelStyle.margin = new RectOffset(10, 10, 5, 5);
            labelStyle.padding = new RectOffset(10, 10, 2, 2);

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 25,
                fontSize = 14,
                margin = new RectOffset(10, 10, 5, 5),
                padding = new RectOffset(10, 10, 2, 2),
                stretchWidth = false
            };

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
                margin = new RectOffset(0, 0, 10, 0),
                alignment = TextAnchor.MiddleLeft
            };

            containerStyle = new GUIStyle()
            {
                padding = new RectOffset(10, 10, 2, 2)
            };
        }

        private void DisplayHeaderContent()
        {
            if (GUILayout.Button(CreateHeaderContent(), GUILayout.Width(windowWidth), GUILayout.Height(bannerHeight)))
            {
                Application.OpenURL(storeUrl);
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Visit the online Synty Search page", this.buttonStyle))
            {
                Application.OpenURL(siteUrl);
            }

            GUIContent setFolderContent = new GUIContent(
                setLocationText,
                string.IsNullOrWhiteSpace(packageFolderPath) ? "Click here to specify the location of your Synty unitypackage files (optional)" : packageFolderPath);
            if (GUILayout.Button(setFolderContent, this.buttonStyle))
            {
                var path = EditorUtility.OpenFolderPanel(setLocationText, packageFolderPath, "");
                string[] files = Directory.GetFiles(path, "polygon*.unitypackage", SearchOption.AllDirectories);
                //HACK - remove underscores to match start of package name with pack
                // (e.g. package = Polygon_Adventure_Unity_Package_2019_4_01, pack = PolygonAdventure)
                ownedPackages = files
                    .Select(x => Path.GetFileName(x))
                    .ToDictionary(x => x.Replace("_", string.Empty).ToLower(), x => x);
                packageFolderPath = path;
            }

            GUILayout.EndHorizontal();
        }

        private void DisplaySearchContent()
        {
            GUILayout.BeginHorizontal();

            searchTerm = GUILayout.TextField(searchTerm, this.textBoxStyle, GUILayout.MinWidth(200));

            bool performedSearch = GUILayout.Button("Search", this.buttonStyle);

            DisplayImageSizeSlider();

            if (performedSearch)
            {
                GetSearchResults(searchTerm);
            }

            GUILayout.EndHorizontal();

            GUILayout.Label(searchMessage, labelStyle);
        }

        private List<PackModel> GetSearchResults(string term)
        {
            searchMessage = "";
            if (searchTerm.Length >= 3)
            {
                string url = siteUrl + apiPart + searchPart + term;
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("Referer", siteUrl);
                    byte[] data = client.DownloadData(url);
                    if (data != null)
                    {
                        var searchData = JsonUtility.FromJson<SearchData>(Encoding.ASCII.GetString(data));
                        if (searchData.success == true)
                        {
                            sortedSearchData = SortSearchResults(searchData);
                            searchMessage = $"Search returned {sortedSearchData.Sum(x => x.searchItems.Count)} results";
                            return sortedSearchData;
                        }
                    }
                }
                searchMessage = "Unable to get search results, please try later";
            }
            else
            {
                searchMessage = "Search term must have at least 3 characters";
            }

            return null;
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
            string ownership = "";
            if (ownedPackages != null && ownedPackages.Count > 0)
            {
                ownership = "- " + (IsPackOwned(pack.packName) ? "Owned" : "Click an item to buy!");
            }

            pack.foldoutIsExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(
                pack.foldoutIsExpanded,
                $"{pack.packName} ({pack.searchItems.Count}) {ownership}");

            if (pack.foldoutIsExpanded)
            {
                DisplayFoldoutContent(pack);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DisplayImageSizeSlider()
        {
            float size = GUILayout.HorizontalSlider(imageWidth, 40, 200, sliderStyle, GUI.skin.horizontalSliderThumb,
            GUILayout.Width(150));

            imageWidth = Mathf.RoundToInt(size);
            imageHeight = Mathf.RoundToInt(size);
        }

        private bool IsPackOwned(string packName)
        {
            //HACK - remove last character to deal with plural pack names (e.g. pirates)
            return ownedPackages.Keys.Any(x => x.StartsWith(packName.Remove(packName.Length - 1).ToLower()));
        }

        private void DisplayFoldoutContent(PackModel pack)
        {
            int horizontalImageSpace = 0;
            EditorGUILayout.BeginHorizontal();

            foreach (var item in pack.searchItems)
            {
                if (item.imageTexture == null)
                {
                    item.imageTexture = GetImageFromUrlAsTexture(siteUrl + apiPart + item.imagePath);
                }

                var itemContent = new GUIContent(item.imageTexture, item.name);
                horizontalImageSpace = CalculateImagePosition(horizontalImageSpace);

                GUILayout.BeginVertical();
                bool clicked = GUILayout.Button(itemContent,
                                     imageStyle,
                                     GUILayout.Width(imageWidth),
                                     GUILayout.Height(imageHeight),
                                     GUILayout.ExpandWidth(false));

                if (clicked && !this.IsPackOwned(pack.packName))
                {
                    Application.OpenURL(pack.packStoreUrl);
                }

                GUILayout.TextArea(item.name, GUILayout.Width(this.imageWidth));
                GUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
        }

        private int CalculateImagePosition(int position)
        {
            position += imageWidth + imageMargin;
            if (position > (base.position.width - 10))
            {
                position = imageWidth + (imageMargin * 2);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }

            return position;
        }

        private GUIContent CreateHeaderContent()
        {
            if (buttonTexture == null)
            {
                buttonTexture = GetImageFromUrlAsTexture(headerTexture);
                buttonContent = new GUIContent(buttonTexture);
            }

            return buttonContent;
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
                    packName = packName,
                    packStoreUrl = results.data
                        .Where(x => x.pack == packName)
                        .Select(x => x.packStoreUrl)
                        .First(),
                    foldoutIsExpanded = false,
                    searchItems = results.data
                        .Where(x => x.pack == packName)
                        .Select(x => new PackItemModel()
                        {
                            name = x.name,
                            imagePath = x.imagePath,
                            imageTexture = null
                        })
                        .ToList()
                });
            }

            return packs;
        }
    }

    [System.Serializable]
    public class SearchData
    {
        public bool success;
        public SearchItem[] data;
    }

    [System.Serializable]
    public class SearchItem
    {
        public string name;
        public string pack;
        public string packStoreUrl;
        public string imagePath;
    }

    public class PackModel
    {
        public string packName;
        public string packStoreUrl;
        public bool foldoutIsExpanded;
        public List<PackItemModel> searchItems;
    }

    public class PackItemModel
    {
        public string name;
        public string imagePath;
        public Texture2D imageTexture;
    }
}