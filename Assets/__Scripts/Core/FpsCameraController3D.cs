using UnityEngine;

public class FpsCameraController3D : MonoBehaviour
{
    [SerializeField] Transform camTransform;
    [SerializeField] Vector2 rotationSens;

    float xRotation = -50;
    float yRotation = 100;


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