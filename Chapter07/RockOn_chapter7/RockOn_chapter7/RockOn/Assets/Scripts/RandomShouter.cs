using UnityEngine;

public class RandomShouter : MonoBehaviour
{
    // The next time the system should shout
    private float _nextShoutTime;

    [Tooltip("The prefab that generates the voice.")]
    public GameObject SpatialVoiceGenerator;

    [SerializeField]
    [Tooltip("The text you want to be said.")]
    private string _textToSay = "Place the guitar here.";

    // Use this for initialization
    private void Start()
    {
        CalculateNewTime();
    }

    private void CalculateNewTime()
    {
        var waitTime = Random.value*5.0f + 5.0f;
        _nextShoutTime = Time.time + waitTime;
    }

    // Update is called once per frame
    private void Update()
    {
        if (Time.time >= _nextShoutTime)
        {
            CalculateNewTime();
            CreateAndPlayVoice();
        }
    }

    private void CreateAndPlayVoice()
    {
        // Calculate the position somewhere
        // around the user at 5 meters distance
        var position = Random.insideUnitSphere*3.0f;
        // Intantiate the object at that position
        // Rotation is of no importance
        var instance = (GameObject) Instantiate(
            SpatialVoiceGenerator,
            position, Quaternion.identity);
        // Set the text property
        var voiceScript = instance.GetComponent<SpatialVoicePlayer>();
        voiceScript.TextToSay = _textToSay;
#if WINDOWS_UWP
        voiceScript.PlayVoice();
#endif
        // Kill the instance, two seconds after creation.
        Destroy(instance, 2.0f);
    }
}