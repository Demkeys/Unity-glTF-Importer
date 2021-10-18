using System;

namespace GLTF
{
[Serializable]
public class GLTFPrimitive
{
    // Set all int field values to -1. During JSON->Object deserialization, any field 
    // that exists in the JSON form will be changed from -1 to something else. If a 
    // field's value remains -1, it is assumed that the field is not present in the 
    // JSON form.
    public GLTFAttribute attributes;
    public int indices = -1;
    public int material = -1;
    public int mode = -1;

}
}