using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
  Rigidbody rigidBody;
  Vector3 velocity;

  // Start is called before the first frame update
  void Start()
  {
    rigidBody = GetComponent<Rigidbody>();
  }

  // Update is called once per frame
  void Update()
  {
    velocity = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized * 10;
  }

  void FixedUpdate()
  {
    rigidBody.MovePosition(rigidBody.position + velocity * Time.fixedDeltaTime);
  }
}
