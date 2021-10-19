/*
Note: This importer is currently using a sort of hackish method for creating assets
meant to be used in the main asset, for example, materials. SubAssets cannot be edited 
as they are. There probably is a way to make them editable, but due to time constraints
I'm implementing this hackish method because it works.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.AssetImporters;
using GLTF;
using System.IO;
using System;

[ScriptedImporter(0, "gltf")]
public class GLTFEmbeddedImporter : ScriptedImporter
{
    // string fileDir;
    // string[] filePathArr;
    GLTFRoot gLTFRoot;
    byte[][] buffersArray;

    // Used when creating assets that are separate from the main asset. For 
    // example, materials. 
    string assetDir = ""; 

    public override void OnImportAsset(AssetImportContext ctx)
    {
        string[] assetPathSplit = assetPath.Split(new char[]{'/'});

        // assetPathSplit.Length-1 excludes the last element, which is 
        // usually the imported asset file name.
        for(int i = 0; i < assetPathSplit.Length-1; i++)
        {
            assetDir += $"{assetPathSplit[i]}";
            if(i < assetPathSplit.Length-2) assetDir += "/";
        }

        // fileDir = $"{Application.dataPath}/GLTFAssets";
        // filePathArr = new string[] {
        //     $"{fileDir}/untitled.gltf",         // 0
        //     $"{fileDir}/untitled1.gltf",        // 1
        //     $"{fileDir}/untitled2.gltf",        // 2
        //     $"{fileDir}/untitled13.gltf",       // 3
        //     $"{fileDir}/untitled14.gltf",       // 4
        //     $"{fileDir}/untitled15.gltf",       // 5
        //     $"{fileDir}/untitled16.gltf",       // 6
        //     $"{fileDir}/untitled17.gltf",       // 7
        //     $"{fileDir}/untitled18.gltf",       // 8
        //     $"{fileDir}/untitled19.gltf"        // 9
        // };

        ImportGLTFData(ctx.assetPath);
        ReadBuffers();
        GenerateGameObject(ref ctx);
        // TestStuff();
    }


    void ImportGLTFData(string filePath)
    {
        // using FileStream fs = File.Open(filePathArr[8], FileMode.Open);
        using FileStream fs = File.Open(filePath, FileMode.Open);
        using StreamReader sr = new StreamReader(fs);
        string gltfFileDataStr = sr.ReadToEnd();
        gLTFRoot = JsonUtility.FromJson<GLTFRoot>(gltfFileDataStr);

    }

    void GenerateGameObject(ref AssetImportContext ctx)
    {

        int nodeIndex = 0; 
        
        Vector3 transPos = gLTFRoot.nodes[nodeIndex].translation != null ? 
            new Vector3(
                gLTFRoot.nodes[nodeIndex].translation[0],
                gLTFRoot.nodes[nodeIndex].translation[1],
                gLTFRoot.nodes[nodeIndex].translation[2]
            ) : Vector3.zero;
        Quaternion transRot = gLTFRoot.nodes[nodeIndex].rotation != null ? 
            new Quaternion(
                gLTFRoot.nodes[nodeIndex].rotation[0],
                gLTFRoot.nodes[nodeIndex].rotation[1],
                gLTFRoot.nodes[nodeIndex].rotation[2],
                gLTFRoot.nodes[nodeIndex].rotation[3]
            ) : Quaternion.identity;
        Vector3 transScale = gLTFRoot.nodes[nodeIndex].scale != null ? 
            new Vector3(
                gLTFRoot.nodes[nodeIndex].scale[0],
                gLTFRoot.nodes[nodeIndex].scale[1],
                gLTFRoot.nodes[nodeIndex].scale[2]
            ) : Vector3.one;


        GameObject go = new GameObject();
        go.name = gLTFRoot.nodes[nodeIndex].name;
        go.transform.position = transPos;
        go.transform.rotation = transRot;
        go.transform.localScale = transScale;
        ctx.AddObjectToAsset(go.name, go);
        ctx.SetMainObject(go);

        int glTFMeshIndex = gLTFRoot.nodes[nodeIndex].mesh;
        Mesh goMesh = GenerateMesh(gLTFRoot, nodeIndex);
        goMesh.name = gLTFRoot.meshes[glTFMeshIndex].name;
        ctx.AddObjectToAsset(goMesh.name, goMesh);
        MeshFilter goMeshFilter = go.AddComponent<MeshFilter>();
        goMeshFilter.mesh = goMesh;
        List<Material> goMats = new List<Material>();
        for(int i = 0; i < gLTFRoot.meshes[glTFMeshIndex].primitives.Length; i++)
        {
            int gltfMatIndex = gLTFRoot.meshes[glTFMeshIndex].primitives[i].material;
            string matFileName = 
                gltfMatIndex != -1 ? gLTFRoot.materials[gltfMatIndex].name : $"SomeMat{i}";
            
            // Check if asset exists
            string[] searchResults = AssetDatabase.FindAssets(
                $"{matFileName} t:material", new string[] {assetDir}
            );

            Material mat;

            // If asset exists, load material and assign to mat.
            if(searchResults.Length != 0)
            {
                string matPath = AssetDatabase.GUIDToAssetPath(searchResults[0]);
                mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            }
            // Else, create new instance of mat and create new asset of it.
            else
            {
                mat = new Material(Shader.Find("Standard"));
                //////////////////////////////////////////////////////////////////////////
                /*
                NOTE: This line is a hackish solution to a problem. At the moment I don't 
                know how to make SubAssets editable so due to time constraints I'm using
                this method. So instead of materials being created as SubAssets they will
                be created as individual assets in the same directory as assetPath.
                */
                AssetDatabase.CreateAsset(mat, $"{assetDir}/{matFileName}.mat");
                //////////////////////////////////////////////////////////////////////////
            }

            mat.name = matFileName;
            
            // This line is for debugging purposes. It assigns a random color
            // to the _Color property of the material.
            // mat.SetColor("_Color", UnityEngine.Random.ColorHSV());

            goMats.Add(mat);
            // ctx.AddObjectToAsset(mat.name, mat);
        }
        MeshRenderer goMeshRenderer = go.AddComponent<MeshRenderer>();
        goMeshRenderer.sharedMaterials = goMats.ToArray();

    }

    
    // Generates mesh from a node in glTFRoot. 
    // NOTE: This code was initially written to using Mesh.SetTriangles() to combine
    // submeshes into the final mesh, but because of how glTF handles meshes with
    // multiple primitives (submeshes) we had to switch over to using
    // Mesh.CombineMeshes() instead. This required a few changes to the original
    // logic, so if you see any lines of code or comments that seem redundant, that is why.
    Mesh GenerateMesh(GLTFRoot glTFRoot, int nodeIndex)
    {
        int glTFMeshIndex = glTFRoot.nodes[nodeIndex].mesh;
        int glTFMeshPrimCount = glTFRoot.meshes[glTFMeshIndex].primitives.Length;
        
        Mesh mainMesh = new Mesh();
        mainMesh.name = glTFRoot.meshes[glTFMeshIndex].name;
        CombineInstance[] meshCombineInstArr = new CombineInstance[glTFMeshPrimCount];

        for(int i = 0; i < glTFMeshPrimCount; i++)
        {
            Mesh mesh = new Mesh();

            List<Vector3> meshVertices = new List<Vector3>();
            List<Vector3> meshNormals = new List<Vector3>();
            List<Vector4> meshTangents = new List<Vector4>();
            List<Vector2> meshUVs = new List<Vector2>();
            List<int> meshTris = new List<int>();

            int bufferViewIndex = 0, byteLength = 0, byteOffset = 0, bufferIndex = 0,
            typeCount = 0, componentSize = 0, typeSize = 0;

            ////////////////////////////////////////////////
            // Vertices
            ////////////////////////////////////////////////
            // Read POSITION data from buffer. POSITION data type is expected to be
            // VEC3(float,float,float).
            int accessorIndex = glTFRoot.meshes[glTFMeshIndex].primitives[i].attributes.POSITION;
            
            // If false, this attribute hasn't been assinged, so skip this block.
            if(accessorIndex != -1) 
            {
                bufferViewIndex = glTFRoot.accessors[accessorIndex].bufferView;
                byteLength = glTFRoot.bufferViews[bufferViewIndex].byteLength;
                byteOffset = glTFRoot.bufferViews[bufferViewIndex].byteOffset;
                bufferIndex = glTFRoot.bufferViews[bufferViewIndex].buffer;
                typeCount = glTFRoot.accessors[accessorIndex].count; // Accessor type count
                componentSize = sizeof(float); // Accessor component size 
                typeSize = sizeof(float) * 3; // Accessor type size

                for(int j = 0; j < typeCount; j++)
                {
                    meshVertices.Add(
                        BuildVector3FromBuffer(bufferIndex, byteOffset, j, typeSize)
                    );
                }
                mesh.vertices = meshVertices.ToArray();
            }

            ////////////////////////////////////////////////
            // Normals
            ////////////////////////////////////////////////
            // Read NORMAL data from buffer. NORMAL data type is expected to be
            // VEC3(float,float,float).
            accessorIndex = glTFRoot.meshes[glTFMeshIndex].primitives[i].attributes.NORMAL;
            
            // If false, this attribute hasn't been assinged, so skip this block.
            if(accessorIndex != -1) 
            {
                bufferViewIndex = glTFRoot.accessors[accessorIndex].bufferView;
                byteLength = glTFRoot.bufferViews[bufferViewIndex].byteLength;
                byteOffset = glTFRoot.bufferViews[bufferViewIndex].byteOffset;
                bufferIndex = glTFRoot.bufferViews[bufferViewIndex].buffer;
                typeCount = glTFRoot.accessors[accessorIndex].count; // Accessor type count
                componentSize = sizeof(float); // Accessor component size 
                typeSize = sizeof(float) * 3; // Accessor type size

                for(int j = 0; j < typeCount; j++)
                {
                    meshNormals.Add(
                        BuildVector3FromBuffer(bufferIndex, byteOffset, j, typeSize)
                    );
                }
                mesh.normals = meshNormals.ToArray();
            }

            ////////////////////////////////////////////////
            // Tangents
            ////////////////////////////////////////////////
            // Read TANGENT data from buffer. TANGENT data type is expected to be
            // VEC4(float,float,float,float).
            accessorIndex = glTFRoot.meshes[glTFMeshIndex].primitives[i].attributes.TANGENT;
            
            // If false, this attribute hasn't been assinged, so skip this block.
            if(accessorIndex != -1)
            {
                bufferViewIndex = glTFRoot.accessors[accessorIndex].bufferView;
                byteLength = glTFRoot.bufferViews[bufferViewIndex].byteLength;
                byteOffset = glTFRoot.bufferViews[bufferViewIndex].byteOffset;
                bufferIndex = glTFRoot.bufferViews[bufferViewIndex].buffer;
                typeCount = glTFRoot.accessors[accessorIndex].count; // Accessor type count
                componentSize = sizeof(float); // Accessor component size 
                typeSize = sizeof(float) * 4; // Accessor type size

                for(int j = 0; j < typeCount; j++)
                {
                    meshTangents.Add(
                            BuildVector4FromBuffer(bufferIndex, byteOffset, j, typeSize)
                    );
                }
                mesh.tangents = meshTangents.ToArray();
            }

            ////////////////////////////////////////////////
            // UVs
            ////////////////////////////////////////////////
            // Read UV data from buffer. UV data type is expected to be
            // VEC2(float,float).
            accessorIndex = glTFRoot.meshes[glTFMeshIndex].primitives[i].attributes.TEXCOORD_0;
            
            // If false, this attribute hasn't been assinged, so skip this block.
            if(accessorIndex != -1)
            {
                bufferViewIndex = glTFRoot.accessors[accessorIndex].bufferView;
                byteLength = glTFRoot.bufferViews[bufferViewIndex].byteLength;
                byteOffset = glTFRoot.bufferViews[bufferViewIndex].byteOffset;
                bufferIndex = glTFRoot.bufferViews[bufferViewIndex].buffer;
                typeCount = glTFRoot.accessors[accessorIndex].count; // Accessor type count
                componentSize = sizeof(float); // Accessor component size 
                typeSize = sizeof(float) * 2; // Accessor type size

                for(int j = 0; j < typeCount; j++)
                {
                    meshUVs.Add(
                            BuildVector2FromBuffer(bufferIndex, byteOffset, j, typeSize)
                    );
                }
                mesh.uv = meshUVs.ToArray();
            }

            ////////////////////////////////////////////////
            // Indices
            ////////////////////////////////////////////////
            // Read Indices data from buffer. Indices data type is expected to be Int16.
            accessorIndex = glTFRoot.meshes[glTFMeshIndex].primitives[i].indices;
            
            // If false, this attribute hasn't been assinged, so skip this block.
            if(accessorIndex != -1) 
            {
                bufferViewIndex = glTFRoot.accessors[accessorIndex].bufferView;
                byteLength = glTFRoot.bufferViews[bufferViewIndex].byteLength;
                byteOffset = glTFRoot.bufferViews[bufferViewIndex].byteOffset;
                bufferIndex = glTFRoot.bufferViews[bufferViewIndex].buffer;
                typeCount = glTFRoot.accessors[accessorIndex].count; // Accessor type count
                componentSize = sizeof(byte); // Accessor component size 
                typeSize = sizeof(UInt16); // Accessor type size

                for(int j = 0; j < typeCount; j++)
                {
                    meshTris.Add(
                        BuildUInt16FromBuffer(bufferIndex, byteOffset, j, typeSize)
                    );
                }
                mesh.triangles = meshTris.ToArray();
            }

            meshCombineInstArr[i] = new CombineInstance();
            meshCombineInstArr[i].mesh = mesh;
        }

        mainMesh.CombineMeshes(meshCombineInstArr, false, false, false);        
        

        return mainMesh;
    }

    // Builds a Vector2 from bytes in buffer, using type information
    // from an accessor and bufferview. This is a glTF helper method to 
    // reduce repetitive steps.
    Vector3 BuildVector2FromBuffer(int bufferIndex, int byteOffset, int iter, int typeSize)
    {
        Vector2 v = new Vector2(
            BitConverter.ToSingle(
                new byte[]{
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+1],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+2],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+3]
                },0
            ),
            BitConverter.ToSingle(
                new byte[]{
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+4],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+5],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+6],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+7]
                },0
            )
        );
        return v;
    }

    // Builds a Vector3 from bytes in buffer, using type information
    // from an accessor and bufferview. This is a glTF helper method to 
    // reduce repetitive steps.
    Vector3 BuildVector3FromBuffer(int bufferIndex, int byteOffset, int iter, int typeSize)
    {
        Vector3 v = new Vector3(
            BitConverter.ToSingle(
                new byte[]{
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+1],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+2],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+3]
                },0
            ),
            BitConverter.ToSingle(
                new byte[]{
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+4],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+5],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+6],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+7]
                },0
            ),
            BitConverter.ToSingle(
                new byte[]{
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+8],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+9],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+10],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+11]
                },0
            )
        );
        return v;
    }

    // Builds a Vector4 from bytes in buffer, using type information
    // from an accessor and bufferview. This is a glTF helper method to 
    // reduce repetitive steps.
    Vector3 BuildVector4FromBuffer(int bufferIndex, int byteOffset, int iter, int typeSize)
    {
        Vector4 v = new Vector4(
            BitConverter.ToSingle(
                new byte[]{
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+1],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+2],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+3]
                },0
            ),
            BitConverter.ToSingle(
                new byte[]{
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+4],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+5],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+6],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+7]
                },0
            ),
            BitConverter.ToSingle(
                new byte[]{
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+8],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+9],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+10],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+11]
                },0
            ),
            BitConverter.ToSingle(
                new byte[]{
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+12],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+13],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+14],
                    buffersArray[bufferIndex][byteOffset+(iter*typeSize)+15]
                },0
            )
        );
        return v;
    }

    // Builds a Int16 from bytes in buffer, using type information
    // from an accessor and bufferview. This is a glTF helper method to 
    // reduce repetitive steps.
    UInt16 BuildUInt16FromBuffer(int bufferIndex, int byteOffset, int iter, int typeSize)
    {
        UInt16 ret = BitConverter.ToUInt16(
            new byte[]{
                buffersArray[bufferIndex][byteOffset+(iter*typeSize)],
                buffersArray[bufferIndex][byteOffset+(iter*typeSize)+1]
            },0
        );
        return ret;
    }

    // Create buffer array containing all buffers from the glFT file.
    // Buffer data is a BLOB. BLOB data will either be embedded in the
    // glTF file and stored in the Uri, or stored in an external file which 
    // will be linked in the Uri.
    void ReadBuffers()
    {
        buffersArray = new byte[gLTFRoot.buffers.Length][];
        for(int i = 0; i < buffersArray.Length; i++)
        {
            byte[] bufferData = new byte[0];

            // Read buffer URI
            string[] splitUri = gLTFRoot.buffers[i].uri.Split(new char[]{','});

            // If true, Uri has embeded data.
            if(splitUri.Length == 2)
            {
                bufferData = Convert.FromBase64String(splitUri[1]);
            }
            else
            {
                // If this block is reached, Uri links to external file, so 
                // buffer data must be read from that file. Implement later.
            }

            buffersArray[i] = bufferData;
        }
    }

    void TestStuff()
    {
        // return;
        // Debug.Log(gLTFRoot.nodes[0].translation == null);
        
        // GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject go = GameObject.Find("RefCubeGO");
        Mesh mesh = go.GetComponent<MeshFilter>().mesh;
        GameObject cubeGO = new GameObject("CubeGO");
        cubeGO.transform.position = new Vector3(0,0,-2);
        MeshFilter cubeGOMeshFilter = cubeGO.AddComponent<MeshFilter>();
        MeshRenderer cubeGOMeshRenderer = cubeGO.AddComponent<MeshRenderer>();
        
        List <Material> cubeGOMats = new List<Material>();
        for(int i = 0; i < mesh.subMeshCount; i++)
        {
            Material cubeGOMat = new Material(Shader.Find("Standard"));
            cubeGOMat.name = $"SomeMat{i}";
            cubeGOMat.SetColor("_Color", UnityEngine.Random.ColorHSV());
            // cubeGOMat.SetTexture("_MainTex", MainTex);
            cubeGOMats.Add(cubeGOMat);
        }

        Mesh cubeGOMesh = new Mesh();
        cubeGOMesh.name = "CubeGOMesh";
        cubeGOMesh.Clear();
        cubeGOMesh.vertices = mesh.vertices;
        // cubeGOMesh.triangles = new int[mesh.triangles.Length];
        cubeGOMesh.normals = mesh.normals;
        cubeGOMesh.tangents = mesh.tangents;
        cubeGOMesh.uv = mesh.uv;
        cubeGOMesh.subMeshCount = mesh.subMeshCount;

        for(int i = 0; i < mesh.subMeshCount; i++)
        {
            SubMeshDescriptor desc = mesh.GetSubMesh(i);
            int[] tris = new int[desc.indexCount];
            for(int j = 0; j < desc.indexCount; j++)
            {
                tris[j] = mesh.triangles[j+desc.indexStart];
            }
            cubeGOMesh.SetTriangles(tris, i);
        }
        
        cubeGOMeshFilter.mesh = cubeGOMesh;
        cubeGOMeshRenderer.sharedMaterials = cubeGOMats.ToArray();
        

    }

}



    // Start is called before the first frame update
    // void Start()
    // {
    //     fileDir = $"{Application.dataPath}/GLTFAssets";
    //     filePathArr = new string[] {
    //         $"{fileDir}/untitled.gltf",         // 0
    //         $"{fileDir}/untitled1.gltf",        // 1
    //         $"{fileDir}/untitled2.gltf",        // 2
    //         $"{fileDir}/untitled13.gltf",       // 3
    //         $"{fileDir}/untitled14.gltf",       // 4
    //         $"{fileDir}/untitled15.gltf",       // 5
    //         $"{fileDir}/untitled16.gltf",       // 6
    //         $"{fileDir}/untitled17.gltf",       // 7
    //         $"{fileDir}/untitled18.gltf",       // 8
    //         $"{fileDir}/untitled19.gltf"        // 9
    //     };

    //     ImportGLTFData();
    //     ReadBuffers();
    //     GenerateGameObjects();
    //     // TestStuff();
    //     // UnityEditor.EditorApplication.isPlaying = false;



    // }
