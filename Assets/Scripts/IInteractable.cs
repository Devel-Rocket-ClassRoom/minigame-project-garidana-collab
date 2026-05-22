using UnityEngine;

public interface IInteractable
{
    string InteractionPrompt { get; }
    Transform Transform {get;}
    // 실제 상호작용 오브젝트에서 
    // public Transform Transform => transform; 이렇게 위치 구현 (거리 계산 위해서) 
    bool CanInteract(GameObject interactor);
    void Interact (GameObject interactor);
}
