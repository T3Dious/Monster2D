using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonsterUI : MonoBehaviour
{
    public Image healthBar;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;
    public MonsterStats monsterStats; // Assign this in the Inspector or via script
    MonsterAI2D monsterAI; // Reference to the MonsterAI2D script
    float monsterCurrentHealth; // Current health of the monster

    void Start()
    {
        monsterAI = GetComponent<MonsterAI2D>(); // Get the MonsterAI2D component
    }

    void Update()
    {
        // // Optionally, update health bar in real-time if needed
        // if (healthBar != null && monsterStats != null)
        //     healthBar.fillAmount = monsterCurrentHealth / monsterStats.Health;
    }

    // Call this when mouse is over the monster
    public void UpdateMonsterUI(MonsterStats hoveredMonsterStats)
    {
        if (hoveredMonsterStats == null)
        {
            ResetUI();
            return;
        }

        monsterAI.UpdateMonsterHealth(); // Update the monster's health
        float targetHealth = monsterAI.currentHealth / hoveredMonsterStats.Health; // Get the current health from the MonsterAI2D script
        healthBar.fillAmount = Mathf.Clamp01(monsterCurrentHealth); // Update the health bar fill amount
        monsterCurrentHealth = Mathf.Lerp(monsterCurrentHealth, targetHealth, Time.deltaTime * 10f); // Smoothly interpolate health bar
        if (nameText != null)
            nameText.text = hoveredMonsterStats.MonsterName;
        if (levelText != null)
            levelText.text = "Lv. " + hoveredMonsterStats.Level.ToString();
    }

    void OnMouseOver()
    {
        UpdateMonsterUI(monsterStats); // Pass the current monster stats to update UI
    }

    void OnMouseExit()
    {
        ResetUI(); // Reset UI when mouse exits
    }

    void ResetUI()
    {
        if (healthBar != null)
            healthBar.fillAmount = 1f; // Reset to full health
        if (nameText != null)
            nameText.text = string.Empty; // Clear name text
        if (levelText != null)
            levelText.text = string.Empty; // Clear level text
    }
}
