using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;


[BurstCompile]
public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    private void Awake()
    {
        Instance = this;
    }


    [SerializeField] private float2 gridPosition;
    [SerializeField] private float2 gridSize;
    [SerializeField] private float tileSize;

    private int gridSizeX, gridSizeZ;
    private int totalCellCount;

    private NativeArray<bool> cellStates;
    private NativeArray<bool> cellNextStates;

    private NativeArray<int> neighborOffsets;

    [SerializeField] private float colorFadeSpeed;


    [ColorUsage(true, true)]
    [SerializeField] private Color aliveColor, deadColor;

    private Matrix4x4[] matrices;
    private MaterialPropertyBlock mPropertyBlock;
    private Vector4[] cellColors;

    private Mesh quadMesh;
    [SerializeField] private Material material;



    [BurstCompile]
    private void Start()
    {
        SetupCellData();

        GPUInstanceHelper.CalculateCellMeshData(gridSizeX, gridSizeZ, tileSize, gridSize, gridPosition, deadColor, out quadMesh, out matrices, out cellColors, out mPropertyBlock);
    }

    [BurstCompile]
    private void SetupCellData()
    {
        gridSizeX = (int)math.round(gridSize.x / tileSize);
        gridSizeZ = (int)math.round(gridSize.y / tileSize);

        totalCellCount = gridSizeX * gridSizeZ;

        cellStates = new NativeArray<bool>(gridSizeX * gridSizeZ, Allocator.Persistent);
        cellNextStates = new NativeArray<bool>(gridSizeX * gridSizeZ, Allocator.Persistent);

        neighborOffsets = new NativeArray<int>(new int[]
        {
            +gridSizeZ,
            -gridSizeZ,
            +1,
            -1,
            +gridSizeZ + 1,
            +gridSizeZ - 1,
            -gridSizeZ + 1,
            -gridSizeZ - 1
        }, Allocator.Persistent);
    }




    [BurstCompile]
    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            OnClickHeld(true);
        }
        else if (Input.GetMouseButton(1))
        {
            OnClickHeld(false);
        }

        Graphics.DrawMeshInstanced(quadMesh, 0, material, matrices, totalCellCount, mPropertyBlock);


        if (TickManager.Paused)
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        for (int i = 0; i < totalCellCount; i++)
        {
            if (cellStates[i] == false)
            {
                cellColors[i] = MoveTowardsColor(cellColors[i], deadColor, colorFadeSpeed * deltaTime);
            }
        }
    }

    [BurstCompile]
    public Color MoveTowardsColor(Color a, Color b, float maxStep)
    {
        return new Color(
            Mathf.MoveTowards(a.r, b.r, maxStep),
            Mathf.MoveTowards(a.g, b.g, maxStep),
            Mathf.MoveTowards(a.b, b.b, maxStep),
            Mathf.MoveTowards(a.a, b.a, maxStep)
        );
    }


    [BurstCompile]
    private void OnClickHeld(bool leftClick)
    {
        Vector2 mousePos = Input.mousePosition;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);

        UpdateCellFromWorldPoint(new float2(worldPos.x, worldPos.y), leftClick);
    }


    [BurstCompile]
    public void UpdateCellFromWorldPoint(float2 worldPosition, bool state)
    {
        worldPosition -= gridPosition;

        float percentX = math.clamp((worldPosition.x + gridSize.x / 2) / gridSize.x, 0, 1);
        float percentZ = math.clamp((worldPosition.y + gridSize.y / 2) / gridSize.y, 0, 1);

        int x = (int)math.floor(percentX * gridSizeX);
        int z = (int)math.floor(percentZ * gridSizeZ);

        x = math.clamp(x, 0, gridSizeX - 1);
        z = math.clamp(z, 0, gridSizeZ - 1);

        int gridId = x * gridSizeZ + z;

        //set cell state and next cellState
        cellStates[gridId] = state;
        cellNextStates[gridId] = state;

        //update visual color of cell
        cellColors[gridId] = state ? aliveColor : deadColor;

        //update cellColors
        mPropertyBlock.SetVectorArray("_Colors", cellColors);
    }


    [BurstCompile]
    public void PerformCycle()
    {
        bool aliveState;
        int aliveNeighbourCount;

        //calculate and save all cellStates, also update mesh Color to visualize the cells
        for (int gridId = 0; gridId < totalCellCount; gridId++)
        {
            //get current cell state
            aliveState = cellStates[gridId];

            aliveNeighbourCount = GetAliveNeighborCount(gridId);


            //if currentCell is alive
            if (aliveState == true)
            {
                //if alive cell does not have exactly 2 or 3 alive neighbours, it dies
                if (aliveNeighbourCount < 2 || aliveNeighbourCount > 3)
                {
                    //save next cell state
                    cellNextStates[gridId] = false;
                }
            }

            //if currentCell is dead, bring it to life if it has exactly 3 neighbours.
            else if (aliveNeighbourCount == 3)
            {
                //save next cell state
                cellNextStates[gridId] = true;
            }
        }

        //apply all calculated cellStates
        for (int gridId = 0; gridId < totalCellCount; gridId++)
        {
            //get saved newState
            aliveState = cellNextStates[gridId];

            cellStates[gridId] = aliveState;

            if (aliveState == true)
            {
                cellColors[gridId] = aliveColor;
            }

            //cellColors[gridId] = aliveState ? aliveColor : deadColor;
        }

        //update cellColors
        mPropertyBlock.SetVectorArray("_Colors", cellColors);
    }



    [BurstCompile]
    private int GetAliveNeighborCount(int gridId)
    {
        int neighbourCount = 0;


        for (int i = 0; i < 8; i++)
        {
            int neighbourId = gridId + neighborOffsets[i];

            // if neighbour cell exists
            if (IsInGrid(neighbourId) && cellStates[neighbourId] == true)
            {
                neighbourCount += 1;

                //after 4 living cells, ant more wont change the outcome of what this coun result is used for
                if (neighbourCount == 4) return 4;
            }
        }

        return neighbourCount;
    }


    [BurstCompile]
    private bool IsInGrid(int gridId)
    {
        if (gridId < 0 || gridId >= gridSizeX * gridSizeZ)
        {
            return false; // Out of array bounds
        }

        int x = gridId % gridSizeX; // Extract X coordinate
        int y = gridId / gridSizeX; // Extract Y coordinate

        return x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeZ;
    }




    private void OnDestroy()
    {
        cellStates.Dispose();
        cellNextStates.Dispose();

        neighborOffsets.Dispose();
    }


