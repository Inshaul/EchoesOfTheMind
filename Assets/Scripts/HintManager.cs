using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HintManager : MonoBehaviour
{
    public Canvas hintCanvas; 
    public TextMeshProUGUI hintText;      

    private Coroutine currentHintRoutine;

    public void ShowHint(string message, float duration = 5f)
    {
        if(currentHintRoutine != null)
            StopCoroutine(currentHintRoutine);

        currentHintRoutine = StartCoroutine(DisplayHint(message, duration));
    }

    public void SetHint(string message)
    {
        hintText.text = message;
        hintCanvas.gameObject.SetActive(true);
    }

    private IEnumerator DisplayHint(string message, float duration)
    {
        hintText.text = message;
        hintCanvas.gameObject.SetActive(true);

        yield return new WaitForSeconds(duration);

        hintCanvas.gameObject.SetActive(false);
    }
}

