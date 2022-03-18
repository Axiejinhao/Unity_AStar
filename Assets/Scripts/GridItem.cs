using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public enum ItemType
{
    Normal,
    Obstacle,
    Start,
    End
}

public class GridItem : MonoBehaviour, IComparable<GridItem>
{
    public int x;
    public int y;
    public Vector3 pos;

    //格子类型
    public ItemType itemType;

    private MeshRenderer _meshRenderer;

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    //F=G+H
    public int F;
    public int G;
    public int H;
    //父节点(发现者)
    public GridItem parent;

    public void SetItemType(ItemType itemType)
    {
        this.itemType = itemType;
        switch (itemType)
        {
            case ItemType.Obstacle:
                _meshRenderer.material.color = Color.blue;
                break;
            case ItemType.Start:
                _meshRenderer.material.color = Color.red;
                break;
            case ItemType.End:
                _meshRenderer.material.color = Color.green;
                break;
        }
    }

    public void SetColor(Color color)
    {
        _meshRenderer.material.color = color;
    }

    public int CompareTo(GridItem other)
    {
        if (this.F < other.F)
        {
            return -1;
        }
        else if (this.F > other.F)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}