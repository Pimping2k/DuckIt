using System;
using DG.Tweening;
using Gameplay.Cardboard;
using UnityEngine;
using Random = UnityEngine.Random;

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

        public static Sequence DropBoxFromPipe(
            Transform box,
            Transform[] pipePoints,
            Transform target,
            float startScale = 0.3f,
            float pipeDuration = 0.8f,
            float fallDuration = 0.45f,
            bool alignToPath = true,
            Action onLanded = null)
        {
            var baseScale = box.localScale;

            var path = new Vector3[pipePoints.Length];
            for (int i = 0; i < pipePoints.Length; i++)
                path[i] = pipePoints[i].position;

            box.SetParent(null);
            box.position = path[0];
            box.localScale = baseScale * startScale;

            var pathTween = box.DOPath(path, pipeDuration, PathType.CatmullRom)
                .SetEase(Ease.Linear);
            if (alignToPath)
                pathTween.SetLookAt(0.01f);

            var seq = DOTween.Sequence().SetTarget(box).SetLink(box.gameObject)
                .Append(pathTween)

                .Append(box.DOMove(target.position, fallDuration).SetEase(Ease.InQuad))
                .Join(box.DOScale(baseScale * 1.1f, fallDuration).SetEase(Ease.OutQuad))
                .Join(box.DORotateQuaternion(target.rotation, fallDuration).SetEase(Ease.OutSine))

                .Append(box.DOScale(baseScale, 0.15f).SetEase(Ease.OutBack))
                .AppendCallback(() => onLanded?.Invoke());

            return seq;
        }
        public static class RestorableItems
        {
            public static Sequence PopItemFromBox(
                Transform item,
                Transform startPoint,
                Transform target,
                float popHeight = 0.5f,
                float popDuration = 0.35f,
                float flightDuration = 0.55f,
                float arcHeight = 0.35f,
                Action onLanded = null)
            {
                var baseScale = item.localScale;

                item.SetParent(null);
                item.SetPositionAndRotation(startPoint.position, startPoint.rotation);
                item.localScale = Vector3.zero;

                var popPos = startPoint.position + Vector3.up * popHeight;
                float tilt = Random.Range(8f, 15f) * (Random.value > 0.5f ? 1f : -1f);

                return DOTween.Sequence().SetTarget(item).SetLink(item.gameObject)
                    .Append(item.DOScale(baseScale, popDuration).SetEase(Ease.OutBack, 2.5f))
                    .Join(item.DOMove(popPos, popDuration).SetEase(Ease.OutCubic))
                    .Join(item.DOPunchRotation(new Vector3(0f, 0f, tilt), popDuration, 6, 0.6f))

                    .AppendInterval(0.06f)
                    
                    .Append(item.DOJump(target.position, arcHeight, 1, flightDuration).SetEase(Ease.InOutSine))
                    .Join(item.DORotateQuaternion(target.rotation, flightDuration).SetEase(Ease.InOutSine))

                    .Append(item.DOScale(Vector3.Scale(baseScale, new Vector3(1.12f, 0.88f, 1.12f)), 0.07f).SetEase(Ease.OutQuad))
                    .Append(item.DOScale(baseScale, 0.2f).SetEase(Ease.OutBack, 2f))
                    .AppendCallback(() => onLanded?.Invoke());
            }
        }
        
        public static class HandsAnimations
        {
            public static Sequence ReachAndReturn(
                Transform hand,
                Vector3 homeLocalPos,
                Quaternion homeLocalRot,
                Vector3 targetWorldPos,
                Quaternion? targetWorldRot = null,
                float reachDuration = 0.25f,
                float holdDuration = 0.05f,
                float returnDuration = 0.3f,
                Action onReached = null)
            {
                var seq = DOTween.Sequence().SetTarget(hand).SetLink(hand.gameObject);

                seq.Append(hand.DOMove(targetWorldPos, reachDuration).SetEase(Ease.OutCubic));
                if (targetWorldRot.HasValue)
                    seq.Join(hand.DORotateQuaternion(targetWorldRot.Value, reachDuration).SetEase(Ease.OutCubic));

                if (onReached != null)
                    seq.AppendCallback(() => onReached());

                if (holdDuration > 0f)
                    seq.AppendInterval(holdDuration);

                seq.Append(hand.DOLocalMove(homeLocalPos, returnDuration).SetEase(Ease.InOutSine));
                seq.Join(hand.DOLocalRotateQuaternion(homeLocalRot, returnDuration).SetEase(Ease.InOutSine));

                return seq;
            }
            
            public static Sequence GrabBoxWithHands(Transform leftHand, Transform rightHand,
                Transform leftGrab, Transform rightGrab, float duration = 0.35f)
            {
                return DOTween.Sequence().SetLink(leftHand.gameObject)
                    .Append(leftHand.DOMove(leftGrab.position, duration).SetEase(Ease.OutCubic))
                    .Join(leftHand.DORotateQuaternion(leftGrab.rotation, duration).SetEase(Ease.OutCubic))
                    .Join(rightHand.DOMove(rightGrab.position, duration).SetEase(Ease.OutCubic))
                    .Join(rightHand.DORotateQuaternion(rightGrab.rotation, duration).SetEase(Ease.OutCubic))
                    .AppendCallback(() =>
                    {
                        leftHand.SetParent(leftGrab, true);
                        rightHand.SetParent(rightGrab, true);
                    });
            }

            public static Sequence MoveBoxToPlace(Transform box, Transform target, float duration = 0.8f)
            {
                return DOTween.Sequence().SetTarget(box).SetLink(box.gameObject)
                    .Append(box.DOJump(target.position, 0.25f, 1, duration).SetEase(Ease.InOutSine))
                    .Join(box.DORotateQuaternion(target.rotation, duration).SetEase(Ease.InOutSine));
            }
            
            public static Sequence GetTool(Transform hand, Transform tool, Transform toolSocket,
                Vector3 lowerOffset,Vector3 homePosition,float lowerDuration = 0.25f, float returnDuration = 0.3f)
            {
                var lowPos = homePosition + lowerOffset;

                return DOTween.Sequence().SetTarget(hand).SetLink(hand.gameObject)
                    .Append(hand.DOLocalMove(lowPos, lowerDuration).SetEase(Ease.InQuad))

                    .AppendCallback(() =>
                    {
                        tool.DOKill();
                        tool.SetParent(toolSocket, true);
                        tool.DOLocalMove(Vector3.zero, 0.12f).SetEase(Ease.OutCubic).SetLink(tool.gameObject);
                        tool.DOLocalRotateQuaternion(Quaternion.identity, 0.12f).SetEase(Ease.OutCubic).SetLink(tool.gameObject);
                    })

                    .Append(hand.DOLocalMove(homePosition, returnDuration).SetEase(Ease.OutCubic));
            }
        }
    }
}