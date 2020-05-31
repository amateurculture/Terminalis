using UnityEngine;

//[RequireComponent(typeof(LineRenderer))]
public class Reticle : MonoBehaviour
{
    public Transform avatar;
    public GameObject reticle;
    public float distanceAppears = 0;
    public float distanceDisappears = 3;

    private void LateUpdate()
    {


        var distance = Vector3.Distance(avatar.position, Camera.main.transform.position);

        if (!reticle.activeSelf && distance >= distanceAppears && distance <= distanceDisappears)
        {
            reticle.SetActive(true);
            return;
        }
        else if (reticle.activeSelf)
            reticle.SetActive(false);
    }

    /*
    public GameObject itemToDrop;
    float angle;
    float azimuth;

    public GameObject hand;
    //LineRenderer lineRenderer;
    [HideInInspector] public bool hitSomething;

    private void OnEnable()
    {
        angle = 0;
        azimuth = 0;
        transform.parent = this.transform;
        transform.localPosition = Vector3.zero;
        //lineRenderer = GetComponent<LineRenderer>();
        //lineRenderer.SetPosition(0, hand.transform.position);
        //lineRenderer.SetPosition(1, transform.localPosition);
    }
    
    void Update()
    {
        if (Input.GetKey(KeyCode.RightBracket)) angle += 2f;
        if (Input.GetKey(KeyCode.LeftBracket)) angle -= 2f;
        if (Input.GetKey(KeyCode.Semicolon))
            azimuth += 2f;
        if (Input.GetKey(KeyCode.Quote))
            azimuth -= 2f;

        var ray = Camera.main.ViewportPointToRay(new Vector3(.5f, .5f, 0));
        var hit = new RaycastHit();

        var layerMask = new LayerMask();
        layerMask = ~(
            (1 << LayerMask.NameToLayer("Ignore Raycast")) |
            (1 << LayerMask.NameToLayer("TransparentFX"))
            );

        if (Physics.Raycast(ray, out hit, 20, layerMask, QueryTriggerInteraction.Collide))
        {
            transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            transform.position = hit.point;
            transform.Rotate(Vector3.up, angle);
            transform.Rotate(Vector3.right, azimuth);
            hitSomething = true;
        }
        else
        {
            transform.position = Camera.main.transform.position;
            transform.position += ray.direction * 20f;
            transform.rotation = Quaternion.FromToRotation(Vector3.up, -ray.direction);
            hitSomething = false;
        }
        
    // below Code block was commented out
        if (Input.GetKeyDown(KeyCode.T))
        {
            GameObject item = Instantiate(itemToDrop);
            item.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            item.transform.position = hit.point;

            var a = angle + hit.transform.eulerAngles.y;
            var n = a > 180 ? -1 : 1;
            var o = (a % 180) * n;
            
            item.transform.eulerAngles = transform.eulerAngles;
        }
        
        //lineRenderer.SetPosition(0, hand.transform.position);
        //lineRenderer.SetPosition(1, transform.position);
        }
     */
}
