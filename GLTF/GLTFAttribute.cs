using System;

namespace GLTF
{
[Serializable]
public class GLTFAttribute
{
    // Set all field values to -1. During JSON->Object deserialization, any field that 
    // exists in the JSON form will be changed from -1 to something else. If a field's
    // value remains -1, it is assumed that the field is not present in the JSON form.
    public int POSITION = -1;
    public int NORMAL = -1;
    public int TANGENT = -1;
    public int TEXCOORD_0 = -1;
    public int TEXCOORD_1 = -1;
    public int TEXCOORD_2 = -1;
}
}