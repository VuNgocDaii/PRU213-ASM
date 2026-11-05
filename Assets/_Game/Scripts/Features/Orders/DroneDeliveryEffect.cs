using System;
using System.Collections;
using UnityEngine;

public class DroneDeliveryEffect : MonoBehaviour
{
    [Header("Drone Visual Object")]
    [SerializeField] private GameObject droneVisual; // Đối tượng hình ảnh của drone
    [SerializeField] private float flightHeight = 6f; // Độ cao xuất phát từ trên trời
    [SerializeField] private float flyDuration = 1.0f; // Thời gian bay mỗi chiều
    [SerializeField] private float landingWaitTime = 0.5f; // Thời gian dừng lấy hàng

    private Vector3 originalLocalPosition;

    private void Awake()
    {
        if (droneVisual != null)
        {
            originalLocalPosition = droneVisual.transform.localPosition;
            droneVisual.SetActive(false);
        }
    }

    public void PlayDelivery(Action onComplete)
    {
        StartCoroutine(DeliveryRoutine(onComplete));
    }

    private IEnumerator DeliveryRoutine(Action onComplete)
    {
        if (droneVisual == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        droneVisual.SetActive(true);
        Vector3 targetPos = originalLocalPosition;
        Vector3 skyPos = targetPos + Vector3.up * flightHeight;

        // 1. Drone bay từ trên trời xuống
        float timer = 0f;
        while (timer < flyDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / flyDuration);
            droneVisual.transform.localPosition = Vector3.Lerp(skyPos, targetPos, t);
            yield return null;
        }
        droneVisual.transform.localPosition = targetPos;

        // 2. Dừng lại lấy hàng
        yield return new WaitForSeconds(landingWaitTime);

        // 3. Drone mang hàng bay vút lên trời
        timer = 0f;
        while (timer < flyDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / flyDuration);
            droneVisual.transform.localPosition = Vector3.Lerp(targetPos, skyPos, t);
            yield return null;
        }

        droneVisual.SetActive(false);
        droneVisual.transform.localPosition = originalLocalPosition;

        onComplete?.Invoke();
    }
}