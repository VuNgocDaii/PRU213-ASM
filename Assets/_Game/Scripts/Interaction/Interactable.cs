using UnityEngine;

public class Interactable : MonoBehaviour
{
    [SerializeField] private string interactionText = "Tương tác";

    public virtual void Interact()
    {
        Debug.Log("Tương tác với " + gameObject.name, this);
    }

    public string GetInteractionText()
    {
        return interactionText;
    }
}
