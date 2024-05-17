using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [Header("[Move]")]
    [SerializeField] private float moveSpeed = 5f;  // �̵��ӵ�
    private Vector3 moveDir;                        // �÷��̾��� �̵� ����
    private Vector3 moveMent;                       // �̵� ������ ���� ���
    private Rigidbody rigidbody;

    [Header("[View]")]
    public Transform viewPoint;                     // View Point�� ���� ĳ������ ���� ȸ���� ����
    public float mouseSensitivity = 1f;              // ���콺�� ȸ���ӵ� ���� ��
    public float verticalRotation;                  // ȸ�� ������� ���� ����
    public Vector2 mouseInput;                      // mouse�� input���� �����ϴ� ����
    public bool inverseMose;                        // ���콺�� ���� ������ ���� �����ϴ� �� ��
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
    public GameObject hiddenObject;                 // 1��Ī �������� ����� ���� ������Ʈ

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

            // �÷��̾��� ��ǲ
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
        // ĳ���� �̵� ����
        float Horizontal = Input.GetAxisRaw("Horizontal");
        float Vertical = Input.GetAxisRaw("Vertical");
        moveDir = new Vector3(Horizontal, 0, Vertical);

        // ĳ���� ȸ��
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;
        moveMent = (transform.forward * moveDir.z) + (transform.right * moveDir.x).normalized;

        // ĳ���� ����
        ButtonJump();
    }


    private void HandleView()
    {
        // ĳ������ �¿�ȸ��
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + mouseInput.x, transform.eulerAngles.z);

        // ĳ������ ����ȸ��
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
        // Rigidbody.Velocity : ���� rigidbody ��ü�� �ӵ�
        // Rigidbody.Velocity�� ���� �̵������ �ӵ��� �ﰢ������ �ݿ��Ͽ� ���������� ���������� ���ϼ� �ִ�
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

            Debug.Log($"�浹�� ������Ʈ�� �̸� : {hit.collider.gameObject.name}");
        }
    }

    #endregion
}
