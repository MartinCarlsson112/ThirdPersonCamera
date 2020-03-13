using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Vector3 InputDirection;
    float speed = 1000;
    CharacterController cc;

    new Camera camera;

    Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        cc = GetComponent<CharacterController>();
        camera = Camera.main;
    }


    // Update is called once per frame
    void Update()
    {
        InputDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        animator.SetFloat("Speed", InputDirection.magnitude);
        InputDirection.Normalize();

        Vector3 input = camera.transform.rotation * InputDirection;
        Vector3 movement = new Vector3(0, 0, 0);

        movement += Vector3.right * input.x;
        movement += Vector3.forward * input.z;
   
       
        movement.Normalize();
        animator.SetBool("IsMoving", movement.sqrMagnitude > 0.1 ? true: false);
        if (movement.sqrMagnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(movement);
        }

        cc.SimpleMove(movement * speed * Time.deltaTime);
 
    }
}
