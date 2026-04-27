using UnityEngine;

public static class Vector3Extensions
{
    public static Vector3 Up(this Vector3 pos) => 
        pos + Vector3.up;
    
    public static Vector3 Down(this Vector3 pos) => 
        pos + Vector3.down;
    
    public static Vector3 Right(this Vector3 pos) => 
        pos + Vector3.right;
    
    public static Vector3 Left(this Vector3 pos) => 
        pos + Vector3.left;
    
    public static Vector3 Forward(this Vector3 pos) => 
        pos + Vector3.forward;
    
    public static Vector3 Back(this Vector3 pos) => 
        pos + Vector3.back;
}