using UnityEngine;

[CreateAssetMenu(fileName = "FirebaseConfig", menuName = "Project Origin/Firebase Config")]
public class FirebaseConfig : ScriptableObject
{
    [Tooltip("Realtime Database URL")]
    public string databaseUrl;

    public bool IsValid => !string.IsNullOrWhiteSpace(databaseUrl);
}
