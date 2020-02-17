using UnityEngine;
using TMPro;

public class NameText : MonoBehaviour
{
    private void OnEnable()
    {
        var field = transform.GetComponent<TextMeshProUGUI>();
        field.text = Brain.instance.player?.name;
    }
}
