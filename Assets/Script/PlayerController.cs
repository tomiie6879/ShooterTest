using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private SpriteRenderer bodySprite;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private SpriteRenderer gunSprite;
    [SerializeField] private Transform firePoint;

    [Header("Shooting")]
    [SerializeField] private WeaponDataSO weaponData;
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Sprite shootingSprite;
    [SerializeField, Min(0f)] private float shootingPoseDuration = 0.12f;
    [SerializeField] private Vector2 muzzleOffset = new Vector2(0.64f, -0.18f);

    // Số viên mỗi giây.
    [SerializeField, HideInInspector] private float fireRate = 5f;
    [SerializeField, HideInInspector] private float bulletSpeed = 15f;

    private Rigidbody2D rb;
    [SerializeField] private CombatPool combatPool;
    private Vector2 moveInput;
    private float nextFireTime;
    private float shootingPoseUntil;
    private bool showShootingPose;
    private bool wasShowingShootingPose;
    private Sprite normalSprite;
    private Vector3 normalVisualPosition;
    private Vector3 shootingSpriteOffset;
    private Quaternion aimRotation;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (combatPool == null)
            combatPool = FindAnyObjectByType<CombatPool>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (bodySprite != null)
        {
            normalSprite = bodySprite.sprite;
            normalVisualPosition = bodySprite.transform.localPosition;
            // Both source images have the same body at the left edge; the gun
            // image is wider. Compensate for its centered pivot to keep the body still.
            if (shootingSprite != null && normalSprite != null)
                shootingSpriteOffset = Vector3.right *
                    (shootingSprite.bounds.size.x - normalSprite.bounds.size.x) * 0.5f;
            bodySprite.flipX = false;
            bodySprite.flipY = false;
        }

        if (shootingSprite != null)
        {
            // The supplied sprite already contains the gun and the whole character.
            if (gunSprite != null && gunSprite != bodySprite)
                gunSprite.enabled = false;
            if (firePoint != null && weaponPivot != null)
            {
                firePoint.SetParent(weaponPivot, false);
                firePoint.localPosition = muzzleOffset;
            }
        }

        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void Update()
    {
        ReadMovement();
        UpdateAnimation();
        AimAndShoot();
    }

    private void LateUpdate()
    {
        if (bodySprite == null || shootingSprite == null)
            return;

        // Apply after Animator so its directional rotation cannot override aiming.
        if (showShootingPose)
        {
            bodySprite.sprite = shootingSprite;
            bodySprite.transform.rotation = aimRotation;
            bodySprite.transform.localPosition = normalVisualPosition +
                bodySprite.transform.localRotation * shootingSpriteOffset;
        }
        else if (wasShowingShootingPose)
        {
            bodySprite.sprite = normalSprite;
            bodySprite.transform.localPosition = normalVisualPosition;
        }
        wasShowingShootingPose = showShootingPose;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            animator.SetFloat(MoveXHash, moveInput.x);
            animator.SetFloat(MoveYHash, moveInput.y);
        }

        animator.SetFloat(SpeedHash, moveInput.magnitude);
    }

    private void ReadMovement()
    {
        moveInput = Vector2.zero;

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            moveInput.y += 1f;

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            moveInput.y -= 1f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            moveInput.x -= 1f;

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            moveInput.x += 1f;

        moveInput = moveInput.normalized;
    }

    private void AimAndShoot()
    {
        showShootingPose = Time.time < shootingPoseUntil;
        Mouse mouse = Mouse.current;

        if (mouse == null || worldCamera == null || weaponPivot == null)
            return;

        // Chiếu vị trí chuột lên mặt phẳng XY của nhân vật.
        Ray ray = worldCamera.ScreenPointToRay(
            mouse.position.ReadValue()
        );

        Plane aimPlane = new Plane(
            Vector3.forward,
            transform.position
        );

        if (!aimPlane.Raycast(ray, out float distance))
            return;

        Vector3 mouseWorldPosition = ray.GetPoint(distance);

        Vector2 aimDirection =
            mouseWorldPosition - weaponPivot.position;

        if (aimDirection.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(
            aimDirection.y,
            aimDirection.x
        ) * Mathf.Rad2Deg;

        weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);
        aimRotation = weaponPivot.rotation;

        if (mouse.leftButton.isPressed && shootingSprite != null)
            showShootingPose = true;

        bool lookingLeft = aimDirection.x < 0f;

        // Giữ hình súng đúng chiều khi ngắm sang trái.
        if (gunSprite != null)
            gunSprite.flipY = lookingLeft;

        if (mouse.leftButton.isPressed &&
            Time.time >= nextFireTime &&
            bulletPrefab != null &&
            firePoint != null)
        {
            Shoot(mouseWorldPosition);
            shootingPoseUntil = Time.time + shootingPoseDuration;
            showShootingPose = true;

            float shotsPerSecond = weaponData != null ? weaponData.FireRate : Mathf.Max(0.1f, fireRate);
            nextFireTime = Time.time + 1f / shotsPerSecond;
        }
    }

    private void Shoot(Vector3 targetPosition)
    {
        Vector2 direction = targetPosition - firePoint.position;
        if (direction.sqrMagnitude < 0.0001f)
            direction = weaponPivot.right;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (combatPool == null)
            return;

        Bullet bullet = combatPool.SpawnBullet(firePoint.position,
            Quaternion.Euler(0f, 0f, angle));
        if (bullet == null)
            return;

        if (weaponData != null)
            bullet.Launch(direction, weaponData, transform);
        else
            bullet.Launch(direction, bulletSpeed, transform);
    }

    private void OnDisable()
    {
        moveInput = Vector2.zero;
        showShootingPose = false;
        wasShowingShootingPose = false;
        shootingPoseUntil = 0f;
        if (bodySprite != null && normalSprite != null)
        {
            bodySprite.sprite = normalSprite;
            bodySprite.transform.localPosition = normalVisualPosition;
        }

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (animator != null)
            animator.SetFloat(SpeedHash, 0f);
    }
}
