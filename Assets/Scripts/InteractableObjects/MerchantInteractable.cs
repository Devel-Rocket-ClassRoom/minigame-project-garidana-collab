using UnityEngine;

public class MerchantInteractable : MonoBehaviour, IInteractable
{
    public string InteractionPrompt => throw new System.NotImplementedException();

    public Transform Transform => throw new System.NotImplementedException();

    public bool CanInteract(GameObject interactor)
    {
        throw new System.NotImplementedException();
    }

    public void Interact(GameObject interactor)
    {
        throw new System.NotImplementedException();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
