using UnityEngine;

[RequireComponent(typeof(PlayerCollisionAndMovement))]
public class PlayerAudioSpeedController : MonoBehaviour
{
    [Tooltip("Wenn true, steuert PD/Mikrofon die Geschwindigkeit")]
    public bool miceOnOff = true;

    private PlayerCollisionAndMovement playerMovement;

    void Awake()
    {
        playerMovement = GetComponent<PlayerCollisionAndMovement>();
    }

    void OnEnable()
    {
        PdSpeedReceiver.OnSpeedChanged += OnPdSpeedChanged;
    }

    void OnDisable()
    {
        PdSpeedReceiver.OnSpeedChanged -= OnPdSpeedChanged;
    }

    private void OnPdSpeedChanged(float speedMultiplier)
    {
        if (!miceOnOff || playerMovement == null) return;

        playerMovement.SetGlobalSpeed(speedMultiplier);
    }
}