#if UNITY_EDITOR

    [SerializeField] private bool drawMasterGizmos;
    [SerializeField] private bool drawTileGizmos;
    [SerializeField] private bool drawTileDataGizmos;

    public void OnDrawGizmos()
    {
        if (drawMasterGizmos == false)
        {
            return;
        }

        Gizmos.DrawWireCube((Vector2)gridPosition, new Vector3(gridSize.x, gridSize.y, 0));

        Vector2 worldBottomLeft = (Vector2)gridPosition - Vector2.right * gridSize.x / 2 - Vector2.up * gridSize.y / 2;
        if (drawTileGizmos)
        {
            int gridSizeX = Mathf.RoundToInt(gridSize.x / tileSize);
            int gridSizeZ = Mathf.RoundToInt(gridSize.y / tileSize);

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(worldBottomLeft + Vector2.right * (x * tileSize + tileSize / 2) + Vector2.up * (z * tileSize + tileSize / 2), new Vector3(tileSize, tileSize, 0));
                }
            }
        }
        if (drawTileDataGizmos && Application.isPlaying)
        {
            for (int x = 0; x < gridSizeX; x++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    if ((x * gridSizeZ + z) % 2 == 1)
                    {
                        Gizmos.color = Color.green;
                    }
                    else
                    {
                        Gizmos.color = Color.black;
                    }

                    Gizmos.DrawCube(worldBottomLeft + Vector2.right * (x * tileSize + tileSize / 2) + Vector2.up * (z * tileSize + tileSize / 2), new Vector3(tileSize / 2, tileSize / 2, 0));
                }
            }
        }
    }

#endif
}