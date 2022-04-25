using System.Collections.Generic;
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
        const int windowWidth = 650;
        const int windowHeight = 500;
        const int bannerHeight = 171;
        const int imageWidth = 120;
        const int imageHeight = 120;
        const int offset = 5;

        GUIStyle imageStyle;
        GUIStyle labelStyle;
        Texture2D buttonTexture = null;
        GUIContent buttonContent;
        string searchMessage = "";
        Vector2 scrollPos;
        string searchTerm = "";
        SearchData searchData = null;
        List<PackModel> sortedSearchData = null;

        [MenuItem("Synty Tools/" + editorTitle)]
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
            imageStyle = new GUIStyle(GUI.skin.label);
            imageStyle.margin = new RectOffset(offset, offset, offset, offset);
            imageStyle.normal.background = new Texture2D(imageWidth, imageHeight);

            labelStyle = new GUIStyle();
            labelStyle.normal.textColor = Color.yellow;
            labelStyle.fontSize = 15;
            labelStyle.alignment = TextAnchor.MiddleCenter;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(CreateHeaderContent(), GUILayout.Width(windowWidth), GUILayout.Height(bannerHeight)))
            {
                Application.OpenURL(storeUrl);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Enter term to search:", GUILayout.ExpandWidth(false));
            searchTerm = GUILayout.TextField(searchTerm, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Search", GUILayout.ExpandWidth(false)))
            {
                GetSearchResults(searchTerm);
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(searchMessage, labelStyle);

            if (sortedSearchData != null)
            {
                DisplaySearchResults();
            }
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
                        searchData = JsonUtility.FromJson<SearchData>(Encoding.ASCII.GetString(data));
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
            pack.foldoutIsExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(
                pack.foldoutIsExpanded,
                $"{pack.packName} ({pack.searchItems.Count})");

            if (pack.foldoutIsExpanded)
            {
                DisplayFoldoutContent(pack);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
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

                if (GUILayout.Button(itemContent,
                                     imageStyle,
                                     GUILayout.Width(imageWidth),
                                     GUILayout.Height(imageHeight),
                                     GUILayout.ExpandWidth(false)))
                {
                    Application.OpenURL(pack.packStoreUrl);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private int CalculateImagePosition(int position)
        {
            position += imageWidth + offset;
            if (position > (base.position.width - 10))
            {
                position = imageWidth + (offset * 2);
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
            Texture2D tex = new Texture2D(2, 2);
            using (WebClient client = new WebClient())
            {
                byte[] data = client.DownloadData(url);
                tex.LoadImage(data);
            }
            return tex;
        }

        private List<PackModel> SortSearchResults(SearchData results)
        {
            List<PackModel> packs = new List<PackModel>();
            var packNames = results.data.Select(x => x.pack).Distinct().ToList();

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