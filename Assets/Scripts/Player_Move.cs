﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Move : MonoBehaviour
{
    [SerializeField] private LayerMask platformLayerMask;
    [SerializeField] private LayerMask enemyLayerMask;
    private Rigidbody2D rb;
    private BoxCollider2D bc;
    public Animator anim;
    public enum State {ground, jump, fall, absorb, slide}
    public ParticleSystem acid;

    private bool killable = false;
    private bool DisableMovement = false;
    public State action;
    public float speed = 10.00f;
    public float jumpVelocity;
    public float wallslideVelocity = 100f;
    public float multiplier = 1.2f;
    private float moveX;
    private float points = 100;
    private int pointSet;
    private bool facingRight;
    public bool Slide;
    public float baseGravity;

    private IEnumerator coroutine;

    //ignore collisions
    bool cantAbsorb;
    private Vector2 playerPos;

    //materials
    private Material matWhite;
    private Material matDefault;
    SpriteRenderer sr;

    // touch Movement 
    public bool moveLeft; // left or right
    public bool dontMove = true; //move or not
    public bool tSlam;

    void Start()
    {
        rb = transform.GetComponent<Rigidbody2D>();
        bc = transform.GetComponent<BoxCollider2D>();
        sr.GetComponent<SpriteRenderer>();
        matWhite = Resources.Load("Font Material", typeof(Material)) as Material;
        matDefault = sr.material;
        Score.scoreAmount = 0;
    }

    void FixedUpdate()
    {
        playerMove();
        anim.SetInteger("state", (int)action);
        CollisionIgnore();
    }

    //Allowing player to move
    public void playerMove()
    {
        if (DisableMovement == false) {
            Jump();
            Movement();
            touchHandleMoving();
            touchSlam();
        }
        else if (DisableMovement == true) {
            rb.velocity = new Vector2(0, 0);
        }
    }

    //Direction fuction
    void Movement() {
        if (Input.GetKey(KeyCode.LeftArrow ))
        {
            dontMove = false;
            moveLeft = true;
            rb.velocity = new Vector2(-speed, rb.velocity.y);
            transform.localScale = new Vector2(-1, 1);
            facingRight = false;
        }
        else {
            if (Input.GetKey(KeyCode.RightArrow))
            {
                dontMove = false;
                moveLeft = false;
                rb.velocity = new Vector2(+speed, rb.velocity.y);
                transform.localScale = new Vector2(1, 1);
                facingRight = true;
            }
            else {
                //dontMove = true;
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
    }

    //Jump Fuction
    void Jump() {
        if (IsGrounded() && Input.GetKeyDown(KeyCode.Space))
        {
            rb.velocity = Vector2.up * jumpVelocity;
        }
        //it works just wount go back
        else if (!IsGrounded() && Input.GetKey(KeyCode.LeftAlt))
        {
            rb.gravityScale = 35;
        }
        else if (!IsGrounded() && !Input.GetKey(KeyCode.LeftAlt)) {
            rb.gravityScale = 8;
        }
    }
    //see when touching floor
    private bool IsGrounded() {

        float extraHeightText = 0.1f;
        RaycastHit2D raycastHit = 
            Physics2D.BoxCast(bc.bounds.center, bc.bounds.size, 0f, Vector2.down, extraHeightText, platformLayerMask);

        RaycastHit2D raycastHit2 =
            Physics2D.BoxCast(bc.bounds.center, bc.bounds.size, 0f, Vector2.down, extraHeightText, enemyLayerMask);
        
            Color rayColor;
            if (raycastHit.collider != null)
            {
                action = State.ground;
                jumpVelocity = 50f;
                rb.gravityScale = baseGravity;
                pointSet = 0;
                rayColor = Color.green;
            }
            
            else if (raycastHit2.collider != null && cantAbsorb == false)
            {
                action = State.absorb;
                acid.Play();
                rayColor = Color.blue;
            }
            
            else
            {
                rayColor = Color.red;
                if (rb.velocity.y < .1f)
                {
                    action = State.fall;
                    killable = true;
                }
                else if (rb.velocity.y > .1f && Slide == false)
                {
                    action = State.jump;
                    killable = false;
                }
                else if (rb.velocity.y > 0f && Slide == true)
                {
                    action = State.slide;
                }
            }

        Debug.DrawRay(bc.bounds.center + new Vector3(bc.bounds.extents.x, 0),
            Vector2.down * (bc.bounds.extents.y + extraHeightText), rayColor);
        Debug.DrawRay(bc.bounds.center - new Vector3(bc.bounds.extents.x, 0),
            Vector2.down * (bc.bounds.extents.y + extraHeightText), rayColor);
        Debug.DrawRay(bc.bounds.center - new Vector3(0, bc.bounds.extents.x, bc.bounds.extents.y + extraHeightText),
            Vector2.right * (bc.bounds.extents.x), rayColor);
        Debug.Log(raycastHit.collider);
        return raycastHit.collider != null;
    }

    //wall slide when jumping up into wall 
    public void wallBounce() {
        if (facingRight == true)
        {
            
            coroutine = wallSlideLeft(0.2f);
            StartCoroutine(coroutine);
        }
        else if (facingRight== false) {
            
            coroutine = wallSlideRight(0.2f);
            StartCoroutine(coroutine);
        }
    }

    IEnumerator wallSlideLeft(float _waitTime)
    {
        if (action == State.jump) {
            rb.velocity = Vector2.up * wallslideVelocity;
            transform.localScale = new Vector2(-1, 1);
            facingRight = false;
            yield return new WaitForSeconds(_waitTime);
        }
    }
    IEnumerator wallSlideRight(float _waitTime)
    {
        if (action == State.jump) {
            rb.velocity = Vector2.up * wallslideVelocity;
            transform.localScale = new Vector2(1, 1);
            facingRight = true;
            yield return new WaitForSeconds(_waitTime);
        }
    }

    //jump up through enemys
    public void CollisionIgnore() {
        playerPos = GameObject.Find("Player").transform.position;
        if (action == State.jump || playerPos.y <= 2.16)
        {
            Physics2D.IgnoreLayerCollision(9, 8, true);
            cantAbsorb = true;
        }
        else {
            cantAbsorb = false;
            Physics2D.IgnoreLayerCollision(9, 8, false);
        }
    }

    //Enemy interaction
    void OnCollisionEnter2D(Collision2D other)
    {
        Blue blue = other.gameObject.GetComponent<Blue>();
        if (!IsGrounded() && killable == true && action == State.absorb)
        {
            killable = false;
            if (other.gameObject.tag == "Enemy")
            {
                killable = false;
                blue.JumpedOn();
                blue.Death();
                coroutine = WaitAndJump(1.5f);
                StartCoroutine(coroutine);
                killable = false;
            }
        }
    }
    IEnumerator WaitAndJump(float _waitTime)
    {
        DisableMovement = true;
        GetComponent<Health>().Damagable = false;
        yield return new WaitForSeconds(_waitTime);
        DisableMovement = false;
        GetComponent<Health>().Damagable = true;
        JumpMultiplier();
        AddPoints();
        
    }
    public void JumpMultiplier() {
        jumpVelocity = multiplier * jumpVelocity;
        if (jumpVelocity > 150f)
        {
            jumpVelocity = 150f;
        }
        rb.velocity = Vector2.up * jumpVelocity;
    }
    
    //Add points when enemy death
    public void AddPoints() {
        pointSet++;
        if (pointSet == 1 || pointSet == 2)
        {
            points = 50;
        }
        else if (pointSet == 3 || pointSet == 4)
        {
            points = 75;
        }
        else if (pointSet == 5 || pointSet == 6)
        {
            points = 125;
        }
        else if (pointSet == 7 || pointSet == 8)
        {
            points = 250;
        }
        else if (pointSet == 9 || pointSet == 10)
        {
            points = 500;
        }
        Score.scoreAmount += points;
    }

    //Spite color when damaged
    public void takingDamage() {
        GetComponent<SpriteRenderer>().color = Color.red;
        coroutine = whitecolor(0.5f);
        StartCoroutine(coroutine);
    }
    IEnumerator whitecolor(float _waitTime)
    {
        yield return new WaitForSeconds(_waitTime);
        GetComponent<SpriteRenderer>().color = Color.white;
    }

    //touch Controls
    public void touchHandleMoving() {
        if (dontMove == true)
        {
            touchStopMoving();
        }
        else if (dontMove == false)
        {
            touchMoving();
        }
    }
    public void touchAllowMovement(bool movement) {
        dontMove = false;
        moveLeft = movement;
    }
    public void touchAllowSlam(bool slam)
    {
        tSlam = slam;
    }
    public void touchDontMove() {
        dontMove = true;
    }
    public void touchJump() {
        if (IsGrounded())
        {
            rb.velocity = Vector2.up * jumpVelocity;
        }
    }
    public void touchMoving() {
        if (dontMove == false && moveLeft == true)
        {
            rb.velocity = new Vector2(-speed, rb.velocity.y);
            transform.localScale = new Vector2(-1, 1);
            facingRight = false;
        }
        else if (dontMove == false && moveLeft == false){
            rb.velocity = new Vector2(+speed, rb.velocity.y);
            transform.localScale = new Vector2(1, 1);
            facingRight = true;
        }
    }
    public void touchStopMoving() {
        rb.velocity = new Vector2(0, rb.velocity.y);
    }
    public void touchSlam() {
        if (!IsGrounded() && tSlam == true)
        {
            rb.gravityScale = 35;
        }
        else if (!IsGrounded() && tSlam == false)
        {
            rb.gravityScale = baseGravity;
        }
    }
}

