using System.Collections.Generic;
using UnityEngine;

public class WorldParentNode :MonoBehaviour
{
    public WORLD_BOUND_TYPE parentType;
    
    public WORLD_BOUND_TYPE GetCurType()
    {
        return parentType;
    }

    public void SetType(WORLD_BOUND_TYPE type)
    {
        parentType = type;

    }

    public List<GameObject> GetAllChild()
    {
        var objs = gameObject.GetComponentsInChildren<WorldChildNode>();
        // var objs2 = gameObject.GetComponentsInChildren<OCObject>();

        List<GameObject> list = new List<GameObject>();
        for (int i = 0; i < objs.Length; i++)
        {
            list.Add(objs[i].gameObject);
        }
        // for (int i = 0; i < objs2.Length; i++)
        // {
        //     list.Add(objs2[i].gameObject);
        // }

        return list;
    }

    public void SetChildType()
    {
        GetChild(transform);
    }

    private void GetChild(Transform tran)
    {
        for (int i = 0; i < tran.childCount; i++)
        {
            var obj = tran.GetChild(i);
            if (obj.GetComponent<OCObject>())
            {   
                if (obj.GetComponent<WorldChildNode>())
                {
                    continue;
                }
                else
                {
                    var node = obj.gameObject.AddComponent<WorldChildNode>();
                    node.SetType(parentType);
                    continue;
                }
                
            }
            else
            {
                if (parentType == WORLD_BOUND_TYPE.Terrain_Mesh)
                {
                    if (obj.GetComponent<MeshRenderer>())
                    {
                        if (obj.GetComponent<WorldChildNode>())
                        {
                            continue;
                        }
                        else
                        {
                            var node = obj.gameObject.AddComponent<WorldChildNode>();
                            node.SetType(parentType);
                        }
                    }
                    else
                    {
                        if (obj.childCount > 0)
                        {
                            GetChild(obj);
                        }
                    }
                }
                else
                {
                    if (obj.childCount > 0)
                    {
                        GetChild(obj);
                    }
                }
               
            }
           
        }
    }
}