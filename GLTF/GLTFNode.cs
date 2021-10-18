using System;
using UnityEngine;

namespace GLTF
{
[Serializable]
public class GLTFNode
{
    // Set all int field values to -1. During JSON->Object deserialization, any field 
    // that exists in the JSON form will be changed from -1 to something else. If a 
    // field's value remains -1, it is assumed that the field is not present in the 
    // JSON form.
    public int[] children;
    public string name;
    public int camera = -1;
    public int skin = -1;
    public float[] matrix;
    public int mesh = -1;
    public float[] rotation;
    public float[] scale;
    public float[] translation;
    public int[] weights;
}
}