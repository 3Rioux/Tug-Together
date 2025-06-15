using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Script to inform the player they are out of the playable bounds and to return within 5 seconds 
/// or it will automatically return the player in front of the border.
/// </summary>
public class OutOfBoundsHandler : MonoBehaviour
{
    [Header("Playable Area")]
    [SerializeField] private List<Collider> playableAreaColliders = new();

    [Header("Player Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float returnOffset = 3f;
    [SerializeField] private float countdownTime = 5f;

    [Header("UI")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;

    private Coroutine countdownCoroutine;
    private bool isPlayerOutOfBounds;


    private void LateUpdate()
    {
        if (!playerTransform) return;

        bool inside = IsPlayerInPlayableArea();

        if (!inside && !isPlayerOutOfBounds)
        {
            isPlayerOutOfBounds = true;
            countdownCoroutine = StartCoroutine(StartCountdown());
        }
        else if (inside && isPlayerOutOfBounds)
        {
            isPlayerOutOfBounds = false;
            StopCountdown();
        }
    }



    private bool IsPlayerInPlayableArea()
    {
        //return playableAreaCollider.bounds.Contains(playerTransform.position);
        foreach (var col in playableAreaColliders)
        {
            if (col.bounds.Contains(playerTransform.position))
                return true;
        }
        return false;
    }

    private IEnumerator StartCountdown()
    {
        float timer = countdownTime;
        countdownPanel.SetActive(true);

        while (timer > 0f)
        {
            countdownText.text = $"Return to play area in {timer:F1}s";
            yield return new WaitForSeconds(0.1f);
            timer -= 0.1f;

            if (IsPlayerInPlayableArea())
            {
                StopCountdown();
                yield break;
            }
        }

        // Timeout
        countdownPanel.SetActive(false);
        TeleportToNearestPoint();
        isPlayerOutOfBounds = false;
    }

    private void StopCountdown()
    {
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        countdownPanel.SetActive(false);
    }

    private void TeleportToNearestPoint()
    {
        Vector3 playerPos = playerTransform.position;
        float closestDist = float.MaxValue;
        Vector3 closestPoint = playerPos;

        foreach (var col in playableAreaColliders)
        {
            Vector3 point = col.ClosestPoint(playerPos);
            float dist = Vector3.Distance(playerPos, point);

            if (dist < closestDist)
            {
                closestDist = dist;
                closestPoint = point;
            }
        }

        Vector3 directionToCenter = (GetApproximateMapCenter() - closestPoint).normalized;
        Vector3 safeReturnPos = closestPoint + directionToCenter * returnOffset;

        playerTransform.position = safeReturnPos;
        playerTransform.forward = directionToCenter;
    }

    private Vector3 GetApproximateMapCenter()
    {
        if (playableAreaColliders.Count == 0)
            return Vector3.zero;

        Vector3 total = Vector3.zero;
        foreach (var col in playableAreaColliders)
        {
            total += col.bounds.center;
        }

        return total / playableAreaColliders.Count;
    }

    //private void TeleportToNearestPoint()
    //{
    //    Vector3 closestPoint = playableAreaCollider.ClosestPoint(playerTransform.position);
    //    Vector3 directionToCenter = (playableAreaCollider.bounds.center - closestPoint).normalized;

    //    Vector3 safeReturnPos = closestPoint + directionToCenter * returnOffset;

    //    playerTransform.position = safeReturnPos;

    //    // Optional: Face away from border
    //    playerTransform.forward = directionToCenter;
    //}

}
