using UnityEngine;

public class FpsCameraController3D : MonoBehaviour
{
    [SerializeField] private Transform camTransform;
    [SerializeField] private Vector2 rotationSens;

	private float xRotation = -50;
	private float yRotation = 100;


    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

	private void Update()
	{
        if (Application.isFocused)
        {
			xRotation += Input.GetAxis("Mouse Y") * rotationSens.x * -1;
			yRotation += Input.GetAxis("Mouse X") * rotationSens.y;
			camTransform.localEulerAngles = new Vector3(xRotation, yRotation, 0);
		}
	}

}