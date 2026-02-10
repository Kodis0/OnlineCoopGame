using UnityEngine;

public class JoinCodeStore : MonoBehaviour
{
    public static JoinCodeStore Instance { get; private set; }
    public string JoinCode { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetCode(string code) => JoinCode = code;
}
