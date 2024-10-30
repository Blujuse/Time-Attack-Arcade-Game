using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineAudio : MonoBehaviour
{
    public AudioSource runningSound;
    public float runningMaxVolume;
    public float runningMaxPitch;

    public AudioSource reverseSound;
    public float reverseMaxVolume;
    public float reverseMaxPitch;

    public AudioSource idleSound;
    public float idleMaxVolume;
    private float speedRatio;

    private float revLimiter;
    public float limiterSound = 1f;
    public float limiterFrequency = 3f;
    public float limiterEngage = 0.8f;

    public AudioSource startSound;
    public bool isEngineAudioRunning;

    private NewCarController carController;

    // Start is called before the first frame update
    void Start()
    {
        carController = GetComponent<NewCarController>();

        idleSound.volume = 0;
        runningSound.volume = 0;
        reverseSound.volume = 0;
    }

    // Update is called once per frame
    void Update()
    {
        float speedSign = 0;
        if (carController)
        {
            speedSign = Mathf.Sign(carController.GetSpeedRatio());
            speedRatio = Mathf.Abs(carController.GetSpeedRatio());
        }

        if (speedRatio > limiterEngage)
        {
            revLimiter = Mathf.Sin((Time.deltaTime * limiterFrequency) + 1f) * limiterSound * (speedRatio - limiterEngage);
        }

        if (isEngineAudioRunning)
        {
            idleSound.volume = Mathf.Lerp(0.1f, idleMaxVolume, speedRatio);

            if (speedSign > 0)
            {
                reverseSound.volume = 0;
                runningSound.volume = Mathf.Lerp(0.3f, runningMaxVolume, speedRatio);
                runningSound.pitch = Mathf.Lerp(0.3f, runningMaxPitch, speedRatio);
            }
            else
            {
                runningSound.volume = 0;
                reverseSound.volume = Mathf.Lerp(0f, reverseMaxVolume, speedRatio);
                reverseSound.pitch = Mathf.Lerp(0.2f, runningMaxPitch, speedRatio);
            }
        }
        else
        {
            idleSound.volume = 0;
            runningSound.volume = 0;
        }
    }

    public IEnumerator StartEngine()
    {
        startSound.Play();
        carController.isEngineRunning = 1;
        yield return new WaitForSeconds(0.6f);
        isEngineAudioRunning = true;
        yield return new WaitForSeconds(0.4f);
        carController.isEngineRunning = 2;
    }
}
