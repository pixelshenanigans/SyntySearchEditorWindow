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
        const string headerTexture = "Assets/Editor/SyntyStore_Importer/synty_search_header.png";
        const string siteUrl = "https://www.syntysearch.com";
        const string apiPart = "/api/";
        const string searchPart = "prefab/search/";
        const int windowWidth = 650;
        const int windowHeight = 500;
        const int bannerHeight = 171;
        const int imageWidth = 120;
        const int imageHeight = 120;
        const int offset = 0;

        Vector2 scrollPos;
        string searchTerm = "";
        SearchData searchData = null;
        List<PackItem> sortedSearchData = null;

        [MenuItem("Synty Tools/Synty Store Search")]
        private static void OpenWindow()
        {
            SyntyStoreSearch window = GetWindow<SyntyStoreSearch>();
            window.titleContent = new GUIContent("Synty Store Search");
            window.minSize = new Vector2(windowWidth, windowHeight);
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void OnGUI()
        {
            GUIStyle imageStyle = new GUIStyle(GUI.skin.label);
            imageStyle.margin = new RectOffset(offset, offset, offset, offset);

            GUIStyle labelStyle = new GUIStyle();
            labelStyle.fontSize = 15;
            labelStyle.normal.textColor = Color.yellow;
            labelStyle.alignment = TextAnchor.MiddleCenter;

            GUILayout.BeginHorizontal();
            var button_tex = GetImageFromUrlAsTexture(headerTexture);
            var button_tex_con = new GUIContent(button_tex);
            if (GUILayout.Button(button_tex_con, GUILayout.Width(windowWidth), GUILayout.Height(bannerHeight)))
            {
                Application.OpenURL("https://syntystore.com/");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Enter term to search (at least 3 characters):", GUILayout.ExpandWidth(false));
            searchTerm = GUILayout.TextField(searchTerm, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("SEARCH", GUILayout.ExpandWidth(false)))
            {
                string url = siteUrl + apiPart + searchPart + searchTerm;
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
                        }
                    }
                }
            }
            GUILayout.EndHorizontal();

            if (sortedSearchData != null)
            {
                GUILayout.Label($"Search returned {sortedSearchData.Sum(x => x.searchItems.Count)} items", labelStyle);
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, true, true);
                for (int i = 0; i < sortedSearchData.Count; i++)
                {
                    sortedSearchData[i].foldoutIsExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(
                        sortedSearchData[i].foldoutIsExpanded,
                        $"{sortedSearchData[i].packName} ({sortedSearchData[i].searchItems.Count})");

                    if (sortedSearchData[i].foldoutIsExpanded)
                    {
                        int horizontalImageSpace = 0;
                        EditorGUILayout.BeginHorizontal();
                        foreach (var item in sortedSearchData[i].searchItems)
                        {
                            if (item.imageTexture == null)
                            {
                                item.imageTexture = GetImageFromUrlAsTexture(siteUrl + apiPart + item.imagePath);
                            }

                            var itemContent = new GUIContent(item.imageTexture, item.name);
                            horizontalImageSpace += imageWidth + offset;
                            if (horizontalImageSpace > (position.width - 10))
                            {
                                horizontalImageSpace = imageWidth + (offset * 2);
                                EditorGUILayout.EndHorizontal();
                                EditorGUILayout.BeginHorizontal();
                            }

                            if (GUILayout.Button(itemContent,
                                                 imageStyle,
                                                 GUILayout.Width(imageWidth),
                                                 GUILayout.Height(imageHeight),
                                                 GUILayout.ExpandWidth(false)))
                            {
                                Application.OpenURL(sortedSearchData[i].packStoreUrl);
                            }

                            //if (horizontalImageSpace == 0)
                            //{
                            //    horizontalImageSpace = imageWidth;
                            //    EditorGUILayout.BeginHorizontal();
                            //}
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
                EditorGUILayout.EndScrollView();
            }
        }
 
        public static Texture2D GetImageFromUrlAsTexture(string url)
        {
            Texture2D tex = new Texture2D(2, 2);
            using (WebClient client = new WebClient())
            {
                byte[] data = client.DownloadData(url);
                tex.LoadImage(data);
            }
            return tex;
        }

        private List<PackItem> SortSearchResults(SearchData results)
        {
            List<PackItem> packs = new List<PackItem>();
            var packNames = results.data.Select(x => x.pack).Distinct().ToList();
            foreach (var packName in packNames)
            {
                packs.Add(new PackItem()
                {
                    packName = packName,
                    packStoreUrl = results.data
                        .Where(x => x.pack == packName)
                        .Select(x => x.packStoreUrl)
                        .First(),
                    foldoutIsExpanded = false,
                    searchItems = results.data
                        .Where(x => x.pack == packName)
                        .Select(x => new PackSearchItem()
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

    public class PackItem
    {
        public string packName;
        public string packStoreUrl;
        public bool foldoutIsExpanded;
        public List<PackSearchItem> searchItems;
    }

    public class PackSearchItem
    {
        public string name;
        public string imagePath;
        public Texture2D imageTexture;
    }
}