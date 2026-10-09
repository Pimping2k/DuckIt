using DG.Tweening;
using Gameplay.Cardboard;
using UnityEngine;

namespace Data
{
    public static class Animations
    {
        private const float JumpHeight = 1.5f;
        private const float FlightDuration = 0.8f;

        public static Sequence LaunchBox(Transform box, Transform target)
        {
            var baseScale = box.localScale;
            float yaw = Random.Range(-360f, 360f);
            float punch = Random.Range(10f, 20f) * (Random.value > 0.5f ? 1f : -1f);

            return DOTween.Sequence().SetTarget(box).SetLink(box.gameObject)
                .Append(box.DOJump(target.position, JumpHeight, 1, FlightDuration).SetEase(Ease.Linear))
                .Join(box.DORotate(new Vector3(0, yaw, 0), FlightDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad))
                .Append(box.DOScale(Vector3.Scale(baseScale, new Vector3(1.2f, 0.8f, 1f)), 0.08f))
                .Append(box.DOScale(baseScale, 0.12f).SetEase(Ease.OutBack))
                .Append(box.DOPunchRotation(new Vector3(0, 0, punch), 0.4f, 8, 0.5f));
        }

        public static Sequence OpenBoxWithKnife(Transform knife, Transform startPoint, Transform endPoint, Transform initialParent)
        {
            return DOTween.Sequence().SetTarget(knife).SetLink(knife.gameObject)
                .Append(knife.DOLocalRotate(startPoint.localEulerAngles, 0.1f))
                .Insert(0f, knife.DOMove(startPoint.position, 0.6f))
                .Insert(0.6f, knife.DOMove(endPoint.position, 0.6f))
                .Insert(1.2f, knife.DOMove(initialParent.position, 0.3f))
                .Insert(1.2f, knife.DOLocalRotate(Vector3.zero, 0.1f));
        }

        public static Sequence OpenBoxFlaps(GameObject box, FlapPose[] flaps, float duration = 0.6f)
        {
            var seq = DOTween.Sequence().SetTarget(box).SetLink(box);

            foreach (var f in flaps)
            {
                if (!f.flap) continue;
                seq.Insert(f.delay,
                    f.flap.DOLocalRotateQuaternion(Quaternion.Euler(f.openEuler), duration).SetEase(Ease.OutCubic));
            }

            return seq;
        }
    }
}