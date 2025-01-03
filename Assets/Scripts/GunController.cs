using System.Collections;
using FishNet.Demo.AdditiveScenes;
using FishNet.Object;
using TMPro;
using UnityEngine;

public class GunController : NetworkBehaviour
{
    [Header("Gun")]
    public byte damage;
    public byte fireRate;
    public float recoil;

    [Header("Bullet")]
    public ushort maxBulletDistance;
    public float bulletLineLerpSpeed;

    [Header("References")]
    public GameObject damageTextPrefab;
    public GameObject bulletHitPrefab;
    public Transform bulletSpawnPoint;
    public Transform cameraPivot;
    public Transform remoteGunPivot;

    LineRenderer lineRenderer;
    Transform aimPivot;
    Vector3 lineHitPoint;
    Vector3 targetPoint;
    Vector3 initialLocalPosition;
    Quaternion initialLocalRotation;
    Quaternion lerpGunLocalRotation;
    bool isClientStarted;
    bool isFiring;
    bool hasFired;

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
        {
            transform.parent = remoteGunPivot;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        aimPivot = new GameObject("AimPivot").transform;
        aimPivot.parent = transform.parent;
        aimPivot.localPosition = Vector3.zero;

        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        lineRenderer = GetComponent<LineRenderer>();
        isClientStarted = true;
    }

    private void OnDisable() {
        isFiring = false;
        hasFired = false;
    }

    void Update()
    {
        if (!isClientStarted)
            return;

        Vector3 lerpGunLocalPosition = initialLocalPosition;
        Quaternion lerpGunLocalRotation = initialLocalRotation;

        if (IsOwner)
        {
            Vector3 targetPoint = Physics.Raycast(cameraPivot.position, cameraPivot.forward, out RaycastHit hit, maxBulletDistance, LayerMask.GetMask("Default"))
            ? hit.point : cameraPivot.position + cameraPivot.forward * maxBulletDistance;

            if (hit.collider && hit.collider.CompareTag("Player"))
            {
                targetPoint += cameraPivot.forward * 0.5f;
            }

            SetTargetHitPointServerRpc(targetPoint);

            if (!isFiring && Input.GetMouseButton(0))
                StartCoroutine(IFire());
        }
        else
        {
            Vector3 localDirectionToTarget = transform.InverseTransformPoint(targetPoint);
            float xAngle = Mathf.Atan2(localDirectionToTarget.y, localDirectionToTarget.z) * Mathf.Rad2Deg;
            float yAngle = Mathf.Atan2(localDirectionToTarget.x, localDirectionToTarget.z) * Mathf.Rad2Deg;
            lerpGunLocalRotation = Quaternion.Euler(-xAngle, yAngle, 0);
        }

        if (hasFired)
        {
            lerpGunLocalPosition = new Vector3(initialLocalPosition.x, initialLocalPosition.y, initialLocalPosition.z - recoil);
            lerpGunLocalRotation = Quaternion.Euler(initialLocalRotation.eulerAngles.x - recoil * 40, initialLocalRotation.eulerAngles.y, initialLocalRotation.eulerAngles.z);

            hasFired = false;
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, lerpGunLocalPosition, Time.deltaTime * 10f);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, lerpGunLocalRotation, Time.deltaTime * 10f);

        lineRenderer.SetPosition(0, Vector3.LerpUnclamped(lineRenderer.GetPosition(0), lineRenderer.GetPosition(1), Time.deltaTime * bulletLineLerpSpeed));

#if UNITY_EDITOR
        if (Physics.Raycast(cameraPivot.position, cameraPivot.forward, out RaycastHit debugHIt, maxBulletDistance, LayerMask.GetMask("Default")))
            Debug.DrawRay(bulletSpawnPoint.position, debugHIt.point - bulletSpawnPoint.position, debugHIt.collider.CompareTag("Player") ? Color.green : Color.red);
        else
            Debug.DrawRay(bulletSpawnPoint.position, cameraPivot.position + cameraPivot.forward * maxBulletDistance - bulletSpawnPoint.position, Color.red);
#endif
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    void SetTargetHitPointServerRpc(Vector3 targetHitPoint)
    {
        SetTargetHitPointObserversRpc(targetHitPoint);
    }

    [ObserversRpc(ExcludeOwner = true, RunLocally = true)]
    void SetTargetHitPointObserversRpc(Vector3 targetHitPoint)
    {
        this.targetPoint = targetHitPoint;
    }

    IEnumerator IFire()
    {
        SetFiringStateServerRpc(true);

        Vector3 camCenter = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0f));
        lineHitPoint = cameraPivot.position + cameraPivot.forward * maxBulletDistance;

        if (Physics.Raycast(camCenter, Camera.main.transform.forward, out RaycastHit hit, maxBulletDistance, LayerMask.GetMask("Default")))
        {
            if (hit.collider.CompareTag("Player") && hit.collider.TryGetComponent(out PlayerBodyPart playerBodyPart))
            {
                PlayerStats playerStatsController = playerBodyPart.player.GetComponent<PlayerStats>();
                float totalDamage = damage * playerBodyPart.damageMultiplier;
                playerStatsController.Health = (byte)Mathf.Clamp(playerStatsController.Health - totalDamage, byte.MinValue, byte.MaxValue);

                GameObject damageText = Instantiate(damageTextPrefab, hit.point, Quaternion.LookRotation(hit.point - cameraPivot.position));
                TMP_Text text = damageText.GetComponent<TMP_Text>();
                text.text = totalDamage.ToString();

                float distance = Vector3.Distance(hit.point, cameraPivot.position);
                text.fontSize = distance / 2f;
                StartCoroutine(IDisappear(damageText));
            }
            else
            {
                GameObject bulletHit = Instantiate(bulletHitPrefab, hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal));
                Spawn(bulletHit);
            }

            lineHitPoint = hit.point;
        }

        lineRenderer = GetComponent<LineRenderer>();
        SetLineRendererPositionsServerRpc(lineHitPoint);

#if UNITY_EDITOR
        Debug.DrawRay(bulletSpawnPoint.position, lineHitPoint - bulletSpawnPoint.position, Color.yellow, 0.25f);
#endif

        yield return new WaitForSeconds(1f / fireRate);

        SetLineRendererPositionsServerRpc(Vector3.zero);

        SetFiringStateServerRpc(false);
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    void SetFiringStateServerRpc(bool state)
    {
        SetFiringStateObserversRpc(state);
    }

    [ObserversRpc(ExcludeOwner = true, RunLocally = true)]
    void SetFiringStateObserversRpc(bool state)
    {
        isFiring = state;
        hasFired = state;
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    void SetLineRendererPositionsServerRpc(Vector3 hitPosition)
    {
        SetLineRendererPositionsObserversRpc(hitPosition);
    }

    [ObserversRpc(ExcludeOwner = true, RunLocally = true)]
    void SetLineRendererPositionsObserversRpc(Vector3 hitPosition)
    {
        lineRenderer.SetPosition(0, hitPosition == Vector3.zero ? Vector3.zero : bulletSpawnPoint.position);
        lineRenderer.SetPosition(1, hitPosition);
    }

    IEnumerator IDisappear(GameObject gameObject)
    {
        TMP_Text text = gameObject.GetComponent<TMP_Text>();

        while (text.color.a > 0)
        {
            Color color = text.color;
            color.a -= Time.deltaTime * 5f;
            text.color = color;
            yield return null;
        }

        Destroy(gameObject);
    }
}
