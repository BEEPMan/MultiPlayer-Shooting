using System.Collections.Generic;
using UnityEngine;

public class GridMap : MonoBehaviour
{
    public Vector2 roomSize;
    public float nodeSize;
    public Vector2 offset;
    [SerializeField]
    Node[,] myNode;
    int nodeCountX;
    int nodeCountY;
    [SerializeField]
    LayerMask obstacle;

    int[] offsetX = { -1, 0, 1, 0 };
    int[] offsetY = { 0, -1, 0, 1 };

    public List<Node> path;

    private void Start()
    {
        //Init();
    }

    public void Init()
    {
        nodeCountX = Mathf.CeilToInt(roomSize.x / nodeSize);
        nodeCountY = Mathf.CeilToInt(roomSize.y / nodeSize);
        myNode = new Node[nodeCountX, nodeCountY];
        for (int x = 0; x < nodeCountX; x++)
        {
            for (int y = 0; y < nodeCountY; y++)
            {
                Vector3 pos = new Vector3(x * nodeSize + offset.x, y * nodeSize + offset.y, 0);
                Collider2D hit = Physics2D.OverlapBox(pos, new Vector2(nodeSize / 2, nodeSize / 2), 0, obstacle);
                bool walkable = hit == null;
                myNode[x, y] = new Node(walkable, pos, x, y);
            }
        }
    }

    public List<Node> FindNeighborNode(Node node)
    {
        List<Node> neighborNode = new List<Node>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                int checkX = node.myX + x;
                int checkY = node.myY + y;
                if (checkX >= 0 && checkX < nodeCountX && checkY >= 0 && checkY < nodeCountY)
                {
                    neighborNode.Add(myNode[checkX, checkY]);
                }
            }
        }
        return neighborNode;
    }

    public Node GetNode(Vector3 worldPosition)
    {
        int posX = Mathf.RoundToInt((worldPosition.x - offset.x) / nodeSize);
        int posY = Mathf.RoundToInt((worldPosition.y - offset.y) / nodeSize);
        return myNode[posX, posY];
    }

    public bool CanAcross(Node from, Node to)
    {
        bool result = myNode[from.myX, to.myY].canWalk && myNode[to.myX, from.myY].canWalk;
        return result;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(roomSize.x, roomSize.y, 1));
        if (myNode != null)
        {
            foreach (Node no in myNode)
            {
                Gizmos.color = (no.canWalk) ? Color.white : Color.red;
                if (path != null)
                {
                    if (path.Contains(no)) //경로에 포함이 된 노드는 검정색큐브로 표시
                    {
                        Gizmos.color = Color.black;
                    }
                }
                Gizmos.DrawCube(no.position, Vector3.one * (nodeSize / 2));
            }
        }
    }
}
