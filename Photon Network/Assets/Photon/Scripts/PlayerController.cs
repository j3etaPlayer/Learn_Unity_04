using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
        InitalizedAttackInfo();
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
        if (pV.IsMine)
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

        if (currentSpeed.magnitude > moveSpeed)
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

    #region Player Attack
    public GameObject bulletImpact;
    public float shootDistance = 10f;
    public float fireCoolTime = 0.1f;
    private float fireCounter;
    public bool isAutomatic;

    [Header("[Overheat System]")]
    public float maxHeat = 10f, heatCount, heatPerShot;
    public float coolRate, overHeatCoolRate;
    private bool overHeated = false;

    public Gun[] allGuns;
    private int currentGunIndex = 0;
    private MuzzleFlash currentMuzzle;

    public PlayerUI playerUI;

    private void PlayerAttack()
    {
        CoolDownFunction();
        SelectGun();
        InputAttack();
    }

    private void SelectGun()
    {
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
        {
            currentGunIndex++;

            if (allGuns.Length - 1 < currentGunIndex)
                currentGunIndex = 0;

            SwitchGun();
            playerUI.SetWeaponSlot(currentGunIndex);
        }

        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
        {
            currentGunIndex--;
            if (0 > currentGunIndex)
                currentGunIndex = allGuns.Length - 1;

            SwitchGun();
            playerUI.SetWeaponSlot(currentGunIndex);
        }

        // todo : allguns �迭�� �����ͷ� ó���ϴ� ����� ���� �ȵ�
        for (int i = 0; i < allGuns.Length; i++)
        {
            if (Input.GetKeyDown((i+1).ToString()))
            {
                currentGunIndex = i;
                SwitchGun();
                playerUI.SetWeaponSlot(currentGunIndex);
            }
        }

    }

    private void CoolDownFunction()
    {
        fireCounter -= Time.deltaTime;
        OverHeatedCoolDown();
    }

    private void OverHeatedCoolDown()
    {
        if (overHeated)
        {
            heatCount -= overHeatCoolRate * Time.deltaTime;

            if(heatCount<=0)
            {
                heatCount = 0;
                overHeated = false;
                playerUI.overheatTextObject.SetActive(false);
                playerUI.heatText.gameObject.SetActive(false);
            }
        }
        else
        {
            heatCount -= coolRate * Time.deltaTime;

            if(heatCount<=0)
            {
                heatCount = 0;
            }
        }
        
        playerUI.currentWeaponSlider.value = heatCount;
        playerUI.heatText.text = "���� : " + heatCount.ToString("0.00");
    }

    private void InputAttack()
    {
        if (Input.GetMouseButtonDown(0) && !isAutomatic && !overHeated)
        {
            if (fireCounter <= 0)
            {
                Shoot();
            }
        }
        if (Input.GetMouseButton(0) && isAutomatic && !overHeated)
        {
            if (fireCounter <= 0)
            {
                Shoot();
            }
        }
    }

    private void InitalizedAttackInfo()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        fireCounter = fireCoolTime;
        currentGunIndex = 0;
        SwitchGun();
    }

    private void Shoot()
    {
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, shootDistance))
        {
            GameObject bulletObj = Instantiate(bulletImpact, hit.point+(hit.normal * 0.002f), Quaternion.LookRotation(hit.normal, Vector3.up));

            currentMuzzle.gameObject.SetActive(true);

            Destroy(bulletObj, 1f);
        }
        
        fireCounter = fireCoolTime;

        ShootHeatSystem();
    }

    private void ShootHeatSystem()
    {
        heatCount = heatCount + heatPerShot;
        if (heatCount >= maxHeat)
        {
            heatCount = maxHeat;
            overHeated = true;
            playerUI.overheatTextObject.SetActive(true);

            playerUI.heatText.gameObject.SetActive(true);
        }

    }

    private void SwitchGun()
    {
        foreach (var gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }
        allGuns[currentGunIndex].gameObject.SetActive(true);

        SetGunAttribute(allGuns[currentGunIndex]);
    }

    private void SetGunAttribute(Gun gun)
    {
        fireCoolTime = gun.fireCoolTime;

        isAutomatic = gun.isAutomatic;
        currentMuzzle = gun.MuzzleFlash.GetComponent<MuzzleFlash>();

        heatPerShot = gun.heatPerShot;

        playerUI.currentWeaponSlider.maxValue = maxHeat;
    }
    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (-transform.up * groundCheckDistance));

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(cam.transform.position, cam.transform.forward * shootDistance);
    }
}

