# SyntySearchEditorWindow
Unity Synty Search editor tool

![Synty Search Screenshot](https://user-images.githubusercontent.com/80069077/167243235-0a3f822d-849b-4b7e-a1b2-06a37c486a3a.PNG)

- A Synty "POLYGON" and "Simple" package organizer that works offline in the Unity editor.
- Use alongside the Synty Package Importer tool.
- The search results are organized by asset package.
- The prefab preview images and name text can be scaled using the slider control. 
- When you set the disc location of your Unity packages, each package will be indicated as being "unowned", "owned" or "imported".
  - Clicking an "unowned" item will navigate you to the Synty store (www.syntystore.com) page for that package.
  - Clicking an "owned" item will prompt you to import the package into your Unity project.
  - Clicking an "imported" item will highlight that prefab in your project window, for easy location!
- You can set multiple package locations.
- Setting a location will re-scan all files in that folder.
- The Unity asset download folder is always scanned when you set a location.
- Adding other locations is useful if you've downloaded unitypackage files from Unity or the Synty store.
- The tool scans each location for any "POLYGON" unitypackage files and "Simple" unitypackage and zip files.
- The tool builds a database of the asset paths. This scan may take a few minutes but the data is cached and is available across all of your Unity projects!
- This a community project and is not affiliated with Synty Studios.

Release Notes 1.4 (Nov 3 2023)
------------------------------

- Removed use of Synty Search website.
- This change limits the search to the Synty packages you own and have downloaded, either to the default Unity asset store location or a folder of your choice.
- Please use the Synty Search page for a complete online experience, but consider this tool as an offline solution to searching for Synty assets that you own.

Release Notes 1.3 (5/14/2022)
------------------------

- "Simple" unitypackage and zip files are now supported!
- Text search now available for "Simple" packages
- Prefab preview images available for "Simple" packages (unitypackage files only)
- Can now click on a search result for an imported "Simple" package to locate the prefab the Unity project window
- Click an "owned" item to import the package (interactive) into your project (unitypackage files only)
- Setting a location will always scan the contents, useful for refreshing the location
- Clicking on package icon will show list of owned packages
- Now resides under Tools menu

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

Known issues
------------
- No way to remove folder locations
