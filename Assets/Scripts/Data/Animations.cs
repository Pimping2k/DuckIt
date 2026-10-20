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

        public static Sequence OpenBoxWithKnife(Transform knife, Transform startPoint, Transform endPoint)
        {
            return DOTween.Sequence().SetTarget(knife).SetLink(knife.gameObject)
                .Append(knife.DOLocalRotate(startPoint.localEulerAngles, 0.1f))
                .Insert(0f, knife.DOMove(startPoint.position, 0.6f))
                .Insert(0.6f, knife.DOMove(endPoint.position, 0.6f))
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
        
        public static Sequence ExtractItemFromBox(Transform item, Transform startPoint, Transform target, float clearanceHeight = 0.4f)
        {
            var baseScale = item.localScale;

            item.SetParent(null);
            return DOTween.Sequence().SetTarget(item).SetLink(item.gameObject)
                .Append(item.DOLocalRotate(Vector3.zero, 0.1f))
                .Append(item.DOMove(startPoint.position, 0f))
                .Join(item.DORotateQuaternion(startPoint.rotation, 0f))
                
                .Append(item.DOMoveY(startPoint.position.y + clearanceHeight, 0.25f).SetEase(Ease.OutQuad))
                
                .Append(item.DOMove(target.position, 0.5f).SetEase(Ease.InOutSine))
                .Join(item.DORotateQuaternion(target.rotation, 0.5f).SetEase(Ease.InOutSine))
                
                .Append(item.DOScale(baseScale * 1.06f, 0.1f).SetEase(Ease.OutQuad))
                .Append(item.DOScale(baseScale, 0.15f).SetEase(Ease.OutBack));
        }
        
        public static Sequence ThrowBoxOffTable(Transform box, Transform target, float arcHeight = 2.5f, float duration = 0.65f)
        {
            var baseScale = box.localScale;
            
            Vector3 randomTumble = new Vector3(
                Random.Range(-120f, 120f),
                Random.Range(-360f, 360f),
                Random.Range(-120f, 120f)
            );
            
            float impactPunch = Random.Range(15f, 25f) * (Random.value > 0.5f ? 1f : -1f);

            return DOTween.Sequence().SetTarget(box).SetLink(box.gameObject)
                .Append(box.DOScale(Vector3.Scale(baseScale, new Vector3(0.95f, 1.08f, 0.95f)), 0.08f).SetEase(Ease.OutQuad))
                
                .Append(box.DOJump(target.position, arcHeight, 1, duration).SetEase(Ease.OutQuad))
                .Join(box.DORotate(box.eulerAngles + randomTumble, duration, RotateMode.FastBeyond360).SetEase(Ease.InQuad))
                
                .Append(box.DOScale(Vector3.Scale(baseScale, new Vector3(1.25f, 0.75f, 1.25f)), 0.06f))
                
                .Append(box.DOScale(baseScale, 0.15f).SetEase(Ease.OutBack))
                
                .Join(box.DOPunchRotation(new Vector3(impactPunch, 0, impactPunch * 0.6f), 0.3f, 10, 0.5f));
        }
        
        public static Sequence SnapItem(Transform item, Transform targetPoint)
        {
            if(!Equals(targetPoint, item))
                item.SetParent(targetPoint);

            return DOTween.Sequence().SetTarget(item).SetLink(item.gameObject)
                .Append(item.DOMove(targetPoint.position, 0.3f))
                .Append(item.DORotateQuaternion(targetPoint.rotation, 0.1f));
        }
        
        public static Sequence SnapTool(Transform tool, Transform targetPoint, float duration = 0.4f)
        {
            tool.DOKill();
            tool.SetParent(targetPoint, true);

            Quaternion tilt = targetPoint.rotation * Quaternion.Euler(-12f, 8f, 5f);
            float moveDuration = duration * 0.8f;

            return DOTween.Sequence()
                .SetTarget(tool)
                .SetLink(tool.gameObject)
                .Append(
                    tool.DOMove(targetPoint.position, moveDuration)
                        .SetEase(Ease.OutCubic)
                )
                .Join(
                    tool.DORotateQuaternion(tilt, moveDuration)
                        .SetEase(Ease.OutSine)
                )
                .Append(
                    tool.DORotateQuaternion(
                        targetPoint.rotation,
                        duration * 0.2f
                    ).SetEase(Ease.OutSine)
                );
        }
    }
}