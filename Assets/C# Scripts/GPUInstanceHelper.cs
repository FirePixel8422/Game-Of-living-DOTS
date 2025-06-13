using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;


public static class GPUInstanceHelper
{
    public static void CalculateCellMeshData(int gridSizeX, int gridSizeZ, float tileSize, float2 gridSize, float2 gridPosition, Color deadColor, out Mesh quadMesh, out Matrix4x4[] matrices, out Vector4[] cellColors, out MaterialPropertyBlock mPropertyBlock)
    {
        List<Matrix4x4> matrixList = new List<Matrix4x4>();
        List<Vector4> colorList = new List<Vector4>();

        Vector2 worldBottomLeft = new Vector2(gridPosition.x, gridPosition.y) - Vector2.right * gridSize.x / 2 - Vector2.up * gridSize.y / 2;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                Vector2 worldPosition = worldBottomLeft + Vector2.right * (x * tileSize + tileSize / 2) + Vector2.up * (z * tileSize + tileSize / 2);


                //GameObject obj = Object.Instantiate(new GameObject("TEXT"), worldPosition + new Vector2(-.125f, .085f), Quaternion.identity);
                //TextMesh textObj = obj.AddComponent<TextMesh>();
                //textObj.text = (x + z * gridSizeX).ToString();
                //textObj.characterSize = 0.0125f;
                //textObj.fontSize = 100;


                Matrix4x4 matrix = Matrix4x4.TRS(worldPosition, Quaternion.identity, Vector3.one * tileSize);
                matrixList.Add(matrix);

                colorList.Add(deadColor);
                
                //colorList.Add(new Color((float)x / gridSizeX / 2, 0, (float)y / gridSizeZ / 2, 1));
            }
        }

        quadMesh = CreateQuad();

        matrices = matrixList.ToArray();
        cellColors = colorList.ToArray();

        mPropertyBlock = new MaterialPropertyBlock();
    }



    private static Mesh CreateQuad()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0)
        };

        int[] triangles = { 0, 2, 1, 2, 3, 1 };

        Vector2[] uv = {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();

        return mesh;
    }
}
