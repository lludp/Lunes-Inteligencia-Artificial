using UnityEngine;
using TMPro;

public class PlayerInventory : MonoBehaviour
{
    public int documentsCollected = 0;
    public bool hasKey = false;

    [Header("UI Reference")]
    public TextMeshProUGUI objectiveText;

    void Start()
    {
        UpdateUI();
    }

    public void AddDocument()
    {
        documentsCollected++;
        UpdateUI();
    }

    public void GrabKey()
    {
        hasKey = true;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (objectiveText == null) return;

        if (documentsCollected < 3)
        {
            objectiveText.text = $"<color=yellow>OBJETIVO:</color> Encuentra los documentos en la casa ({documentsCollected}/3).\n<color=red>CUIDADO:</color> Las muñecas te observan, y la niña esta cerca.";
        }
        else if (documentsCollected >= 3 && !hasKey)
        {
            objectiveText.text = "<color=yellow>OBJETIVO:</color> Encuentra la llave en la iglesia.\n<color=red>CUIDADO:</color> El pastor tiene la llave... si te ve, huirá.";
        }
        else if (hasKey)
        {
            objectiveText.text = "<color=green>¡OBJETIVO COMPLETADO!</color>\nBusca la salida y huye de este lugar.";
        }
    }
}