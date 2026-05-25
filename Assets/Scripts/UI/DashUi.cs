using UnityEngine;
using UnityEngine.UI;

public class DashUi : MonoBehaviour
{
    public PlayerMovement playerMovement;

    private void Update()
    {
        float progress = playerMovement.DashCooldownProgress;
        gameObject.GetComponent<Image>().fillAmount = progress;
    }
}
