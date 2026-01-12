using UnityEngine;

public class SetColorToRed : MonoBehaviour
{
    void Start()
    {
        // getting the renderer component to change material
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // simply setting the color to red for visual debug maybe
            rend.material.color = Color.red;
        }
    }
}