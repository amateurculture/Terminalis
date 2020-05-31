using UnityEngine;
using TMPro;

/*** 
 * Class CurrencyText
 * 
 * Developer: Fiona Schultz
 * Last modified: Oct-19-2019
 * 
 * This class connects currency text with the player class.
 *
 */

public class CurrencyText : MonoBehaviour
{
    private Agent agent;
    private TextMeshProUGUI textMesh;

    void Start()
    {
        GameObject player = Brain.instance.player.gameObject;
        textMesh = transform.GetComponent<TextMeshProUGUI>();
        agent = Brain.instance.player;
        textMesh.text = "$" + agent?.value.ToString("N2");
    }

    void UpdateCurrency()
    {
        textMesh.text = "$" + agent?.value.ToString("N2");
    }
}
