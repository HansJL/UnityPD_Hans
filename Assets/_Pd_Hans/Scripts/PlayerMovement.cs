using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed;
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    private void Update()
    {
        // Input erfassen
        moveInput.x = Input.GetAxisRaw("Horizontal"); // A/D
        moveInput.y = Input.GetAxisRaw("Vertical");   // W/S
    
        // Diagonale Bewegung normalisieren
        if (moveInput.magnitude > 1f)
            moveInput.Normalize();
    }
    private void FixedUpdate()
    {
        // Bewegung anwenden
        rb.position += moveInput * moveSpeed;
    }
}
