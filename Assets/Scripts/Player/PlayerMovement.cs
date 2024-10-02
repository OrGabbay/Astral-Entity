using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    #region Objects & Headers
    private Rigidbody2D rb;
    private Animator anim;
    private CapsuleCollider2D col;

    private bool canBeControlled;
    private float defaultGravityScale;


    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float doubleJumpForce;
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashDuration;
    private float xInput;
    private float yInput;
    private bool canDoubleJump;
    private bool isDashing;

    [Header("Buffer jump && Coyote jump")]
    [SerializeField] private float bufferJumpWindow = 0.25f;
    private float bufferJumpActivated = -1;
    [SerializeField] private float coyoteJumpWindow = 0.5f;
    private float coyoteJumpActivated = -1;

    [Header("Collision")]
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private LayerMask whatIsGround;
    private bool isWallDetected;
    private bool isGrounded;
    private bool isAirBorne;

    [Header("Wall interactions")]
    [SerializeField] private float wallJumpDuration = .6f;
    [SerializeField] private Vector2 wallJumpForce;
    private bool isWallJumping;

    [Header("KnockBack")]
    [SerializeField] private float knockBackDuration = 1;
    [SerializeField] private Vector2 knockBackForce;
    private bool isKnocked;

    [Header("VFX")]
    [SerializeField] private GameObject DeathVFX;

    private bool facingRight = true;
    private int facingDirection = 1;
    #endregion


    #region Main Program
    // Awake is called at the start of the initialization
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
        col = GetComponent<CapsuleCollider2D>();
    }
    private void Start()
    {
        defaultGravityScale = rb.gravityScale;
        RespawnFinished(false);
    }

    // Update is called once per frame
    void Update()
    {
        HandleAirBorneStatus();

        if (!canBeControlled)
            return;

        if (isKnocked)
            return;

        HandleInput();
        HandleWallSlide();
        HandleMovement();
        HandleFlip();
        HandleCollision();
        HandleAnimation();

        RespawnFinished(canBeControlled);
    }

    public void RespawnFinished(bool finished)
    {

        if (finished)
        {
            rb.gravityScale = defaultGravityScale;
            canBeControlled = true;
            col.enabled = true;
        }
        else
        {
            rb.gravityScale = 0;
            canBeControlled = false;
            col.enabled = false;
        }
    }
    #endregion


    #region Handlers
    private void HandleInput()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.K))
            KnockBack();

        if (Input.GetKeyDown(KeyCode.W))
        {
            jumpButton();
            RequestBufferJump();
        }

        if (Input.GetKeyDown(KeyCode.Space))
            dashButton();
    }
    private void HandleCollision()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, whatIsGround);
        isWallDetected = Physics2D.Raycast(transform.position, Vector2.right * facingDirection, wallCheckDistance, whatIsGround);
    }
    private void HandleMovement()
    {
        if (isWallDetected)
            return;
        if (isWallJumping)
            return;
        if (isDashing)
            return;

        rb.velocity = new Vector2(xInput * moveSpeed, rb.velocity.y);
    }
    private void HandleAnimation()
    {
        anim.SetFloat("xVelocity", rb.velocity.x);
        anim.SetFloat("yVelocity", rb.velocity.y);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isWallDetected", isWallDetected);
    }
    #endregion


    #region Knockback
    public void KnockBack()
    {
        if (isKnocked)
            return;
        StartCoroutine(KnockBackRoutine());
        anim.SetTrigger("knockback");
        rb.velocity = new Vector2(knockBackForce.x * -facingDirection, knockBackForce.y * -facingDirection);
    }
    private IEnumerator KnockBackRoutine()
    {
        isKnocked = true;
        yield return new WaitForSeconds(knockBackDuration);
        isKnocked = false;
    }
    #endregion


    #region Death
    public void Die()
    {
        GameObject newDeathVFX = Instantiate(DeathVFX, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    #endregion


    #region Airborne
    private void HandleAirBorneStatus()
    {
        if (isGrounded && isAirBorne)
            HandleLanding();
        else if (!isGrounded && !isAirBorne)
            BecomeAirBorne();
    }
    private void BecomeAirBorne()
    {
        isAirBorne = true;

        if (rb.velocity.y <= 0)
        {
            ActivateCoyoteJump();
        }
    }
    private void HandleLanding()
    {
        isAirBorne = false;
        canDoubleJump = true;
        AttemptBufferJump();
    }
    #endregion


    #region Buffer & Coyote Jump
    private void RequestBufferJump()
    {
        if (isAirBorne)
            bufferJumpActivated = Time.time;
    }
    private void AttemptBufferJump()
    {
        if (Time.time < bufferJumpActivated + bufferJumpWindow)
        {
            bufferJumpActivated = Time.time - 1;
            Jump();
        }
    }
    private void ActivateCoyoteJump() => coyoteJumpActivated = Time.time;
    private void CancelCoyoteJump() => coyoteJumpActivated = Time.time - 1;
    #endregion


    #region Basic Jumps
    private void jumpButton()
    {
        bool coyoteJumpAvalible = Time.time < coyoteJumpActivated + coyoteJumpWindow;

        if (isGrounded || coyoteJumpAvalible)
        {
            Jump();
        }
        else if (isWallDetected)
        {
            WallJump();
        }
        else if (isAirBorne && canDoubleJump)
        {
            DoubleJump();
        }

        CancelCoyoteJump();
    }
    private void Jump() => rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    private void DoubleJump()
    {
        canDoubleJump = false;
        rb.velocity = new Vector2(rb.velocity.x, doubleJumpForce);
        anim.SetTrigger("doublejump");
    }
    #endregion


    #region Wall
    private void WallJump()
    {
        canDoubleJump = true;
        rb.velocity = new Vector2(wallJumpForce.x * -facingDirection, wallJumpForce.y);

        Flip();

        StopAllCoroutines();
        StartCoroutine(WallJumpRoutine());
    }
    private IEnumerator WallJumpRoutine()
    {
        isWallJumping = true;

        yield return new WaitForSeconds(wallJumpDuration);

        isWallJumping = false;

    }
    private void HandleWallSlide()
    {
        bool canWallSlide = isWallDetected && rb.velocity.y < 0;
        float yModifier = yInput < 0 ? 1 : .05f;

        if (!canWallSlide)
            return;

        rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * yModifier);
    }
    #endregion


    #region Dash
    private IEnumerator DashRoutine()
    {
        isDashing = true;

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
    }
    private void Dash()
    {
        rb.velocity = new Vector2(dashSpeed * facingDirection, rb.velocity.y);
        StartCoroutine(DashRoutine());
    }
    private void dashButton()
    {
        if (isWallDetected)
            return;
        if (isWallJumping)
            return;
        Dash();
    }
    #endregion


    #region Flip
    private void HandleFlip()
    {
        if (xInput < 0 && facingRight || xInput > 0 && !facingRight)
        {
            Flip();
        }
    }
    private void Flip()
    {
        transform.Rotate(0, 180, 0);
        facingDirection *= -1;
        facingRight = !facingRight;
    }
    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y - groundCheckDistance));
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x + (wallCheckDistance * facingDirection), transform.position.y));
    }
}
