using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ShipEngineAudioSource : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody shipRigidbody;
    [SerializeField] private AudioSource engineAudioSource;

    [Header("Speed")]
    [SerializeField] private float maxSpeed = 100f;

    [Header("Pitch")]
    [SerializeField] private float minPitch = 0.7f;
    [SerializeField] private float maxPitch = 1.4f;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float minVolume = 0.15f;
    [SerializeField, Range(0f, 1f)] private float maxVolume = 0.8f;

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 2f;

    private void Awake()
    {
        if (engineAudioSource == null)
        {   
            engineAudioSource = GetComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (shipRigidbody == null || engineAudioSource == null)
        {    
            return;
        }

        // Calcula a velocidade entre 0 e 1.
        float speed = shipRigidbody.linearVelocity.magnitude;
        float speedRatio = Mathf.Clamp01(speed / maxSpeed);

        // Define o pitch e volume pretendidos.
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, speedRatio);

        float targetVolume = Mathf.Lerp(minVolume, maxVolume, speedRatio);

        // Faz a transição de forma suave.
        engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch,targetPitch,smoothSpeed * Time.deltaTime);

        engineAudioSource.volume = Mathf.Lerp(engineAudioSource.volume,targetVolume,smoothSpeed * Time.deltaTime);
    }
}
