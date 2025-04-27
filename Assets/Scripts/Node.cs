using UnityEngine;

public class Node
{
    public bool canWalk;
    public Vector3 position;
    public int myX;
    public int myY;
    public int gCost;
    public int hCost;

    public Node parent;
    
    public Node(bool walkable, Vector3 pos, int x, int y)
    {
        canWalk = walkable;
        position = pos;
        myX = x;
        myY = y;
    }

    public int fCost
    {
        get { return gCost + hCost; }
    }
}
