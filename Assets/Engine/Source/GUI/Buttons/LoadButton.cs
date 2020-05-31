using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadButton : MonoBehaviour
{
    public string sceneName;

    public TMP_InputField nameField;
    public TMP_Dropdown genderDropdown;
    public TMP_Dropdown sexDropdown;
    public TMP_Dropdown attractionDropdown;
    public Image skinImage;
    public Image hairImage;

    void Awake()
    {
        transform.GetComponent<Button>()?.onClick.AddListener(Action);
    }

    void Action()
    {
        if (sceneName == "")
            Brain.instance.LoadScene("", false);
        else
        {
            if (nameField != null)
                Brain.instance.player.name = nameField.text;

            /*
            if (genderDropdown != null)
            {
                switch (genderDropdown.value)
                {
                    case 0:
                        Brain.instance.player.gender = Globals.Gender.Male;
                        break;
                    case 1:
                        Brain.instance.player.gender = Globals.Gender.Female;
                        break;
                    default:
                        Brain.instance.player.gender = Globals.Gender.NonBinary;
                        break;
                }
            }

            if (sexDropdown != null)
            {
                switch (sexDropdown.value)
                {
                    case 0:
                        Brain.instance.player.sex = Globals.Sex.Male;
                        break;
                    case 1:
                        Brain.instance.player.sex = Globals.Sex.Female;
                        break;
                    default:
                        Brain.instance.player.sex = Globals.Sex.Hermaphrodite;
                        break;
                }
            }

            if (attractionDropdown != null)
            {
                switch (attractionDropdown.value)
                {
                    case 0:
                        Brain.instance.player.attraction = Globals.Attraction.Heterosexual;
                        break;
                    case 1:
                        Brain.instance.player.attraction = Globals.Attraction.Homosexual;
                        break;
                    default:
                        Brain.instance.player.attraction = Globals.Attraction.Pansexual;
                        break;
                }
            }
            
            switch (genderDropdown.value)
            {
                case 1:
                    Brain.instance.player.gender = Globals.Gender.Female;
                    break;
                case 0:
                    Brain.instance.player.gender = Globals.Gender.Male;
                    break;
                default:
                    Brain.instance.player.gender = Globals.Gender.NonBinary;
                    break;
            }
            */

            Brain.instance.LoadScene(sceneName, true);
        }
    }
}
