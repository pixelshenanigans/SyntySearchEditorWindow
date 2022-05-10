# SyntySearchEditorWindow
Unity Synty Search editor tool

![Synty Search Screenshot](https://user-images.githubusercontent.com/80069077/167243235-0a3f822d-849b-4b7e-a1b2-06a37c486a3a.PNG)

- A Synty "POLYGON" and "Simple" package organizer that integrates with Synty search.
- Use alongside the Synty Package Importer tool.
- This tool makes use of the community-built Synty search page (www.syntysearch.com) and provides the same search in a convenient Unity editor window.
- The search results are filtered by asset package.
- The prefab preview images and name text can be scaled using the slider control. 
- If you have set the location of your Unity packages, each package will be indicated as being "unowned", "owned" or "imported".
  - Clicking an "unowned" item will navigate you to the Synty store (www.syntystore.com) page for that package.
  - Clicking an "owned" item will prompt you to import the package into your Unity project.
  - Clicking an "imported" item will highlight that prefab in your project window, for easy location!
- You can set multiple package locations.
- Setting a location you have already scanned will re-scan all files in that folder.
- The Unity asset download folder is always scanned when you set a location.
- Adding other locations is useful if you've downloaded unitypackage files from the Synty store.
- The tool scans each location for any "POLYGON" unitypackage files and "Simple" unitypackage and zip files. It then builds a database of the asset paths. This scan will take a few minutes but the data is cached and is available across all of your Unity projects!
- This a community project and is not affiliated with Synty Studios.

Release Notes 1.3 (5/10/2022)
------------------------

- Click an "owned" item to import the package (interactive) into your project
- Setting a location will always scan the contents, useful for refreshing the location
- "Simple" unitypackage and zip files are scanned

Release Notes 1.2 (5/7/2022)
------------------------

- Search image size saved in EditorPrefs
- Number of imported and owned packages displayed
- UI improvements

Release Notes 1.1  (5/5/2022)
------------------------

- Fixed image resizing
- Can now use ENTER key to perform search
- Reduced size of asset database cache

Roadmap
-------
- Provide way to rescan file locations

Known issues
------------
- No way to remove folder locations
- Package scanning is done on the UI thread, so is blocking