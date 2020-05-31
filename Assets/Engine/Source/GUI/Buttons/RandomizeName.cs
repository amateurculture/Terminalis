using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RandomizeName : MonoBehaviour
{
    public GameObject outlet;
    TMP_InputField nameField;
    TMP_Dropdown gender;

    void Start()
    {
        transform.GetComponent<Button>()?.onClick.AddListener(Action);
    }

    private void Awake()
    {
        gender = transform.parent.transform.Find("Gender Dropdown")?.GetComponent<TMP_Dropdown>();
        nameField = transform.parent.transform.Find("InputField (TMP)")?.GetComponent<TMP_InputField>();
    }

    void Action()
    {
        nameField.text = Brain.instance.getFullname(gender.value);
    }
}
