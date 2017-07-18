using UnityEngine;
using UnityEngine.Windows.Speech;
using Random = System.Random;

public class VoiceRecognizer : MonoBehaviour
{
    [Tooltip("The object you want to place")]
    public GameObject objectToPlace;

    private void Start()
    {
        var keywordRecognizer = new KeywordRecognizer(new[] {"Place"});
        keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
        keywordRecognizer.Start();
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        var confidence = args.confidence;
        if (args.text == "Place" &&
            (confidence == ConfidenceLevel.Medium || confidence == ConfidenceLevel.High))
        {
            Place();
        }
    }

    private void Place()
    {
        if (objectToPlace == null)
            return;

        var distance = new Random().Next(2, 10);
        var location =
            transform.position +
            transform.forward*distance;

        Instantiate(
            objectToPlace,
            location,
            Quaternion.LookRotation(transform.up, transform.forward));
    }
}