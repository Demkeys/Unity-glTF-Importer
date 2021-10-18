using System;

namespace GLTF
{
[Serializable]
public class GLTFBuffer
{
    // Set all int field values to -1. During JSON->Object deserialization, any field 
    // that exists in the JSON form will be changed from -1 to something else. If a 
    // field's value remains -1, it is assumed that the field is not present in the 
    // JSON form.
    public string name;
    public int byteLength = -1;
    public string uri;
}
}