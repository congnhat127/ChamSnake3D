#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class TestGlbLoading
{
    [MenuItem("Tools/Rong Ran/Test GLB Loading")]
    public static void Test()
    {
        var me = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RongRan/Models/Me_VN.glb");
        var con = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RongRan/Models/Con1_VN.glb");
        var thay = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RongRan/Models/ThayThuoc.glb");
        var sanDinh = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RongRan/Models/Man1_BoiCanh.glb");

        Debug.Log($"[TestGlbLoading] Me_VN: {(me != null ? "OK (" + me.name + ")" : "NULL")}");
        Debug.Log($"[TestGlbLoading] Con1_VN: {(con != null ? "OK (" + con.name + ")" : "NULL")}");
        Debug.Log($"[TestGlbLoading] ThayThuoc: {(thay != null ? "OK (" + thay.name + ")" : "NULL")}");
        Debug.Log($"[TestGlbLoading] Man1_BoiCanh: {(sanDinh != null ? "OK (" + sanDinh.name + ")" : "NULL")}");
    }
}
#endif
