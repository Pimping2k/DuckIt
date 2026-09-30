using UnityEngine;

namespace Gameplay.FreeCamera
{
    [RequireComponent(typeof(Camera))]
    public class FreeCamera : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float normalSpeed = 10f;
        [SerializeField] private float fastSpeed = 30f;
        [SerializeField] private float sensitivity = 3f;

        private float rotationX;
        private float rotationY;

        private void Start()
        {
            Vector3 euler = transform.eulerAngles;
            rotationX = euler.y;
            rotationY = euler.x;
        }

        private void Update()
        {
            // Вращение камерой только при зажатой ПКМ
            if (Input.GetMouseButton(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                rotationX += Input.GetAxis("Mouse X") * sensitivity;
                rotationY -= Input.GetAxis("Mouse Y") * sensitivity;
                rotationY = Mathf.Clamp(rotationY, -89f, 89f);

                transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);
            }
            else if (Input.GetMouseButtonUp(1))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Расчет скорости
            float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : normalSpeed;

            // Ввод перемещения (WASD)
            Vector3 moveInput = new Vector3(
                Input.GetAxisRaw("Horizontal"),
                0f,
                Input.GetAxisRaw("Vertical")
            ).normalized;

            // Вверх / вниз (Space / LeftCtrl или E / Q)
            float verticalInput = 0f;
            if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E)) verticalInput += 1f;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.Q)) verticalInput -= 1f;

            // Направление относительно поворота камеры
            Vector3 moveDirection = transform.right * moveInput.x 
                                    + transform.forward * moveInput.z 
                                    + Vector3.up * verticalInput;

            transform.position += moveDirection * currentSpeed * Time.deltaTime;
        }
    }
}