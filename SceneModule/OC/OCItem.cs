using System;
using UnityEngine;
public class OCItem 
{
    public string uid;
    public OCObject prefab;
    public Vector3 pos;
    public Vector3 ang;
    public OCItem(OCObject prefab,Vector3 pos,Vector3 ang)
    { 
        this.uid=System.Guid.NewGuid().ToString();
        this.prefab = prefab;
        this.pos = pos;
        this.ang = ang;
    }

    public void UpdateVisible(Vector3 cameraPos)
    {
        prefab.UpdatePrefab(this.pos,cameraPos);
    }
    
    public void CheckNearShow(Vector3 cameraPos)
    {
        prefab.CheckSmallNearShow(this.pos,cameraPos);
    }
}