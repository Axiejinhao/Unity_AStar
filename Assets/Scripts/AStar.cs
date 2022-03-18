using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class AStar : MonoBehaviour
{
    [Header("小方格预设体")] public GameObject cubePrefab;
    [Header("地面缩放对应的实际长度")] public int gridScale = 10;
    [Header("小方格的边长")] public float cubeLength = 0.5f;
    [Header("地形网格长度方向小方格的个数")] public int gridLength;
    [Header("地形网格宽度方向小方格的个数")] public int gridWidth;
    [Header("障碍物出现的比例")] [Range(0, 100)] public int obstacleScale = 30;
    [Space] [Header("起点坐标")] public int startX = 0;
    public int startY = 0;
    [Header("终点坐标")] public int endX = 0;
    public int endY = 0;
    [Header("摄影机")] public Transform followCamera;

    private Vector3 cameraDis;
    //存储所有的格子
    private GridItem[,] _gridItems;
    //摄像机跟随的位置
    private Vector3 followPos;
    //路径总长度
    private int pathCount;

    private void Start()
    {
        GridInit();
        //计算起点和摄像机的初始距离
        cameraDis = followCamera.position - _gridItems[startX,startY].pos;
        // Debug.Log(cameraDis);
        AStarPathFinding();
        followPos = _gridItems[startX, startY].pos;
    }

    private void Update()
    {
        followCamera.position = Vector3.Lerp(followCamera.position, followPos + cameraDis,Time.deltaTime * 3f) ;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }

    private void GridInit()
    {
        //计算网格中小方格长宽方向的个数
        gridLength = (int) (transform.localScale.x * gridScale / cubeLength);
        gridWidth = (int) (transform.localScale.z * gridScale / cubeLength);
        //初始化数组
        _gridItems = new GridItem[gridLength, gridWidth];

        for (int i = 0; i < gridLength; i++)
        {
            for (int j = 0; j < gridWidth; j++)
            {
                //地形偏移量
                Vector3 gridOffset = new Vector3(-gridScale / 2 * transform.localScale.x, 0,
                    -gridScale / 2 * transform.localScale.z);
                //小方块偏移量
                Vector3 cubeOffset = new Vector3(cubeLength / 2, 0, cubeLength / 2);

                //生成小方格
                GameObject cube = Instantiate(cubePrefab,
                    new Vector3((float) i * cubeLength, 0, (float) j * cubeLength) +
                    gridOffset + cubeOffset + transform.position,
                    Quaternion.identity);
                cube.transform.SetParent(transform);

                //获取脚本组件GridItem
                GridItem item = cube.GetComponent<GridItem>();
                //存储格子到数组
                _gridItems[i, j] = item;
                //获取世界坐标
                _gridItems[i, j].pos = cube.transform.position;
                //设置Item坐标
                item.x = i;
                item.y = j;
                //设置Item的类型
                int ran = Random.Range(1, 101);
                if (ran <= obstacleScale)
                {
                    item.SetItemType(ItemType.Obstacle);
                }
            }
        }

        //设置起点和终点格子
        try
        {
            _gridItems[startX, startY].SetItemType((ItemType.Start));
            _gridItems[endX, endY].SetItemType((ItemType.End));
        }
        catch (IndexOutOfRangeException e)
        {
            startX = Mathf.Clamp(startX, 0, gridLength - 1);
            startY = Mathf.Clamp(startY, 0, gridWidth - 1);
            endX = Mathf.Clamp(endX, 0, gridLength - 1);
            endY = Mathf.Clamp(endY, 0, gridWidth - 1);
            _gridItems[startX, startY].SetItemType((ItemType.Start));
            _gridItems[endX, endY].SetItemType((ItemType.End));
            Debug.LogWarning("起点坐标或终点坐标设置错误");
        }
    }

    #region A Star Need Field

    //开放队列,存储所有待计算的格子
    private List<GridItem> openList;
    //关闭队列,存储所有被发现的格子
    private List<GridItem> closeList;
    //路径栈
    private Stack<GridItem> pathStack;

    #endregion

    /// <summary>
    /// A*寻路
    /// </summary>
    private void AStarPathFinding()
    {
        //初始化列表
        openList = new List<GridItem>();
        closeList = new List<GridItem>();
        pathStack = new Stack<GridItem>();

        //将起点放置到开启列表
        openList.Add(_gridItems[startX, startY]);
        //开启循环
        while (true)
        {
            //按照F值从小到大排序
            openList.Sort();
            //找到F值最小的格子
            GridItem center = openList[0];
            if (center.itemType == ItemType.End)
            {
                GeneratePath(center);
                break;
            }

            //以center为中心计算身边的8个格子
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    if (i == 0 && j == 0)
                    {
                        continue;
                    }

                    //新格子的坐标
                    int x = center.x + i;
                    int y = center.y + j;

                    //判断下标越界
                    if (x < 0 || x >= gridLength || y < 0 || y >= gridWidth)
                    {
                        continue;
                    }

                    GridItem crtItem = _gridItems[x, y];
                    //判断障碍物
                    if (crtItem.itemType == ItemType.Obstacle)
                    {
                        continue;
                    }

                    //该点曾作为中心被计算过
                    if (closeList.Contains(crtItem))
                    {
                        continue;
                    }

                    int G = CountOffsetG(i, j) + center.G;
                    //比较新旧G值,如果该格子未被访问过或原G值比新G值大
                    if (crtItem.G == 0 || crtItem.G > G)
                    {
                        //更新发现者
                        crtItem.parent = center;
                        crtItem.G = G;

                        int H = CountH(x, y);
                        crtItem.H = H;
                        //计算F值
                        crtItem.F = crtItem.G + crtItem.H;
                    }

                    //将当前格子添加到openList
                    if (!openList.Contains(crtItem))
                    {
                        openList.Add(crtItem);
                    }
                }
            }

            //将center从openList中移除,并添加到closeList
            openList.Remove(center);
            closeList.Add(center);

            //遍历结束,没有查询到终点
            if (openList.Count == 0)
            {
                Debug.Log("无法找到路径");
                break;
            }
        }
    }

    /// <summary>
    /// 生成路径
    /// </summary>
    /// <param name="item"></param>
    private void GeneratePath(GridItem item)
    {
        //当前元素入栈
        pathStack.Push(item);
        if (item.parent != null)
        {
            //递归查找
            GeneratePath(item.parent);
        }
        else
        {
            pathCount = pathStack.Count;
            //生成路径
            StartCoroutine(ShowPath());
        }
    }

    IEnumerator ShowPath()
    {
        while (pathStack.Count > 0)
        {
            yield return new WaitForSeconds(0.3f);
            GridItem item = pathStack.Pop();
            if (item.itemType == ItemType.Normal)
            {
                followPos = item.pos;
                item.SetColor(Color.Lerp(Color.red, Color.green, (pathCount - pathStack.Count) * 1f / pathCount));
            }
        }
    }


    /// <summary>
    /// 计算H值
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private int CountH(int x, int y)
    {
        int newx = 10 * (x - endX > 0 ? x - endX : endX - x);
        int newy = 10 * (y - endY > 0 ? y - endY : endY - y);
        return newx + newy;
    }

    /// <summary>
    /// 计算G值
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    private int CountOffsetG(int i, int j)
    {
        if (i == 0 || j == 0)
        {
            return 10;
        }

        return 14;
    }
}