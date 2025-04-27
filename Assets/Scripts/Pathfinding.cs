using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    GridMap grid;

    private void Awake()
    {
        grid = GetComponent<GridMap>();
    }

    public void ChangeRoom(Room room)
    {
        grid.offset = new Vector2(room.transform.position.x + grid.nodeSize / 2 - grid.roomSize.x / 2, room.transform.position.y + grid.nodeSize / 2 - grid.roomSize.y / 2);
        grid.Init();
    }

    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        Node startNode = grid.GetNode(startPos);
        Node targetNode = grid.GetNode(targetPos);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];

            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
                return RetracePath(startNode, targetNode);
            foreach (Node neighbor in grid.FindNeighborNode(currentNode))
            {
                if (!neighbor.canWalk || closedSet.Contains(neighbor)) continue;

                if(!grid.CanAcross(currentNode, neighbor))
                    continue;

                int newCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }
        return null;
    }

    public List<Node> FindNonDiagonalPath(Vector3 startPos, Vector3 targetPos)
    {
        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        Node startNode = grid.GetNode(startPos);
        Node targetNode = grid.GetNode(targetPos);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];

            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
                return RetracePath(startNode, targetNode);
            foreach (Node neighbor in grid.FindNeighborNode(currentNode))
            {
                if (!neighbor.canWalk || closedSet.Contains(neighbor)) continue;

                int newCostToNeighbor = currentNode.gCost + GetNonDiagonalDistance(currentNode, neighbor);
                if (newCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newCostToNeighbor;
                    neighbor.hCost = GetNonDiagonalDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }
        return null;
    }

    List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        grid.path = path;
        return path;
    }

    int GetDistance(Node a, Node b)
    {
        int distX = Mathf.Abs(a.myX - b.myX);
        int distY = Mathf.Abs(a.myY - b.myY);

        if (distX > distY)
            return 14 * distY + 10 * (distX - distY);
        return 14 * distX + 10 * (distY - distX);
    }

    int GetNonDiagonalDistance(Node a, Node b)
    {
        return 10 * (Mathf.Abs(a.myX - b.myX) + Mathf.Abs(a.myY - b.myY));
    }
}
