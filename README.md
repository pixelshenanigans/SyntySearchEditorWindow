# SyntySearchEditorWindow
Unity Synty Search editor tool

- Use alongside the Synty Packge Importer tool.
- This tool makes use of the community-built Synty search page [www.syntysearch.com] and provides this same search feature in a convenient Unity editor window.
- The search reults are filtered by asset package.
- The prefab preview images and name text can be scaled using the slider control. 
- If you have set the location of your Unity packages, each package will be indicated as being "unowned", "owned" or "imported".
  - Clicking on any "unowned" item will take you to the Synty store page for that package
  - Clicking on any "imported" item will highlight that prefab in your project window, making it easy to find what you're looking for!
- You can set multiple package locations.
- The Unity aasset download folder is always scanned.
- Adding other locations is useful if you've downloaded the unitypackage file from the Synty store.
- The tool scans each location for any Polygon unitypackage files and saves a list of the assets. These are cached and this scan is only done once.
