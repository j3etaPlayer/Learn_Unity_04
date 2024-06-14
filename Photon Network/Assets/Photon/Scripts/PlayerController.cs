using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 5f;    // 플레이어 이동 속도
    private Vector3 moveDir;                          // 플레이어의 이동 방향
    private Vector3 moveMent;                         // 이동 변수에 의한 결과
    private Rigidbody rigidbody;

    [Header("View")]
    public Transform viewPoint;                       // View Point를 통해서 캐릭터의 상하 회전을 구현
    public float mouseSensitiy = 1f;                  // 마우스의 회전 속도 제어 값
    public float verticalRotation;                    // 회전 변경 사항 저장 변수
    private Vector2 mouseInput;                      // Mouse의 Input값을 저장하는 변수
    public bool inverseMouse;                        // 마우스를 위로 움직일 때 아래로 회전할지, 위로 회전할지 결정하는 변수
    public Camera cam;                               // Player 본인이 소유한 카메라

    [Header("Jump")]
    public KeyCode JumpkeyCode = KeyCode.Space;
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 5f;
    [SerializeField] private Transform groundCheckPosition;
    public bool isGrounded;
    private float yVel;

    [Header("photon Component")]
    PhotonView PV;                                   // PV객체를 이용하여 인스턴스된 오브젝트의 소유권 확인
    public GameObject hiddenObject;                  // 1인칭 시점에서 숨기고 싶은 오브젝트

    [Header("Player")]
    public int maxHP = 100;                         // 플레이어가 시작할 때 최대 HP
    private int currentHP;                          // 시작할 때 MaxHP로 시작하고, TakeDamage에서 수치가 변동. 0보다 작을 때 isPlayerDead = true
    public bool isPlayerDead;                       // 플레이어가 죽음 로직 관리
    private Animator animator;


    private void Awake()
    {
        InitializeCompoments();
        PhotonSetup();
    }

    private void InitializeCompoments()
    {
        rigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    private void PhotonSetup()
    {
        if (!photonView.IsMine)  // 내 플레이어가 아니면 카메라를 비활성화
        {
            cam.gameObject.SetActive(false);
            playerUI.gameObject.SetActive(false);
            gameObject.tag = "OtherPlayer";
        }
        else
        {
            isPlayerDead = false;
            playerUI.deathScreenObject.SetActive(false);

            if (hiddenObject != null)
                hiddenObject.gameObject.SetActive(false);

            currentHP = maxHP;
            playerUI.playerHPText.text = $"{currentHP} / {maxHP}";
        }
    }

    private void Start()
    {
        InitalizeAttackInfo();
    }

    private void OnEnable()    // Respawn 시에도 Player 데이터 초기화
    {
        PhotonSetup();
    }

    // Update is called once per frame 
    // 컴퓨터 마다 Frame을 생성하는 성능이 다르기 때문에 Time.deltaTime 컴퓨터간의 같은 시간에 같은 횟수를 보장해주었다.
    void Update()
    {
        if (photonView.IsMine && isPlayerDead) return;          // 플레이어의 소유권이 나이고, 플레이어가 죽음 상태일 때 코드를 멈춘다.

        if (photonView.IsMine)
        {
            CheckCollider();
            HandleAnimation();

            // 플레이어의 인풋 
            HandleInput();
            HandleView();

            PlayerAttack();
        }
        else // 다른 Client에게 받아온 위치 정보를 동기화한다. (큰 차이가 있을 경우 수정 -> 지연 보상)
        {
            //if ((transform.position - curPos).sqrMagnitude >= 100)
            //    transform.position = curPos;
        }
    }

    private void FixedUpdate()
    {
        // Rigidbody AddForce(관성의 힘을 제어해주는 함수) - moveSpeed 만큼만 움직일
        Move();
        LimitSpeed();
    }

    private void HandleAnimation()
    {
        // speed
        animator.SetFloat("speed", rigidbody.velocity.magnitude);   // 0 ~ 1 0.01
        // isGround
        animator.SetBool("isGrounded", isGrounded);
    }

    private void HandleInput()
    {
        // 캐릭터 이동 방향
        float Horizontal = Input.GetAxisRaw("Horizontal");
        float Vertical = Input.GetAxisRaw("Vertical");
        moveDir = new Vector3(Horizontal, 0, Vertical);

        // 캐릭터 회전
        mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitiy;
        moveMent = (transform.forward * moveDir.z) + (transform.right * moveDir.x).normalized;

        // 캐릭터 점프
        ButtonJump();
    }

    private void HandleView()
    {
        // 캐릭터의 좌우 회전
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + mouseInput.x, transform.eulerAngles.z);
        // 캐릭터의 상하 회전
        verticalRotation += mouseInput.y;
        verticalRotation = Mathf.Clamp(verticalRotation, -60f, 60);

        if (inverseMouse)
            viewPoint.rotation = Quaternion.Euler(verticalRotation, transform.eulerAngles.y, transform.eulerAngles.z);
        else
            viewPoint.rotation = Quaternion.Euler(-verticalRotation, transform.eulerAngles.y, transform.eulerAngles.z);
    }

    private void LimitSpeed()
    {
        // Rigidbody. Velocity : 현재 Rigidbody 객체의 속도
        // Rigidbody.velocity 현재 캐릭터의 속도, 방향을 즉시 변경시킨다. 비현실적인 움직임으로 보일 수 있다.
        Vector3 curretSpeed = new Vector3(rigidbody.velocity.x, 0, rigidbody.velocity.z);

        if (curretSpeed.magnitude > moveSpeed)
        {
            Vector3 limitSpeed = curretSpeed.normalized * moveSpeed;
            rigidbody.velocity = new Vector3(limitSpeed.x, rigidbody.velocity.y, limitSpeed.z);
        }
    }

    private void Move()
    {
        rigidbody.AddForce(moveMent * moveSpeed, ForceMode.Impulse);  // 점점 속도가 빨리지는
    }

    private void ButtonJump()
    {
        // 조건문 :  키를 입력 + 현재 상태 땅인지 아닌지
        if (Input.GetKeyDown(JumpkeyCode) && isGrounded)
        {
            Jump();
        }
    }
    private void Jump()
    {
        rigidbody.velocity = new Vector3(rigidbody.velocity.x, 0, rigidbody.velocity.z);

        rigidbody.AddForce(transform.up * jumpPower, ForceMode.Impulse);
    }

    private void CheckCollider()
    {
        // Physics
        isGrounded = Physics.Raycast(groundCheckPosition.position, -transform.up, groundCheckDistance, groundLayer);
    }

    #region Player Attack
    public GameObject bulletImpact;      // 플레이어의 공격의 피격 효과 인스턴스
    public float bulletAliveTime = 2f;
    public float shootDistance = 10f;    // 최대 사격 거리
    public float fireCoolTime = 0.1f;
    private float fireCounter;
    public bool isAutomatic;             // True일 때 연사 공격 가능. false일 때 단일 클릭으로 공격 가능

    [Header("오버히트 시스템")]
    public float maxHeat = 10f, heatCount, heatPerShot;    // 열기를 저장하는 변수
    public float coolRate, overHeatCoolRate;  // 열기를 식히기 위한 변수 : overHeatcoolRate가 coolRate보다 커야 한다.
    private bool overHeated = false;          // maxHeat에 도달하면 true, heatCount <= 0 다시 false

    public Gun[] allGuns;
    private int currentGunIndex = 0;
    private int currentGunPower;
    private MuzzleFlash currentMuzzle;

    public PlayerUI playerUI;


    // 플레이어의 입력 -> 로직 -> (조건 - Physics.Raycast)실제 효과 처리
    // 공격했다는 사실
    private void PlayerAttack()
    {
        CoolDownFunction();
        SelectGun();
        InputAttack();
    }

    private void CoolDownFunction()
    {
        fireCounter -= Time.deltaTime;
        OverHeatedCoolDown();
    }

    // Update마다, 현재 HeatCount 계산해서, OverHeat인지 판별하는 함수
    private void OverHeatedCoolDown()
    {
        // 현재 OverHeat 상태    
        if (overHeated)
        {
            heatCount -= overHeatCoolRate * Time.deltaTime;

            if (heatCount <= 0)
            {
                heatCount = 0;
                overHeated = false;
                // UI에서 OverHeat 표시를 해제
                playerUI.overHeatTextObject.SetActive(false);
            }
        }
        // !overHeated
        else
        {
            heatCount -= coolRate * Time.deltaTime;
            if (heatCount <= 0)
                heatCount = 0;
        }

        playerUI.currentWeaponSlider.value = heatCount;
    }

    private void SelectGun() // 마우스 휠 버튼 이용해서 1 ~ N 등록된 무기를 변경 기능 
                             // 1번 -> 1번 무기, 2번 -> 2번 무기, 3번 3번무기
    {
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
        {
            currentGunIndex++;

            // 예외 사항.. 
            // 배열의 길이보다 큰 경우  // 0 ~ n(Length - 1) 
            if (currentGunIndex >= allGuns.Length)
                currentGunIndex = 0;

            // 총을 변경
            SwitchGun();
            playerUI.SetWeaponSlot(currentGunIndex);
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
        {
            currentGunIndex--;

            // 예외 사항.. 
            // 배열의 길이보다 큰 경우  // 0 ~ n(Length - 1) 
            if (currentGunIndex < 0)
                currentGunIndex = allGuns.Length - 1;

            // 총을 변경
            SwitchGun();
            playerUI.SetWeaponSlot(currentGunIndex);
        }

        // 키보드 숫자 입력(1,2,3)으로 무기 변경하기

        // 키보드의 숫자를 입력 받는다. => 숫자를 변수로 바꾼다 => if , else if 반복문 변환
        // 이 코드를 리뷰하고, 개선할 사항이 있으면 알려줘 (코드 리뷰)

        // Todo : allGuns 배열을 데이터로 처리하는 기능이 구현 안됨
        for (int i = 0; i < allGuns.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                currentGunIndex = i;
                SwitchGun();
                playerUI.SetWeaponSlot(currentGunIndex);
            }
        }
        // 입력 받은 숫자를 allGuns 배열의 Gun 데이터로 변환한다. 

    }

    private void InputAttack()
    {
        if (Input.GetMouseButtonDown(0) && !isAutomatic && !overHeated)  // 마우스를 눌렀을 때
        {
            if (fireCounter <= 0)
                photonView.RPC(nameof(ShootRPC), RpcTarget.AllBuffered);

            allGuns[currentGunIndex].ShotSound.Play();
        }

        if (Input.GetMouseButton(0) && isAutomatic && !overHeated)  // Mouse Up되기 전 까지 계속.. 코드 블럭 실행
        {
            if (fireCounter <= 0)
                photonView.RPC(nameof(ShootRPC), RpcTarget.AllBuffered);
        }
    }

    private void InitalizeAttackInfo()
    {
        Cursor.lockState = CursorLockMode.Locked;   // Boolean 옵션 - Check Locked true, false
        Cursor.visible = false;
        fireCounter = fireCoolTime;
        currentGunIndex = 0;
        SwitchGun();
    }
    // 들여 쓰기 핫 키 : 드래그 후 ctrl + K + D 
    [PunRPC]
    private void ShootRPC()
    {
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, shootDistance))
        {
            // Tag를 이용한 조건문.. Player Tag 대상에게 Effect발생. 공격을 받았음. 함수를
            //if(hit.collider.CompareTag("Player") && !hit.collider.GetComponent<PhotonView>().IsMine)
            //    TakeDamageRPC(hit.collider.GetComponent<PhotonView>().name, 10);

            if (hit.collider.CompareTag("OtherPlayer"))
            {
                hit.collider.gameObject.GetPhotonView().RPC(nameof(TakeDamageRPC), RpcTarget.AllBuffered,
                    photonView.Owner.NickName, currentGunPower, PhotonNetwork.LocalPlayer.ActorNumber);
            }

            // Raycast가 Hit한 지점에 object가 생성된다.
            // 생성된 각도... 
            // 생성되는 위치가 오브젝트랑 겹쳐보이는 현상...
            GameObject bulletObject = Instantiate(bulletImpact, hit.point + (hit.normal * 0.002f), Quaternion.LookRotation(hit.normal, Vector3.up));

            // 일정 시간 후에 인스턴스한 오브젝트를 파괴한다.
            Destroy(bulletObject, bulletAliveTime);
        }

        allGuns[currentGunIndex].ShotSound.Play();

        // 공격할 때 Muzzle Effect 발생
        currentMuzzle.gameObject.SetActive(true);
        //  사격이 끝날 때, 사격 쿨타임을 리셋
        fireCounter = fireCoolTime;
        // OverHeat값을 계산 함수
        ShootHeatSystem();
    }

    [PunRPC]
    private void TakeDamageRPC(string name, int damage, int actorNumber)
    {
        // 디버그로 받은 데미지 출력
        if (photonView.IsMine)
        {
            Debug.Log($"데미지 입은 대상 : {name}이 {damage} 만큼 피해를 입음");

            currentHP -= damage;
            isPlayerDead = currentHP <= 0;

            if (isPlayerDead)
            {
                MatchManager.Instance.UpdateStatsSend(actorNumber, 0, 1);
                // 죽었을 때 UI 출력
                playerUI.ShowDeathMessage(name);
                // 플레이어 Respawn 기능 구현
                SpawnPlayer.Instance.Die();
            }

            playerUI.playerHPText.text = $"{currentHP} / {maxHP}";
        }

    }

    private void ShootHeatSystem()
    {
        heatCount = heatCount + heatPerShot;

        if (heatCount >= maxHeat)
        {
            heatCount = maxHeat;
            overHeated = true;
            // 오버히트 ui 활성화
            playerUI.overHeatTextObject.SetActive(true);
        }
    }

    private void SwitchGun()
    {
        // RPC 함수 호출 코드
        photonView.RPC(nameof(SwitchGunRPC), RpcTarget.AllBuffered);
    }

    [PunRPC]
    private void SwitchGunRPC()
    {
        // allGuns안에 있는 모든 오브젝트 비활성화.
        foreach (var gun in allGuns)
            gun.gameObject.SetActive(false);
        // allGuns[currentGunIndex] 해당하는 오브젝트 활성화.
        allGuns[currentGunIndex].gameObject.SetActive(true);

        // Gun을 매개 변수로 사용하는 Gun 정보 동기화 함수
        SetGunAttribute(allGuns[currentGunIndex]);
    }

    private void SetGunAttribute(Gun gun) // Class -> Data 
    {
        fireCoolTime = gun.fireCoolTime;
        // Gun이 갖고 있는 속성을 Player Controller 변수와 연결 시키기
        isAutomatic = gun.isAutomatic;
        currentMuzzle = gun.MuzzleFlash.GetComponent<MuzzleFlash>();
        heatPerShot = gun.heatPerShot;
        shootDistance = gun.shootDistance;
        currentGunPower = gun.gunPower;

        // maxHeat Value가 결정되고 나서 작성
        playerUI.currentWeaponSlider.maxValue = maxHeat;
    }

    #endregion
    // Alt + 방향키(아래) : 코드를 움직일 수 있다.
    private void OnDrawGizmos()
    {
        // 에디터 실행해서.  DrawLine 땅에 닿는 길이를 설정. 땅과 충돌을 하는지 보기 위한 Gizmo 함수
        Gizmos.color = Color.red;
        Gizmos.DrawLine(groundCheckPosition.position, groundCheckPosition.position + (-transform.up * groundCheckDistance));
        // 플레이어의 사격 범위를 파악하기 위한 Gizmo 함수
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(cam.transform.position, cam.transform.forward * shootDistance);
    }

    private Vector3 curPos; // 동기화를 위해 받아온 변수를 저장하는 곳
    private float lag;

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // 보낼 정보를 isWriting 작성하면, 그 정보를 IsReading으로 읽어온다.
        // 주의사항 : 반드시 보낼 변수의 순서를 똑같이 해줘야 한다.

        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(rigidbody.velocity);
            stream.SendNext(currentHP);
            stream.SendNext(currentGunIndex);
        }
        else if (stream.IsReading)
        {
            curPos = (Vector3)stream.ReceiveNext();
            rigidbody.velocity = (Vector3)stream.ReceiveNext();
            currentHP = (int)stream.ReceiveNext();
            currentGunIndex = (int)stream.ReceiveNext();
        }

        lag = Mathf.Abs((float)(PhotonNetwork.Time - info.timestamp));
    }
}
