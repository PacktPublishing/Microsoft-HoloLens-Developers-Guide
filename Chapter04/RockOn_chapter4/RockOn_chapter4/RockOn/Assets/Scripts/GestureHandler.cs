using UnityEngine;
using UnityEngine.VR.WSA.Input;
using Random = System.Random;

public class GestureHandler : MonoBehaviour
{
    private GestureRecognizer _gestureRecognizer;
    public GameObject objectToPlace;

    private void Start()
    {
        _gestureRecognizer = new GestureRecognizer();
        _gestureRecognizer.TappedEvent += GestureRecognizerOnTappedEvent;

        _gestureRecognizer.StartCapturingGestures();
    }

    private void GestureRecognizerOnTappedEvent(
        InteractionSourceKind source,
        int tapCount,
        Ray headRay)
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