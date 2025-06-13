using System.Collections;
using Unity.Burst;
using UnityEngine;


[BurstCompile]
public class TickManager : MonoBehaviour
{
    private GridManager gridManager;

    [SerializeField] private float tickDelay;
    [SerializeField] private bool paused;

    public static bool Paused;




    [BurstCompile]
    private void Start()
    {
        gridManager = GridManager.Instance;

        StartCoroutine(TickLoop());
    }



    [BurstCompile]
    private IEnumerator TickLoop()
    {
        float elapsed = 0;

        while (true)
        {
            yield return null;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                paused = !paused;
                Paused = paused;
            }

            if (paused) continue;


            elapsed += Time.deltaTime;

            if (elapsed > tickDelay)
            {
                elapsed = 0;

                gridManager.PerformCycle();
            }
        }
    }
}
