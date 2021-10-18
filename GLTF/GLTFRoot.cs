using System;

namespace GLTF
{
[Serializable]
public class GLTFRoot
{
    // Set all int field values to -1. During JSON->Object deserialization, any field 
    // that exists in the JSON form will be changed from -1 to something else. If a 
    // field's value remains -1, it is assumed that the field is not present in the 
    // JSON form.
    public GLTFAsset asset;
    public int scene = -1;
    public GLTFScene[] scenes;
    public GLTFNode[] nodes;
    public GLTFMaterial[] materials;
    public GLTFMesh[] meshes;
    public GLTFAccessor[] accessors;
    public GLTFBufferView[] bufferViews;
    public GLTFBuffer[] buffers;
}
}