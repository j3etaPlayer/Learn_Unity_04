using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [Header("[Move]")]
    [SerializeField] private float moveSpeed = 5f;  // 이동속도
    private Vector3 moveDir;                        // 플레이어의 이동 방향
    private Vector3 moveMent;                       // 이동 변수에 의한 결과
    private Rigidbody rigidbody;

    [Header("[View]")]
    public Transform viewPoint;                     // View Point를 통해 캐릭터의 상하 회전을 구현
    public float mouseSensitivity = 1f;              // 마우스의 회전속도 제어 값
    public float verticalRotation;                  // 회전 변경사항 저장 변수
    public Vector2 mouseInput;                      // mouse의 input값을 저장하는 변수
    public bool inverseMose;                        // 마우스의 상하 반전을 할지 결정하는 변 수
    public Camera cam;

    [Header("[Jump]")]
    [SerializeField] public KeyCode jumpkeyCode = KeyCode.Space;
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 5f;
    public bool isGrounded;
    private float yVel;

    [Header("[Photon Component]")]
    PhotonView pV;
    public GameObject hiddenObject;                 // 1인칭 시점에서 숨기고 싶은 오브젝트

    private void Awake()
    {
        InitializeCompoinents();
        PhotonSetUp();
    }
    private void InitializeCompoinents()
    {
        pV = GetComponent<PhotonView>();
        rigidbody = GetComponent<Rigidbody>();
    }

    private void PhotonSetUp()
    {
        if (!pV.IsMine)
        {
            cam.gameObject.SetActive(false);
            if (hiddenObject != null)
            {
                hiddenObject.layer = 0;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (pV.IsMine)
        {
            CheckCollier();

            // 플레이어의 인풋
            HandleInput();
            HandleView();
            
            PlayerAttack();
        }
    }

    private void FixedUpdate()
    {
        if(pV.IsMine)
        {
            // rigidbody
            Move();
            LimitSpeed();
        }
    }

    private void HandleInput()
    {
        // 캐릭터 이동 방향
        float Horizontal = Input.GetAxisRaw("Horizontal");
        float Vertical = Input.GetAxisRaw("Vertical");
        moveDir = new Vector3(Horizontal, 0, Vertical);

        // 캐릭터 회전
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;
        moveMent = (transform.forward * moveDir.z) + (transform.right * moveDir.x).normalized;

        // 캐릭터 점프
        ButtonJump();
    }


    private void HandleView()
    {
        // 캐릭터의 좌우회전
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + mouseInput.x, transform.eulerAngles.z);

        // 캐릭터의 상하회전
        verticalRotation += mouseInput.y;
        verticalRotation = Mathf.Clamp(verticalRotation, -60f, 60);

        if (inverseMose)
        {
            viewPoint.rotation = Quaternion.Euler(verticalRotation, transform.eulerAngles.y, transform.eulerAngles.z);
        }
        else
        {
            viewPoint.rotation = Quaternion.Euler(-verticalRotation, transform.eulerAngles.y, transform.eulerAngles.z);
        }
    }

    private void LimitSpeed()
    {
        // Rigidbody.Velocity : 현재 rigidbody 객체의 속도
        // Rigidbody.Velocity는 현재 이동방향과 속도를 즉각적으로 반영하여 비현실적인 움직임으로 보일수 있다
        Vector3 currentSpeed = new Vector3(rigidbody.velocity.x, 0, rigidbody.velocity.z);

        if(currentSpeed.magnitude > moveSpeed)
        {
            Vector3 limitSpeed = currentSpeed.normalized * moveSpeed;
            rigidbody.velocity = new Vector3(limitSpeed.x, rigidbody.velocity.y, limitSpeed.z);
        }
    }

    private void Move()
    {
        rigidbody.AddForce(moveMent * moveSpeed, ForceMode.Impulse);
    }

    private void ButtonJump()
    {
        if (Input.GetKeyDown(jumpkeyCode) && isGrounded)
        {
            Jump();
        }
    }

    private void Jump()
    {
        rigidbody.velocity = new Vector3(rigidbody.velocity.x, 0, rigidbody.velocity.z);

        rigidbody.AddForce(transform.up * jumpPower, ForceMode.Impulse);
    }

    private void CheckCollier()
    {
        isGrounded = Physics.Raycast(transform.position, -transform.up, groundCheckDistance, groundLayer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (-transform.up * groundCheckDistance));
    }

    #region Player Attack

    private void PlayerAttack()
    {
        Shoot();
    }

    private void Shoot()
    {
        if(Input.GetMouseButtonDown(0))
        {
            Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 100f);

            Debug.Log($"충돌한 오브젝트의 이름 : {hit.collider.gameObject.name}");
        }
    }

    #endregion
}
