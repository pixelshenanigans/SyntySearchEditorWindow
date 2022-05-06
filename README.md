# SyntySearchEditorWindow
Unity Synty Search editor tool

![Alt text](./EditorWindow.png "Editor Window")

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
- The tool scans each location for any "Polygon" unitypackage files and builds a database of the asset paths. This scan will take a few minutes but the data is cached and is available across all of your Unity projects!
- This a community project and is not affiliated with Synty Studios.

Release Notes (5/5/2022)
------------------------

- Fixed image resizing
- Can now use ENTER key to perform search
- Reduced size of asset database cache

Roadmap
-------
- Clear message between searches
- Save image and font size in editor prefs
- Provide way to rescan file locations

Known issues
------------
- No way to remove folder locations
- Package scanning is done on the UI thread, so is blocking