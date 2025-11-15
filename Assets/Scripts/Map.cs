using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
    [SerializeField] private RectTransform gridRoot;

    private readonly List<List<RectTransform>> grids = new();

    public int Width => grids[0].Count;
    public int Height => grids.Count;

    private void Start()
    {
        for (var y = 0; y < gridRoot.childCount; y++)
        {
            var gy = gridRoot.GetChild(y) as RectTransform;

            grids.Add(new());

            for (var x = 0; x < gy.childCount; x++)
            {
                var gx = gy.GetChild(x) as RectTransform;

                grids[y].Add(gx);
            }
        }
    }

    public RectTransform GetGrid(int x, int y) => grids[y][x];

    public RectTransform GetGrid(Vector2Int pos) => GetGrid(pos.x, pos.y);
}
