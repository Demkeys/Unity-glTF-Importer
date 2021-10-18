# Unity-glTF-Importer
A very basic glTF importer for Unity. I made this for educational purpose so that I can understand the glTF file format better. You are free to use the code in this repo however your like.
----
This is a Scripted Importer to import \*.gltf files. Currently only supports glTF Embedded files. Support for glTF Separate files and glTF Binary files might be added at a later time. 

### Features:
- Reads and processes first node of the glTF file. Currently only Mesh geometry datais processed. Support for Animation, Skinning, etc. data may be added in the future.
- Creates a prefab containing:
    - Gameobject (Main Object) containing:
      - MeshFilter
      - MeshRenderer
    - Mesh (Sub Object)
    - One or more Materails (Sub Objects)
- Supports meshes with multiple materials.

### Notes: 
- Child gameobjects are currently not supported. Support may be added in the future.
- Materials are created, however they cannot be edited, so after the glTF file has been imported, you 

__Future note: To add support for child gameobjects, maybe change the logic a bit so that all the nodes are scanned and the first node that contains children is converted to a gameobject, along with all it's child gameobjects.__
