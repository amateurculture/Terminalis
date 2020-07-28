using UnityEngine;
using TMPro;
using System.Globalization;

public class RandomizeCharacter : MonoBehaviour
{
    TMP_Dropdown sex;
    TMP_Dropdown gender;
    TMP_Dropdown attraction;
    TMP_InputField nameField;

    public TextAsset maleNames;
    public TextAsset femaleNames;
    public TextAsset lastNames;

    private void Awake()
    {
        sex = transform.Find("Sex Dropdown")?.GetComponent<TMP_Dropdown>();
        gender = transform.Find("Gender Dropdown")?.GetComponent<TMP_Dropdown>();
        attraction = transform.Find("Attraction Dropdown")?.GetComponent<TMP_Dropdown>();
        nameField = transform.Find("InputField (TMP)")?.GetComponent<TMP_InputField>();
    }

    private void OnEnable()
    {
        sex.value = Random.Range(0, 3);
        gender.value = Random.Range(0, 3);
        attraction.value = Random.Range(0, 3);

        if (nameField != null)
            nameField.text = Brain.instance.getFullname(gender.value);
    }
}
