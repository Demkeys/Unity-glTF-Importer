using System;

namespace GLTF
{
[Serializable]
public class GLTFBufferView
{
    // Set all int field values to -1. During JSON->Object deserialization, any field 
    // that exists in the JSON form will be changed from -1 to something else. If a 
    // field's value remains -1, it is assumed that the field is not present in the 
    // JSON form.
    public int buffer = -1;
    public int byteOffset = -1;
    public int byteLength = -1;
    public int byteStride = -1;
    public int target = -1;
    public string name;
}
}