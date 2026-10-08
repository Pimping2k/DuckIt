using DG.Tweening;
using UnityEngine;

namespace Data
{
    public static class Animations
    {
        private static float jumpHeight = 1.5f;
        private static float flightDuration = 0.8f;
        
        public static void LaunchBox(Transform targetObject, Transform targetPoint)
        {
            float randomZRotation = Random.Range(-360f, 360f);

            float randomPunchStrength = Random.Range(10f, 20f) * (Random.value > 0.5f ? 1f : -1f);

            Sequence sequence = DOTween.Sequence();

            sequence.Append(targetObject.DOJump(targetPoint.position, jumpHeight, 1, flightDuration)
                .SetEase(Ease.Linear));

            sequence.Join(targetObject.DORotate(new Vector3(0, randomZRotation, 0), flightDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuad));

            sequence.Append(targetObject.DOScale(new Vector3(1.2f, 0.8f, 1f), 0.08f));
            sequence.Append(targetObject.DOScale(Vector3.one, 0.12f).SetEase(Ease.OutBack));

            sequence.Append(targetObject.DOPunchRotation(new Vector3(0, 0, randomPunchStrength), 0.4f, 8, 0.5f));
        }
    }
}