using UnityEngine;
 
public class Tree 
{
    public Bounds Bounds;
    private OCNode root;
    public int maxDepth = 7;
    public int maxChildCount = 4;
    public Tree(Bounds bound)
    {
        this.Bounds = bound;
        this.root = new OCNode(bound, 0, this);
    }
    //插入数据
    public void InserData(OCItem data)
    {
        root.InserData(data);
    }
    
    //插入数据
    public void RemoveData(OCItem data)
    {
        root.RemoveData(data);
    }
    public void DrawBound()
    {
        root.DrawBound();
    }
    public void TriggerMove(Plane[] planes)
    {
        root.TriggerMove(planes);
    }
}