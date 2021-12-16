using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarBehaviour : MonoBehaviour
{
    public Slider slider;
    public Image fill;
    private Color low = Color.red;
    private Color high = Color.green;
    public Vector3 offset;

    public void setHealth(float health, float maxHealth)
    {
        slider.maxValue = maxHealth;
        slider.value = health;
        fill.color = Color.Lerp(low, high, slider.normalizedValue);
    }

    public void makeActive(bool active)
    {
        slider.gameObject.SetActive(active);
    }

    // Update is called once per frame
    void Update()
    {
        slider.transform.position = Camera.main.WorldToScreenPoint(transform.parent.position + offset);
        fill.color = Color.Lerp(low, high, slider.normalizedValue);
    }
}
