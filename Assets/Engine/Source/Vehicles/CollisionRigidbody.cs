using System.Collections;
using UnityEngine;

public class CollisionRigidbody : MonoBehaviour
{
    Vector3 position;
    Quaternion rotation;
    Rigidbody rigid;
    Coroutine co;
    BoxCollider boxCollider;

    void Start()
    {
        position = transform.position;
        rotation = transform.rotation;
        boxCollider = GetComponent<BoxCollider>();
    }

    IEnumerator Respawn(Collision collision)
    {
        Vector3 hitVector = collision.transform.position - transform.position;
        hitVector.y = 0;
        hitVector = hitVector.normalized;

        rigid = gameObject.AddComponent(typeof(Rigidbody)) as Rigidbody;
        rigid.AddForce(hitVector * 500f);

        collision.rigidbody.AddForce(hitVector * 10000f);

        yield return new WaitForSeconds(6);

        Destroy(rigid);
        boxCollider.enabled = false;
        
        yield return new WaitForSeconds(60);

        transform.position = position; 
        transform.rotation = rotation;
        boxCollider.enabled = true;
        co = null;
    }

    bool alreadyHit;

    private void OnCollisionEnter(Collision collision)
    {
        if (!alreadyHit && collision.transform.tag == "Vehicle")
        {
            alreadyHit = true;
            if (co == null) co = StartCoroutine(Respawn(collision));
        }
    }
}
