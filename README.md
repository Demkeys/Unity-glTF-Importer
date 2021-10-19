# Unity-glTF-Importer
### A very basic glTF importer for Unity. I made this for educational purpose so that I can understand the glTF file format better. You are free to use the code in this repo however your like.
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
- SubAssets are not editable in the Inspector by default. So to allow materials to be editable, the material assets are created spearately and referenced in the main asset. Since they are individual assets you will be able to edit them in the inspector. This is a temp fix implemented due to time constraints.
- When a glTF file is imported, if a material it requires already exists in the same directory, that material will be referenced. If it doesn't exist, a new material asset will be created in the directory. Something to look out for here is that a material (that already exists in the directory) that is being referenced could be a SubAsset of another asset and might not be editable. It's a not a huge problem but something to keep in mind.

__Future note: To add support for child gameobjects, maybe change the logic a bit so that all the nodes are scanned and the first node that contains children is converted to a gameobject, along with all it's child gameobjects.__

### Usage Instructions:
Import GLTF folder into your project. This will import the Scripted Importer along with all the other files needed. You can then drag and drop a \*.gltf file into your project and the importer will generate a prefab out of it.

### Tested with Unity 2021.1.0f1.
