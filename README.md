# SyntySearchEditorWindow
Unity Synty Search editor tool

- A Synty "Polygon" package organizer that integrates with Synty search.
- Use alongside the Synty Package Importer tool.
- This tool makes use of the community-built Synty search page (www.syntysearch.com) and provides the same search in a convenient Unity editor window.
- The search results are filtered by asset package.
- The prefab preview images and name text can be scaled using the slider control. 
- If you have set the location of your Unity packages, each package will be indicated as being "unowned", "owned" or "imported".
  - Clicking on any "unowned" item will take you to the Synty store (www.syntystore.com) page for that package.
  - Clicking on any "imported" item will highlight that prefab in your project window, making it easy to find what you're looking for!
- You can set multiple package locations.
- The Unity asset download folder is always scanned when you set a location.
- Adding other locations is useful if you've downloaded unitypackage files from the Synty store.
- The tool scans each location for any "Polygon" unitypackage files and builds a database of the asset paths. This scan will take a few minutes but the data is
cached and is available across all of your Unity projects.
- This a community project and is not affiliated with Synty Studios.

Roadmap
-------
- Scan packages in the default Unity download folder at startup
- Fix positions of scaled images
- Reduce number of times images are displayed (performance)
- Clear message between searches
- Make Search work with Enter key
- Save image and font size in editor prefs
- Provide way to rescan file locations

Known issues
------------
- Image positioning doesn't always work
- No way to remove folder locations
- Package scanning is done on the UI thread, so is blocking
- Image/font size for search results is not saved
- Current scripts have a dependency on Newtonsoft.Json (https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.0/manual/index.html)
- Download the unitypackage file for a working version (without the new features)