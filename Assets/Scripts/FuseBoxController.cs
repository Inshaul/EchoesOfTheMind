using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class FuseBoxController : MonoBehaviour
{
    public List<RoomLightController> roomControllers;


    void Start()
    {
        TurnOffAllRooms();
    }
    public void CutPowerToRoom(string roomName)
    {
        var room = roomControllers.Find(r => r.roomName == roomName);
        if (room != null) room.TurnOff();
    }
    public void TurnOffAllRooms()
    {
        Debug.Log("Turn off log");
        foreach (var room in roomControllers)
        {
            room.TurnOff();
        }
    }
    public void SetRoomToHell(string roomName)
    {
        var room = roomControllers.Find(r => r.roomName == roomName);
        if (room != null)
        {
            room.SetLightColor(Color.red);
            room.ActivateFire();
        }
    }

    public void FlickerRoom(string roomName, bool loop=false)
    {
        var room = roomControllers.Find(r => r.roomName == roomName);
        if (room != null)
        {
            room.StartFlicker(0f, 0.15f, loop);
            //room.StartFlicker(4f, 0.1f);
        } 

    }

    public void RestoreAllRooms()
    {
        foreach (var room in roomControllers)
        {
            room.SetLightColor(Color.white);
            room.TurnOn();
            room.DeactivateFire();
        }
    }
}
