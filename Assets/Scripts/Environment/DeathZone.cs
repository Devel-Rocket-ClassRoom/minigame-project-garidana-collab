using UnityEngine;

public class DeathZone : MonoBehaviour
{
    [SerializeField] private bool _isSkyDeath;

    private void OnTriggerEnter(Collider other) => Kill(other.GetComponentInParent<PlayerStats>());
    private void OnTriggerStay(Collider other) => Kill(other.GetComponentInParent<PlayerStats>());
    private void OnCollisionEnter(Collision collision) => Kill(collision.gameObject.GetComponentInParent<PlayerStats>());

    private void Kill(PlayerStats playerStats)
    {
        if (playerStats == null || playerStats.IsDead) return;
        GameOverUi.IsSkyDeath = _isSkyDeath;
        playerStats.InstantKill();
    }
}
