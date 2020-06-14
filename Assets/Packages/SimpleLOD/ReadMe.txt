SimpleLOD 1.6
-------------
Combine multiple meshes into 1 mesh with submeshes per unique material
Reduce triangles
Generate LOD levels
Bake texture atlases
remove submeshes
remove hidden triangles


Workflow
--------
Select a gameObject
Click Tools -> Simple LOD in Unity's menu
Read the text on the popup window and use one of the options


Documentation
-------------
The documentation is available online at http://orbcreation.com/orbcreation/docu.orb?1126


Package Contents
----------------
SimpleLOD / ReadMe.txt   (this file)
SimpleLOD / LODMaker.cs   (the algorithm that reduces triangles)
SimpleLOD / LODSwitcher.cs   (better alternative for Unity's LODGroup component)
SimpleLOD / Editor / SimpleLOD_Editor   (Main SimpleLOD editor window)
SimpleLOD / Editor / SimpleLOD_MaterialPopUp   (Editor window for baking atlases)
SimpleLOD / Editor / SimpleLOD_MergePopup   (Editor window for merging meshes)
SimpleLOD / Editor / SimpleLOD_RemoveHiddenPopUp   (Editor window for removing hidden triangles)
SimpleLOD / Editor / SimpleLOD_Remove   (Editor window for removing entire submeshes)
SimpleLOD / Extensions / FloatExtensions.cs  (Extensions to default Unity class)
SimpleLOD / Extensions / MeshExtensions.cs  (Extensions to default Unity class)
SimpleLOD / Extensions / GameObjectExtensions.cs  (Extensions to default Unity class)
SimpleLOD / Extensions / TransformExtensions.cs  (Extensions to default Unity class)
SimpleLOD / Extensions / Vector3Extensions.cs  (Extensions to default Unity class)
SimpleLOD / Extensions / Texture2DExtensions.cs  (Extensions to default Unity class)
SimpleLOD / Extensions / RectExtensions.cs  (Extensions to default Unity class)
SimpleLOD / Demo  (Files needed ofr the demo only)


Minimal needed in your project
------------------------------
The following files need to be somewhere in your project folders:
- LODMaker.cs 
- LODSwitcher.cs (only if you use it)
- FloatExtensions.cs
- MeshExtensions.cs
- GameObjectExtensions.cs
- TransformExtensions.cs
- Vector3Extensions.cs
- Texture2DExtensions.cs
- RectExtensions.cs

When you want to use the editor windows:
SimpleLOD_Editor
SimpleLOD_MaterialPopUp
SimpleLOD_MergePopup
SimpleLOD_RemoveHiddenPopUp
SimpleLOD_Remove

All the rest can go.


Using multiple Orbcreation packages
-----------------------------------
The code files with the extensions to default classes like GameObjectExtensions.cs may also be included in other Orbcreation packages. In that case you may end up with multiple copies of the same file, or even multiple versions of these files. In this you are advised to merge the contents of the files together.


C# and Javascript
-----------------
If you have a Javascript project that uses the SimpleLOD package, you will have to place the scripts in the "Standard Assets", "Pro Standard Assets" or "Plugins" folder and your Javascripts outside of these folders. The code inside the "Standard Assets", "Pro Standard Assets" or "Plugins" is compiled first and the code outside is compiled in a later step making the Types defined in the compilation step (the C# scripts) available to later compilation steps (your Javascript scripts).


