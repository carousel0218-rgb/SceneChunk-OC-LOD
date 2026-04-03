using System.Collections.Generic;
using System;
using UnityEngine;
public class OCObject : MonoBehaviour
{
    [SerializeField]
    public OCItemOcclusionType occlusionType = OCItemOcclusionType.Occluder;
    [SerializeField]
    public OCItemCheckType checkType = OCItemCheckType.BIG;

    private bool isShow = true;

    public Dictionary<OCItemCheckType,int> TYPE_DIS = new Dictionary<OCItemCheckType, int>{
        {OCItemCheckType.BIG,1000},{OCItemCheckType.MIDDLE,275},{OCItemCheckType.SMALL,60},
        {OCItemCheckType.ACTIVITY,200}
    };

    private float CheckDis = 100;
    public int GetCheckDis(){
        return  TYPE_DIS[checkType];
    }

    public void SetIsShow(bool value)
    {
        if (gameObject.activeSelf == value)
        {
            isShow = value;
        }
        else
        {
            isShow = gameObject.activeSelf;
        }
       
    }

    public bool GetShow()
    {
        return isShow;
    }
    
    /// <summary>
    /// check 物体是否在一定距离内
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private bool IsNear(Vector3 pos,Vector3 pos2, float dis = 0){
        if(dis == 0){
            dis = CheckDis;
        }

        pos.y = 0;
        pos2.y = 0;
        return Vector3.Distance(pos,pos2) <= dis;
    }

    public void CheckSmallNearShow(Vector3 pos,Vector3 pos2){
       SetVisible(IsNear(pos,pos2,GetCheckDis()));
    }

    public void UpdatePrefab(Vector3 pos,Vector3 curCameraPos)
    {
        SetVisible(IsNear(pos,curCameraPos,GetCheckDis()/2.0f));
    }

    public void SetVisible(bool value)
    {
        if(gameObject == null) return;
        if(isShow != value){
            gameObject.SetActive(value);
            isShow = value;
        }

        // Util.SetVisible(gameObject,value);
        // gameObject.SetActive(value);


        // if(checkType == OCItemCheckType.MIDDLE){
        //     // Ray();
        // }
    }

    private void Ray(){
        // RaycastHit hitInfo;
        // Vector3 direction = (transform.position - PlayerCamera.Instance.Camera.transform.position).normalized;
        // float distance = Vector3.Distance(transform.position,  PlayerCamera.Instance.Camera.transform.position);
        // if (Physics.Raycast(PlayerCamera.Instance.Camera.transform.position, direction, out hitInfo, distance))
        // {
        //     Console.LogError("___"+hitInfo);
        // }
    }

}

public enum OCItemOcclusionType
{
    Occluder,
    Occludee,
}

public enum OCItemCheckType
{
    BIG = 0,
    MIDDLE = 1,
    SMALL = 2,
    ACTIVITY = 3,
}