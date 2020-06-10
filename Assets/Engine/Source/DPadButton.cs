using UnityEngine;

public class DPadButton : MonoBehaviour
{
    public bool left, right, up, down;
    private float _LastX, _LastY;

    private void Update()
    {
        float x = Input.GetAxis("Dpad Horizontal");
        float y = Input.GetAxis("Dpad Vertical");

        left = false;
        right = false;
        up = false;
        down = false;

        if (_LastX != x)
        {
            if (x == -1)
                left = true;
            else if (x == 1)
                right = true;
        }

        if (_LastY != y)
        {
            if (y == -1)
                down = true;
            else if (y == 1)
                up = true;
        }

        _LastX = x;
        _LastY = y;
    }
}