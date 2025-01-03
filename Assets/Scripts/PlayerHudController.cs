using TMPro;
using UnityEngine;

public class PlayerHudController : MonoBehaviour
{
    [Header("References")]
    public TMP_Text healthText;
    public RectTransform healthBarContainer;
    public RectTransform healthBar;

    private void Awake()
    {
        GameManager.Instance.localPlayerHud = this;
    }

    public void UpdateHealthBar(float health, float maxHealth)
    {
        healthText.text = health.ToString();
        healthBar.sizeDelta = new Vector2(-(healthBarContainer.sizeDelta.x - health / maxHealth * healthBarContainer.sizeDelta.x), healthBar.sizeDelta.y);
    }
}
