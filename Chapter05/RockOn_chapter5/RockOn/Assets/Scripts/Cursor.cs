using UnityEngine;

public class Cursor : MonoBehaviour
{
    private GameObject _objectBeingHit;
    [Tooltip("The gameobject to be used as cursor.")] public Transform cursor;

    [Tooltip("The position the cursor is placed at when nothing is hit.")] public float maxDistance = 5.0f;

    // Use this for initialization
    private void Start()
    {
    }

    // Update is called once per frame
private void Update()
{
    if (cursor == null)
        return;

    var camTrans = Camera.main.transform;

    RaycastHit raycastHit;

    if (Physics.Raycast(new Ray(camTrans.position, camTrans.forward), out raycastHit))
    {
        cursor.position = raycastHit.point;
        if (_objectBeingHit != raycastHit.transform.gameObject)
        {
            _objectBeingHit = raycastHit.transform.gameObject;
            _objectBeingHit.SendMessageUpwards("PlaySounds");
        }
    }
    else
    {
        cursor.position = camTrans.position + camTrans.forward*maxDistance;
        cursor.up = camTrans.forward;
        _objectBeingHit = null;
    }
}
}