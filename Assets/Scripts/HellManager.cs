using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HellManager : MonoBehaviour
{
    [Header("Room Setup")]
    public List<RoomLightController> roomControllers;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip hellRoomSFX;

    [Header("Timing")]
    public float delayBeforeHellRoom = 15f;

    private HashSet<RoomLightController> usedHellRooms = new HashSet<RoomLightController>();
    private RoomLightController currentHellRoom;

    private bool hellRoomActive = false;

    private void Start()
    {
        //StartCoroutine(DelayedHellRoomActivation());
    }

    public IEnumerator DelayedHellRoomActivation()
    {
        yield return new WaitForSeconds(delayBeforeHellRoom);
        AssignNextHellRoom();
    }

    public void AssignNextHellRoom()
    {
        if (hellRoomActive)
            return;

        List<RoomLightController> availableRooms = new List<RoomLightController>();

        foreach (var room in roomControllers)
        {
            if (!usedHellRooms.Contains(room))
            {
                availableRooms.Add(room);
            }
        }

        if (availableRooms.Count == 0)
        {
            Debug.LogWarning("No more rooms left to make into hell.");
            return;
        }

        RoomLightController selectedRoom = availableRooms[Random.Range(0, availableRooms.Count)];

        Debug.LogWarning("selectedroom: "+selectedRoom.name);
        if (currentHellRoom != null)
        {
            currentHellRoom.DisableRoomToHell();
        }

        selectedRoom.SetRoomToHell();

        usedHellRooms.Add(selectedRoom);
        currentHellRoom = selectedRoom;
        hellRoomActive = true;

        if (audioSource != null && hellRoomSFX != null)
        {
            audioSource.PlayOneShot(hellRoomSFX);
        }

        Debug.Log("Hell Room Activated: " + selectedRoom.roomName);
    }

    public void ResetHellRooms()
    {
        usedHellRooms.Clear();
        currentHellRoom = null;
        hellRoomActive = false;
    }
}
