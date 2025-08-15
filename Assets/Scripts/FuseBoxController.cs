using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
public class FuseBoxController : MonoBehaviour
{
    public List<RoomLightController> roomControllers;
    public RoomLightController chandalierLights;
    public bool powerRestored = false;
    private bool hasActivatedFuseBox = false;

    public FuseBoxLever fuseBoxLever;
    public void ActivateFuseBox()
    {
        if (!hasActivatedFuseBox)
        {
            hasActivatedFuseBox = true;
            powerRestored = true;

            Debug.Log("Fuse box activated for the first time!");

            GameDirector.Instance.OnFirstPowerRestored();
        }
        RestoreAllRooms();
    }
    public void CutPowerToRoom(string roomName)
    {
        var room = roomControllers.Find(r => r.roomName == roomName);
        if (room != null) room.TurnOff();
    }
    public void TurnOffAllRooms()
    {
        Debug.Log("Turn off log");
        powerRestored = false;
        foreach (var room in roomControllers)
        {
            room.TurnOff();
            room.DisableRoomToHell();
        }
    }

    public void FlickerRoom(string roomName, bool doFlicker = true)
    {
        var room = roomControllers.Find(r => r.roomName == roomName);
        if (room != null)
        {
            if (doFlicker && room.isLit)
                {
                    room.StartSmoothFlicker(0f, 0.1f, 1.5f, 10f, true);
                }
                else
                {
                    room.StopFlicker();
                }
        }

    }

    public void FlickerChandalierRoom(bool doFlicker = true)
    {
        if (chandalierLights != null)
        {
            if (doFlicker && chandalierLights.isLit)
                {
                    chandalierLights.StartSmoothFlicker(0f, 0.1f, 1.5f, 10f, true);
                }
                else
                {
                    chandalierLights.StopFlicker();
                }
        }
    }

    public void FlickerLights(bool doFlicker = true)
    {
        for (int i = 0; i < roomControllers.Count; i++)
        {
            if (roomControllers[i] != null)
            {
                if (doFlicker && roomControllers[i].isLit)
                {
                    roomControllers[i].StartSmoothFlicker(0f, 0.1f, 1.5f, 10f, true);
                }
                else
                {
                    roomControllers[i].StopFlicker();
                }
            }
        }
    }

    public void RestoreAllRooms()
    {
        foreach (var room in roomControllers)
        {
            room.SetLightColor(Color.white);
            room.TurnOn();
            room.DeactivateFire();
            room.DisableHellDoorTrigger();
        }
    }
}
