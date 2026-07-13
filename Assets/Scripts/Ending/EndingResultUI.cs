using TMPro;
using UnityEngine;

public class EndingResultUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text recordText;
    [SerializeField] private GameObject happyPhoto;
    [SerializeField] private GameObject badPhoto;

    private void OnEnable()
    {
        var result = GameResultManager.Instance != null ? GameResultManager.Instance.CurrentResult : new GameResultData();
        if (happyPhoto != null) happyPhoto.SetActive(result.endingType == EndingType.Happy);
        if (badPhoto != null) badPhoto.SetActive(result.endingType == EndingType.Bad);
        if (titleText != null) titleText.text = result.endingType == EndingType.Happy ? "최애 구출 성공!" : "구조 실패";
        if (recordText != null)
        {
            var minutes = Mathf.FloorToInt(result.totalPlayTime / 60f);
            var seconds = result.totalPlayTime % 60f;
            recordText.text = result.endingType == EndingType.Happy
                ? $"최종 기록: {minutes:00}:{seconds:00.00}\n사망: {result.deathCount}회\n최소 남은 시간: {result.lastMapRemainingTime:0.00}초"
                    + (result.isNewBest ? "\n\n새로운 최고 기록!" : string.Empty)
                : $"제한 시간 안에 도착하지 못했습니다.\n\n실패한 구역: 제{result.lastMapIndex + 1}구역\n도달 기록: {minutes:00}:{seconds:00.00}\n사망: {result.deathCount}회\n남은 시간: 0초\n\n조금만 더 빨랐다면…";
        }
    }
}
